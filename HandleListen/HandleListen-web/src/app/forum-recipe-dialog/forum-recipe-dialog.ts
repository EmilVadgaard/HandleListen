import { Component, inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ForumService } from '../forum.service';
import { SharedRecipeDetail } from '../shared-recipe';
import { UNIT_LABELS } from '../unit-of-measure';

export interface ForumRecipeDialogData {
  sharedRecipeId: number;
}

@Component({
  selector: 'app-forum-recipe-dialog',
  imports: [MatDialogModule, MatButtonModule, MatIconModule, MatChipsModule, MatListModule, MatProgressSpinnerModule],
  templateUrl: './forum-recipe-dialog.html',
  styleUrl: './forum-recipe-dialog.css',
})
export class ForumRecipeDialog {
  private dialogRef = inject(MatDialogRef<ForumRecipeDialog, boolean | undefined>);
  private forumService = inject(ForumService);
  data = inject<ForumRecipeDialogData>(MAT_DIALOG_DATA);

  unitLabels = UNIT_LABELS;
  recipe = signal<SharedRecipeDetail | null>(null);
  loading = signal(true);
  adding = signal(false);
  added = signal(false);
  error = signal<string | null>(null);

  constructor() {
    this.forumService.getById(this.data.sharedRecipeId).subscribe({
      next: recipe => {
        this.recipe.set(recipe);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Kunne ikke hente opskriften.');
      }
    });
  }

  addToMyBook() {
    if (this.adding() || this.added()) return;
    this.adding.set(true);
    this.error.set(null);
    this.forumService.addToMyBook(this.data.sharedRecipeId).subscribe({
      next: () => {
        this.adding.set(false);
        this.added.set(true);
      },
      error: () => {
        this.adding.set(false);
        this.error.set('Kunne ikke tilføje opskriften til din opskriftsbog.');
      }
    });
  }

  close() {
    this.dialogRef.close(this.added());
  }
}
