import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { PlanService } from '../plan.service';
import { Plan, PlanType } from '../plan';

const MONTH_NAMES = [
  'Januar', 'Februar', 'Marts', 'April', 'Maj', 'Juni',
  'Juli', 'August', 'September', 'Oktober', 'November', 'December',
];

@Component({
  selector: 'app-add-plan-dialog',
  imports: [FormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule],
  templateUrl: './add-plan-dialog.html',
  styleUrl: './add-plan-dialog.css',
})
export class AddPlanDialog {
  private dialogRef = inject(MatDialogRef<AddPlanDialog, Plan | undefined>);
  private planService = inject(PlanService);

  readonly months = MONTH_NAMES.map((name, index) => ({ value: index + 1, name }));
  readonly years = Array.from({ length: 7 }, (_, i) => new Date().getFullYear() - 1 + i);

  name = '';
  type: PlanType = 'Standard';
  dayCount = 7;
  month = new Date().getMonth() + 1;
  year = new Date().getFullYear();
  saving = signal(false);
  error = signal<string | null>(null);

  save() {
    const name = this.name.trim();
    if (!name) return;

    this.saving.set(true);
    this.error.set(null);
    this.planService.create({
      name,
      type: this.type,
      dayCount: this.type === 'Weekly' ? this.dayCount : null,
      month: this.type === 'Monthly' ? this.month : null,
      year: this.type === 'Monthly' ? this.year : null,
    }).subscribe({
      next: plan => {
        this.saving.set(false);
        this.dialogRef.close(plan);
      },
      error: err => {
        this.saving.set(false);
        this.error.set(typeof err.error === 'string' ? err.error : 'Kunne ikke oprette planen.');
      }
    });
  }

  close() {
    this.dialogRef.close();
  }
}
