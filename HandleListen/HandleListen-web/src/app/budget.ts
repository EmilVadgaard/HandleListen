export type BudgetEntryType = 'Income' | 'Expense';

export interface BudgetFixedEntry {
    id: number;
    name: string;
    type: BudgetEntryType;
    amount: number;
}

export interface BudgetVariableCategory {
    id: number;
    name: string;
    type: BudgetEntryType;
    expectedAmount: number;
    actualAmount: number;
}

export interface BudgetMember {
    userId: string;
    email: string;
}

export interface BudgetInvite {
    id: number;
    fromEmail: string;
    toEmail: string;
    isIncoming: boolean;
    createdAt: string;
}
