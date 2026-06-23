import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../auth';
import { MatButtonModule } from '@angular/material/button'

@Component({
  selector: 'app-header',
  imports: [RouterLink, MatButtonModule],
  templateUrl: './header.html',
  styleUrl: './header.css',
})

export class Header {
  auth = inject(AuthService);
  private router = inject(Router);

  logout() {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
