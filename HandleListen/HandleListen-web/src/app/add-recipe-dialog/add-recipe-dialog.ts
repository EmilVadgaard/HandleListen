import { Component, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { forkJoin, of } from 'rxjs';
import { RecipeService } from '../recipe.service';
import { Recipe } from '../recipe';

@Component({
  selector: 'app-add-recipe-dialog',
  imports: [FormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, MatChipsModule, MatAutocompleteModule],
  templateUrl: './add-recipe-dialog.html',
  styleUrl: './add-recipe-dialog.css',
})
export class AddRecipeDialog {
  private dialogRef = inject(MatDialogRef<AddRecipeDialog, Recipe | undefined>);
  private recipeService = inject(RecipeService);

  title = '';
  description = '';
  saving = signal(false);
  error = signal<string | null>(null);

  tags = signal<string[]>([]);
  tagInput = '';
  tagSuggestions = signal<string[]>([]);
  filteredTagSuggestions = computed(() => {
    const query = this.tagInput.trim().toLowerCase();
    const current = this.tags().map(t => t.toLowerCase());
    return this.tagSuggestions()
      .filter(s => !current.includes(s.toLowerCase()))
      .filter(s => !query || s.toLowerCase().includes(query));
  });

  constructor() {
    this.recipeService.getTagSuggestions().subscribe(tags => this.tagSuggestions.set(tags));
  }

  addTag(tag: string) {
    const trimmed = tag.trim();
    if (!trimmed || this.tags().some(t => t.toLowerCase() === trimmed.toLowerCase())) {
      this.tagInput = '';
      return;
    }
    this.tags.update(tags => [...tags, trimmed]);
    this.tagInput = '';
  }

  onTagOptionSelected(event: MatAutocompleteSelectedEvent) {
    this.addTag(event.option.value as string);
  }

  removeTag(tag: string) {
    this.tags.update(tags => tags.filter(t => t !== tag));
  }

  save() {
    const title = this.title.trim();
    if (!title) return;

    this.saving.set(true);
    this.error.set(null);
    this.recipeService.create(title, this.description.trim()).subscribe({
      next: recipe => {
        const tagRequests = this.tags().map(tag => this.recipeService.addTag(recipe.id, tag));
        (tagRequests.length > 0 ? forkJoin(tagRequests) : of([])).subscribe({
          next: createdTags => {
            this.saving.set(false);
            this.dialogRef.close({ ...recipe, tags: createdTags });
          },
          error: () => {
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
