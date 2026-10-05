import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { RecipeIngredient } from './recipe-ingredient';
import { UnitOfMeasure } from './unit-of-measure';

@Injectable({ providedIn: 'root' })
export class RecipeIngredientService {
    private http = inject(HttpClient);
    private readonly baseUrl = '/api/recipe-ingredients';

    getByRecipe(recipeId: number): Observable<RecipeIngredient[]> {
        return this.http.get<RecipeIngredient[]>(`${this.baseUrl}/by-recipe/${recipeId}`);
    }

    create(name: string, quantity: number, recipeId: number, amount: number | null = null, unit: UnitOfMeasure | null = null, category: string = ''): Observable<RecipeIngredient> {
        return this.http.post<RecipeIngredient>(this.baseUrl, { name, quantity, recipeId, amount, unit, category });
    }

    update(ingredient: RecipeIngredient): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/${ingredient.id}`, ingredient);
    }

    delete(id: number): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${id}`);
    }
}
