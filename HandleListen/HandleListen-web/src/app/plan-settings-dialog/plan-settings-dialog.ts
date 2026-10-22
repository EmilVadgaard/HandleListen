import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { PlanService, Guest } from '../plan.service';
import { Plan } from '../plan';

export interface PlanSettingsResult {
  renamedTo?: string;
  deleted?: boolean;
}

@Component({
  selector: 'app-plan-settings-dialog',
  imports: [FormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, MatListModule, MatDividerModule],
  templateUrl: './plan-settings-dialog.html',
  styleUrl: './plan-settings-dialog.css',
})
export class PlanSettingsDialog {
  private dialogRef = inject(MatDialogRef<PlanSettingsDialog, PlanSettingsResult | undefined>);
  private planService = inject(PlanService);
  plan = inject<Plan>(MAT_DIALOG_DATA);

  renameValue = this.plan.name;
  shareEmail = '';
  shareError = signal<string | null>(null);
  guests = signal<Guest[]>([]);

  constructor() {
    if (this.plan.isOwner) {
      this.planService.getGuests(this.plan.id).subscribe(guests => this.guests.set(guests));
    }
  }

  rename() {
    const name = this.renameValue.trim();
    if (!name || name === this.plan.name) return;
    this.planService.rename(this.plan.id, name).subscribe(() => {
      this.dialogRef.close({ renamedTo: name });
    });
  }

  share() {
    const email = this.shareEmail.trim();
    if (!email) return;
    this.shareError.set(null);
    this.planService.addGuest(this.plan.id, email).subscribe({
      next: guest => {
        this.guests.update(guests => [...guests, guest]);
        this.shareEmail = '';
      },
      error: err => this.shareError.set(typeof err.error === 'string' ? err.error : 'Kunne ikke dele planen.')
    });
  }

  removeGuest(guest: Guest) {
    this.planService.removeGuest(this.plan.id, guest.id).subscribe(() => {
      this.guests.update(guests => guests.filter(g => g.id !== guest.id));
    });
  }

  deletePlan() {
    if (!confirm(`Er du sikker på at du vil slette "${this.plan.name}"? Dette kan ikke fortrydes.`)) return;
    this.planService.delete(this.plan.id).subscribe(() => {
      this.dialogRef.close({ deleted: true });
    });
  }

  close() {
    this.dialogRef.close();
  }
}
