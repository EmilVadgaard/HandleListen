import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { UnitOfMeasure } from './unit-of-measure';

export interface KnownItem {
    id: number;
    canonicalName: string;
    category: string;
    defaultUnit: UnitOfMeasure | null;
    aliases: string[];
}

@Injectable({ providedIn: 'root' })
export class KnownItemService {
    private http = inject(HttpClient);
    private readonly baseUrl = '/api/known-items';

    search(query: string): Observable<KnownItem[]> {
        const params = new HttpParams().set('search', query);
        return this.http.get<KnownItem[]>(this.baseUrl, { params });
    }

    create(name: string, category: string, defaultUnit: UnitOfMeasure | null = null): Observable<KnownItem> {
        return this.http.post<KnownItem>(this.baseUrl, { name, category, defaultUnit });
    }

    addAlias(knownItemId: number, alias: string): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/${knownItemId}/aliases`, { alias });
    }
}
