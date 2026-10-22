import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CalendarEvent, CalendarEventOccurrence } from './calendar-event';

@Injectable({ providedIn: 'root' })
export class CalendarService {
    private http = inject(HttpClient);
    private readonly baseUrl = '/api/calendar-events';

    getByRange(start: string, end: string): Observable<CalendarEventOccurrence[]> {
        return this.http.get<CalendarEventOccurrence[]>(this.baseUrl, { params: { start, end } });
    }

    getById(id: number): Observable<CalendarEvent> {
        return this.http.get<CalendarEvent>(`${this.baseUrl}/${id}`);
    }

    create(event: Omit<CalendarEvent, 'id' | 'calendarId'>): Observable<CalendarEvent> {
        return this.http.post<CalendarEvent>(this.baseUrl, event);
    }

    update(id: number, event: Omit<CalendarEvent, 'id' | 'calendarId'>): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/${id}`, { id, ...event });
    }

    delete(id: number): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${id}`);
    }

    deleteOccurrence(id: number, date: string): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${id}/occurrences/${date}`);
    }
}
