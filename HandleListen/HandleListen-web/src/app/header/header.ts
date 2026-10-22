import { Component, inject, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../auth';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatBadgeModule } from '@angular/material/badge';
import { MatMenuModule } from '@angular/material/menu';
import { InviteNotificationsService, PendingInvite } from '../invite-notifications.service';

@Component({
  selector: 'app-header',
  imports: [RouterLink, MatButtonModule, MatToolbarModule, MatIconModule, MatBadgeModule, MatMenuModule],
  templateUrl: './header.html',
  styleUrl: './header.css',
})

export class Header {
  auth = inject(AuthService);
  notifications = inject(InviteNotificationsService);

  menuToggle = output<void>();

  inviteLabel(invite: PendingInvite): string {
    return invite.type === 'calendar' ? 'vil dele en kalender med dig' : 'vil dele en opskriftsbog med dig';
  }

  accept(invite: PendingInvite) {
    this.notifications.accept(invite);
  }

  decline(invite: PendingInvite) {
    this.notifications.decline(invite);
  }
}
