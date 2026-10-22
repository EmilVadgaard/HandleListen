import { Component, signal, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { PlanService } from '../plan.service';
import { RealtimeService } from '../realtime.service';
import { Plan } from '../plan';
import { AddPlanDialog } from '../add-plan-dialog/add-plan-dialog';

const TYPE_LABELS: Record<Plan['type'], string> = {
  Standard: 'Standard',
  Weekly: 'Ugeplan',
  Monthly: 'Månedsplan',
};

@Component({
  selector: 'app-planer-liste',
  imports: [MatCardModule, MatIconModule, MatChipsModule],
  templateUrl: './planer-liste.html',
  styleUrl: './planer-liste.css',
})
export class PlanerListe implements OnInit {
  private planService = inject(PlanService);
  private realtime = inject(RealtimeService);
  private dialog = inject(MatDialog);
  private router = inject(Router);

  plans = signal<Plan[]>([]);

  ngOnInit() {
    this.refresh();
    this.realtime.onPlansChanged(() => this.refresh());
  }

  typeLabel(type: Plan['type']): string {
    return TYPE_LABELS[type];
  }

  private refresh() {
    this.planService.getAll().subscribe(plans => this.plans.set(plans));
  }

  openAddPlan() {
    const dialogRef = this.dialog.open(AddPlanDialog, {
      width: '480px',
      maxWidth: '90vw',
    });

    dialogRef.afterClosed().subscribe((created?: Plan) => {
      if (created) {
        this.router.navigate(['/planer', created.id]);
      }
    });
  }

  openPlan(plan: Plan) {
    this.router.navigate(['/planer', plan.id]);
  }
}
