export type ReminderUnit = 'Hours' | 'Days';
export type RecurrenceFrequency = 'Weekly' | 'Monthly' | 'Yearly';

export interface CalendarEvent {
    id: number;
    calendarId: number;
    title: string;
    description: string;
    date: string;
    time: string | null;
    reminderValue: number | null;
    reminderUnit: ReminderUnit | null;
    recurrence: RecurrenceFrequency | null;
    pushReminderEnabled: boolean;
    emailReminderEnabled: boolean;
}

// What GetByRange returns: one row per computed occurrence (recurring events expand into many),
// so `date` here is that occurrence's actual date, not necessarily the series' original date.
export type CalendarEventOccurrence = Omit<CalendarEvent, 'calendarId'>;
