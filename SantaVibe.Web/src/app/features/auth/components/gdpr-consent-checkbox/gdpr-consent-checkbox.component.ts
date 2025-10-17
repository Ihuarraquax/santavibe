import { Component, input, model, output, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

/**
 * GDPR consent checkbox component with link to terms and conditions.
 * Supports two-way binding and validation error display.
 */
@Component({
  selector: 'app-gdpr-consent-checkbox',
  templateUrl: './gdpr-consent-checkbox.component.html',
  styleUrls: ['./gdpr-consent-checkbox.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule]
})
export class GdprConsentCheckboxComponent {
  /**
   * Two-way bindable checkbox value using model signal.
   */
  checked = model.required<boolean>();

  /**
   * Array of validation error messages.
   */
  errors = input<string[]>([]);

  /**
   * Whether the checkbox has been touched/interacted with.
   */
  touched = input<boolean>(false);

  /**
   * Whether the checkbox is disabled.
   */
  disabled = input<boolean>(false);

  /**
   * Emits when the checkbox loses focus.
   */
  blur = output<void>();

  /**
   * Emits when the checkbox value changes.
   */
  change = output<boolean>();

  /**
   * Handles checkbox change event.
   */
  onChange(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.checked.set(target.checked);
    this.change.emit(target.checked);
  }

  /**
   * Handles checkbox blur event.
   */
  onBlur(): void {
    this.blur.emit();
  }

  /**
   * Returns whether to show validation errors.
   */
  get shouldShowErrors(): boolean {
    return this.touched() && this.errors().length > 0;
  }

  /**
   * Returns CSS classes for the checkbox based on validation state.
   */
  get checkboxClasses(): string {
    const baseClasses = 'w-4 h-4 rounded focus:ring-3 focus:ring-blue-300 dark:focus:ring-blue-600';
    const normalClasses = 'text-blue-600 bg-gray-100 border-gray-300 dark:bg-gray-700 dark:border-gray-600';
    const errorClasses = 'text-red-600 bg-red-100 border-red-300 dark:bg-gray-700 dark:border-red-600';

    return this.shouldShowErrors
      ? `${baseClasses} ${errorClasses}`
      : `${baseClasses} ${normalClasses}`;
  }

  /**
   * Returns CSS classes for the label based on validation state.
   */
  get labelClasses(): string {
    const baseClasses = 'text-sm font-medium';
    const normalClasses = 'text-gray-900 dark:text-gray-300';
    const errorClasses = 'text-red-700 dark:text-red-500';

    return this.shouldShowErrors
      ? `${baseClasses} ${errorClasses}`
      : `${baseClasses} ${normalClasses}`;
  }
}
