import { Component, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { KnownItemService, KnownItemAdmin } from '../known-item.service';
import { UnitOfMeasure, ALL_UNITS, UNIT_LABELS } from '../unit-of-measure';
import { CATEGORIES } from '../category';

export interface EditKnownItemDialogData {
  item: KnownItemAdmin;
}

export interface EditKnownItemDialogResult {
  deleted?: boolean;
  updated?: KnownItemAdmin;
}

@Component({
  selector: 'app-edit-known-item-dialog',
  imports: [FormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule, MatIconModule, MatChipsModule, MatAutocompleteModule],
  templateUrl: './edit-known-item-dialog.html',
  styleUrl: './edit-known-item-dialog.css',
})
export class EditKnownItemDialog {
  private dialogRef = inject(MatDialogRef<EditKnownItemDialog, EditKnownItemDialogResult | undefined>);
  private knownItemService = inject(KnownItemService);
  data = inject<EditKnownItemDialogData>(MAT_DIALOG_DATA);

  item = signal(this.data.item);
  canonicalName = this.data.item.canonicalName;
  category = signal(this.data.item.category);
  defaultUnit: UnitOfMeasure | null = this.data.item.defaultUnit;
  units = ALL_UNITS;
  unitLabels = UNIT_LABELS;
  newAlias = '';
  error = signal<string | null>(null);
  saving = signal(false);

  private changed = false;

  filteredCategories = computed(() => {
    const query = this.category().trim().toLowerCase();
    return CATEGORIES.filter(c => !query || c.toLowerCase().includes(query));
  });

  save() {
    const name = this.canonicalName.trim();
    const category = this.category().trim();
    if (!name || !category) {
      this.error.set('Navn og kategori er obligatoriske.');
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    this.knownItemService.update(this.item().id, name, category, this.defaultUnit).subscribe({
      next: () => {
        this.saving.set(false);
        this.item.update(i => ({ ...i, canonicalName: name, category, defaultUnit: this.defaultUnit }));
        this.changed = true;
      },
      error: () => {
        this.saving.set(false);
        this.error.set('Kunne ikke gemme ændringerne.');
      }
    });
  }

  addAlias() {
    const alias = this.newAlias.trim();
    if (!alias) return;

    this.error.set(null);
    this.knownItemService.addAlias(this.item().id, alias).subscribe({
      next: () => {
        // The add-alias endpoint returns no body (just the new alias's id would be needed to
        // let it be removed again right away) - refetch this item so its real id is in hand.
        this.knownItemService.getAllForAdmin().subscribe(all => {
          const refreshed = all.find(i => i.id === this.item().id);
          if (refreshed) this.item.set(refreshed);
        });
        this.newAlias = '';
        this.changed = true;
      },
      error: () => this.error.set('Kunne ikke tilføje navnet - findes det allerede?')
    });
  }

  removeAlias(aliasId: number) {
    this.knownItemService.removeAlias(this.item().id, aliasId).subscribe({
      next: () => {
        this.item.update(i => ({ ...i, aliases: i.aliases.filter(a => a.id !== aliasId) }));
        this.changed = true;
      },
      error: () => this.error.set('Kunne ikke fjerne navnet - et kendt item skal have mindst ét.')
    });
  }

  deleteItem() {
    if (!confirm(`Er du sikker på at du vil slette "${this.item().canonicalName}"? Dette kan ikke fortrydes.`)) return;
    this.knownItemService.delete(this.item().id).subscribe(() => {
      this.dialogRef.close({ deleted: true });
    });
  }

  close() {
    this.dialogRef.close(this.changed ? { updated: this.item() } : undefined);
  }
}
