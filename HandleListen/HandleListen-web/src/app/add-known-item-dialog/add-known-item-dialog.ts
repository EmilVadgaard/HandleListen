import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatRadioModule } from '@angular/material/radio';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { KnownItemService, KnownItem } from '../known-item.service';
import { UnitOfMeasure, ALL_UNITS, UNIT_LABELS } from '../unit-of-measure';
import { CATEGORIES } from '../category';

export interface AddKnownItemDialogData {
  name: string;
}

export interface AddKnownItemResult {
  category: string;
  defaultUnit: UnitOfMeasure | null;
}

@Component({
  selector: 'app-add-known-item-dialog',
  imports: [FormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule, MatRadioModule, MatAutocompleteModule],
  templateUrl: './add-known-item-dialog.html',
  styleUrl: './add-known-item-dialog.css',
})
export class AddKnownItemDialog {
  private dialogRef = inject(MatDialogRef<AddKnownItemDialog, AddKnownItemResult | undefined>);
  private knownItemService = inject(KnownItemService);
  data = inject<AddKnownItemDialogData>(MAT_DIALOG_DATA);

  categories = CATEGORIES;
  units = ALL_UNITS;
  unitLabels = UNIT_LABELS;
  mode = signal<'new' | 'alias'>('new');
  category = '';
  defaultUnit: UnitOfMeasure | null = null;
  error = signal<string | null>(null);
  saving = signal(false);

  existingValue = signal<KnownItem | string>('');
  existingResults = signal<KnownItem[]>([]);
  private static readonly SEARCH_DEBOUNCE_MS = 250;
  private searchTimer?: ReturnType<typeof setTimeout>;

  onExistingInputChange(value: KnownItem | string) {
    this.existingValue.set(value);
    clearTimeout(this.searchTimer);

    if (typeof value !== 'string') return;

    const trimmed = value.trim();
    if (!trimmed) {
      this.existingResults.set([]);
      return;
    }
    this.searchTimer = setTimeout(() => {
      this.knownItemService.search(trimmed).subscribe(items => this.existingResults.set(items));
    }, AddKnownItemDialog.SEARCH_DEBOUNCE_MS);
  }

  displayExisting(item: KnownItem | string | null): string {
    if (!item) return '';
    return typeof item === 'string' ? item : `${item.canonicalName} (${item.category})`;
  }

  save() {
    this.error.set(null);
    if (this.mode() === 'new') {
      if (!this.category) {
        this.error.set('Vælg en kategori.');
        return;
      }
      this.saving.set(true);
      this.knownItemService.create(this.data.name, this.category, this.defaultUnit).subscribe({
        next: item => {
          this.saving.set(false);
          this.dialogRef.close({ category: item.category, defaultUnit: item.defaultUnit });
        },
        error: () => {
          this.saving.set(false);
          this.error.set('Kunne ikke oprette det kendte item. Findes det allerede?');
        }
      });
    } else {
      const target = this.existingValue();
      if (typeof target === 'string') {
        this.error.set('Vælg et eksisterende item fra listen.');
        return;
      }
      this.saving.set(true);
      this.knownItemService.addAlias(target.id, this.data.name).subscribe({
        next: () => {
          this.saving.set(false);
          this.dialogRef.close({ category: target.category, defaultUnit: target.defaultUnit });
        },
        error: () => {
          this.saving.set(false);
          this.error.set('Kunne ikke tilføje alias. Findes navnet allerede?');
        }
      });
    }
  }

  close() {
    this.dialogRef.close();
  }
}
