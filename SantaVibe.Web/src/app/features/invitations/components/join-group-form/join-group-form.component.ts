import { Component, input, output, inject, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { BudgetSuggestionInputComponent } from '../../../../shared/components/budget-suggestion-input/budget-suggestion-input.component';
import { JoinGroupFormData } from '../../models/invitation.types';
import { decimalPlacesValidator } from '../../validators/budget.validators';

/**
 * Form component for joining a group with optional budget suggestion.
 * Handles form validation, displays loading state during submission,
 * and emits join event with form data.
 */
@Component({
  selector: 'app-join-group-form',
  template: `
    <div class="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 border border-gray-200 dark:border-gray-700">
      <h2 class="text-lg font-semibold text-gray-900 dark:text-white mb-4">
        Dołącz do grupy: {{ groupName() }}
      </h2>

      <form [formGroup]="joinForm" (ngSubmit)="handleSubmit()" class="space-y-6">
        <app-budget-suggestion-input
          [control]="joinForm.controls.budgetSuggestion"
          [disabled]="isJoining()" />

        <div class="bg-gray-50 dark:bg-gray-700/50 rounded-lg p-4 border border-gray-200 dark:border-gray-600">
          <p class="text-sm text-gray-600 dark:text-gray-400">
            💡 Sugestia budżetu jest opcjonalna. Organizator może wykorzystać ją do ustalenia
            finalnego budżetu dla całej grupy.
          </p>
        </div>

        <button
          type="submit"
          [disabled]="isJoining() || joinForm.invalid"
          class="w-full px-4 py-3 bg-green-600 text-white rounded-lg hover:bg-green-700
                 disabled:bg-gray-400 dark:disabled:bg-gray-600 disabled:cursor-not-allowed
                 focus:outline-none focus:ring-2 focus:ring-green-500 focus:ring-offset-2
                 dark:focus:ring-offset-gray-800 transition-colors duration-200 font-medium
                 flex items-center justify-center gap-2">
          @if (isJoining()) {
            <svg class="animate-spin h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            <span>Dołączanie...</span>
          } @else {
            <span>🎁 Dołącz do grupy</span>
          }
        </button>
      </form>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, BudgetSuggestionInputComponent]
})
export class JoinGroupFormComponent {
  private fb = inject(FormBuilder);

  /**
   * Group name to display
   */
  groupName = input.required<string>();

  /**
   * Whether join request is in progress
   */
  isJoining = input.required<boolean>();

  /**
   * Emits when form is submitted with valid data
   */
  submitted = output<JoinGroupFormData>();

  /**
   * Reactive form with budget suggestion field
   */
  readonly joinForm = this.fb.group({
    budgetSuggestion: this.fb.control<number | null>(null, [
      Validators.min(0.01),
      Validators.max(99999999.99),
      decimalPlacesValidator(2)
    ])
  });

  /**
   * Handles form submission
   */
  handleSubmit(): void {
    if (this.joinForm.valid && !this.isJoining()) {
      const formData: JoinGroupFormData = {
        budgetSuggestion: this.joinForm.value.budgetSuggestion ?? null
      };
      this.submitted.emit(formData);
    }
  }
}
