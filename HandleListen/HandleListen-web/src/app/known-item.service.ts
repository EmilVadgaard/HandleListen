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

export interface KnownItemAlias {
    id: number;
    alias: string;
}

export interface KnownItemAdmin {
    id: number;
    canonicalName: string;
    category: string;
    defaultUnit: UnitOfMeasure | null;
    aliases: KnownItemAlias[];
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

    getAllForAdmin(): Observable<KnownItemAdmin[]> {
        return this.http.get<KnownItemAdmin[]>(`${this.baseUrl}/all`);
    }

    update(id: number, canonicalName: string, category: string, defaultUnit: UnitOfMeasure | null): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/${id}`, { canonicalName, category, defaultUnit });
    }

    delete(id: number): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${id}`);
    }

    removeAlias(knownItemId: number, aliasId: number): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${knownItemId}/aliases/${aliasId}`);
    }
}
