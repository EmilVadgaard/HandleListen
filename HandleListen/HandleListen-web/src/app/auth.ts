import { Injectable,  inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

interface LoginResponse {
  tokenType: string;
  accessToken: string;
  expiresIn: number;
  refreshToken: string;
}

@Injectable({ providedIn: 'root'})
export class AuthService {
    private http = inject(HttpClient);
    private readonly baseUrl = 'http://localhost:5232/api/auth';

    token = signal<string | null>(null);
    isLoggedIn = computed(() => this.token() !== null);

    email = signal<string | null>(null);

    register(email: string, password: string): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/register`, { email, password });
    }

    login(email: string, password: string): Observable<LoginResponse> {
        return this.http.post<LoginResponse>(`${this.baseUrl}/login`, { email, password }).pipe(
            tap(response => { 
                this.token.set(response.accessToken); 
                this.email.set(email);
            })
        );
    }

    logout() {
        this.token.set(null);
        this.email.set(null);
    }
}
