import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CalendarMember, CalendarInvite } from './calendar-invite';

@Injectable({ providedIn: 'root' })
export class CalendarSharingService {
    private http = inject(HttpClient);
    private readonly baseUrl = '/api/calendar';

    getMyCalendarId(): Observable<{ id: number }> {
        return this.http.get<{ id: number }>(this.baseUrl);
    }

    getMembers(): Observable<CalendarMember[]> {
        return this.http.get<CalendarMember[]>(`${this.baseUrl}/members`);
    }

    getInvites(): Observable<CalendarInvite[]> {
        return this.http.get<CalendarInvite[]>(`${this.baseUrl}/invites`);
    }

    createInvite(email: string): Observable<CalendarInvite> {
        return this.http.post<CalendarInvite>(`${this.baseUrl}/invites`, { email });
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
