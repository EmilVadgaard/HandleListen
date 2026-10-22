import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BudgetFixedEntry, BudgetVariableCategory, BudgetEntryType } from './budget';

export interface FixedEntryRequest {
    name: string;
    type: BudgetEntryType;
    amount: number;
}

export interface VariableCategoryRequest {
    name: string;
    type: BudgetEntryType;
    expectedAmount: number;
    actualAmount: number;
}

@Injectable({ providedIn: 'root' })
export class BudgetService {
    private http = inject(HttpClient);
    private readonly baseUrl = '/api/budget';

    getFixedEntries(): Observable<BudgetFixedEntry[]> {
        return this.http.get<BudgetFixedEntry[]>(`${this.baseUrl}/fixed-entries`);
    }

    createFixedEntry(request: FixedEntryRequest): Observable<BudgetFixedEntry> {
        return this.http.post<BudgetFixedEntry>(`${this.baseUrl}/fixed-entries`, request);
    }

    updateFixedEntry(id: number, request: FixedEntryRequest): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/fixed-entries/${id}`, request);
    }

    deleteFixedEntry(id: number): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/fixed-entries/${id}`);
    }

    getVariableCategories(): Observable<BudgetVariableCategory[]> {
        return this.http.get<BudgetVariableCategory[]>(`${this.baseUrl}/variable-categories`);
    }

    createVariableCategory(request: VariableCategoryRequest): Observable<BudgetVariableCategory> {
        return this.http.post<BudgetVariableCategory>(`${this.baseUrl}/variable-categories`, request);
    }

    updateVariableCategory(id: number, request: VariableCategoryRequest): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/variable-categories/${id}`, request);
    }

    deleteVariableCategory(id: number): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/variable-categories/${id}`);
    }
}
