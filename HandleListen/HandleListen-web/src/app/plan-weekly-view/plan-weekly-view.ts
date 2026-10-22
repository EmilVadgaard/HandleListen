import { Component, input, output, inject, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { PlanService } from '../plan.service';
import { Plan, PlanTodo } from '../plan';

const TIME_PATTERN = /^([01]\d|2[0-3]):([0-5]\d)$/;

@Component({
  selector: 'app-plan-weekly-view',
  imports: [FormsModule, MatCardModule, MatListModule, MatIconModule, MatButtonModule, MatCheckboxModule, MatFormFieldModule, MatInputModule],
  templateUrl: './plan-weekly-view.html',
  styleUrl: './plan-weekly-view.css',
})
export class PlanWeeklyView {
  private planService = inject(PlanService);

  plan = input.required<Plan>();
  todos = input.required<PlanTodo[]>();
  changed = output<void>();

  days = computed(() => {
    const plan = this.plan();
    const count = plan.dayCount ?? 7;
    return Array.from({ length: count }, (_, dayIndex) => ({
      dayIndex,
      label: this.dayLabel(plan, dayIndex),
    }));
  });

  todosByDay = computed(() => {
    const map = new Map<number, PlanTodo[]>();
    for (const todo of this.todos()) {
      if (todo.dayIndex === null) continue;
      const list = map.get(todo.dayIndex) ?? [];
      list.push(todo);
      map.set(todo.dayIndex, list);
    }
    for (const list of map.values()) list.sort((a, b) => (a.time ?? '').localeCompare(b.time ?? ''));
    return map;
  });

  addingForDay: number | null = null;
  editingTodoId: number | null = null;
  title = '';
  time = '';
  formError: string | null = null;

  todosForDay(dayIndex: number): PlanTodo[] {
    return this.todosByDay().get(dayIndex) ?? [];
  }

  startAdd(dayIndex: number) {
    this.addingForDay = dayIndex;
    this.editingTodoId = null;
    this.title = '';
    this.time = '';
    this.formError = null;
  }

  startEdit(todo: PlanTodo) {
    this.editingTodoId = todo.id;
    this.addingForDay = todo.dayIndex;
    this.title = todo.title;
    this.time = todo.time ? todo.time.slice(0, 5) : '';
    this.formError = null;
  }

  cancel() {
    this.addingForDay = null;
    this.editingTodoId = null;
  }

  save() {
    const title = this.title.trim();
    if (!title) return;

    const trimmedTime = this.time.trim();
    if (trimmedTime && !TIME_PATTERN.test(trimmedTime)) {
      this.formError = 'Tidspunkt skal skrives som tt:mm, f.eks. 14:30.';
      return;
    }
    this.formError = null;
    const time = trimmedTime ? `${trimmedTime}:00` : null;

    if (this.editingTodoId !== null) {
      this.planService.updateTodo(this.plan().id, this.editingTodoId, { title, done: false, time }).subscribe(() => {
        this.changed.emit();
        this.cancel();
      });
    } else if (this.addingForDay !== null) {
      this.planService.addTodo(this.plan().id, { title, dayIndex: this.addingForDay, time }).subscribe(() => {
        this.changed.emit();
        this.startAdd(this.addingForDay!);
      });
    }
  }

  toggleDone(todo: PlanTodo) {
    this.planService.updateTodo(this.plan().id, todo.id, { title: todo.title, done: !todo.done, dayIndex: todo.dayIndex, time: todo.time }).subscribe(() => this.changed.emit());
  }

  deleteTodo(todo: PlanTodo) {
    this.planService.deleteTodo(this.plan().id, todo.id).subscribe(() => {
      this.changed.emit();
      if (this.editingTodoId === todo.id) this.cancel();
    });
  }

  private dayLabel(plan: Plan, dayIndex: number): string {
    if (!plan.isActive || !plan.activatedDate) return `Dag ${dayIndex + 1}`;
    const date = new Date(plan.activatedDate + 'T00:00:00');
    date.setDate(date.getDate() + dayIndex);
    const label = date.toLocaleDateString('da-DK', { weekday: 'short', day: 'numeric', month: 'short' });
    return label.charAt(0).toUpperCase() + label.slice(1);
  }
}
