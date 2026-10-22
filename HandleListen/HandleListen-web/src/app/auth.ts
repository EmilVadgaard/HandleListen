import { Injectable,  inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, tap, catchError, throwError, map, shareReplay } from 'rxjs';

interface LoginResponse {
  tokenType: string;
  accessToken: string;
  expiresIn: number;
  refreshToken: string;
}

interface MeResponse {
  email: string;
  roles: string[];
}

@Injectable({ providedIn: 'root'})
export class AuthService {
    private http = inject(HttpClient);
    private readonly baseUrl = '/api/auth';

    token = signal<string | null>(localStorage.getItem('token'));
    refreshToken = signal<string | null>(localStorage.getItem('refreshToken'));
    email = signal<string | null>(localStorage.getItem('email'));
    roles = signal<string[]>(JSON.parse(localStorage.getItem('roles') ?? '[]'));

    isLoggedIn = computed(() => this.token() !== null);
    canCurate = computed(() => this.roles().includes('Owner') || this.roles().includes('Moderator'));
    canImportAi = computed(() => this.roles().includes('Owner') || this.roles().includes('AiImporter'));
    isOwner = computed(() => this.roles().includes('Owner'));

    private refreshInProgress: Observable<LoginResponse> | null = null;

    constructor() {
        if (this.token()) {
            this.refreshRoles();
        }
    }

    refreshRoles() {
        this.http.get<MeResponse>('/api/account/me').subscribe({
            next: res => {
                this.roles.set(res.roles);
                localStorage.setItem('roles', JSON.stringify(res.roles));
            },
            // Only an actual "you're not allowed" response means the roles really changed -
            // a network blip (common on mobile switching between wifi/cellular) shouldn't
            // silently strip curator rights until the next successful refresh.
            error: (err: HttpErrorResponse) => {
                if (err.status === 401 || err.status === 403) {
                    this.roles.set([]);
                    localStorage.removeItem('roles');
                }
            }
        });
    }

    register(email: string, password: string): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/register`, { email, password }).pipe(
            catchError((err: HttpErrorResponse) => {
            return throwError(() => new Error((Object.values(err.error.errors)[0] as string[])[0]));
            })
        );
    }

    login(email: string, password: string): Observable<LoginResponse> {
        return this.http.post<LoginResponse>(`${this.baseUrl}/login`, { email, password }).pipe(
            tap(response => {
                this.applyTokens(response);
                this.email.set(email);
                localStorage.setItem('email', email);
                this.refreshRoles();
            })
        );
    }

    // Called by the HTTP interceptor when a request comes back 401, so an expired access token
    // never surfaces to the user during normal use - the refresh token (valid much longer) is
    // used to get a fresh pair silently. Concurrent 401s share the same in-flight call instead
    // of each firing their own refresh request.
    refreshAccessToken(): Observable<LoginResponse> {
        if (this.refreshInProgress) return this.refreshInProgress;

        const refreshToken = this.refreshToken();
        if (!refreshToken) {
            return throwError(() => new Error('No refresh token available'));
        }

        this.refreshInProgress = this.http.post<LoginResponse>(`${this.baseUrl}/refresh`, { refreshToken }).pipe(
            tap(response => this.applyTokens(response)),
            catchError(err => {
                this.refreshInProgress = null;
                return throwError(() => err);
            }),
            shareReplay(1)
        );
        return this.refreshInProgress;
    }

    private applyTokens(response: LoginResponse) {
        this.token.set(response.accessToken);
        this.refreshToken.set(response.refreshToken);
        localStorage.setItem('token', response.accessToken);
        localStorage.setItem('refreshToken', response.refreshToken);
        this.refreshInProgress = null;
    }

    logout() {
        this.token.set(null);
        this.refreshToken.set(null);
        this.email.set(null);
        this.roles.set([]);
        localStorage.removeItem('token');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('email');
        localStorage.removeItem('roles');
    }

    // Used when even the refresh token is no longer valid. A full page reload (not router
    // navigation) is deliberate - it's the only way to reliably clear accumulated state like
    // the SignalR connection, which is exactly what manually refreshing the page already fixed.
    forceLogoutAndReload() {
        this.logout();
        window.location.href = '/login?sessionExpired=true';
    }

    forgotPassword(email: string): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/forgotPassword`, { email });
    }

    resetPassword(email: string, resetCode: string, newPassword: string): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/resetPassword`, { email, resetCode, newPassword }).pipe(
            catchError((err: HttpErrorResponse) => {
                const message = err.error?.errors ? (Object.values(err.error.errors)[0] as string[])[0] : 'Nulstilling af adgangskode mislykkedes.';
                return throwError(() => new Error(message));
            })
        );
    }

    confirmEmail(userId: string, code: string): Observable<void> {
        // The backend replies with a plain-text body on success, not JSON, so we must ask for
        // text explicitly here or Angular's HttpClient fails to parse an otherwise-successful response.
        return this.http.get(`${this.baseUrl}/confirmEmail`, { params: { userId, code }, responseType: 'text' }).pipe(
            map(() => undefined)
        );
    }
}
