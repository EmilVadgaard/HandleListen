import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatBadgeModule } from '@angular/material/badge';
import { MatDialog } from '@angular/material/dialog';
import { CalendarService } from '../calendar.service';
import { CalendarSharingService } from '../calendar-sharing.service';
import { RealtimeService } from '../realtime.service';
import { CalendarEvent } from '../calendar-event';
import { DayEventsDialog, DayEventsDialogData } from '../day-events-dialog/day-events-dialog';
import { ShareCalendarDialog } from '../share-calendar-dialog/share-calendar-dialog';

const WEEKDAY_LABELS = ['Man', 'Tir', 'Ons', 'Tor', 'Fre', 'Lør', 'Søn'];

@Component({
  selector: 'app-kalender',
  imports: [MatIconModule, MatButtonModule, MatBadgeModule],
  templateUrl: './kalender.html',
  styleUrl: './kalender.css',
})
export class Kalender implements OnInit {
  private calendarService = inject(CalendarService);
  private sharingService = inject(CalendarSharingService);
  private realtime = inject(RealtimeService);
  private dialog = inject(MatDialog);

  readonly weekdayLabels = WEEKDAY_LABELS;

  currentDate = signal(new Date());
  events = signal<CalendarEvent[]>([]);
  currentCalendarId = signal<number | null>(null);
  pendingInviteCount = signal(0);

  monthLabel = computed(() => {
    const label = this.currentDate().toLocaleDateString('da-DK', { month: 'long', year: 'numeric' });
    return label.charAt(0).toUpperCase() + label.slice(1);
  });

  gridDays = computed(() => {
    const d = this.currentDate();
    const start = this.startOfGrid(d.getFullYear(), d.getMonth());
    const end = this.endOfGrid(d.getFullYear(), d.getMonth());
    const days: Date[] = [];
    for (let day = new Date(start); day <= end; day.setDate(day.getDate() + 1)) {
      days.push(new Date(day));
    }
    return days;
  });

  private eventsByDate = computed(() => {
    const map = new Map<string, CalendarEvent[]>();
    for (const event of this.events()) {
      const list = map.get(event.date) ?? [];
      list.push(event);
      map.set(event.date, list);
    }
    return map;
  });

  ngOnInit() {
    this.refreshCalendarId();
    this.loadEvents();
    this.refreshInviteCount();

    this.realtime.onCalendarEventsChanged(calendarId => {
      if (calendarId === this.currentCalendarId()) {
        this.loadEvents();
      }
    });

    this.realtime.onCalendarChanged(() => {
      this.refreshCalendarId();
      this.loadEvents();
      this.refreshInviteCount();
    });

    this.realtime.onReconnected(() => {
      const id = this.currentCalendarId();
      if (id !== null) this.realtime.joinCalendar(id);
    });
  }

  eventsForDay(day: Date): CalendarEvent[] {
    return this.eventsByDate().get(this.toISODate(day)) ?? [];
  }

  isCurrentMonth(day: Date): boolean {
    return day.getMonth() === this.currentDate().getMonth();
  }

  isToday(day: Date): boolean {
    return this.toISODate(day) === this.toISODate(new Date());
  }

  prevMonth() {
    const d = this.currentDate();
    this.currentDate.set(new Date(d.getFullYear(), d.getMonth() - 1, 1));
    this.loadEvents();
  }

  nextMonth() {
    const d = this.currentDate();
    this.currentDate.set(new Date(d.getFullYear(), d.getMonth() + 1, 1));
    this.loadEvents();
  }

  goToToday() {
    this.currentDate.set(new Date());
    this.loadEvents();
  }

  openDay(day: Date) {
    const date = this.toISODate(day);
    const data: DayEventsDialogData = { date, events: this.eventsForDay(day) };
    const ref = this.dialog.open(DayEventsDialog, { data, width: '480px', maxWidth: '95vw' });
    ref.afterClosed().subscribe(changed => {
      if (changed) this.loadEvents();
    });
  }

  openShareDialog() {
    const ref = this.dialog.open(ShareCalendarDialog, { width: '420px', maxWidth: '90vw' });
    ref.afterClosed().subscribe(changed => {
      this.refreshInviteCount();
      if (changed) {
        this.refreshCalendarId();
        this.loadEvents();
      }
    });
  }

  private refreshCalendarId() {
    this.sharingService.getMyCalendarId().subscribe(({ id }) => {
      const previous = this.currentCalendarId();
      if (previous !== null && previous !== id) {
        this.realtime.leaveCalendar(previous);
      }
      this.currentCalendarId.set(id);
      this.realtime.joinCalendar(id);
    });
  }

  private refreshInviteCount() {
    this.sharingService.getInvites().subscribe(invites => {
      this.pendingInviteCount.set(invites.filter(i => i.isIncoming).length);
    });
  }

  private loadEvents() {
    const d = this.currentDate();
    const start = this.startOfGrid(d.getFullYear(), d.getMonth());
    const end = this.endOfGrid(d.getFullYear(), d.getMonth());
    this.calendarService.getByRange(this.toISODate(start), this.toISODate(end)).subscribe(events => {
      this.events.set(events);
    });
  }

  private startOfGrid(year: number, month: number): Date {
    const first = new Date(year, month, 1);
    const offset = (first.getDay() + 6) % 7; // Monday = 0
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

  private toISODate(d: Date): string {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }
}
