import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ShoppingList } from './shopping-list';

export interface Guest {
    id: string;
    email: string;
}

@Injectable({ providedIn: 'root' })
export class ShoppingListService {
    private http = inject(HttpClient);
    private readonly baseUrl = '/api/shopping-lists';

    getAll(): Observable<ShoppingList[]> {
        return this.http.get<ShoppingList[]>(this.baseUrl);
    }

    create(name: string): Observable<ShoppingList> {
        return this.http.post<ShoppingList>(this.baseUrl, { name });
    }

    rename(id: number, name: string): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/${id}`, { name });
    }

    delete(id: number): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${id}`);
    }

    getGuests(id: number): Observable<Guest[]> {
        return this.http.get<Guest[]>(`${this.baseUrl}/${id}/guests`);
    }

    addGuest(id: number, email: string): Observable<Guest> {
        return this.http.post<Guest>(`${this.baseUrl}/${id}/guests`, { email });
    }

    removeGuest(id: number, guestId: string): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${id}/guests/${guestId}`);
    }

    merge(listIds: number[], name: string | null = null): Observable<ShoppingList> {
        return this.http.post<ShoppingList>(`${this.baseUrl}/merge`, { name, listIds });
    }
}
