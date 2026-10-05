export interface RecipeSummary {
    id: number;
    title: string;
}

export interface MealPlan {
    id: number;
    name: string;
    recipes: RecipeSummary[];
}

export interface GeneratedListItem {
    name: string;
    category: string;
    quantity: number;
    amountSummary: string | null;
}
