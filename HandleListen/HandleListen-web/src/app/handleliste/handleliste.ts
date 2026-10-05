import { Component, signal, computed, inject, OnInit, ChangeDetectorRef, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { ShoppingService } from '../shopping.service';
import { ShoppingListService } from '../shopping-list.service';
import { RealtimeService } from '../realtime.service';
import { ShoppingItem } from '../shopping-item';
import { ShoppingList } from '../shopping-list';
import { ListSettingsDialog, ListSettingsResult } from '../list-settings-dialog/list-settings-dialog';
import { ItemNameField } from '../item-name-field/item-name-field';

@Component({
  selector: 'app-handleliste',
  imports: [FormsModule, MatButtonModule, MatIconModule, MatCardModule, MatFormFieldModule, MatInputModule, MatListModule, ItemNameField],
  templateUrl: './handleliste.html',
  styleUrl: './handleliste.css',
})
export class Handleliste implements OnInit {
  private shoppingService = inject(ShoppingService);
  private shoppingListService = inject(ShoppingListService);
  private realtime = inject(RealtimeService);
  private dialog = inject(MatDialog);
  private cdr = inject(ChangeDetectorRef);
  nameField = viewChild(ItemNameField);

  lists = signal<ShoppingList[]>([]);
  selectedListId = signal<number | null>(null);
  items = signal<ShoppingItem[]>([]);

  isAddingList = signal(false);
  newListName = '';

  groupedItems = computed(() => {
    const groups = new Map<string, ShoppingItem[]>();
    for (const item of this.items()) {
      const key = item.category || 'Diverse';
      const list = groups.get(key) || [];
      list.push(item);
      groups.set(key, list);
    }
    return Array.from(groups, ([category, items]) => ({ category, items }));
  });

  newName = '';
  newCategory = '';
  newQuantity = 1;

  ngOnInit() {
    this.refreshLists();

    this.realtime.onItemsChanged(listId => {
      if (listId === this.selectedListId()) {
        this.shoppingService.getByList(listId).subscribe(items => this.items.set(items));
      }
    });

    this.realtime.onListsChanged(() => this.refreshLists());

    this.realtime.onReconnected(() => {
      const id = this.selectedListId();
      if (id !== null) this.realtime.joinList(id);
    });
  }

  private refreshLists() {
    this.shoppingListService.getAll().subscribe(lists => {
      this.lists.set(lists);
      const currentId = this.selectedListId();
      const stillExists = currentId !== null && lists.some(l => l.id === currentId);
      if (!stillExists) {
        if (lists.length > 0) {
          this.selectList(lists[0].id);
        } else {
          this.selectedListId.set(null);
          this.items.set([]);
        }
      }
    });
  }

  selectList(id: number) {
    const previousId = this.selectedListId();
    if (previousId !== null && previousId !== id) {
      this.realtime.leaveList(previousId);
    }
    this.selectedListId.set(id);
    this.realtime.joinList(id);
    this.shoppingService.getByList(id).subscribe(items => {
      this.items.set(items);
    });
  }

  startAddList() {
    this.isAddingList.set(true);
  }

  cancelAddList() {
    this.newListName = '';
    this.isAddingList.set(false);
  }

  confirmAddList() {
    const name = this.newListName.trim();
    if (!name) return;
    this.shoppingListService.create(name).subscribe({
      next: created => {
        this.lists.update(lists => [...lists, created]);
        this.newListName = '';
        this.isAddingList.set(false);
        this.selectList(created.id);
      },
      error: err => console.error('Failed to create list', err)
    });
  }

  openListSettings(list: ShoppingList) {
    const dialogRef = this.dialog.open(ListSettingsDialog, {
      data: list,
      width: '420px',
      maxWidth: '90vw',
    });

    dialogRef.afterClosed().subscribe((result?: ListSettingsResult) => {
      if (!result) return;

      if (result.deleted) {
        this.lists.update(lists => lists.filter(l => l.id !== list.id));
        if (this.selectedListId() === list.id) {
          const remaining = this.lists();
          if (remaining.length > 0) {
            this.selectList(remaining[0].id);
          } else {
            this.selectedListId.set(null);
            this.items.set([]);
          }
        }
      } else if (result.renamedTo) {
        this.lists.update(lists => lists.map(l => l.id === list.id ? { ...l, name: result.renamedTo! } : l));
      }
    });
  }

  onCategorySuggested(category: string) {
    this.newCategory = category;
    this.cdr.markForCheck();
  }

  add() {
    const listId = this.selectedListId();
    if (!listId || !this.newName.trim()) return;
    this.newCategory = this.newCategory.toLowerCase().trim() || 'Diverse';
    this.newCategory = this.newCategory.charAt(0).toUpperCase() + this.newCategory.slice(1);
    this.shoppingService.create(this.newName, this.newCategory, this.newQuantity, listId).subscribe(created => {
      this.items.update(list => [...list, created]);
      this.newName = '';
      this.newCategory = '';
      this.newQuantity = 1;
      this.nameField()?.reset();
    });
  }

  quantity(item: ShoppingItem, updatedQuantity: number) {
    if (updatedQuantity < 1) return;
    const updated = { ...item, quantity: updatedQuantity };
    this.shoppingService.update(updated).subscribe(() => {
      this.items.update(list => list.map(i => i.id === item.id ? updated : i));
    });
  }

  remove(id: number) {
    this.shoppingService.delete(id).subscribe({
      next: () => {
        this.items.update(list => list.filter(i => i.id !== id));
      },
      error: err => console.error('Failed to delete item', err)
    });
  }
}
