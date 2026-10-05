import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { MealPlan, GeneratedListItem } from './meal-plan';
import { ShoppingList } from './shopping-list';

@Injectable({ providedIn: 'root' })
export class MealPlanService {
    private http = inject(HttpClient);
    private readonly baseUrl = '/api/meal-plans';

    getAll(): Observable<MealPlan[]> {
        return this.http.get<MealPlan[]>(this.baseUrl);
    }

    create(name: string, recipeIds: number[]): Observable<MealPlan> {
        return this.http.post<MealPlan>(this.baseUrl, { name, recipeIds });
    }

    delete(id: number): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${id}`);
    }

    getGeneratePreview(id: number): Observable<GeneratedListItem[]> {
        return this.http.get<GeneratedListItem[]>(`${this.baseUrl}/${id}/generate-preview`);
    }

    generate(id: number, listName: string, items: GeneratedListItem[]): Observable<ShoppingList> {
        const payload = items.map(i => ({ name: i.name, category: i.category, quantity: i.quantity }));
        return this.http.post<ShoppingList>(`${this.baseUrl}/${id}/generate`, { listName, items: payload });
    }
}
