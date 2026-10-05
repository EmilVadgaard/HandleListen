import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../auth';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-forgot-password',
  imports: [FormsModule, RouterLink, MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  templateUrl: './forgot-password.html',
  styleUrl: './forgot-password.css',
})
export class ForgotPassword {
  private auth = inject(AuthService);

  email = '';
  submitted = signal(false);

  submit() {
    if (!this.email.trim()) return;
    this.auth.forgotPassword(this.email.trim()).subscribe({
      next: () => this.submitted.set(true),
      // Always show the same confirmation, whether or not the email exists, so we don't leak
      // which addresses are registered.
      error: () => this.submitted.set(true)
    });
  }
}
