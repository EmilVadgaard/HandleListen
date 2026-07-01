import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ShoppingListService } from '../shopping-list.service';
import { ShoppingList } from '../shopping-list';

@Component({
  selector: 'app-list',
  imports: [ FormsModule ],
  templateUrl: './list.html',
  styleUrl: './list.css',
})
export class List implements OnInit {
  private shoppingListService = inject(ShoppingListService);

  lists = signal<ShoppingList[]>([]);

  newName = '';

  ngOnInit(): void {
    this.shoppingListService.getAll().subscribe((lists) => {
      this.lists.set(lists);
    });
  }

  add() {
    if (!this.newName.trim()) return;
    this.shoppingListService.create(this.newName).subscribe((created) => {
      this.lists.update((list) => [...list, created]);
      this.newName = '';
    });
  }

  remove(id: number) {
    this.shoppingListService.delete(id).subscribe({
      next: () => {
        this.lists.update((list) => list.filter((l) => l.id !== id));
      },
    });
  }
}
