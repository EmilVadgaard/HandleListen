export type UnitOfMeasure = 'Gram' | 'Kilogram' | 'Milliliter' | 'Deciliter' | 'Liter' | 'Piece';

export const UNIT_LABELS: Record<UnitOfMeasure, string> = {
    Gram: 'g',
    Kilogram: 'kg',
    Milliliter: 'ml',
    Deciliter: 'dl',
    Liter: 'l',
    Piece: 'stk',
};

export const ALL_UNITS: UnitOfMeasure[] = ['Gram', 'Kilogram', 'Milliliter', 'Deciliter', 'Liter', 'Piece'];
