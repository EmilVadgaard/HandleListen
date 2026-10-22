import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Capacitor } from '@capacitor/core';
import { PushNotifications } from '@capacitor/push-notifications';

@Injectable({ providedIn: 'root' })
export class PushNotificationService {
    private http = inject(HttpClient);
    private readonly baseUrl = '/api/push-subscriptions';
    private initialized = false;
    private lastToken: string | null = null;

    async initialize(): Promise<void> {
        // Web push isn't wired up yet - this only does anything inside the native Android shell.
        if (!Capacitor.isNativePlatform() || this.initialized) return;
        this.initialized = true;

        const permission = await PushNotifications.checkPermissions();
        if (permission.receive !== 'granted') {
            const requested = await PushNotifications.requestPermissions();
            if (requested.receive !== 'granted') return;
        }

        PushNotifications.addListener('registration', token => {
            this.lastToken = token.value;
            this.http.post(this.baseUrl, { token: token.value }).subscribe({
                error: err => console.error('Failed to register push token', err)
            });
        });

        PushNotifications.addListener('registrationError', err => {
            console.error('Push registration failed', err);
        });

        await PushNotifications.register();
        await this.createChannels();
    }

    // Called on logout - otherwise a shared device would keep delivering this user's reminders
    // to whoever's logged in next, until (if ever) a fresh registration event happens to
    // reassign the token. Resets `initialized` too, so the next login re-registers properly
    // instead of silently staying bound to nobody.
    unregister(): void {
        if (!this.lastToken) return;
        this.http.delete(this.baseUrl, { body: { token: this.lastToken } }).subscribe({
            error: err => console.error('Failed to unregister push token', err)
        });
        this.lastToken = null;
        this.initialized = false;
    }

    private async createChannels(): Promise<void> {
        // Two channels, so a reminder only makes noise once: the first alert for an event uses
        // the normal channel (sound + vibration), and every later re-send of the SAME event
        // (the hourly nag while it's still unacknowledged) uses the quiet channel instead - it
        // still updates the notification's text/timestamp, just without buzzing the phone again.
        try {
            await PushNotifications.createChannel({
                id: 'reminders',
                name: 'Påmindelser',
                description: 'Den første notifikation for en kalender-påmindelse',
                importance: 4,
                visibility: 1,
                vibration: true,
            });
            await PushNotifications.createChannel({
                id: 'reminders-quiet',
                name: 'Gentagne påmindelser',
                description: 'Opdateringer af en påmindelse du allerede har fået besked om',
                importance: 2,
                visibility: 1,
                vibration: false,
            });
        } catch (err) {
            console.error('Failed to create notification channels', err);
        }
    }
}
