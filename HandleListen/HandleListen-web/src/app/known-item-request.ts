export interface KnownItemRequest {
    id: number;
    name: string;
    sourceDescription: string | null;
    requestedByEmail: string;
    createdAt: string;
    lastSeenAt: string;
    seenCount: number;
}
