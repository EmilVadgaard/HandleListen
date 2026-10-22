import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { forkJoin } from 'rxjs';
import { CalendarService } from '../calendar.service';
import { PlanService } from '../plan.service';
import { NoteService } from '../note.service';
import { MealPlanService } from '../meal-plan.service';
import { RealtimeService } from '../realtime.service';
import { MealPlan } from '../meal-plan';
import { PersonalNote } from '../note';
import { StandardPlanTodo } from '../plan';
import { FeedCard, compareFeedCards } from '../feed-card';
import { MealPlanCardDialog, MealPlanCardDialogResult } from '../meal-plan-card-dialog/meal-plan-card-dialog';

const LOOKAHEAD_DAYS = 30;

@Component({
  selector: 'app-min-side',
  imports: [FormsModule, MatCardModule, MatIconModule, MatButtonModule, MatCheckboxModule, MatFormFieldModule, MatInputModule],
  templateUrl: './min-side.html',
  styleUrl: './min-side.css',
})
export class MinSide implements OnInit {
  private calendarService = inject(CalendarService);
  private planService = inject(PlanService);
  private noteService = inject(NoteService);
  private mealPlanService = inject(MealPlanService);
  private realtime = inject(RealtimeService);
  private dialog = inject(MatDialog);

  currentMealPlan = signal<MealPlan | undefined>(undefined);
  feedCards = signal<FeedCard[]>([]);
  sidebarNotes = signal<PersonalNote[]>([]);
  sidebarTodos = signal<StandardPlanTodo[]>([]);

  newNoteText = '';
  newNoteDate = '';

  ngOnInit() {
    this.refresh();
    this.realtime.onMealPlansChanged(() => this.refreshMealPlan());
    this.realtime.onCalendarChanged(() => this.refresh());
    this.realtime.onPlansChanged(() => this.refresh());
  }

  private toISODate(d: Date): string {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }

  private refresh() {
    const today = this.toISODate(new Date());
    const end = this.toISODate(new Date(Date.now() + LOOKAHEAD_DAYS * 24 * 60 * 60 * 1000));

    forkJoin({
      events: this.calendarService.getByRange(today, end),
      todos: this.planService.getUpcomingTodos(today, LOOKAHEAD_DAYS),
      notes: this.noteService.getAll(),
      mealPlans: this.mealPlanService.getAll(),
      standardTodos: this.planService.getStandardTodos(),
    }).subscribe(({ events, todos, notes, mealPlans, standardTodos }) => {
      const datedNotes = notes.filter(n => n.date !== null);
      const undatedNotes = notes.filter(n => n.date === null);

      const cards: FeedCard[] = [
        ...events.map(e => ({ kind: 'event' as const, date: e.date, time: e.time, id: e.id, title: e.title, description: e.description })),
        ...datedNotes.map(n => ({ kind: 'note' as const, date: n.date!, id: n.id, text: n.text })),
        ...todos.map(t => ({ kind: 'todo' as const, date: t.date, time: t.time, id: t.todoId, title: t.title, done: t.done, planId: t.planId, planName: t.planName })),
      ].sort(compareFeedCards);

      this.feedCards.set(cards);
      this.sidebarNotes.set(undatedNotes);
      this.sidebarTodos.set(standardTodos);
      this.currentMealPlan.set(mealPlans.find(p => p.isCurrent));
    });
  }

  private refreshMealPlan() {
    this.mealPlanService.getAll().subscribe(plans => this.currentMealPlan.set(plans.find(p => p.isCurrent)));
  }

  addNote() {
    const text = this.newNoteText.trim();
    if (!text) return;
    const date = this.newNoteDate || null;
    this.noteService.create(text, date).subscribe(created => {
      if (created.date) {
        this.feedCards.update(cards => [...cards, { kind: 'note' as const, date: created.date!, id: created.id, text: created.text }].sort(compareFeedCards));
      } else {
        this.sidebarNotes.update(notes => [...notes, created]);
      }
      this.newNoteText = '';
      this.newNoteDate = '';
    });
  }

  deleteNote(note: PersonalNote) {
    this.noteService.delete(note.id).subscribe(() => {
      this.sidebarNotes.update(notes => notes.filter(n => n.id !== note.id));
      this.feedCards.update(cards => cards.filter(c => !(c.kind === 'note' && c.id === note.id)));
    });
  }

  openMealPlan(plan: MealPlan) {
    const ref = this.dialog.open(MealPlanCardDialog, { data: plan, width: '480px', maxWidth: '90vw' });
    ref.afterClosed().subscribe((result?: MealPlanCardDialogResult) => {
      if (result?.deleted || result?.setCurrent) this.refreshMealPlan();
    });
  }
}
