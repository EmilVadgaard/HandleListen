import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { RecipeBookMember, RecipeBookInvite } from './recipe-book-invite';

@Injectable({ providedIn: 'root' })
export class RecipeBookSharingService {
    private http = inject(HttpClient);
    private readonly baseUrl = '/api/recipe-book';

    getMyBookId(): Observable<{ id: number }> {
        return this.http.get<{ id: number }>(this.baseUrl);
    }

    getMembers(): Observable<RecipeBookMember[]> {
        return this.http.get<RecipeBookMember[]>(`${this.baseUrl}/members`);
    }

    getInvites(): Observable<RecipeBookInvite[]> {
        return this.http.get<RecipeBookInvite[]>(`${this.baseUrl}/invites`);
    }

    createInvite(email: string): Observable<RecipeBookInvite> {
        return this.http.post<RecipeBookInvite>(`${this.baseUrl}/invites`, { email });
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
