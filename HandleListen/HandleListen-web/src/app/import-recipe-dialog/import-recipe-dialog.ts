import { Component, inject, signal, computed, viewChild, ElementRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTabsModule } from '@angular/material/tabs';
import { forkJoin, of, Observable } from 'rxjs';
import { RecipeImportService } from '../recipe-import.service';
import { RecipeService } from '../recipe.service';
import { RecipeIngredientService } from '../recipe-ingredient.service';
import { ImportedIngredient, ImportedRecipe } from '../imported-recipe';
import { Recipe } from '../recipe';
import { ItemNameField } from '../item-name-field/item-name-field';
import { UnitOfMeasure, ALL_UNITS, UNIT_LABELS } from '../unit-of-measure';
import { CATEGORIES } from '../category';
import { AuthService } from '../auth';
import { isMobileDevice } from '../device';

@Component({
  selector: 'app-import-recipe-dialog',
  imports: [FormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, MatSelectModule, MatAutocompleteModule, MatListModule, MatProgressSpinnerModule, MatTabsModule, ItemNameField],
  templateUrl: './import-recipe-dialog.html',
  styleUrl: './import-recipe-dialog.css',
})
export class ImportRecipeDialog {
  private dialogRef = inject(MatDialogRef<ImportRecipeDialog, Recipe | undefined>);
  private importService = inject(RecipeImportService);
  private recipeService = inject(RecipeService);
  private ingredientService = inject(RecipeIngredientService);
  private auth = inject(AuthService);

  canImportAi = this.auth.canImportAi;
  isMobile = isMobileDevice();

  galleryInput = viewChild<ElementRef<HTMLInputElement>>('galleryInput');
  cameraInput = viewChild<ElementRef<HTMLInputElement>>('cameraInput');

  url = '';
  pastedText = '';
  selectedFile: File | null = null;

  loading = signal(false);
  error = signal<string | null>(null);
  saving = signal(false);
  imported = signal(false);
  fixingIngredients = signal(false);

  title = '';
  description = '';
  ingredients = signal<ImportedIngredient[]>([]);

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

  fetchFromUrl() {
    const trimmed = this.url.trim();
    if (!trimmed) return;
    this.runImport(this.importService.importFromUrl(trimmed), 'Kunne ikke finde opskriftsdata på denne side. Prøv en anden side, eller opret opskriften manuelt.');
  }

  fetchFromText() {
    const trimmed = this.pastedText.trim();
    if (!trimmed) return;
    this.runImport(this.importService.importFromAiText(trimmed), 'Kunne ikke finde en opskrift i den indsatte tekst.');
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    this.selectedFile = input.files?.[0] ?? null;
  }

  openGalleryPicker() {
    this.galleryInput()?.nativeElement.click();
  }

  openCamera() {
    this.cameraInput()?.nativeElement.click();
  }

  fetchFromImage() {
    if (!this.selectedFile) return;
    this.runImport(this.importService.importFromAiImage(this.selectedFile), 'Kunne ikke finde en opskrift på billedet.');
  }

  private runImport(request: Observable<ImportedRecipe>, errorMessage: string) {
    this.loading.set(true);
    this.error.set(null);
    request.subscribe({
      next: (result: ImportedRecipe) => {
        this.loading.set(false);
        this.title = result.title;
        this.description = result.description;
        this.ingredients.set(result.ingredients);
        this.imported.set(true);
      },
      error: () => {
        this.loading.set(false);
        this.error.set(errorMessage);
      }
    });
  }

  onUnitSuggested(unit: UnitOfMeasure | null) {
    this.newIngredientUnit = unit;
  }

  onCategorySuggested(category: string) {
    this.newIngredientCategory.set(category);
  }

  addIngredient() {
    const name = this.newIngredientName.trim();
    if (!name) return;
    this.ingredients.update(list => [...list, {
      name,
      quantity: this.newIngredientQuantity,
      amount: this.newIngredientAmount,
      unit: this.newIngredientUnit,
      category: this.newIngredientCategory().trim() || null
    }]);
    this.newIngredientName = '';
    this.newIngredientQuantity = 1;
    this.newIngredientAmount = null;
    this.newIngredientUnit = null;
    this.newIngredientCategory.set('');
  }

  removeIngredient(index: number) {
    this.ingredients.update(list => list.filter((_, i) => i !== index));
  }

  aiFixIngredients() {
    const current = this.ingredients();
    if (current.length === 0 || this.fixingIngredients()) return;

    this.fixingIngredients.set(true);
    this.error.set(null);
    this.importService.cleanIngredientNames(current.map(i => i.name)).subscribe({
      next: cleaned => {
        this.fixingIngredients.set(false);
        this.ingredients.update(list => list.map((ingredient, i) => ({
          ...ingredient,
          name: cleaned[i]?.name ?? ingredient.name,
          category: cleaned[i]?.category ?? ingredient.category,
        })));
      },
      error: () => {
        this.fixingIngredients.set(false);
        this.error.set('Kunne ikke rette ingredienserne til.');
      }
    });
  }

  save() {
    const title = this.title.trim();
    if (!title) return;

    this.saving.set(true);
    this.error.set(null);
    this.recipeService.create(title, this.description).subscribe({
      next: recipe => {
        const toCreate = this.ingredients();
        const requests = toCreate.map(i =>
          this.ingredientService.create(i.name, i.quantity, recipe.id, i.amount, i.unit, i.category ?? ''));
        (requests.length > 0 ? forkJoin(requests) : of([])).subscribe({
          next: () => {
            this.saving.set(false);
            this.dialogRef.close(recipe);
          },
          error: () => {
            // The recipe itself was created successfully even if an ingredient failed partway
            // through - still hand it back rather than leaving the user stuck on a draft.
            this.saving.set(false);
            this.dialogRef.close(recipe);
          }
        });
      },
      error: () => {
        this.saving.set(false);
        this.error.set('Kunne ikke oprette opskriften.');
      }
    });
  }

  close() {
    this.dialogRef.close();
  }
}
