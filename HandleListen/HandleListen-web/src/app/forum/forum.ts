import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { ForumService } from '../forum.service';
import { SharedRecipe, TopSearch } from '../shared-recipe';
import { ForumRecipeDialog } from '../forum-recipe-dialog/forum-recipe-dialog';

@Component({
  selector: 'app-forum',
  imports: [FormsModule, MatFormFieldModule, MatInputModule, MatIconModule, MatCardModule, MatChipsModule],
  templateUrl: './forum.html',
  styleUrl: './forum.css',
})
export class Forum {
  private forumService = inject(ForumService);
  private dialog = inject(MatDialog);

  private static readonly SEARCH_DEBOUNCE_MS = 400;
  private searchTimer?: ReturnType<typeof setTimeout>;
  private lastLoggedQuery = '';

  searchQuery = '';
  recipes = signal<SharedRecipe[]>([]);
  topSearches = signal<TopSearch[]>([]);
  loading = signal(false);

  constructor() {
    this.refresh();
    this.refreshTopSearches();
  }

  onSearchInput() {
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.refresh();
      this.maybeLogSearch();
    }, Forum.SEARCH_DEBOUNCE_MS);
  }

  private maybeLogSearch() {
    const trimmed = this.searchQuery.trim();
    if (!trimmed || trimmed.toLowerCase() === this.lastLoggedQuery.toLowerCase()) return;
    this.lastLoggedQuery = trimmed;
    this.forumService.logSearch(trimmed).subscribe();
  }

  // A top-search card runs the search but doesn't log it again - otherwise a popular term
  // would keep reinforcing its own rank every time someone clicked it from the suggestion row.
  searchFor(query: string) {
    this.searchQuery = query;
    this.refresh();
  }

  private refresh() {
    this.loading.set(true);
    this.forumService.getAll(this.searchQuery).subscribe({
      next: recipes => {
        this.loading.set(false);
        this.recipes.set(recipes);
      },
      error: () => this.loading.set(false)
    });
  }

  private refreshTopSearches() {
    this.forumService.getTopSearches().subscribe(searches => this.topSearches.set(searches));
  }

  openRecipe(recipe: SharedRecipe) {
    const ref = this.dialog.open(ForumRecipeDialog, { data: { sharedRecipeId: recipe.id }, width: '480px', maxWidth: '90vw' });
    ref.afterClosed().subscribe(added => {
      // AddedCount changed, which affects "most used" sort order.
      if (added) this.refresh();
    });
  }
}
