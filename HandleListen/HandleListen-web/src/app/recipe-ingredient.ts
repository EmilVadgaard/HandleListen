import { UnitOfMeasure } from './unit-of-measure';

export interface RecipeIngredient {
    id: number;
    recipeId: number;
    name: string;
    quantity: number;
    category: string;
    amount: number | null;
    unit: UnitOfMeasure | null;
}
