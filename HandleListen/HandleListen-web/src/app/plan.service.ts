import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Plan, PlanDetail, PlanTodo, PlanType, UpcomingPlanTodo, StandardPlanTodo } from './plan';

export interface Guest {
    id: string;
    email: string;
}

export interface CreatePlanRequest {
    name: string;
    type: PlanType;
    dayCount?: number | null;
    month?: number | null;
    year?: number | null;
}

export interface TodoRequest {
    title: string;
    done?: boolean;
    dayIndex?: number | null;
    dayOfMonth?: number | null;
    time?: string | null;
}

@Injectable({ providedIn: 'root' })
export class PlanService {
    private http = inject(HttpClient);
    private readonly baseUrl = '/api/plans';

    getAll(): Observable<Plan[]> {
        return this.http.get<Plan[]>(this.baseUrl);
    }

    getUpcomingTodos(today: string, days = 30): Observable<UpcomingPlanTodo[]> {
        return this.http.get<UpcomingPlanTodo[]>(`${this.baseUrl}/upcoming-todos`, { params: { today, days } });
    }

    getStandardTodos(): Observable<StandardPlanTodo[]> {
        return this.http.get<StandardPlanTodo[]>(`${this.baseUrl}/standard-todos`);
    }

    getById(id: number): Observable<PlanDetail> {
        return this.http.get<PlanDetail>(`${this.baseUrl}/${id}`);
    }

    create(request: CreatePlanRequest): Observable<Plan> {
        return this.http.post<Plan>(this.baseUrl, request);
    }

    rename(id: number, name: string): Observable<void> {
        return this.http.patch<void>(`${this.baseUrl}/${id}`, { name });
    }

    updateStructure(id: number, fields: { dayCount?: number; month?: number; year?: number }): Observable<void> {
        return this.http.patch<void>(`${this.baseUrl}/${id}`, fields);
    }

    activate(id: number, clientToday: string): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/${id}/activate`, { clientToday });
    }

    delete(id: number): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${id}`);
    }

    addTodo(id: number, request: TodoRequest): Observable<PlanTodo> {
        return this.http.post<PlanTodo>(`${this.baseUrl}/${id}/todos`, request);
    }

    updateTodo(id: number, todoId: number, request: TodoRequest): Observable<void> {
        return this.http.patch<void>(`${this.baseUrl}/${id}/todos/${todoId}`, request);
    }

    reorderTodos(id: number, todoIdsInOrder: number[]): Observable<void> {
        return this.http.patch<void>(`${this.baseUrl}/${id}/todos/reorder`, { todoIdsInOrder });
    }

    deleteTodo(id: number, todoId: number): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${id}/todos/${todoId}`);
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
}
