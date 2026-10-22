import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatRadioModule } from '@angular/material/radio';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { AccountService, BlockedUser } from '../account.service';
import { DigestService, DigestFrequency } from '../digest.service';
import { ThemeService } from '../theme.service';
import { AuthService } from '../auth';
import { DeleteAccountDialog } from '../delete-account-dialog/delete-account-dialog';

@Component({
  selector: 'app-settings',
  imports: [FormsModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, MatRadioModule, MatSlideToggleModule, MatListModule, MatDividerModule],
  templateUrl: './settings.html',
  styleUrl: './settings.css',
})
export class Settings {
  private accountService = inject(AccountService);
  private digestService = inject(DigestService);
  private auth = inject(AuthService);
  private dialog = inject(MatDialog);
  theme = inject(ThemeService);

  oldPassword = '';
  newPassword = '';
  passwordMessage = signal<string | null>(null);
  passwordSaving = signal(false);

  digestFrequency: DigestFrequency | 'None' = 'None';
  digestMessage = signal<string | null>(null);
  digestSaving = signal(false);
  digestSending = signal(false);

  blockedUsers = signal<BlockedUser[]>([]);
  newBlockEmail = '';
  blockError = signal<string | null>(null);

  constructor() {
    this.digestService.getPreference().subscribe(pref => {
      this.digestFrequency = pref.frequency ?? 'None';
    });
    this.refreshBlockedUsers();
  }

  private refreshBlockedUsers() {
    this.accountService.getBlockedUsers().subscribe(users => this.blockedUsers.set(users));
  }

  changePassword() {
    if (!this.oldPassword || !this.newPassword) return;
    this.passwordSaving.set(true);
    this.passwordMessage.set(null);
    this.accountService.changePassword(this.oldPassword, this.newPassword).subscribe({
      next: () => {
        this.passwordSaving.set(false);
        this.passwordMessage.set('Adgangskode ændret.');
        this.oldPassword = '';
        this.newPassword = '';
      },
      error: () => {
        this.passwordSaving.set(false);
        this.passwordMessage.set('Kunne ikke ændre adgangskoden - tjek at den nuværende adgangskode er korrekt.');
      }
    });
  }

  saveDigestPreference() {
    this.digestSaving.set(true);
    this.digestMessage.set(null);
    const value = this.digestFrequency === 'None' ? null : this.digestFrequency;
    this.digestService.setPreference(value).subscribe({
      next: () => {
        this.digestSaving.set(false);
        this.digestMessage.set('Gemt.');
      },
      error: () => {
        this.digestSaving.set(false);
        this.digestMessage.set('Kunne ikke gemme.');
      }
    });
  }

  sendDigestNow() {
    if (this.digestFrequency === 'None') return;
    this.digestSending.set(true);
    this.digestMessage.set(null);
    this.digestService.sendNow(this.digestFrequency).subscribe({
      next: result => {
        this.digestSending.set(false);
        this.digestMessage.set(result.sent ? 'Mail sendt.' : 'Ingen kommende begivenheder i perioden - ingen mail sendt.');
      },
      error: () => {
        this.digestSending.set(false);
        this.digestMessage.set('Kunne ikke sende mailen.');
      }
    });
  }

  blockUser() {
    const email = this.newBlockEmail.trim();
    if (!email) return;
    this.blockError.set(null);
    this.accountService.blockUser(email).subscribe({
      next: () => {
        this.newBlockEmail = '';
        this.refreshBlockedUsers();
      },
      error: () => this.blockError.set('Kunne ikke blokere - findes der en bruger med den email?')
    });
  }

  unblockUser(user: BlockedUser) {
    this.accountService.unblockUser(user.id).subscribe(() => this.refreshBlockedUsers());
  }

  openDeleteAccountDialog() {
    const ref = this.dialog.open(DeleteAccountDialog, { width: '420px', maxWidth: '90vw' });
    ref.afterClosed().subscribe(deleted => {
      if (deleted) {
        this.auth.logout();
        window.location.href = '/login';
      }
    });
  }
}
