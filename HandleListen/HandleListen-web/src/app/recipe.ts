export interface RecipeTag {
    id: number;
    tag: string;
}

export interface Recipe {
    id: number;
    title: string;
    description: string;
    tags: RecipeTag[];
}
