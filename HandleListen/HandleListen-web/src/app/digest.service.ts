import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export type DigestFrequency = 'Daily' | 'Weekly' | 'Monthly';

export interface DigestPreference {
    frequency: DigestFrequency | null;
}

@Injectable({ providedIn: 'root' })
export class DigestService {
    private http = inject(HttpClient);
    private readonly baseUrl = '/api/digest';

    getPreference(): Observable<DigestPreference> {
        return this.http.get<DigestPreference>(`${this.baseUrl}/preference`);
    }

    setPreference(frequency: DigestFrequency | null): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/preference`, { frequency });
    }

    sendNow(frequency: DigestFrequency): Observable<{ sent: boolean }> {
        return this.http.post<{ sent: boolean }>(`${this.baseUrl}/send-now`, { frequency });
    }
}
