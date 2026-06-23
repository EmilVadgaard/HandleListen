import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ShoppingService } from '../shopping.service';
import { ShoppingItem } from '../shopping-item';

@Component({
  selector: 'app-handleliste',
  imports: [ FormsModule ],
  templateUrl: './handleliste.html',
  styleUrl: './handleliste.css',
})
export class Handleliste implements OnInit {
  private shoppingService = inject(ShoppingService);

  items = signal<ShoppingItem[]>([]);

  groupedItems = computed(() => {
    const groups = new Map<string, ShoppingItem[]>();
    for (const item of this.items()) {
      const key = item.category || 'Miscellaneous';
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
    this.shoppingService.getAll().subscribe(items => {
      this.items.set(items);
    });
  }

  add() {
    if (!this.newName.trim()) return;
    this.newCategory = this.newCategory.toLowerCase().trim() || 'Miscellaneous';
    this.newCategory = this.newCategory.charAt(0).toUpperCase() + this.newCategory.slice(1);
    this.shoppingService.create(this.newName, this.newCategory, this.newQuantity).subscribe(created => {
      this.items.update(list => [...list, created]);
      this.newName = '';
      this.newCategory = '';
      this.newQuantity = 1;
    });
  }

  quantity(item: ShoppingItem, updatedQuantity: number) {
    if (updatedQuantity < 1) return;
    const updated = { ...item, quantity: updatedQuantity};
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
