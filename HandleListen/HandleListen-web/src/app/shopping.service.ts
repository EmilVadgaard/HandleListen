import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ShoppingItem } from './shopping-item';
import { UnitOfMeasure } from './unit-of-measure';

export interface CategorySuggestion {
    category: string | null;
    source: 'Known' | 'History' | null;
    defaultUnit: UnitOfMeasure | null;
}

@Injectable({ providedIn: 'root' })
export class ShoppingService {
    private http = inject(HttpClient);
    private readonly baseUrl = '/api/shopping-items';

    getByList(shoppingListId: number): Observable<ShoppingItem[]> {
        return this.http.get<ShoppingItem[]>(`${this.baseUrl}/by-list/${shoppingListId}`);
    }

    suggestCategory(name: string): Observable<CategorySuggestion> {
        const params = new HttpParams().set('name', name);
        return this.http.get<CategorySuggestion>('/api/known-items/suggest', { params });
    }

    create(name: string, category: string, quantity: number, shoppingListId: number): Observable<ShoppingItem> {
        return this.http.post<ShoppingItem>(this.baseUrl, { name, category, quantity, shoppingListId });
    }

    update(item: ShoppingItem): Observable<ShoppingItem> {
        return this.http.put<ShoppingItem>(`${this.baseUrl}/${item.id}`, item);
    }

    delete(id: number): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${id}`);
    }
}
