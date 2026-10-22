import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog } from '@angular/material/dialog';
import { PlanService } from '../plan.service';
import { RealtimeService } from '../realtime.service';
import { Plan, PlanTodo } from '../plan';
import { PlanSettingsDialog, PlanSettingsResult } from '../plan-settings-dialog/plan-settings-dialog';
import { PlanStandardView } from '../plan-standard-view/plan-standard-view';
import { PlanWeeklyView } from '../plan-weekly-view/plan-weekly-view';
import { PlanMonthlyView } from '../plan-monthly-view/plan-monthly-view';

const TYPE_LABELS: Record<Plan['type'], string> = {
  Standard: 'Standard',
  Weekly: 'Ugeplan',
  Monthly: 'Månedsplan',
};

@Component({
  selector: 'app-plan-detail',
  imports: [MatButtonModule, MatIconModule, MatChipsModule, PlanStandardView, PlanWeeklyView, PlanMonthlyView],
  templateUrl: './plan-detail.html',
  styleUrl: './plan-detail.css',
})
export class PlanDetail implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private planService = inject(PlanService);
  private realtime = inject(RealtimeService);
  private dialog = inject(MatDialog);

  planId = Number(this.route.snapshot.paramMap.get('id'));
  plan = signal<Plan | null>(null);
  todos = signal<PlanTodo[]>([]);
  activating = signal(false);

  ngOnInit() {
    this.refresh();
    this.realtime.joinPlan(this.planId);
    this.realtime.onPlanChanged(planId => {
      if (planId === this.planId) this.refresh();
    });
    this.realtime.onReconnected(() => this.realtime.joinPlan(this.planId));
  }

  ngOnDestroy() {
    this.realtime.leavePlan(this.planId);
  }

  typeLabel(type: Plan['type']): string {
    return TYPE_LABELS[type];
  }

  refresh() {
    this.planService.getById(this.planId).subscribe({
      next: detail => {
        this.plan.set(detail.plan);
        this.todos.set(detail.todos);
      },
      error: () => this.router.navigate(['/planer']),
    });
  }

  activate() {
    if (!confirm('Aktivér planen? Når planen er aktiveret, kan dens struktur (antal dage/måned) ikke ændres igen.')) return;

    this.activating.set(true);
    const clientToday = this.toISODate(new Date());
    this.planService.activate(this.planId, clientToday).subscribe({
      next: () => {
        this.activating.set(false);
        this.refresh();
      },
      error: () => this.activating.set(false),
    });
  }

  openSettings() {
    const plan = this.plan();
    if (!plan) return;

    const ref = this.dialog.open(PlanSettingsDialog, { data: plan, width: '420px', maxWidth: '90vw' });
    ref.afterClosed().subscribe((result?: PlanSettingsResult) => {
      if (!result) return;
      if (result.deleted) {
        this.router.navigate(['/planer']);
      } else if (result.renamedTo) {
        this.refresh();
      }
    });
  }

  print() {
    window.print();
  }

  private toISODate(d: Date): string {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }
}
