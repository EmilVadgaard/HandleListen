import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { ShoppingListService, Guest } from '../shopping-list.service';
import { ShoppingList } from '../shopping-list';

export interface ListSettingsResult {
  renamedTo?: string;
  deleted?: boolean;
}

@Component({
  selector: 'app-list-settings-dialog',
  imports: [FormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, MatListModule, MatDividerModule],
  templateUrl: './list-settings-dialog.html',
  styleUrl: './list-settings-dialog.css',
})
export class ListSettingsDialog {
  private dialogRef = inject(MatDialogRef<ListSettingsDialog, ListSettingsResult | undefined>);
  private shoppingListService = inject(ShoppingListService);
  list = inject<ShoppingList>(MAT_DIALOG_DATA);

  renameValue = this.list.name;
  shareEmail = '';
  shareError = signal<string | null>(null);
  guests = signal<Guest[]>([]);

  constructor() {
    if (this.list.isOwner) {
      this.shoppingListService.getGuests(this.list.id).subscribe(guests => this.guests.set(guests));
    }
  }

  rename() {
    const name = this.renameValue.trim();
    if (!name || name === this.list.name) return;
    this.shoppingListService.rename(this.list.id, name).subscribe(() => {
      this.dialogRef.close({ renamedTo: name });
    });
  }

  share() {
    const email = this.shareEmail.trim();
    if (!email) return;
    this.shareError.set(null);
    this.shoppingListService.addGuest(this.list.id, email).subscribe({
      next: guest => {
        this.guests.update(guests => [...guests, guest]);
        this.shareEmail = '';
      },
      error: err => this.shareError.set(typeof err.error === 'string' ? err.error : 'Kunne ikke dele listen.')
    });
  }

  removeGuest(guest: Guest) {
    this.shoppingListService.removeGuest(this.list.id, guest.id).subscribe(() => {
      this.guests.update(guests => guests.filter(g => g.id !== guest.id));
    });
  }

  deleteList() {
    if (!confirm(`Er du sikker på at du vil slette "${this.list.name}"? Dette kan ikke fortrydes.`)) return;
    this.shoppingListService.delete(this.list.id).subscribe(() => {
      this.dialogRef.close({ deleted: true });
    });
  }

  close() {
    this.dialogRef.close();
  }
}
