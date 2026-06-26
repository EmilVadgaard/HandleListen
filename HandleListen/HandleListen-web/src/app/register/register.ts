import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../auth';
import { Router } from '@angular/router';

@Component({
  selector: 'app-register',
  imports: [FormsModule],
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
      this.errorMessage.set('Passwords do not match.');
      return;
    }
    this.auth.register(this.email, this.password).subscribe({
      next: () => this.router.navigate(['/login']),
      error: (err: Error) => this.errorMessage.set(err.message)
    });
  }
}
