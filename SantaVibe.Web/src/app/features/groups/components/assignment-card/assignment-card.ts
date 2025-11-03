import { Component, input, computed, ChangeDetectionStrategy } from '@angular/core';
import { DecimalPipe } from '@angular/common';

/**
 * Displays the user's Secret Santa assignment (post-draw).
 * Shows recipient name, budget, and wishlist.
 */
@Component({
  selector: 'app-assignment-card',
  imports: [DecimalPipe],
  templateUrl: './assignment-card.html',
  styleUrl: './assignment-card.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AssignmentCardComponent {
  // Input signals
  recipientFirstName = input.required<string>();
  recipientLastName = input.required<string>();
  budget = input.required<number>();
  hasWishlist = input.required<boolean>();
  wishlistContent = input<string | null>(null);

  // Computed
  recipientFullName = computed(() =>
    `${this.recipientFirstName()} ${this.recipientLastName()}`.trim()
  );
}
