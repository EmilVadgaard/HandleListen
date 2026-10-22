import { Component, inject, signal } from '@angular/core';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { AccountService } from '../account.service';

@Component({
  selector: 'app-delete-account-dialog',
  imports: [MatDialogModule, MatButtonModule],
  templateUrl: './delete-account-dialog.html',
  styleUrl: './delete-account-dialog.css',
})
export class DeleteAccountDialog {
  private dialogRef = inject(MatDialogRef<DeleteAccountDialog, boolean>);
  private accountService = inject(AccountService);

  deleting = signal(false);
  error = signal<string | null>(null);

  confirmDelete() {
    this.deleting.set(true);
    this.error.set(null);
    this.accountService.deleteMyAccount().subscribe({
      next: () => this.dialogRef.close(true),
      error: () => {
        this.deleting.set(false);
        this.error.set('Kunne ikke slette kontoen. Prøv igen, eller kontakt Emil.');
      }
    });
  }

  cancel() {
    this.dialogRef.close(false);
  }
}
