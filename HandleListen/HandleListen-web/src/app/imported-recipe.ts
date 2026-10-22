import { UnitOfMeasure } from './unit-of-measure';

export interface ImportedIngredient {
    name: string;
    quantity: number;
    amount: number | null;
    unit: UnitOfMeasure | null;
    category: string | null;
}

export interface ImportedRecipe {
    title: string;
    description: string;
    ingredients: ImportedIngredient[];
}
