export type PlanType = 'Standard' | 'Weekly' | 'Monthly';

export interface Plan {
    id: number;
    name: string;
    type: PlanType;
    isActive: boolean;
    activatedDate: string | null;
    dayCount: number | null;
    month: number | null;
    year: number | null;
    isOwner: boolean;
    ownerEmail: string;
}

export interface PlanTodo {
    id: number;
    title: string;
    done: boolean;
    dayIndex: number | null;
    dayOfMonth: number | null;
    time: string | null;
    resolvedDate: string | null;
    sortOrder: number;
}

export interface PlanDetail {
    plan: Plan;
    todos: PlanTodo[];
}
