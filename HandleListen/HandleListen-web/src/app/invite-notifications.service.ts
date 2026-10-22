import { Injectable, inject, signal, computed, effect } from '@angular/core';
import { forkJoin } from 'rxjs';
import { AuthService } from './auth';
import { RealtimeService } from './realtime.service';
import { CalendarSharingService } from './calendar-sharing.service';
import { RecipeBookSharingService } from './recipe-book-sharing.service';

export interface PendingInvite {
    id: number;
    type: 'calendar' | 'recipeBook';
    fromEmail: string;
    createdAt: string;
}

// Root-level so the bell in the header (always mounted) sees pending invites regardless of
// which page the user is currently on - the per-page badges on Kalender/Opskrifter only updated
// while that exact page happened to be open, which is the gap this closes.
@Injectable({ providedIn: 'root' })
export class InviteNotificationsService {
    private auth = inject(AuthService);
    private realtime = inject(RealtimeService);
    private calendarSharing = inject(CalendarSharingService);
    private recipeBookSharing = inject(RecipeBookSharingService);

    pendingInvites = signal<PendingInvite[]>([]);
    pendingCount = computed(() => this.pendingInvites().length);

    constructor() {
        effect(() => {
            if (this.auth.isLoggedIn()) this.refresh();
        });
        this.realtime.onCalendarChanged(() => this.refresh());
        this.realtime.onRecipesChanged(() => this.refresh());
    }

    refresh() {
        if (!this.auth.isLoggedIn()) return;
        forkJoin([this.calendarSharing.getInvites(), this.recipeBookSharing.getInvites()]).subscribe({
            next: ([calendarInvites, recipeBookInvites]) => {
                this.pendingInvites.set([
                    ...calendarInvites.filter(i => i.isIncoming).map(i => ({ id: i.id, type: 'calendar' as const, fromEmail: i.fromEmail, createdAt: i.createdAt })),
                    ...recipeBookInvites.filter(i => i.isIncoming).map(i => ({ id: i.id, type: 'recipeBook' as const, fromEmail: i.fromEmail, createdAt: i.createdAt })),
                ]);
            },
            error: () => { /* leave last-known state - a transient failure shouldn't drop the badge */ }
        });
    }

    accept(invite: PendingInvite) {
        const request = invite.type === 'calendar' ? this.calendarSharing.acceptInvite(invite.id) : this.recipeBookSharing.acceptInvite(invite.id);
        request.subscribe(() => this.refresh());
    }

    decline(invite: PendingInvite) {
        const request = invite.type === 'calendar' ? this.calendarSharing.declineInvite(invite.id) : this.recipeBookSharing.declineInvite(invite.id);
        request.subscribe(() => this.refresh());
    }
}
