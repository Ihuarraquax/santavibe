import { Component, input, output, signal, computed, effect, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';

/**
 * Presentational component for inputting budget suggestion.
 * Validates positive number with max 2 decimal places.
 */
@Component({
  selector: 'app-budget-suggestion',
  imports: [FormsModule, LoadingSpinnerComponent],
  templateUrl: './budget-suggestion.html',
  styleUrl: './budget-suggestion.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BudgetSuggestionComponent {
  // Input signals
  initialAmount = input<number | null>(null);
  isEditable = input<boolean>(true);
  isSaving = input<boolean>(false);

  // Output signals
  save = output<number | null>();

  // Internal state
  budgetAmount = signal<number | null>(null);

  // Computed signals
  isValid = computed(() => {
    const amount = this.budgetAmount();
    if (amount === null || amount === undefined) return true; // optional
    return amount > 0 && this.hasMaxTwoDecimals(amount);
  });

  isDirty = computed(() => this.budgetAmount() !== this.initialAmount());

  validationError = computed(() => {
    if (this.isValid()) return null;
    const amount = this.budgetAmount();
    if (amount !== null && amount !== undefined && amount <= 0) {
      return 'Budżet musi być liczbą dodatnią';
    }
    return 'Budżet może mieć maksymalnie 2 miejsca po przecinku';
  });

  canSave = computed(() =>
    this.isValid() && this.isDirty() && !this.isSaving()
  );

  constructor() {
    // Sync initial amount to internal signal
    effect(() => {
      this.budgetAmount.set(this.initialAmount());
    });
  }

  onAmountChange(value: number | null): void {
    this.budgetAmount.set(value);
  }

  onSave(): void {
    if (this.canSave()) {
      this.save.emit(this.budgetAmount());
    }
  }

  /**
   * Validates that a number has maximum 2 decimal places.
   */
  private hasMaxTwoDecimals(value: number): boolean {
    return /^\d+(\.\d{1,2})?$/.test(value.toString());
  }
}
