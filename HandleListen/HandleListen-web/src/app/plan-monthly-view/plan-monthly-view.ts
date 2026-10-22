import { Component, input, output, inject, computed } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { Plan, PlanTodo } from '../plan';
import { PlanDayDialog, PlanDayDialogData } from '../plan-day-dialog/plan-day-dialog';

const WEEKDAY_LABELS = ['Man', 'Tir', 'Ons', 'Tor', 'Fre', 'Lør', 'Søn'];

@Component({
  selector: 'app-plan-monthly-view',
  imports: [MatIconModule],
  templateUrl: './plan-monthly-view.html',
  styleUrl: './plan-monthly-view.css',
})
export class PlanMonthlyView {
  private dialog = inject(MatDialog);

  plan = input.required<Plan>();
  todos = input.required<PlanTodo[]>();
  changed = output<void>();

  readonly weekdayLabels = WEEKDAY_LABELS;

  gridDays = computed(() => {
    const plan = this.plan();
    const year = plan.year!;
    const month = plan.month! - 1; // JS Date month is 0-based
    const start = this.startOfGrid(year, month);
    const end = this.endOfGrid(year, month);
    const days: Date[] = [];
    for (let day = new Date(start); day <= end; day.setDate(day.getDate() + 1)) {
      days.push(new Date(day));
    }
    return days;
  });

  private todosByDay = computed(() => {
    const map = new Map<number, PlanTodo[]>();
    for (const todo of this.todos()) {
      if (todo.dayOfMonth === null) continue;
      const list = map.get(todo.dayOfMonth) ?? [];
      list.push(todo);
      map.set(todo.dayOfMonth, list);
    }
    return map;
  });

  todosForDay(day: Date): PlanTodo[] {
    if (!this.isCurrentMonth(day)) return [];
    return this.todosByDay().get(day.getDate()) ?? [];
  }

  isCurrentMonth(day: Date): boolean {
    return day.getMonth() === this.plan().month! - 1;
  }

  openDay(day: Date) {
    if (!this.isCurrentMonth(day)) return;

    const dayOfMonth = day.getDate();
    const label = day.toLocaleDateString('da-DK', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
    const data: PlanDayDialogData = {
      plan: this.plan(),
      dayOfMonth,
      dateLabel: label.charAt(0).toUpperCase() + label.slice(1),
      todos: this.todosForDay(day),
    };
    const ref = this.dialog.open(PlanDayDialog, { data, width: '480px', maxWidth: '95vw' });
    ref.afterClosed().subscribe(changed => {
      if (changed) this.changed.emit();
    });
  }

  private startOfGrid(year: number, month: number): Date {
    const first = new Date(year, month, 1);
    const offset = (first.getDay() + 6) % 7;
    const start = new Date(first);
    start.setDate(first.getDate() - offset);
    return start;
  }

  private endOfGrid(year: number, month: number): Date {
    const last = new Date(year, month + 1, 0);
    const offset = (last.getDay() + 6) % 7;
    const end = new Date(last);
    end.setDate(last.getDate() + (6 - offset));
    return end;
  }
}
