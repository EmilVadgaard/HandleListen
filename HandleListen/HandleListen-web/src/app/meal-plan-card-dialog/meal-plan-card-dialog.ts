import { Component, inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { MealPlanService } from '../meal-plan.service';
import { MealPlan } from '../meal-plan';
import { GenerateListDialog, GenerateListDialogData } from '../generate-list-dialog/generate-list-dialog';
import { ShoppingList } from '../shopping-list';

export interface MealPlanCardDialogResult {
  deleted?: boolean;
  generatedList?: ShoppingList;
}

@Component({
  selector: 'app-meal-plan-card-dialog',
  imports: [MatDialogModule, MatButtonModule, MatIconModule, MatListModule, MatDividerModule],
  templateUrl: './meal-plan-card-dialog.html',
  styleUrl: './meal-plan-card-dialog.css',
})
export class MealPlanCardDialog {
  private dialogRef = inject(MatDialogRef<MealPlanCardDialog, MealPlanCardDialogResult | undefined>);
  private mealPlanService = inject(MealPlanService);
  private dialog = inject(MatDialog);
  plan = inject<MealPlan>(MAT_DIALOG_DATA);

  private generatedList = signal<ShoppingList | undefined>(undefined);

  openGenerateDialog() {
    const data: GenerateListDialogData = { mealPlanId: this.plan.id, defaultName: this.plan.name };
    const ref = this.dialog.open(GenerateListDialog, { data, width: '600px', maxWidth: '95vw', maxHeight: '90vh' });
    ref.afterClosed().subscribe((list?: ShoppingList) => {
      if (list) {
        this.generatedList.set(list);
        this.close();
      }
    });
  }

  deletePlan() {
    if (!confirm(`Er du sikker på at du vil slette madplanen "${this.plan.name}"? Dette kan ikke fortrydes.`)) return;
    this.mealPlanService.delete(this.plan.id).subscribe(() => {
      this.dialogRef.close({ deleted: true });
    });
  }

  close() {
    this.dialogRef.close(this.generatedList() ? { generatedList: this.generatedList() } : undefined);
  }
}
