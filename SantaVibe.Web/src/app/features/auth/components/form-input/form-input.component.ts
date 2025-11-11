import { Component, input, output, model, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

/**
 * Reusable form input component with validation error display.
 * Supports two-way binding and shows errors only when field is touched.
 */
@Component({
  selector: 'app-form-input',
  templateUrl: './form-input.component.html',
  styleUrls: ['./form-input.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule]
})
export class FormInputComponent {
  /**
   * Two-way bindable input value using model signal.
   */
  value = model.required<string>();

  /**
   * Field label to display above the input.
   */
  label = input.required<string>();

  /**
   * Input type (text, email, password, etc.).
   */
  type = input<string>('text');

  /**
   * Placeholder text for the input.
   */
  placeholder = input<string>('');

  /**
   * Whether the field is required (adds asterisk to label).
   */
  required = input<boolean>(false);

  /**
   * Array of validation error messages.
   */
  errors = input<string[]>([]);

  /**
   * Whether the field has been touched/focused.
   */
  touched = input<boolean>(false);

  /**
   * Autocomplete attribute value.
   */
  autocomplete = input<string>('off');

  /**
   * Whether the input is disabled.
   */
  disabled = input<boolean>(false);

  /**
   * Emits when the input loses focus.
   */
  blurred = output<void>();

  /**
   * Emits when the input value changes.
   */
  valueChange = output<string>();

  /**
   * Handles input blur event.
   */
  onBlur(): void {
    this.blurred.emit();
  }

  /**
   * Handles input change event.
   */
  onInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.value.set(target.value);
    this.valueChange.emit(target.value);
  }

  /**
   * Returns whether to show validation errors.
   * Errors are shown only when the field is touched and has errors.
   */
  get shouldShowErrors(): boolean {
    return this.touched() && this.errors().length > 0;
  }

  /**
   * Returns CSS classes for the input based on validation state.
   */
  get inputClasses(): string {
    const baseClasses = 'block w-full p-2.5 text-sm rounded-lg transition-colors';
    const normalClasses = 'bg-gray-50 border border-gray-300 text-gray-900 focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400 dark:text-white dark:focus:ring-blue-500 dark:focus:border-blue-500';
    const errorClasses = 'bg-red-50 border border-red-500 text-red-900 placeholder-red-700 focus:ring-red-500 focus:border-red-500 dark:bg-gray-700 dark:text-red-500 dark:placeholder-red-500 dark:border-red-500';
    const disabledClasses = 'cursor-not-allowed opacity-50';

    if (this.disabled()) {
      return `${baseClasses} ${normalClasses} ${disabledClasses}`;
    }

    return this.shouldShowErrors
      ? `${baseClasses} ${errorClasses}`
      : `${baseClasses} ${normalClasses}`;
  }

  /**
   * Returns CSS classes for the label based on validation state.
   */
  get labelClasses(): string {
    const baseClasses = 'block mb-2 text-sm font-medium';
    const normalClasses = 'text-gray-900 dark:text-white';
    const errorClasses = 'text-red-700 dark:text-red-500';

    return this.shouldShowErrors
      ? `${baseClasses} ${errorClasses}`
      : `${baseClasses} ${normalClasses}`;
  }
}
