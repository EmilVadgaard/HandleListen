import { UnitOfMeasure } from './unit-of-measure';

export interface SharedRecipe {
    id: number;
    title: string;
    description: string;
    sharedByEmail: string;
    sharedAt: string;
    addedCount: number;
    tags: string[];
    sharedByMe: boolean;
}

export interface SharedRecipeIngredient {
    name: string;
    quantity: number;
    amount: number | null;
    unit: UnitOfMeasure | null;
    category: string;
}

export interface SharedRecipeDetail extends SharedRecipe {
    ingredients: SharedRecipeIngredient[];
}

export interface TopSearch {
    query: string;
    count: number;
}
