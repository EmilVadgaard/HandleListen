import { Component, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatListModule } from '@angular/material/list';
import { RecipeService } from '../recipe.service';
import { MealPlanService } from '../meal-plan.service';
import { Recipe } from '../recipe';
import { MealPlan } from '../meal-plan';

@Component({
  selector: 'app-add-meal-plan-dialog',
  imports: [FormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, MatCheckboxModule, MatListModule],
  templateUrl: './add-meal-plan-dialog.html',
  styleUrl: './add-meal-plan-dialog.css',
})
export class AddMealPlanDialog {
  private dialogRef = inject(MatDialogRef<AddMealPlanDialog, MealPlan | undefined>);
  private recipeService = inject(RecipeService);
  private mealPlanService = inject(MealPlanService);

  name = '';
  searchQuery = '';
  recipes = signal<Recipe[]>([]);
  selectedIds = signal<Set<number>>(new Set());
  saving = signal(false);
  error = signal<string | null>(null);

  filteredRecipes = computed(() => {
    const query = this.searchQuery.trim().toLowerCase();
    if (!query) return this.recipes();
    return this.recipes().filter(r =>
      r.title.toLowerCase().includes(query) ||
      r.tags.some(t => t.tag.toLowerCase().includes(query)));
  });

  constructor() {
    this.recipeService.getAll().subscribe(recipes => this.recipes.set(recipes));
  }

  toggleRecipe(id: number) {
    this.selectedIds.update(ids => {
      const next = new Set(ids);
      if (next.has(id)) next.delete(id); else next.add(id);
      return next;
    });
  }

  isSelected(id: number): boolean {
    return this.selectedIds().has(id);
  }

  save() {
    const name = this.name.trim();
    if (!name) return;

    this.saving.set(true);
    this.error.set(null);
    this.mealPlanService.create(name, Array.from(this.selectedIds())).subscribe({
      next: plan => {
        this.saving.set(false);
        this.dialogRef.close(plan);
      },
      error: () => {
        this.saving.set(false);
        this.error.set('Kunne ikke oprette madplanen.');
      }
    });
  }

  close() {
    this.dialogRef.close();
  }
}
