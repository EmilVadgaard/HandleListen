export interface CalendarMember {
    userId: string;
    email: string;
}

export interface CalendarInvite {
    id: number;
    fromEmail: string;
    toEmail: string;
    isIncoming: boolean;
    createdAt: string;
}
