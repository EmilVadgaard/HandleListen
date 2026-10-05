import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatBadgeModule } from '@angular/material/badge';
import { MatChipsModule } from '@angular/material/chips';
import { RecipeService } from '../recipe.service';
import { RecipeBookSharingService } from '../recipe-book-sharing.service';
import { RealtimeService } from '../realtime.service';
import { Recipe } from '../recipe';
import { AddRecipeDialog } from '../add-recipe-dialog/add-recipe-dialog';
import { RecipeCardDialog, RecipeCardDialogResult } from '../recipe-card-dialog/recipe-card-dialog';
import { ShareRecipeBookDialog } from '../share-recipe-book-dialog/share-recipe-book-dialog';

@Component({
  selector: 'app-opskrifter',
  imports: [FormsModule, MatCardModule, MatIconModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatBadgeModule, MatChipsModule],
  templateUrl: './opskrifter.html',
  styleUrl: './opskrifter.css',
})
export class Opskrifter implements OnInit {
  private recipeService = inject(RecipeService);
  private sharingService = inject(RecipeBookSharingService);
  private realtime = inject(RealtimeService);
  private dialog = inject(MatDialog);

  recipes = signal<Recipe[]>([]);
  searchQuery = '';
  selectedTags = signal<Set<string>>(new Set());
  pendingInviteCount = signal(0);

  allTags = computed(() => {
    const tags = new Set<string>();
    for (const recipe of this.recipes()) {
      for (const tag of recipe.tags) tags.add(tag.tag);
    }
    return Array.from(tags).sort((a, b) => a.localeCompare(b, 'da'));
  });

  filteredRecipes = computed(() => {
    const query = this.searchQuery.trim().toLowerCase();
    const required = this.selectedTags();
    return this.recipes().filter(r => {
      const matchesQuery = !query ||
        r.title.toLowerCase().includes(query) ||
        r.tags.some(t => t.tag.toLowerCase().includes(query));
      if (!matchesQuery) return false;

      if (required.size === 0) return true;
      const recipeTags = new Set(r.tags.map(t => t.tag.toLowerCase()));
      return Array.from(required).every(t => recipeTags.has(t.toLowerCase()));
    });
  });

  ngOnInit() {
    this.refreshRecipes();
    this.refreshInviteCount();
    this.realtime.onRecipesChanged(() => {
      this.refreshRecipes();
      this.refreshInviteCount();
    });
  }

  private refreshRecipes() {
    this.recipeService.getAll().subscribe(recipes => this.recipes.set(recipes));
  }

  private refreshInviteCount() {
    this.sharingService.getInvites().subscribe(invites => {
      this.pendingInviteCount.set(invites.filter(i => i.isIncoming).length);
    });
  }

  toggleTagFilter(tag: string) {
    this.selectedTags.update(tags => {
      const next = new Set(tags);
      if (next.has(tag)) next.delete(tag); else next.add(tag);
      return next;
    });
  }

  isTagSelected(tag: string): boolean {
    return this.selectedTags().has(tag);
  }

  openAddRecipe() {
    const dialogRef = this.dialog.open(AddRecipeDialog, {
      width: '480px',
      maxWidth: '90vw',
    });

    dialogRef.afterClosed().subscribe((created?: Recipe) => {
      if (created) {
        // The backend's realtime "RecipesChanged" broadcast also reaches this same client
        // and may already have refreshed the grid via refreshRecipes() by the time this
        // callback runs - guard against appending the same recipe twice.
        this.recipes.update(recipes => recipes.some(r => r.id === created.id) ? recipes : [...recipes, created]);
      }
    });
  }

  openRecipe(recipe: Recipe) {
    const dialogRef = this.dialog.open(RecipeCardDialog, {
      data: recipe,
      width: '600px',
      maxWidth: '95vw',
      maxHeight: '90vh',
    });

    dialogRef.afterClosed().subscribe((result?: RecipeCardDialogResult) => {
      if (!result) return;

      if (result.deleted) {
        this.recipes.update(recipes => recipes.filter(r => r.id !== recipe.id));
      } else if (result.updated) {
        this.recipes.update(recipes => recipes.map(r => r.id === recipe.id ? result.updated! : r));
      }
    });
  }

  openShareDialog() {
    const ref = this.dialog.open(ShareRecipeBookDialog, { width: '420px', maxWidth: '90vw' });
    ref.afterClosed().subscribe(changed => {
      this.refreshInviteCount();
      if (changed) this.refreshRecipes();
    });
  }
}
