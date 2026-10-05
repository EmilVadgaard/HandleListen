import { Component, signal, inject, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MealPlanService } from '../meal-plan.service';
import { RealtimeService } from '../realtime.service';
import { MealPlan } from '../meal-plan';
import { AddMealPlanDialog } from '../add-meal-plan-dialog/add-meal-plan-dialog';
import { MealPlanCardDialog, MealPlanCardDialogResult } from '../meal-plan-card-dialog/meal-plan-card-dialog';

@Component({
  selector: 'app-madplaner-liste',
  imports: [MatCardModule, MatIconModule],
  templateUrl: './madplaner-liste.html',
  styleUrl: './madplaner-liste.css',
})
export class MadplanerListe implements OnInit {
  private mealPlanService = inject(MealPlanService);
  private realtime = inject(RealtimeService);
  private dialog = inject(MatDialog);

  plans = signal<MealPlan[]>([]);

  ngOnInit() {
    this.refresh();
    this.realtime.onMealPlansChanged(() => this.refresh());
  }

  private refresh() {
    this.mealPlanService.getAll().subscribe(plans => this.plans.set(plans));
  }

  openAddPlan() {
    const dialogRef = this.dialog.open(AddMealPlanDialog, {
      width: '520px',
      maxWidth: '90vw',
      maxHeight: '90vh',
    });

    dialogRef.afterClosed().subscribe((created?: MealPlan) => {
      if (created) {
        // The backend's realtime "MealPlansChanged" broadcast also reaches this same client
        // (it's sent to every book member, including the creator) and may already have
        // refreshed the list via refresh() by the time this callback runs - guard against
        // appending the same plan twice.
        this.plans.update(plans => plans.some(p => p.id === created.id) ? plans : [...plans, created]);
      }
    });
  }

  openPlan(plan: MealPlan) {
    const dialogRef = this.dialog.open(MealPlanCardDialog, {
      data: plan,
      width: '480px',
      maxWidth: '90vw',
    });

    dialogRef.afterClosed().subscribe((result?: MealPlanCardDialogResult) => {
      if (!result) return;
      if (result.deleted) {
        this.plans.update(plans => plans.filter(p => p.id !== plan.id));
      }
    });
  }
}
