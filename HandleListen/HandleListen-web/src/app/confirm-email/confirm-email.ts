import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../auth';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

type Status = 'loading' | 'success' | 'error';

@Component({
  selector: 'app-confirm-email',
  imports: [RouterLink, MatCardModule, MatButtonModule, MatProgressSpinnerModule],
  templateUrl: './confirm-email.html',
  styleUrl: './confirm-email.css',
})
export class ConfirmEmail implements OnInit {
  private route = inject(ActivatedRoute);
  private auth = inject(AuthService);

  status = signal<Status>('loading');

  ngOnInit() {
    const params = this.route.snapshot.queryParamMap;
    const userId = params.get('userId');
    const code = params.get('code');
    if (!userId || !code) {
      this.status.set('error');
      return;
    }

    this.auth.confirmEmail(userId, code).subscribe({
      next: () => this.status.set('success'),
      error: () => this.status.set('error')
    });
  }
}
