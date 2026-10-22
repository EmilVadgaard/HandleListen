import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BudgetMember, BudgetInvite } from './budget';

@Injectable({ providedIn: 'root' })
export class BudgetSharingService {
    private http = inject(HttpClient);
    private readonly baseUrl = '/api/budget';

    getMyBudgetId(): Observable<{ id: number }> {
        return this.http.get<{ id: number }>(this.baseUrl);
    }

    getMembers(): Observable<BudgetMember[]> {
        return this.http.get<BudgetMember[]>(`${this.baseUrl}/members`);
    }

    getInvites(): Observable<BudgetInvite[]> {
        return this.http.get<BudgetInvite[]>(`${this.baseUrl}/invites`);
    }

    createInvite(email: string): Observable<BudgetInvite> {
        return this.http.post<BudgetInvite>(`${this.baseUrl}/invites`, { email });
    }

    acceptInvite(id: number): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/invites/${id}/accept`, {});
    }

    declineInvite(id: number): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/invites/${id}/decline`, {});
    }

    cancelInvite(id: number): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/invites/${id}`);
    }

    leave(): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/leave`, {});
    }
}
