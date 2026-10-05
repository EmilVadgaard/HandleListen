import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatListModule } from '@angular/material/list';
import { ShoppingListService } from '../shopping-list.service';
import { ShoppingList } from '../shopping-list';

export interface MergeListsDialogData {
  lists: ShoppingList[];
}

@Component({
  selector: 'app-merge-lists-dialog',
  imports: [FormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatCheckboxModule, MatListModule],
  templateUrl: './merge-lists-dialog.html',
  styleUrl: './merge-lists-dialog.css',
})
export class MergeListsDialog {
  private dialogRef = inject(MatDialogRef<MergeListsDialog, ShoppingList | undefined>);
  private shoppingListService = inject(ShoppingListService);
  data = inject<MergeListsDialogData>(MAT_DIALOG_DATA);

  name = '';
  selectedIds = signal<Set<number>>(new Set());
  saving = signal(false);
  error = signal<string | null>(null);

  toggleList(id: number) {
    this.selectedIds.update(ids => {
      const next = new Set(ids);
      if (next.has(id)) next.delete(id); else next.add(id);
      return next;
    });
  }

  isSelected(id: number): boolean {
    return this.selectedIds().has(id);
  }

  save() {
    const ids = Array.from(this.selectedIds());
    if (ids.length < 2) {
      this.error.set('Vælg mindst to lister.');
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    this.shoppingListService.merge(ids, this.name.trim() || null).subscribe({
      next: list => {
        this.saving.set(false);
        this.dialogRef.close(list);
      },
      error: () => {
        this.saving.set(false);
        this.error.set('Kunne ikke sammenlægge listerne.');
      }
    });
  }

  close() {
    this.dialogRef.close();
  }
}
