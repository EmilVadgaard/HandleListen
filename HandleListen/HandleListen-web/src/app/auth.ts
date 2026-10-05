import { Injectable,  inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, tap, catchError, throwError, map } from 'rxjs';

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
    email = signal<string | null>(localStorage.getItem('email'));
    roles = signal<string[]>(JSON.parse(localStorage.getItem('roles') ?? '[]'));

    isLoggedIn = computed(() => this.token() !== null);
    canCurate = computed(() => this.roles().includes('Owner') || this.roles().includes('Moderator'));

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
            error: () => {
                this.roles.set([]);
                localStorage.removeItem('roles');
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
                this.token.set(response.accessToken);
                this.email.set(email);
                localStorage.setItem('token', response.accessToken);
                localStorage.setItem('email', email);
                this.refreshRoles();
            })
        );
    }

    logout() {
        this.token.set(null);
        this.email.set(null);
        this.roles.set([]);
        localStorage.removeItem('token');
        localStorage.removeItem('email');
        localStorage.removeItem('roles');
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
