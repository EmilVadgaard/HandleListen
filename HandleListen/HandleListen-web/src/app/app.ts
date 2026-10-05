import { Component, signal, inject } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { Header } from './header/header';
import { AuthService } from './auth';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, Header, MatSidenavModule, MatListModule, MatIconModule, MatButtonModule, MatDividerModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  auth = inject(AuthService);
  private router = inject(Router);

  sidenavOpen = signal(false);

  openSidenav() {
    this.sidenavOpen.set(true);
  }

  closeSidenav() {
    this.sidenavOpen.set(false);
  }

  logout() {
    this.auth.logout();
    this.closeSidenav();
    this.router.navigate(['/login']);
  }
}
