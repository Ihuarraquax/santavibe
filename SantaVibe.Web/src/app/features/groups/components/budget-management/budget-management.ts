import { Component, input, computed, ChangeDetectionStrategy } from '@angular/core';
import { DecimalPipe } from '@angular/common';

/**
 * Displays anonymous budget suggestions from participants (organizer only, pre-draw).
 * Shows suggestion count and list of suggested amounts in sorted order.
 */
@Component({
  selector: 'app-budget-management',
  imports: [DecimalPipe],
  templateUrl: './budget-management.html',
  styleUrl: './budget-management.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BudgetManagementComponent {
  // Input signals
  budgetSuggestions = input.required<number[]>();
  participantCount = input.required<number>();
  currentBudget = input<number | null>(null);

  // Computed signals
  suggestionCount = computed(() => this.budgetSuggestions().length);
}
