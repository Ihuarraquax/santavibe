import { Component, input, output, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Reusable component for displaying error messages (validation errors, API errors).
 * Supports dismissible alerts and different severity levels.
 */
@Component({
  selector: 'app-error-alert',
  templateUrl: './error-alert.component.html',
  styleUrls: ['./error-alert.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule]
})
export class ErrorAlertComponent {
  /**
   * Error message to display. If null, alert is not shown.
   */
  error = input<string | null>(null);

  /**
   * Whether the alert can be dismissed by the user.
   */
  dismissible = input<boolean>(true);

  /**
   * Alert severity level which affects styling.
   */
  severity = input<'error' | 'warning' | 'info'>('error');

  /**
   * Emits when the dismiss button is clicked.
   */
  dismiss = output<void>();

  /**
   * Handles the dismiss button click.
   */
  onDismiss(): void {
    this.dismiss.emit();
  }

  /**
   * Returns CSS classes based on severity level.
   */
  get alertClasses(): string {
    const baseClasses = 'flex items-center p-4 mb-4 rounded-lg';

    switch (this.severity()) {
      case 'error':
        return `${baseClasses} text-red-800 bg-red-50 dark:bg-gray-800 dark:text-red-400`;
      case 'warning':
        return `${baseClasses} text-yellow-800 bg-yellow-50 dark:bg-gray-800 dark:text-yellow-300`;
      case 'info':
        return `${baseClasses} text-blue-800 bg-blue-50 dark:bg-gray-800 dark:text-blue-400`;
      default:
        return `${baseClasses} text-red-800 bg-red-50 dark:bg-gray-800 dark:text-red-400`;
    }
  }

  /**
   * Returns icon SVG path based on severity level.
   */
  get iconPath(): string {
    switch (this.severity()) {
      case 'error':
        return 'M10 .5a9.5 9.5 0 1 0 9.5 9.5A9.51 9.51 0 0 0 10 .5ZM9.5 4a1.5 1.5 0 1 1 0 3 1.5 1.5 0 0 1 0-3ZM12 15H8a1 1 0 0 1 0-2h1v-3H8a1 1 0 0 1 0-2h2a1 1 0 0 1 1 1v4h1a1 1 0 0 1 0 2Z';
      case 'warning':
        return 'M10 .5a9.5 9.5 0 1 0 9.5 9.5A9.51 9.51 0 0 0 10 .5ZM9.5 4a1.5 1.5 0 1 1 0 3 1.5 1.5 0 0 1 0-3ZM12 15H8a1 1 0 0 1 0-2h1v-3H8a1 1 0 0 1 0-2h2a1 1 0 0 1 1 1v4h1a1 1 0 0 1 0 2Z';
      case 'info':
        return 'M10 .5a9.5 9.5 0 1 0 9.5 9.5A9.51 9.51 0 0 0 10 .5ZM9.5 4a1.5 1.5 0 1 1 0 3 1.5 1.5 0 0 1 0-3ZM12 15H8a1 1 0 0 1 0-2h1v-3H8a1 1 0 0 1 0-2h2a1 1 0 0 1 1 1v4h1a1 1 0 0 1 0 2Z';
      default:
        return 'M10 .5a9.5 9.5 0 1 0 9.5 9.5A9.51 9.51 0 0 0 10 .5ZM9.5 4a1.5 1.5 0 1 1 0 3 1.5 1.5 0 0 1 0-3ZM12 15H8a1 1 0 0 1 0-2h1v-3H8a1 1 0 0 1 0-2h2a1 1 0 0 1 1 1v4h1a1 1 0 0 1 0 2Z';
    }
  }
}
