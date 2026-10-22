import { Component, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatTabsModule } from '@angular/material/tabs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDialog } from '@angular/material/dialog';
import { AdminService } from '../admin.service';
import { KnownItemService, KnownItemAdmin } from '../known-item.service';
import { KnownItemRequest } from '../known-item-request';
import { AddKnownItemDialog, AddKnownItemDialogData } from '../add-known-item-dialog/add-known-item-dialog';
import { EditKnownItemDialog, EditKnownItemDialogData, EditKnownItemDialogResult } from '../edit-known-item-dialog/edit-known-item-dialog';

@Component({
  selector: 'app-admin',
  imports: [FormsModule, MatTabsModule, MatButtonModule, MatIconModule, MatListModule, MatFormFieldModule, MatInputModule],
  templateUrl: './admin.html',
  styleUrl: './admin.css',
})
export class Admin {
  private adminService = inject(AdminService);
  private knownItemService = inject(KnownItemService);
  private dialog = inject(MatDialog);

  requests = signal<KnownItemRequest[]>([]);
  knownItems = signal<KnownItemAdmin[]>([]);
  itemSearch = '';

  filteredKnownItems = computed(() => {
    const query = this.itemSearch.trim().toLowerCase();
    if (!query) return this.knownItems();
    return this.knownItems().filter(i =>
      i.canonicalName.toLowerCase().includes(query) ||
      i.category.toLowerCase().includes(query) ||
      i.aliases.some(a => a.alias.includes(query)));
  });

  constructor() {
    this.refreshRequests();
    this.refreshKnownItems();
  }

  private refreshRequests() {
    this.adminService.getKnownItemRequests().subscribe(requests => this.requests.set(requests));
  }

  private refreshKnownItems() {
    this.knownItemService.getAllForAdmin().subscribe(items => this.knownItems.set(items));
  }

  resolveRequest(request: KnownItemRequest, openDialog: boolean) {
    if (!openDialog) {
      this.adminService.resolveKnownItemRequest(request.id).subscribe(() => this.refreshRequests());
      return;
    }

    const ref = this.dialog.open<AddKnownItemDialog, AddKnownItemDialogData>(AddKnownItemDialog, {
      data: { name: request.name },
      width: '420px',
      maxWidth: '90vw',
    });
    ref.afterClosed().subscribe(result => {
      if (!result) return;
      this.adminService.resolveKnownItemRequest(request.id).subscribe(() => {
        this.refreshRequests();
        this.refreshKnownItems();
      });
    });
  }

  addKnownItem() {
    const name = prompt('Navn på den nye kendte vare:')?.trim();
    if (!name) return;

    const ref = this.dialog.open<AddKnownItemDialog, AddKnownItemDialogData>(AddKnownItemDialog, {
      data: { name },
      width: '420px',
      maxWidth: '90vw',
    });
    ref.afterClosed().subscribe(result => {
      if (result) this.refreshKnownItems();
    });
  }

  editKnownItem(item: KnownItemAdmin) {
    const ref = this.dialog.open<EditKnownItemDialog, EditKnownItemDialogData, EditKnownItemDialogResult | undefined>(EditKnownItemDialog, {
      data: { item },
      width: '480px',
      maxWidth: '90vw',
    });
    ref.afterClosed().subscribe(result => {
      if (result?.deleted || result?.updated) this.refreshKnownItems();
    });
  }
}
