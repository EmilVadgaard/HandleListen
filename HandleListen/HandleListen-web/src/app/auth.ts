import { Injectable,  inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, tap, catchError, throwError } from 'rxjs';

interface LoginResponse {
  tokenType: string;
  accessToken: string;
  expiresIn: number;
  refreshToken: string;
}

@Injectable({ providedIn: 'root'})
export class AuthService {
    private http = inject(HttpClient);
    private readonly baseUrl = '/api/auth';

    token = signal<string | null>(localStorage.getItem('token'));
    email = signal<string | null>(localStorage.getItem('email'));

    isLoggedIn = computed(() => this.token() !== null);

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
            })
        );
    }

    logout() {
        this.token.set(null);
        this.email.set(null);
        localStorage.removeItem('token');
        localStorage.removeItem('email');
    }
}
