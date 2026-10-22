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
import { ForumService } from '../forum.service';
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
  private forumService = inject(ForumService);
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
  newIngredientCategory = signal('');
  filteredIngredientCategories = computed(() => {
    const query = this.newIngredientCategory().trim().toLowerCase();
    return CATEGORIES.filter(c => !query || c.toLowerCase().includes(query));
  });
  units = ALL_UNITS;
  unitLabels = UNIT_LABELS;

  editingIngredientId = signal<number | null>(null);
  editIngredientName = '';
  editIngredientCategory = signal('');
  filteredEditIngredientCategories = computed(() => {
    const query = this.editIngredientCategory().trim().toLowerCase();
    return CATEGORIES.filter(c => !query || c.toLowerCase().includes(query));
  });

  // Grouped under a heading per category, in the fixed CATEGORIES order (so the list roughly
  // follows a shopping trip) - same grouped-list pattern as Handleliste. A category not in that
  // list (freely typed via the known-item dialog or the ingredient row itself) gets its own
  // heading too, sorted after all the known ones instead of disappearing at index -1; no
  // category at all falls under "Diverse", matching Handleliste's own fallback name.
  groupedIngredients = computed(() => {
    const indexOf = (category: string) => {
      const i = category ? CATEGORIES.indexOf(category) : -1;
      return i === -1 ? CATEGORIES.length : i;
    };
    const groups = new Map<string, RecipeIngredient[]>();
    for (const ingredient of this.ingredients()) {
      const key = ingredient.category || 'Diverse';
      const list = groups.get(key) || [];
      list.push(ingredient);
      groups.set(key, list);
    }
    return Array.from(groups, ([category, items]) => ({ category, items }))
      .sort((a, b) => indexOf(a.category) - indexOf(b.category));
  });

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

  sharedRecipeId = signal<number | null>(null);
  sharing = signal(false);

  constructor() {
    const recipe = this.recipe();
    this.ingredientService.getByRecipe(recipe.id).subscribe(ingredients => this.ingredients.set(ingredients));
    this.recipeService.getTagSuggestions().subscribe(tags => this.tagSuggestions.set(tags));
    this.forumService.getShareStatus(recipe.id).subscribe({
      next: shared => this.sharedRecipeId.set(shared.id),
      error: () => { /* not shared yet - stays null */ }
    });
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
    this.newIngredientCategory.set(category);
  }

  addIngredient() {
    const recipeId = this.recipe().id;
    if (!this.newIngredientName.trim()) return;
    this.ingredientService.create(this.newIngredientName, this.newIngredientQuantity, recipeId, this.newIngredientAmount, this.newIngredientUnit, this.newIngredientCategory().trim()).subscribe(created => {
      // The backend's realtime "RecipeIngredientsChanged" broadcast also reaches this same
      // client and may already have refreshed the list by the time this callback runs - guard
      // against appending the same ingredient twice.
      this.ingredients.update(list => list.some(i => i.id === created.id) ? list : [...list, created]);
      this.newIngredientName = '';
      this.newIngredientQuantity = 1;
      this.newIngredientAmount = null;
      this.newIngredientUnit = null;
      this.newIngredientCategory.set('');
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

  startEditIngredient(ingredient: RecipeIngredient) {
    this.editingIngredientId.set(ingredient.id);
    this.editIngredientName = ingredient.name;
    this.editIngredientCategory.set(ingredient.category);
  }

  onEditIngredientCategorySuggested(category: string) {
    this.editIngredientCategory.set(category);
  }

  cancelEditIngredient() {
    this.editingIngredientId.set(null);
  }

  saveEditIngredient(ingredient: RecipeIngredient) {
    const name = this.editIngredientName.trim();
    if (!name) return;
    const updated = { ...ingredient, name, category: this.editIngredientCategory().trim() };
    this.ingredientService.update(updated).subscribe(() => {
      this.ingredients.update(list => list.map(i => i.id === ingredient.id ? updated : i));
      this.editingIngredientId.set(null);
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

  toggleShare() {
    if (this.sharing()) return;
    this.sharing.set(true);

    const sharedId = this.sharedRecipeId();
    if (sharedId === null) {
      this.forumService.share(this.recipe().id).subscribe({
        next: shared => {
          this.sharing.set(false);
          this.sharedRecipeId.set(shared.id);
        },
        error: () => this.sharing.set(false)
      });
    } else {
      this.forumService.unshare(sharedId).subscribe({
        next: () => {
          this.sharing.set(false);
          this.sharedRecipeId.set(null);
        },
        error: () => this.sharing.set(false)
      });
    }
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
