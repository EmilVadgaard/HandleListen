import { Component, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MealPlanService } from '../meal-plan.service';
import { GeneratedListItem } from '../meal-plan';
import { ShoppingList } from '../shopping-list';

export interface GenerateListDialogData {
  mealPlanId: number;
  defaultName: string;
}

@Component({
  selector: 'app-generate-list-dialog',
  imports: [FormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, MatListModule],
  templateUrl: './generate-list-dialog.html',
  styleUrl: './generate-list-dialog.css',
})
export class GenerateListDialog {
  private dialogRef = inject(MatDialogRef<GenerateListDialog, ShoppingList | undefined>);
  private mealPlanService = inject(MealPlanService);
  data = inject<GenerateListDialogData>(MAT_DIALOG_DATA);

  listName = this.data.defaultName;
  items = signal<GeneratedListItem[]>([]);
  loading = signal(true);
  saving = signal(false);
  error = signal<string | null>(null);

  groupedItems = computed(() => {
    const groups = new Map<string, GeneratedListItem[]>();
    for (const item of this.items()) {
      const list = groups.get(item.category) ?? [];
      list.push(item);
      groups.set(item.category, list);
    }
    return Array.from(groups, ([category, items]) => ({ category, items }));
  });

  constructor() {
    this.mealPlanService.getGeneratePreview(this.data.mealPlanId).subscribe(items => {
      this.items.set(items);
      this.loading.set(false);
    });
  }

  updateQuantity(item: GeneratedListItem, newQuantity: number) {
    if (newQuantity < 1) return;
    this.items.update(list => list.map(i => i === item ? { ...i, quantity: newQuantity } : i));
  }

  removeItem(item: GeneratedListItem) {
    this.items.update(list => list.filter(i => i !== item));
  }

  confirm() {
    const name = this.listName.trim();
    if (!name || this.items().length === 0) return;

    this.saving.set(true);
    this.error.set(null);
    this.mealPlanService.generate(this.data.mealPlanId, name, this.items()).subscribe({
      next: list => {
        this.saving.set(false);
        this.dialogRef.close(list);
      },
      error: () => {
        this.saving.set(false);
        this.error.set('Kunne ikke oprette listen.');
      }
    });
  }

  close() {
    this.dialogRef.close();
  }
}
