import { Component, input, computed, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PasswordStrengthResult } from '../../models/registration.types';
import { ValidationService } from '../../services/validation.service';

/**
 * Component that displays real-time password strength feedback.
 * Shows visual strength meter and individual requirement checks.
 */
@Component({
  selector: 'app-password-strength-indicator',
  templateUrl: './password-strength-indicator.component.html',
  styleUrls: ['./password-strength-indicator.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule]
})
export class PasswordStrengthIndicatorComponent {
  private validationService = inject(ValidationService);

  /**
   * Current password value to analyze.
   */
  password = input.required<string>();

  /**
   * Computed password strength result based on current password.
   */
  strengthResult = computed<PasswordStrengthResult>(() => {
    return this.validationService.calculatePasswordStrength(this.password());
  });

  /**
   * Returns CSS classes for the strength meter bar based on strength level.
   */
  get strengthMeterClasses(): string {
    const level = this.strengthResult().level;
    const baseClasses = 'h-2 rounded-full transition-all duration-300';

    switch (level) {
      case 'weak':
        return `${baseClasses} bg-red-500 w-1/4`;
      case 'fair':
        return `${baseClasses} bg-yellow-500 w-2/4`;
      case 'good':
        return `${baseClasses} bg-blue-500 w-3/4`;
      case 'strong':
        return `${baseClasses} bg-green-500 w-full`;
      default:
        return `${baseClasses} bg-gray-300 w-0`;
    }
  }

  /**
   * Returns text label for current strength level.
   */
  get strengthLabel(): string {
    const level = this.strengthResult().level;

    switch (level) {
      case 'weak':
        return 'Słabe';
      case 'fair':
        return 'Średnie';
      case 'good':
        return 'Dobre';
      case 'strong':
        return 'Silne';
      default:
        return '';
    }
  }

  /**
   * Returns CSS classes for the strength label text based on strength level.
   */
  get strengthLabelClasses(): string {
    const level = this.strengthResult().level;

    switch (level) {
      case 'weak':
        return 'text-red-600 dark:text-red-400';
      case 'fair':
        return 'text-yellow-600 dark:text-yellow-400';
      case 'good':
        return 'text-blue-600 dark:text-blue-400';
      case 'strong':
        return 'text-green-600 dark:text-green-400';
      default:
        return 'text-gray-600 dark:text-gray-400';
    }
  }

  /**
   * Returns icon SVG for a requirement check (checkmark or X).
   */
  getCheckIcon(isMet: boolean): string {
    if (isMet) {
      // Checkmark icon
      return 'M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z';
    } else {
      // X icon
      return 'M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z';
    }
  }

  /**
   * Returns CSS classes for a requirement check icon.
   */
  getCheckIconClasses(isMet: boolean): string {
    return isMet
      ? 'text-green-600 dark:text-green-400'
      : 'text-red-600 dark:text-red-400';
  }
}
