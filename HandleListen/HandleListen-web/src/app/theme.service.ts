import { Injectable, signal } from '@angular/core';

const STORAGE_KEY = 'theme';

// Angular Material 3's theme() mixin already generates both light and dark variants of every
// --mat-sys-* variable - switching themes is just setting color-scheme, no extra CSS needed.
@Injectable({ providedIn: 'root' })
export class ThemeService {
    isDark = signal(this.readStoredPreference());

    constructor() {
        this.apply(this.isDark());
    }

    toggle() {
        const next = !this.isDark();
        this.isDark.set(next);
        this.apply(next);
        try {
            localStorage.setItem(STORAGE_KEY, next ? 'dark' : 'light');
        } catch {
            // Private browsing / blocked storage - theme still applies for this session.
        }
    }

    private readStoredPreference(): boolean {
        try {
            return localStorage.getItem(STORAGE_KEY) === 'dark';
        } catch {
            return false;
        }
    }

    private apply(dark: boolean) {
        document.body.style.colorScheme = dark ? 'dark' : 'light';
    }
}
