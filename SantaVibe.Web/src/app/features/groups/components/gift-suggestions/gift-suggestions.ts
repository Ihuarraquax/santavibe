import { Component, input, output, ChangeDetectionStrategy } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { GiftSuggestion } from '../../models/group-details.viewmodel';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';

/**
 * Provides AI gift suggestions (post-draw).
 * Shows generate button, loading state, and suggestion cards.
 */
@Component({
  selector: 'app-gift-suggestions',
  imports: [DecimalPipe, LoadingSpinnerComponent],
  templateUrl: './gift-suggestions.html',
  styleUrl: './gift-suggestions.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class GiftSuggestionsComponent {
  // Input signals
  suggestions = input<GiftSuggestion[]>([]);
  isGenerating = input<boolean>(false);
  error = input<string | null>(null);

  // Output signals
  generate = output<void>();
  cancel = output<void>();
}
