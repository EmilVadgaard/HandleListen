import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { PlanService } from '../plan.service';
import { Plan, PlanTodo } from '../plan';

export interface PlanDayDialogData {
  plan: Plan;
  dayOfMonth: number;
  dateLabel: string;
  todos: PlanTodo[];
}

const TIME_PATTERN = /^([01]\d|2[0-3]):([0-5]\d)$/;

@Component({
  selector: 'app-plan-day-dialog',
  imports: [FormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatCheckboxModule, MatButtonModule, MatIconModule, MatListModule, MatDividerModule],
  templateUrl: './plan-day-dialog.html',
  styleUrl: './plan-day-dialog.css',
})
export class PlanDayDialog {
  private dialogRef = inject(MatDialogRef<PlanDayDialog, boolean>);
  private planService = inject(PlanService);
  data = inject<PlanDayDialogData>(MAT_DIALOG_DATA);

  todos = signal<PlanTodo[]>(this.data.todos);
  changed = false;

  editingId: number | null = null;
  title = '';
  time = '';
  formError = signal<string | null>(null);

  startAdd() {
    this.editingId = null;
    this.title = '';
    this.time = '';
    this.formError.set(null);
  }

  startEdit(todo: PlanTodo) {
    this.editingId = todo.id;
    this.title = todo.title;
    this.time = todo.time ? todo.time.slice(0, 5) : '';
    this.formError.set(null);
  }

  toggleDone(todo: PlanTodo) {
    this.planService.updateTodo(this.data.plan.id, todo.id, { title: todo.title, done: !todo.done, dayOfMonth: this.data.dayOfMonth, time: todo.time }).subscribe(() => {
      this.todos.update(list => list.map(t => t.id === todo.id ? { ...t, done: !t.done } : t));
      this.changed = true;
    });
  }

  save() {
    const title = this.title.trim();
    if (!title) return;

    const trimmedTime = this.time.trim();
    if (trimmedTime && !TIME_PATTERN.test(trimmedTime)) {
      this.formError.set('Tidspunkt skal skrives som tt:mm, f.eks. 14:30.');
      return;
    }
    this.formError.set(null);
    const time = trimmedTime ? `${trimmedTime}:00` : null;

    if (this.editingId !== null) {
      const id = this.editingId;
      this.planService.updateTodo(this.data.plan.id, id, { title, done: false, dayOfMonth: this.data.dayOfMonth, time }).subscribe(() => {
        this.todos.update(list => list.map(t => t.id === id ? { ...t, title, time } : t));
        this.changed = true;
        this.startAdd();
      });
    } else {
      this.planService.addTodo(this.data.plan.id, { title, dayOfMonth: this.data.dayOfMonth, time }).subscribe(created => {
        this.todos.update(list => [...list, created]);
        this.changed = true;
        this.startAdd();
      });
    }
  }

  deleteTodo(todo: PlanTodo) {
    this.planService.deleteTodo(this.data.plan.id, todo.id).subscribe(() => {
      this.todos.update(list => list.filter(t => t.id !== todo.id));
      this.changed = true;
      if (this.editingId === todo.id) this.startAdd();
    });
  }

  close() {
    this.dialogRef.close(this.changed);
  }
}
