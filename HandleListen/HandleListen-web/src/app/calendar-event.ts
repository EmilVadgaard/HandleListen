export type ReminderUnit = 'Hours' | 'Days';

export interface CalendarEvent {
    id: number;
    calendarId: number;
    title: string;
    description: string;
    date: string;
    time: string | null;
    reminderValue: number | null;
    reminderUnit: ReminderUnit | null;
}
