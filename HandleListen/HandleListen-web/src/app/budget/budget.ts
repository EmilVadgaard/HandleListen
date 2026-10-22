import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { forkJoin } from 'rxjs';
import { BudgetService } from '../budget.service';
import { RealtimeService } from '../realtime.service';
import { BudgetFixedEntry, BudgetVariableCategory, BudgetEntryType } from '../budget';
import { ShareBudgetDialog } from '../share-budget-dialog/share-budget-dialog';

function signedAmount(type: BudgetEntryType, amount: number): number {
  return type === 'Income' ? amount : -amount;
}

@Component({
  selector: 'app-budget',
  imports: [FormsModule, MatCardModule, MatIconModule, MatButtonModule, MatListModule, MatFormFieldModule, MatInputModule, MatSelectModule],
  templateUrl: './budget.html',
  styleUrl: './budget.css',
})
export class Budget implements OnInit {
  private budgetService = inject(BudgetService);
  private realtime = inject(RealtimeService);
  private dialog = inject(MatDialog);

  fixedEntries = signal<BudgetFixedEntry[]>([]);
  variableCategories = signal<BudgetVariableCategory[]>([]);

  incomeFixed = computed(() => this.fixedEntries().filter(e => e.type === 'Income'));
  expenseFixed = computed(() => this.fixedEntries().filter(e => e.type === 'Expense'));

  expectedSurplus = computed(() =>
    this.fixedEntries().reduce((sum, e) => sum + signedAmount(e.type, e.amount), 0) +
    this.variableCategories().reduce((sum, c) => sum + signedAmount(c.type, c.expectedAmount), 0));

  actualSurplus = computed(() =>
    this.fixedEntries().reduce((sum, e) => sum + signedAmount(e.type, e.amount), 0) +
    this.variableCategories().reduce((sum, c) => sum + signedAmount(c.type, c.actualAmount), 0));

  editingFixedId: number | null = null;
  editFixedName = '';
  editFixedType: BudgetEntryType = 'Expense';
  editFixedAmount = 0;

  addingFixedType: BudgetEntryType | null = null;
  newFixedName = '';
  newFixedAmount = 0;

  editingVariableNameId: number | null = null;
  editVariableName = '';

  isAddingVariable = false;
  newVariableName = '';
  newVariableType: BudgetEntryType = 'Expense';

  ngOnInit() {
    this.refresh();
    this.realtime.onBudgetChanged(() => this.refresh());
  }

  private refresh() {
    forkJoin({
      fixedEntries: this.budgetService.getFixedEntries(),
      variableCategories: this.budgetService.getVariableCategories(),
    }).subscribe(({ fixedEntries, variableCategories }) => {
      this.fixedEntries.set(fixedEntries);
      this.variableCategories.set(variableCategories);
    });
  }

  openShareDialog() {
    const ref = this.dialog.open(ShareBudgetDialog, { width: '420px', maxWidth: '90vw' });
    ref.afterClosed().subscribe(changed => {
      if (changed) this.refresh();
    });
  }

  startAddFixed(type: BudgetEntryType) {
    this.addingFixedType = type;
    this.newFixedName = '';
    this.newFixedAmount = 0;
  }

  cancelAddFixed() {
    this.addingFixedType = null;
  }

  confirmAddFixed() {
    const name = this.newFixedName.trim();
    if (!name || this.addingFixedType === null) return;
    this.budgetService.createFixedEntry({ name, type: this.addingFixedType, amount: this.newFixedAmount }).subscribe(created => {
      this.fixedEntries.update(entries => [...entries, created]);
      this.addingFixedType = null;
    });
  }

  startEditFixed(entry: BudgetFixedEntry) {
    this.editingFixedId = entry.id;
    this.editFixedName = entry.name;
    this.editFixedType = entry.type;
    this.editFixedAmount = entry.amount;
  }

  cancelEditFixed() {
    this.editingFixedId = null;
  }

  saveEditFixed(entry: BudgetFixedEntry) {
    const name = this.editFixedName.trim();
    if (!name) return;
    this.budgetService.updateFixedEntry(entry.id, { name, type: this.editFixedType, amount: this.editFixedAmount }).subscribe(() => {
      this.fixedEntries.update(entries => entries.map(e => e.id === entry.id ? { ...e, name, type: this.editFixedType, amount: this.editFixedAmount } : e));
      this.editingFixedId = null;
    });
  }

  deleteFixedEntry(entry: BudgetFixedEntry) {
    this.budgetService.deleteFixedEntry(entry.id).subscribe(() => {
      this.fixedEntries.update(entries => entries.filter(e => e.id !== entry.id));
    });
  }

  startAddVariable() {
    this.isAddingVariable = true;
    this.newVariableName = '';
    this.newVariableType = 'Expense';
  }

  cancelAddVariable() {
    this.isAddingVariable = false;
  }

  confirmAddVariable() {
    const name = this.newVariableName.trim();
    if (!name) return;
    this.budgetService.createVariableCategory({ name, type: this.newVariableType, expectedAmount: 0, actualAmount: 0 }).subscribe(created => {
      this.variableCategories.update(categories => [...categories, created]);
      this.isAddingVariable = false;
    });
  }

  startEditVariableName(category: BudgetVariableCategory) {
    this.editingVariableNameId = category.id;
    this.editVariableName = category.name;
  }

  cancelEditVariableName() {
    this.editingVariableNameId = null;
  }

  saveEditVariableName(category: BudgetVariableCategory) {
    const name = this.editVariableName.trim();
    if (!name) return;
    this.budgetService.updateVariableCategory(category.id, { name, type: category.type, expectedAmount: category.expectedAmount, actualAmount: category.actualAmount }).subscribe(() => {
      this.variableCategories.update(categories => categories.map(c => c.id === category.id ? { ...c, name } : c));
      this.editingVariableNameId = null;
    });
  }

  updateExpectedAmount(category: BudgetVariableCategory, value: string) {
    const expectedAmount = Number(value) || 0;
    this.budgetService.updateVariableCategory(category.id, { name: category.name, type: category.type, expectedAmount, actualAmount: category.actualAmount }).subscribe(() => {
      this.variableCategories.update(categories => categories.map(c => c.id === category.id ? { ...c, expectedAmount } : c));
    });
  }

  updateActualAmount(category: BudgetVariableCategory, value: string) {
    const actualAmount = Number(value) || 0;
    this.budgetService.updateVariableCategory(category.id, { name: category.name, type: category.type, expectedAmount: category.expectedAmount, actualAmount }).subscribe(() => {
      this.variableCategories.update(categories => categories.map(c => c.id === category.id ? { ...c, actualAmount } : c));
    });
  }

  deleteVariableCategory(category: BudgetVariableCategory) {
    this.budgetService.deleteVariableCategory(category.id).subscribe(() => {
      this.variableCategories.update(categories => categories.filter(c => c.id !== category.id));
    });
  }
}
