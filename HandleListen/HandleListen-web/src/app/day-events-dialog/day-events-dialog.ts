import { Component, inject, signal, ChangeDetectorRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { CalendarService } from '../calendar.service';
import { CalendarEventOccurrence, ReminderUnit, RecurrenceFrequency } from '../calendar-event';

export interface DayEventsDialogData {
  date: string;
  events: CalendarEventOccurrence[];
}

type ReminderMode = 'none' | 'hours' | 'days';

const TIME_PATTERN = /^([01]\d|2[0-3]):([0-5]\d)$/;

@Component({
  selector: 'app-day-events-dialog',
  imports: [FormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatCheckboxModule, MatButtonModule, MatIconModule, MatListModule, MatDividerModule],
  templateUrl: './day-events-dialog.html',
  styleUrl: './day-events-dialog.css',
})
export class DayEventsDialog {
  private dialogRef = inject(MatDialogRef<DayEventsDialog, boolean>);
  private calendarService = inject(CalendarService);
  private cdr = inject(ChangeDetectorRef);
  data = inject<DayEventsDialogData>(MAT_DIALOG_DATA);

  events = signal<CalendarEventOccurrence[]>(this.data.events);
  changed = false;

  editingId: number | null = null;
  // The series' true first-occurrence date, used (instead of whichever occurrence the user
  // clicked into) when saving an edit to a recurring event - using the clicked occurrence's own
  // date instead would silently move the series' anchor, corrupting a clamped day (e.g. editing
  // the Feb 28 instance of a "day 31, every month" series would permanently turn it into a
  // "day 28" series).
  private anchorDate = '';
  title = '';
  description = '';
  allDay = true;
  time = '';
  reminderMode: ReminderMode = 'none';
  reminderValue = 1;
  recurrence: RecurrenceFrequency | '' = '';
  pushReminderEnabled = true;
  emailReminderEnabled = false;
  formError = signal<string | null>(null);

  get dateLabel(): string {
    const d = new Date(this.data.date + 'T00:00:00');
    const label = d.toLocaleDateString('da-DK', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
    return label.charAt(0).toUpperCase() + label.slice(1);
  }

  get isEditingRecurring(): boolean {
    return this.editingId !== null && this.recurrence !== '';
  }

  startAdd() {
    this.editingId = null;
    this.anchorDate = this.data.date;
    this.title = '';
    this.description = '';
    this.allDay = true;
    this.time = '';
    this.reminderMode = 'none';
    this.reminderValue = 1;
    this.recurrence = '';
    this.pushReminderEnabled = true;
    this.emailReminderEnabled = false;
    this.formError.set(null);
  }

  startEdit(event: CalendarEventOccurrence) {
    this.editingId = event.id;
    this.anchorDate = this.data.date;
    this.title = event.title;
    this.description = event.description;
    this.allDay = event.time === null;
    this.time = event.time ? event.time.slice(0, 5) : '';
    this.reminderMode = event.reminderUnit === 'Hours' ? 'hours' : event.reminderUnit === 'Days' ? 'days' : 'none';
    this.reminderValue = event.reminderValue ?? 1;
    this.recurrence = event.recurrence ?? '';
    this.pushReminderEnabled = event.pushReminderEnabled;
    this.emailReminderEnabled = event.emailReminderEnabled;
    this.formError.set(null);

    if (event.recurrence) {
      // The clicked occurrence's date may be a later (or clamped) one - fetch the series' real
      // anchor date so save() never overwrites it with the wrong value.
      this.calendarService.getById(event.id).subscribe(full => {
        this.anchorDate = full.date;
      });
    }
  }

  save() {
    if (!this.title.trim()) return;

    this.formError.set(null);

    const trimmedTime = this.time.trim();
    if (!this.allDay && trimmedTime && !TIME_PATTERN.test(trimmedTime)) {
      this.formError.set('Tidspunkt skal skrives som tt:mm, f.eks. 14:30.');
      return;
    }

    const reminderUnit: ReminderUnit | null = this.reminderMode === 'none' ? null : this.reminderMode === 'hours' ? 'Hours' : 'Days';
    const payload = {
      title: this.title.trim(),
      description: this.description.trim(),
      date: this.anchorDate,
      time: this.allDay || !trimmedTime ? null : `${trimmedTime}:00`,
      reminderValue: this.reminderMode === 'none' ? null : this.reminderValue,
      reminderUnit,
      recurrence: this.recurrence === '' ? null : this.recurrence,
      pushReminderEnabled: this.reminderMode === 'none' ? true : this.pushReminderEnabled,
      emailReminderEnabled: this.reminderMode === 'none' ? false : this.emailReminderEnabled,
    };

    if (this.editingId !== null) {
      const id = this.editingId;
      this.calendarService.update(id, payload).subscribe({
        next: () => {
          this.events.update(list => list.map(e => e.id === id ? { ...e, ...payload } : e));
          this.changed = true;
          this.startAdd();
          this.cdr.markForCheck();
        },
        error: err => {
          console.error('Failed to update event', err);
          this.formError.set('Kunne ikke gemme ændringerne.');
          this.cdr.markForCheck();
        }
      });
    } else {
      this.calendarService.create(payload).subscribe({
        next: created => {
          this.events.update(list => [...list, created]);
          this.changed = true;
          this.startAdd();
          this.cdr.markForCheck();
        },
        error: err => {
          console.error('Failed to create event', err);
          this.formError.set('Kunne ikke tilføje eventet.');
          this.cdr.markForCheck();
        }
      });
    }
  }

  removeOccurrence(event: CalendarEventOccurrence) {
    if (!confirm(`Slet kun denne forekomst af "${event.title}"?`)) return;
    this.calendarService.deleteOccurrence(event.id, event.date).subscribe({
      next: () => {
        this.events.update(list => list.filter(e => e.id !== event.id));
        this.changed = true;
        if (this.editingId === event.id) this.startAdd();
        this.cdr.markForCheck();
      },
      error: err => console.error('Failed to delete occurrence', err)
    });
  }

  removeSeries(event: CalendarEventOccurrence) {
    const message = event.recurrence
      ? `Slet HELE den gentagende serie "${event.title}"? Dette fjerner alle forekomster, ikke kun denne.`
      : `Slet "${event.title}"?`;
    if (!confirm(message)) return;
    this.calendarService.delete(event.id).subscribe({
      next: () => {
        this.events.update(list => list.filter(e => e.id !== event.id));
        this.changed = true;
        if (this.editingId === event.id) this.startAdd();
        this.cdr.markForCheck();
      },
      error: err => console.error('Failed to delete event', err)
    });
  }

  close() {
    this.dialogRef.close(this.changed);
  }
}
