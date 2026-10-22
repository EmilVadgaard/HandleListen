import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PersonalNote } from './note';

@Injectable({ providedIn: 'root' })
export class NoteService {
    private http = inject(HttpClient);
    private readonly baseUrl = '/api/personal-notes';

    getAll(): Observable<PersonalNote[]> {
        return this.http.get<PersonalNote[]>(this.baseUrl);
    }

    create(text: string, date: string | null): Observable<PersonalNote> {
        return this.http.post<PersonalNote>(this.baseUrl, { text, date });
    }

    update(id: number, text: string, date: string | null): Observable<void> {
        return this.http.patch<void>(`${this.baseUrl}/${id}`, { text, date });
    }

    delete(id: number): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${id}`);
    }
}
