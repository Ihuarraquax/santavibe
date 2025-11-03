import { Component, input, ChangeDetectionStrategy } from '@angular/core';
import { DrawValidationViewModel } from '../../models/group-details.viewmodel';

/**
 * Displays the current validation status for draw execution (organizer only, pre-draw).
 * Shows checkmark if valid or error icon with list of validation errors.
 */
@Component({
  selector: 'app-draw-validation',
  imports: [],
  templateUrl: './draw-validation.html',
  styleUrl: './draw-validation.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DrawValidationComponent {
  // Input signals
  validationResult = input.required<DrawValidationViewModel>();
  participantCount = input.required<number>();
  exclusionRuleCount = input.required<number>();
}
