import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../auth';

@Component({
  selector: 'app-login',
  imports: [FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css',
})

export class Login {
  private auth = inject(AuthService);

  email = '';
  password = '';
  errorMessage = signal<string | null>(null);

  register() {
    this.errorMessage.set(null);
    this.auth.register(this.email, this.password).subscribe({
      next: () => this.login(),
      error: () => this.errorMessage.set('Registration failed. Please try again.')
    });
  }

  login() {
    this.errorMessage.set(null);
    this.auth.login(this.email, this.password).subscribe({
      next: () => { this.email = ''; this.password = ''; },
      error: () => this.errorMessage.set('Login failed. Please check your credentials and try again.')
    });
  }
}
