import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { KnownItemRequest } from './known-item-request';

@Injectable({ providedIn: 'root' })
export class AdminService {
    private http = inject(HttpClient);
    private readonly baseUrl = '/api/admin';

    // Root-level so the "Administrator" nav badge (always mounted) can show the queue count
    // without the Owner needing to open the Admin page first - refreshed on app load and again
    // whenever the Admin page itself resolves a request.
    pendingCount = signal(0);

    getKnownItemRequests(): Observable<KnownItemRequest[]> {
        return this.http.get<KnownItemRequest[]>(`${this.baseUrl}/known-item-requests`).pipe(
            tap(requests => this.pendingCount.set(requests.length))
        );
    }

    resolveKnownItemRequest(id: number): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/known-item-requests/${id}/resolve`, {});
    }
}
