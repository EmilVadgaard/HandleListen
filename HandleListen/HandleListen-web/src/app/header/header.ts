import { Component, inject, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../auth';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-header',
  imports: [RouterLink, MatButtonModule, MatToolbarModule, MatIconModule],
  templateUrl: './header.html',
  styleUrl: './header.css',
})

export class Header {
  auth = inject(AuthService);

  menuToggle = output<void>();
}
