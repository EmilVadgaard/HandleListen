import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../auth';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-register',
  imports: [FormsModule, RouterLink, MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register {
  private router = inject(Router);
  private auth = inject(AuthService);

  email = '';
  password = '';
  confirmPassword = '';
  errorMessage = signal<string | null>(null);

  register() {
    this.errorMessage.set(null);
    if (this.password !== this.confirmPassword) {
      this.errorMessage.set('Adgangskoderne er ikke ens.');
      return;
    }
    this.auth.register(this.email, this.password).subscribe({
      next: () => this.router.navigate(['/login']),
      error: (err: Error) => this.errorMessage.set(err.message)
    });
  }
}
