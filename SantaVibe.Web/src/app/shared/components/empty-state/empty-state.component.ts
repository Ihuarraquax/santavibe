import { Component, input, output, ChangeDetectionStrategy } from '@angular/core';

/**
 * Displays a friendly message and call-to-action when the user has no groups.
 * Provides clear guidance on what actions the user can take.
 */
@Component({
  selector: 'app-empty-state',
  template: `
    <div class="flex flex-col items-center justify-center py-12 px-4 text-center">
      <!-- Illustration/Icon -->
      <svg class="w-24 h-24 text-gray-400 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
      </svg>

      <!-- Message -->
      <h3 class="text-xl font-semibold text-gray-900 mb-2">
        {{ message() }}
      </h3>

      @if (description()) {
        <p class="text-gray-600 mb-6 max-w-md">
          {{ description() }}
        </p>
      }

      <!-- Action Button -->
      @if (actionLabel()) {
        <button
          (click)="handleAction()"
          class="inline-flex items-center px-5 py-2.5 text-sm font-medium text-center text-white bg-blue-700 rounded-lg hover:bg-blue-800 focus:ring-4 focus:outline-none focus:ring-blue-300 dark:bg-blue-600 dark:hover:bg-blue-700 dark:focus:ring-blue-800">
          @if (actionIcon()) {
            <svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
          }
          {{ actionLabel() }}
        </button>
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EmptyStateComponent {
  message = input.required<string>();
  description = input<string>();
  actionLabel = input<string>();
  actionIcon = input<string>();

  action = output<void>();

  handleAction(): void {
    this.action.emit();
  }
}
