import { ShoppingItem } from "./shopping-item";

export interface ShoppingList {
    id: number;
    name: string;
    userId: string;
    items: ShoppingItem[];
}