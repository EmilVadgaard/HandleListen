import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface BlockedUser {
    id: number;
    email: string;
}

@Injectable({ providedIn: 'root' })
export class AccountService {
    private http = inject(HttpClient);

    acceptPrivacyPolicy(email: string): Observable<void> {
        return this.http.post<void>('/api/account/accept-privacy-policy', { email });
    }

    getBlockedUsers(): Observable<BlockedUser[]> {
        return this.http.get<BlockedUser[]>('/api/account/blocked-users');
    }

    blockUser(email: string): Observable<void> {
        return this.http.post<void>('/api/account/blocked-users', { email });
    }

    unblockUser(id: number): Observable<void> {
        return this.http.delete<void>(`/api/account/blocked-users/${id}`);
    }

    deleteMyAccount(): Observable<void> {
        return this.http.delete<void>('/api/account');
    }

    // ASP.NET Identity's built-in endpoint (MapIdentityApi) - no custom backend code needed.
    changePassword(oldPassword: string, newPassword: string): Observable<void> {
        return this.http.post<void>('/api/auth/manage/info', { oldPassword, newPassword });
    }
}
