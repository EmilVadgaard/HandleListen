import { Component, input, output, inject, signal, effect } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { DragDropModule, CdkDragDrop, moveItemInArray } from '@angular/cdk/drag-drop';
import { PlanService } from '../plan.service';
import { Plan, PlanTodo } from '../plan';

@Component({
  selector: 'app-plan-standard-view',
  imports: [FormsModule, MatListModule, MatIconModule, MatButtonModule, MatCheckboxModule, MatFormFieldModule, MatInputModule, DragDropModule],
  templateUrl: './plan-standard-view.html',
  styleUrl: './plan-standard-view.css',
})
export class PlanStandardView {
  private planService = inject(PlanService);

  plan = input.required<Plan>();
  todos = input.required<PlanTodo[]>();
  changed = output<void>();

  orderedTodos = signal<PlanTodo[]>([]);
  newTitle = '';

  constructor() {
    effect(() => {
      this.orderedTodos.set([...this.todos()].sort((a, b) => a.sortOrder - b.sortOrder));
    });
  }

  toggleDone(todo: PlanTodo) {
    this.planService.updateTodo(this.plan().id, todo.id, { title: todo.title, done: !todo.done }).subscribe(() => this.changed.emit());
  }

  deleteTodo(todo: PlanTodo) {
    this.planService.deleteTodo(this.plan().id, todo.id).subscribe(() => this.changed.emit());
  }

  addTodo() {
    const title = this.newTitle.trim();
    if (!title) return;
    this.planService.addTodo(this.plan().id, { title }).subscribe(() => {
      this.newTitle = '';
      this.changed.emit();
    });
  }

  drop(event: CdkDragDrop<PlanTodo[]>) {
    const reordered = [...this.orderedTodos()];
    moveItemInArray(reordered, event.previousIndex, event.currentIndex);
    this.orderedTodos.set(reordered);
    this.planService.reorderTodos(this.plan().id, reordered.map(t => t.id)).subscribe(() => this.changed.emit());
  }
}
