import { Component, inject, signal, computed, OnDestroy, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { MatChipsModule } from '@angular/material/chips';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { RecipeService } from '../recipe.service';
import { RecipeIngredientService } from '../recipe-ingredient.service';
import { RealtimeService } from '../realtime.service';
import { Recipe } from '../recipe';
import { RecipeIngredient } from '../recipe-ingredient';
import { ItemNameField } from '../item-name-field/item-name-field';
import { MatSelectModule } from '@angular/material/select';
import { UnitOfMeasure, ALL_UNITS, UNIT_LABELS } from '../unit-of-measure';
import { CATEGORIES } from '../category';

export interface RecipeCardDialogResult {
  deleted?: boolean;
  updated?: Recipe;
}

@Component({
  selector: 'app-recipe-card-dialog',
  imports: [FormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, MatListModule, MatDividerModule, MatChipsModule, MatAutocompleteModule, MatSelectModule, ItemNameField],
  templateUrl: './recipe-card-dialog.html',
  styleUrl: './recipe-card-dialog.css',
})
export class RecipeCardDialog implements OnDestroy {
  private dialogRef = inject(MatDialogRef<RecipeCardDialog, RecipeCardDialogResult | undefined>);
  private recipeService = inject(RecipeService);
  private ingredientService = inject(RecipeIngredientService);
  private realtime = inject(RealtimeService);
  recipe = signal(inject<Recipe>(MAT_DIALOG_DATA));
  nameField = viewChild(ItemNameField);

  editing = signal(false);
  titleDraft = '';
  descriptionDraft = '';

  ingredients = signal<RecipeIngredient[]>([]);
  newIngredientName = '';
  newIngredientQuantity = 1;
  newIngredientAmount: number | null = null;
  newIngredientUnit: UnitOfMeasure | null = null;
  newIngredientCategory = '';
  units = ALL_UNITS;
  unitLabels = UNIT_LABELS;
  categories = CATEGORIES;

  tagInput = '';
  tagSuggestions = signal<string[]>([]);
  filteredTagSuggestions = computed(() => {
    const query = this.tagInput.trim().toLowerCase();
    const current = this.recipe().tags.map(t => t.tag.toLowerCase());
    return this.tagSuggestions()
      .filter(s => !current.includes(s.toLowerCase()))
      .filter(s => !query || s.toLowerCase().includes(query));
  });

  private changed = false;

  constructor() {
    const recipe = this.recipe();
    this.ingredientService.getByRecipe(recipe.id).subscribe(ingredients => this.ingredients.set(ingredients));
    this.recipeService.getTagSuggestions().subscribe(tags => this.tagSuggestions.set(tags));
    this.realtime.joinRecipe(recipe.id);
    this.realtime.onRecipeIngredientsChanged(recipeId => {
      // Ingredient-only event: title/description/tags are already kept in sync by their own
      // optimistic updates (saveDetails/addTag/removeTag), so there's no need to also refetch
      // the whole recipe here - that was a redundant extra round trip on every ingredient edit.
      if (recipeId === this.recipe().id) {
        this.ingredientService.getByRecipe(recipeId).subscribe(ingredients => this.ingredients.set(ingredients));
      }
    });
  }

  ngOnDestroy() {
    this.realtime.leaveRecipe(this.recipe().id);
  }

  startEditing() {
    const recipe = this.recipe();
    this.titleDraft = recipe.title;
    this.descriptionDraft = recipe.description;
    this.editing.set(true);
  }

  stopEditing() {
    this.editing.set(false);
  }

  saveDetails() {
    const title = this.titleDraft.trim();
    if (!title) return;
    const recipe = this.recipe();
    this.recipeService.update(recipe.id, title, this.descriptionDraft).subscribe(() => {
      const updated = { ...recipe, title, description: this.descriptionDraft };
      this.recipe.set(updated);
      this.changed = true;
      this.editing.set(false);
    });
  }

  onUnitSuggested(unit: UnitOfMeasure | null) {
    this.newIngredientUnit = unit;
  }

  onCategorySuggested(category: string) {
    this.newIngredientCategory = category;
  }

  addIngredient() {
    const recipeId = this.recipe().id;
    if (!this.newIngredientName.trim()) return;
    this.ingredientService.create(this.newIngredientName, this.newIngredientQuantity, recipeId, this.newIngredientAmount, this.newIngredientUnit, this.newIngredientCategory).subscribe(created => {
      // The backend's realtime "RecipeIngredientsChanged" broadcast also reaches this same
      // client and may already have refreshed the list by the time this callback runs - guard
      // against appending the same ingredient twice.
      this.ingredients.update(list => list.some(i => i.id === created.id) ? list : [...list, created]);
      this.newIngredientName = '';
      this.newIngredientQuantity = 1;
      this.newIngredientAmount = null;
      this.newIngredientUnit = null;
      this.newIngredientCategory = '';
      this.nameField()?.reset();
    });
  }

  ingredientQuantity(ingredient: RecipeIngredient, updatedQuantity: number) {
    if (updatedQuantity < 1) return;
    const updated = { ...ingredient, quantity: updatedQuantity };
    this.ingredientService.update(updated).subscribe(() => {
      this.ingredients.update(list => list.map(i => i.id === ingredient.id ? updated : i));
    });
  }

  updateIngredientAmount(ingredient: RecipeIngredient, amount: number | null, unit: UnitOfMeasure | null) {
    const updated = { ...ingredient, amount, unit };
    this.ingredientService.update(updated).subscribe(() => {
      this.ingredients.update(list => list.map(i => i.id === ingredient.id ? updated : i));
    });
  }

  removeIngredient(id: number) {
    this.ingredientService.delete(id).subscribe({
      next: () => this.ingredients.update(list => list.filter(i => i.id !== id)),
      error: err => console.error('Failed to delete ingredient', err)
    });
  }

  addTag(tag: string) {
    const trimmed = tag.trim();
    const recipe = this.recipe();
    if (!trimmed || recipe.tags.some(t => t.tag.toLowerCase() === trimmed.toLowerCase())) {
      this.tagInput = '';
      return;
    }
    this.recipeService.addTag(recipe.id, trimmed).subscribe(newTag => {
      this.recipe.update(r => ({ ...r, tags: [...r.tags, newTag] }));
      this.tagInput = '';
      this.changed = true;
    });
  }

  onTagOptionSelected(event: MatAutocompleteSelectedEvent) {
    this.addTag(event.option.value as string);
  }

  removeTag(tagId: number) {
    const recipe = this.recipe();
    this.recipeService.removeTag(recipe.id, tagId).subscribe(() => {
      this.recipe.update(r => ({ ...r, tags: r.tags.filter(t => t.id !== tagId) }));
      this.changed = true;
    });
  }

  deleteRecipe() {
    const recipe = this.recipe();
    if (!confirm(`Er du sikker på at du vil slette "${recipe.title}"? Dette kan ikke fortrydes.`)) return;
    this.recipeService.delete(recipe.id).subscribe(() => {
      this.dialogRef.close({ deleted: true });
    });
  }

  close() {
    this.dialogRef.close(this.changed ? { updated: this.recipe() } : undefined);
  }
}
