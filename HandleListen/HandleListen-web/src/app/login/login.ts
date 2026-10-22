import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { AuthService } from '../auth';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink, MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  private router = inject(Router);
  private auth = inject(AuthService);
  private route = inject(ActivatedRoute);

  email = '';
  password = '';
  errorMessage = signal<string | null>(
    this.route.snapshot.queryParamMap.get('sessionExpired') ? 'Din session er udløbet. Log venligst ind igen.' : null
  );

  login() {
    this.errorMessage.set(null);
    this.auth.login(this.email, this.password).subscribe({
      next: () => this.router.navigate(['/handleliste']),
      error: () => this.errorMessage.set('Forkert email eller adgangskode.')
    });
  }
}
