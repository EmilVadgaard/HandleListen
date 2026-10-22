import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../auth';
import { AccountService } from '../account.service';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';

@Component({
  selector: 'app-register',
  imports: [FormsModule, RouterLink, MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatCheckboxModule],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register {
  private router = inject(Router);
  private auth = inject(AuthService);
  private accountService = inject(AccountService);

  email = '';
  password = '';
  confirmPassword = '';
  acceptedPrivacyPolicy = false;
  errorMessage = signal<string | null>(null);

  register() {
    this.errorMessage.set(null);
    if (this.password !== this.confirmPassword) {
      this.errorMessage.set('Adgangskoderne er ikke ens.');
      return;
    }
    if (!this.acceptedPrivacyPolicy) {
      this.errorMessage.set('Du skal acceptere privatlivspolitikken for at oprette en konto.');
      return;
    }

    this.auth.register(this.email, this.password).subscribe({
      next: () => {
        // Best-effort - the account exists either way, and the user can still read the policy
        // later. Not registering agreement shouldn't block them from using the app.
        this.accountService.acceptPrivacyPolicy(this.email).subscribe();
        this.router.navigate(['/login']);
      },
      error: (err: Error) => this.errorMessage.set(err.message)
    });
  }
}
