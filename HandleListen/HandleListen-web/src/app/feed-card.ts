export type FeedCard =
    | { kind: 'event'; date: string; time: string | null; id: number; title: string; description: string }
    | { kind: 'note'; date: string; id: number; text: string }
    | { kind: 'todo'; date: string; time: string | null; id: number; title: string; done: boolean; planId: number; planName: string };

export function compareFeedCards(a: FeedCard, b: FeedCard): number {
    if (a.date !== b.date) return a.date.localeCompare(b.date);
    const aTime = 'time' in a ? a.time : null;
    const bTime = 'time' in b ? b.time : null;
    if (aTime === bTime) return 0;
    if (aTime === null) return 1;
    if (bTime === null) return -1;
    return aTime.localeCompare(bTime);
}
