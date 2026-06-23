import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ShoppingItem } from './shopping-item';

@Injectable({ providedIn: 'root' })
export class ShoppingService {
    private http = inject(HttpClient);
    private readonly baseUrl = 'http://localhost:5232/api/ShoppingItem';

    getAll(): Observable<ShoppingItem[]> {
        return this.http.get<ShoppingItem[]>(this.baseUrl);
    }

    create(name: string, category: string, quantity: number): Observable<ShoppingItem> {
        return this.http.post<ShoppingItem>(this.baseUrl, { name, category, quantity });
    }

    update(item: ShoppingItem): Observable<ShoppingItem> {
        return this.http.put<ShoppingItem>(`${this.baseUrl}/${item.id}`, item);
    }

    delete(id: number): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${id}`);
    }
}
