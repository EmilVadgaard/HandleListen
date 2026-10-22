import { Component, inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { forkJoin } from 'rxjs';
import { MealPlanService } from '../meal-plan.service';
import { MealPlan } from '../meal-plan';
import { GenerateListDialog, GenerateListDialogData } from '../generate-list-dialog/generate-list-dialog';
import { ShoppingList } from '../shopping-list';
import { RecipeService } from '../recipe.service';
import { RecipeIngredientService } from '../recipe-ingredient.service';
import { Recipe } from '../recipe';
import { RecipeIngredient } from '../recipe-ingredient';
import { UNIT_LABELS } from '../unit-of-measure';

export interface MealPlanCardDialogResult {
  deleted?: boolean;
  generatedList?: ShoppingList;
}

export interface FullRecipeForPrint {
  recipe: Recipe;
  ingredients: RecipeIngredient[];
}

@Component({
  selector: 'app-meal-plan-card-dialog',
  imports: [MatDialogModule, MatButtonModule, MatIconModule, MatListModule, MatDividerModule],
  templateUrl: './meal-plan-card-dialog.html',
  styleUrl: './meal-plan-card-dialog.css',
})
export class MealPlanCardDialog {
  private dialogRef = inject(MatDialogRef<MealPlanCardDialog, MealPlanCardDialogResult | undefined>);
  private mealPlanService = inject(MealPlanService);
  private dialog = inject(MatDialog);
  private recipeService = inject(RecipeService);
  private ingredientService = inject(RecipeIngredientService);
  plan = inject<MealPlan>(MAT_DIALOG_DATA);

  readonly unitLabels = UNIT_LABELS;
  printingFullRecipes = signal(false);
  fullRecipesForPrint = signal<FullRecipeForPrint[]>([]);

  private generatedList = signal<ShoppingList | undefined>(undefined);

  openGenerateDialog() {
    const data: GenerateListDialogData = { mealPlanId: this.plan.id, defaultName: this.plan.name };
    const ref = this.dialog.open(GenerateListDialog, { data, width: '600px', maxWidth: '95vw', maxHeight: '90vh' });
    ref.afterClosed().subscribe((list?: ShoppingList) => {
      if (list) {
        this.generatedList.set(list);
        this.close();
      }
    });
  }

  deletePlan() {
    if (!confirm(`Er du sikker på at du vil slette madplanen "${this.plan.name}"? Dette kan ikke fortrydes.`)) return;
    this.mealPlanService.delete(this.plan.id).subscribe(() => {
      this.dialogRef.close({ deleted: true });
    });
  }

  close() {
    this.dialogRef.close(this.generatedList() ? { generatedList: this.generatedList() } : undefined);
  }

  groupIngredients(ingredients: RecipeIngredient[]) {
    const groups = new Map<string, RecipeIngredient[]>();
    for (const ingredient of ingredients) {
      const key = ingredient.category || 'Diverse';
      const list = groups.get(key) || [];
      list.push(ingredient);
      groups.set(key, list);
    }
    return Array.from(groups, ([category, items]) => ({ category, items }));
  }

  printNamesOnly() {
    window.print();
  }

  printWithRecipes() {
    if (this.plan.recipes.length === 0) {
      window.print();
      return;
    }

    const requests = this.plan.recipes.map(r => forkJoin({
      recipe: this.recipeService.getById(r.id),
      ingredients: this.ingredientService.getByRecipe(r.id),
    }));

    forkJoin(requests).subscribe(results => {
      this.fullRecipesForPrint.set(results);
      this.printingFullRecipes.set(true);
      // Give change detection a moment to render the full-recipe print block before the
      // browser's print dialog captures the page - calling window.print() synchronously
      // risks printing the old DOM.
      setTimeout(() => {
        window.print();
        this.printingFullRecipes.set(false);
      }, 50);
    });
  }
}
