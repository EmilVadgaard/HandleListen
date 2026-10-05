import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../auth';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-reset-password',
  imports: [FormsModule, RouterLink, MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  templateUrl: './reset-password.html',
  styleUrl: './reset-password.css',
})
export class ResetPassword implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private auth = inject(AuthService);

  private email = '';
  private code = '';
  linkInvalid = signal(false);

  password = '';
  confirmPassword = '';
  errorMessage = signal<string | null>(null);
  success = signal(false);

  ngOnInit() {
    const params = this.route.snapshot.queryParamMap;
    this.email = params.get('email') ?? '';
    this.code = params.get('code') ?? '';
    if (!this.email || !this.code) {
      this.linkInvalid.set(true);
    }
  }

  submit() {
    this.errorMessage.set(null);
    if (this.password !== this.confirmPassword) {
      this.errorMessage.set('Adgangskoderne er ikke ens.');
      return;
    }
    this.auth.resetPassword(this.email, this.code, this.password).subscribe({
      next: () => this.success.set(true),
      error: (err: Error) => this.errorMessage.set(err.message)
    });
  }

  goToLogin() {
    this.router.navigate(['/login']);
  }
}
