import { Component, input, output, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';

/**
 * Provides final budget input and "Execute Draw" button (organizer only, pre-draw).
 * Shows confirmation dialog and loading state during execution.
 */
@Component({
  selector: 'app-draw-execution',
  imports: [FormsModule, LoadingSpinnerComponent],
  templateUrl: './draw-execution.html',
  styleUrl: './draw-execution.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DrawExecutionComponent {
  // Input signals
  isValidationPassed = input.required<boolean>();
  isExecuting = input<boolean>(false);

  // Output signals
  executeDraw = output<number>(); // budget amount

  // Internal state
  budgetAmount = signal<number | null>(null);
  showConfirmDialog = signal<boolean>(false);

  // Computed validation
  isBudgetValid = computed(() => {
    const amount = this.budgetAmount();
    if (!amount) return false;
    return amount > 0 && this.hasMaxTwoDecimals(amount);
  });

  canExecute = computed(() =>
    this.isValidationPassed() &&
    this.isBudgetValid() &&
    !this.isExecuting()
  );

  budgetError = computed(() => {
    const amount = this.budgetAmount();
    if (!amount) return 'Budżet jest wymagany';
    if (amount <= 0) return 'Budżet musi być dodatni';
    if (!this.hasMaxTwoDecimals(amount)) return 'Maksymalnie 2 miejsca po przecinku';
    return null;
  });

  buttonTooltip = computed(() => {
    if (!this.isValidationPassed()) {
      return 'Nie można wykonać losowania: występują błędy walidacji';
    }
    if (!this.isBudgetValid()) {
      return 'Wprowadź poprawny budżet';
    }
    return 'Wykonaj losowanie Świętego Mikołaja';
  });

  onExecuteClick(): void {
    if (this.canExecute()) {
      this.showConfirmDialog.set(true);
    }
  }

  onConfirmed(): void {
    this.showConfirmDialog.set(false);
    this.executeDraw.emit(this.budgetAmount()!);
  }

  onCancelled(): void {
    this.showConfirmDialog.set(false);
  }

  private hasMaxTwoDecimals(value: number): boolean {
    return /^\d+(\.\d{1,2})?$/.test(value.toString());
  }
}
