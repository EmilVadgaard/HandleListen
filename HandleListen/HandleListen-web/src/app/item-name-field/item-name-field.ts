import { Component, input, model, output, signal, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { ShoppingService, CategorySuggestion } from '../shopping.service';
import { AuthService } from '../auth';
import { AddKnownItemDialog, AddKnownItemDialogData, AddKnownItemResult } from '../add-known-item-dialog/add-known-item-dialog';

@Component({
  selector: 'app-item-name-field',
  imports: [FormsModule, MatFormFieldModule, MatInputModule, MatIconModule, MatTooltipModule, MatButtonModule],
  templateUrl: './item-name-field.html',
  styleUrl: './item-name-field.css',
})
export class ItemNameField {
  private shoppingService = inject(ShoppingService);
  private auth = inject(AuthService);
  private dialog = inject(MatDialog);

  name = model.required<string>();
  label = input('Vare');

  categorySuggested = output<string>();
  enterPressed = output<void>();

  suggestion = signal<CategorySuggestion | null>(null);
  canCurate = this.auth.canCurate;

  private static readonly DEBOUNCE_MS = 350;
  private lookupTimer?: ReturnType<typeof setTimeout>;

  onNameChange(value: string) {
    this.name.set(value);
    clearTimeout(this.lookupTimer);
    const trimmed = value.trim();
    if (!trimmed) {
      this.suggestion.set(null);
      return;
    }
    this.lookupTimer = setTimeout(() => {
      this.shoppingService.suggestCategory(trimmed).subscribe(result => {
        this.suggestion.set(result);
        if (result.category) {
          this.categorySuggested.emit(result.category);
        }
      });
    }, ItemNameField.DEBOUNCE_MS);
  }

  openAddKnownItemDialog() {
    const trimmed = this.name().trim();
    if (!trimmed) return;
    const dialogRef = this.dialog.open<AddKnownItemDialog, AddKnownItemDialogData, AddKnownItemResult>(AddKnownItemDialog, {
      data: { name: trimmed },
      width: '420px',
      maxWidth: '90vw',
    });
    dialogRef.afterClosed().subscribe(result => {
      if (!result) return;
      this.suggestion.set({ category: result.category, source: 'Known' });
      this.categorySuggested.emit(result.category);
    });
  }

  reset() {
    this.suggestion.set(null);
    clearTimeout(this.lookupTimer);
  }
}
