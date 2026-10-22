import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SharedRecipe, SharedRecipeDetail, TopSearch } from './shared-recipe';
import { Recipe } from './recipe';

@Injectable({ providedIn: 'root' })
export class ForumService {
    private http = inject(HttpClient);
    private readonly baseUrl = '/api/forum';

    getAll(search?: string): Observable<SharedRecipe[]> {
        const params: Record<string, string> = {};
        if (search?.trim()) params['search'] = search.trim();
        return this.http.get<SharedRecipe[]>(this.baseUrl, { params });
    }

    getById(id: number): Observable<SharedRecipeDetail> {
        return this.http.get<SharedRecipeDetail>(`${this.baseUrl}/${id}`);
    }

    share(recipeId: number): Observable<SharedRecipe> {
        return this.http.post<SharedRecipe>(`${this.baseUrl}/share`, { recipeId });
    }

    unshare(sharedRecipeId: number): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/shared/${sharedRecipeId}`);
    }

    getShareStatus(recipeId: number): Observable<SharedRecipe> {
        return this.http.get<SharedRecipe>(`${this.baseUrl}/shared/by-recipe/${recipeId}`);
    }

    addToMyBook(sharedRecipeId: number): Observable<Recipe> {
        return this.http.post<Recipe>(`${this.baseUrl}/${sharedRecipeId}/add`, {});
    }

    logSearch(query: string): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/search-log`, { query });
    }

    getTopSearches(): Observable<TopSearch[]> {
        return this.http.get<TopSearch[]>(`${this.baseUrl}/top-searches`);
    }
}
