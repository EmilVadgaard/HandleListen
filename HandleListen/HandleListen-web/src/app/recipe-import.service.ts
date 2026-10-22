import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ImportedRecipe } from './imported-recipe';

export interface CleanedIngredientName {
    name: string;
    category: string | null;
}

@Injectable({ providedIn: 'root' })
export class RecipeImportService {
    private http = inject(HttpClient);

    importFromUrl(url: string): Observable<ImportedRecipe> {
        return this.http.post<ImportedRecipe>('/api/recipes/import/url', { url });
    }

    importFromAiText(text: string): Observable<ImportedRecipe> {
        return this.http.post<ImportedRecipe>('/api/recipes/import/ai-text', { text });
    }

    importFromAiImage(file: File): Observable<ImportedRecipe> {
        const formData = new FormData();
        formData.append('file', file);
        return this.http.post<ImportedRecipe>('/api/recipes/import/ai-image', formData);
    }

    cleanIngredientNames(names: string[]): Observable<CleanedIngredientName[]> {
        return this.http.post<CleanedIngredientName[]>('/api/recipes/import/ai-clean-ingredients', { names });
    }
}
