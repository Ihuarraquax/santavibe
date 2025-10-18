import { Component, input, output, ChangeDetectionStrategy } from '@angular/core';

/**
 * Displays error messages with retry and dismiss options.
 * Provides user-friendly error feedback with actionable buttons.
 */
@Component({
  selector: 'app-error-alert-retry',
  template: `
    <div class="flex p-4 mb-4 text-red-800 border border-red-300 rounded-lg bg-red-50" role="alert">
      <svg class="flex-shrink-0 w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
        <path fill-rule="evenodd" d="M10 .5a9.5 9.5 0 1 0 9.5 9.5A9.51 9.51 0 0 0 10 .5ZM9.5 4a1.5 1.5 0 1 1 0 3 1.5 1.5 0 0 1 0-3ZM12 15H8a1 1 0 0 1 0-2h1v-3H8a1 1 0 0 1 0-2h2a1 1 0 0 1 1 1v4h1a1 1 0 0 1 0 2Z" clip-rule="evenodd"/>
      </svg>
      <div class="ml-3 text-sm font-medium flex-grow">
        {{ message() }}
      </div>
      <div class="flex gap-2">
        <button
          (click)="handleRetry()"
          type="button"
          class="text-red-800 bg-transparent border border-red-800 hover:bg-red-900 hover:text-white focus:ring-4 focus:outline-none focus:ring-red-300 font-medium rounded-lg text-xs px-3 py-1.5 text-center">
          Spróbuj ponownie
        </button>
        <button
          type="button"
          (click)="handleDismiss()"
          class="ml-auto -mx-1.5 -my-1.5 bg-red-50 text-red-500 rounded-lg focus:ring-2 focus:ring-red-400 p-1.5 hover:bg-red-200 inline-flex h-8 w-8 items-center justify-center">
          <span class="sr-only">Zamknij</span>
          <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
            <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd"/>
          </svg>
        </button>
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ErrorAlertRetryComponent {
  message = input.required<string>();

  retry = output<void>();
  dismiss = output<void>();

  handleRetry(): void {
    this.retry.emit();
  }

  handleDismiss(): void {
    this.dismiss.emit();
  }
}
