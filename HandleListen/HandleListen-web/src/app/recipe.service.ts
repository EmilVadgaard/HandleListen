import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Recipe, RecipeTag } from './recipe';

@Injectable({ providedIn: 'root' })
export class RecipeService {
    private http = inject(HttpClient);
    private readonly baseUrl = '/api/recipes';

    getAll(): Observable<Recipe[]> {
        return this.http.get<Recipe[]>(this.baseUrl);
    }

    getById(id: number): Observable<Recipe> {
        return this.http.get<Recipe>(`${this.baseUrl}/${id}`);
    }

    create(title: string, description: string): Observable<Recipe> {
        return this.http.post<Recipe>(this.baseUrl, { title, description });
    }

    update(id: number, title: string, description: string): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/${id}`, { title, description });
    }

    delete(id: number): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${id}`);
    }

    getTagSuggestions(): Observable<string[]> {
        return this.http.get<string[]>(`${this.baseUrl}/tags`);
    }

    addTag(recipeId: number, tag: string): Observable<RecipeTag> {
        return this.http.post<RecipeTag>(`${this.baseUrl}/${recipeId}/tags`, { tag });
    }

    removeTag(recipeId: number, tagId: number): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${recipeId}/tags/${tagId}`);
    }
}
