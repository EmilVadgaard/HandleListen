import { Component, signal, inject, effect } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { Header } from './header/header';
import { AuthService } from './auth';
import { PushNotificationService } from './push-notification.service';
import { ThemeService } from './theme.service';
import { AdminService } from './admin.service';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatBadgeModule } from '@angular/material/badge';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, Header, MatSidenavModule, MatListModule, MatIconModule, MatButtonModule, MatDividerModule, MatBadgeModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  auth = inject(AuthService);
  private pushNotifications = inject(PushNotificationService);
  // Injected for its side effect only - its constructor applies the stored (or default) theme
  // on every app load, regardless of which page/route is first.
  private theme = inject(ThemeService);
  adminService = inject(AdminService);

  sidenavOpen = signal(false);

  constructor() {
    effect(() => {
      if (this.auth.isLoggedIn()) {
        this.pushNotifications.initialize();
      }
      if (this.auth.isOwner()) {
        this.adminService.getKnownItemRequests().subscribe();
      }
    });
  }

  openSidenav() {
    this.sidenavOpen.set(true);
  }

  closeSidenav() {
    this.sidenavOpen.set(false);
  }

  logout() {
    this.pushNotifications.unregister();
    this.auth.logout();
    this.closeSidenav();
    // A full reload (not router navigation) is deliberate here, mirroring
    // AuthService.forceLogoutAndReload(): logging out should always land on a clean slate,
    // rather than risk leftover state (e.g. a native permission dialog mid-flight inside the
    // Capacitor WebView) leaving the login page unresponsive until the app is restarted.
    window.location.href = '/login';
  }
}
