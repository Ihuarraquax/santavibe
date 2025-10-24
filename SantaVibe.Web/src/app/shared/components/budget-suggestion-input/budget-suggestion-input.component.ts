import { Component, input, ChangeDetectionStrategy } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';

/**
 * Reusable input component for budget suggestion with PLN currency label,
 * validation feedback, and proper number formatting.
 * Integrates with Angular Reactive Forms.
 */
@Component({
  selector: 'app-budget-suggestion-input',
  template: `
    <div class="space-y-2">
      <label for="budget-suggestion" class="block text-sm font-medium text-gray-700 dark:text-gray-300">
        Sugestia budżetu (opcjonalnie)
      </label>

      <div class="relative">
        <input
          id="budget-suggestion"
          type="number"
          step="0.01"
          min="0.01"
          [formControl]="control()"
          [disabled]="disabled()"
          placeholder="np. 50.00"
          [class]="inputClasses"
        />

        <div class="absolute inset-y-0 right-0 pr-3 flex items-center pointer-events-none">
          <span class="text-gray-500 dark:text-gray-400 text-sm">PLN</span>
        </div>
      </div>

      @if (shouldShowErrors) {
        <p class="text-sm text-red-600 dark:text-red-400">
          @if (control().hasError('min')) {
            Budżet musi być większy niż 0.01 PLN
          }
          @if (control().hasError('max')) {
            Budżet nie może przekraczać 99,999,999.99 PLN
          }
          @if (control().hasError('decimalPlaces')) {
            Budżet może mieć maksymalnie 2 miejsca po przecinku
          }
        </p>
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule]
})
export class BudgetSuggestionInputComponent {
  /**
   * Form control for the budget input
   */
  control = input.required<FormControl<number | null>>();

  /**
   * Whether the input is disabled
   */
  disabled = input<boolean>(false);

  /**
   * Returns whether to show validation errors.
   * Errors are shown only when the field is touched and has errors.
   */
  get shouldShowErrors(): boolean {
    return this.control().invalid && this.control().touched;
  }

  /**
   * Returns CSS classes for the input based on validation state.
   */
  get inputClasses(): string {
    const baseClasses = 'block w-full rounded-lg border pr-12 p-2.5 text-sm transition-colors';
    const normalClasses = 'bg-gray-50 border-gray-300 text-gray-900 focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400 dark:text-white dark:focus:ring-blue-500 dark:focus:border-blue-500';
    const errorClasses = 'bg-red-50 border-red-300 text-red-900 placeholder-red-700 focus:ring-red-500 focus:border-red-500 dark:bg-gray-700 dark:text-red-400 dark:placeholder-red-400 dark:border-red-500';
    const disabledClasses = 'bg-gray-100 cursor-not-allowed opacity-60 dark:bg-gray-800';

    if (this.disabled()) {
      return `${baseClasses} ${normalClasses} ${disabledClasses}`;
    }

    return this.shouldShowErrors
      ? `${baseClasses} ${errorClasses}`
      : `${baseClasses} ${normalClasses}`;
  }
}
