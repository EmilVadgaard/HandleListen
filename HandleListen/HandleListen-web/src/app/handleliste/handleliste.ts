import { Component, signal, inject, OnInit } from '@angular/core';
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
  newName = '';
  newQuantity = 1;

  ngOnInit() {
    this.shoppingService.getAll().subscribe(items => {
      this.items.set(items);
    });
  }

  add() {
    if (!this.newName.trim()) return;
    this.shoppingService.create(this.newName, this.newQuantity).subscribe(created => {
      this.items.update(list => [...list, created]);
      this.newName = '';
      this.newQuantity = 1;
    });
  }

  toggle(item: ShoppingItem) {
    const updated = { ...item, isBought: !item.isBought };
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
