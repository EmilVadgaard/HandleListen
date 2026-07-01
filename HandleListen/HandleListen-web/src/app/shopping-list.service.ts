import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ShoppingList } from './shopping-list';

// TODO: Jeg har lige ændret baseURL, byg på ny, og lig ind på siden.
@Injectable({ providedIn: 'root' })
export class ShoppingListService {
    private http = inject(HttpClient);
    private readonly baseUrl = '/api/shopping-lists';

    getAll(): Observable<ShoppingList[]> {
        return this.http.get<ShoppingList[]>(this.baseUrl);
    }

    getByListId(id: number): Observable<ShoppingList> {
        return this.http.get<ShoppingList>(`${this.baseUrl}/${id}`);
    }

    create(name: string): Observable<ShoppingList> {
        return this.http.post<ShoppingList>(this.baseUrl, { name });
    }

    update(item: ShoppingList): Observable<ShoppingList> {
        return this.http.put<ShoppingList>(`${this.baseUrl}/${item.id}`, item);
    }

    delete(id: number): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${id}`);
    }
}
