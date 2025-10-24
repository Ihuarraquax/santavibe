import { Component, output, ChangeDetectionStrategy } from '@angular/core';

/**
 * Displays call-to-action buttons for unauthenticated users to either login or register.
 * Emits navigation events to parent component for handling.
 */
@Component({
  selector: 'app-unauthenticated-actions',
  template: `
    <div class="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 border border-gray-200 dark:border-gray-700">
      <h2 class="text-lg font-semibold text-gray-900 dark:text-white mb-4">
        Dołącz do grupy
      </h2>

      <p class="text-gray-600 dark:text-gray-400 mb-6">
        Aby dołączyć do tej grupy, musisz się zalogować lub zarejestrować.
      </p>

      <div class="space-y-3">
        <button
          type="button"
          (click)="login.emit()"
          class="w-full px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700
                 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2
                 dark:focus:ring-offset-gray-800 transition-colors duration-200 font-medium">
          Zaloguj się, aby dołączyć
        </button>

        <button
          type="button"
          (click)="register.emit()"
          class="w-full px-4 py-2 bg-white dark:bg-gray-700 text-blue-600 dark:text-blue-400
                 border-2 border-blue-600 dark:border-blue-500 rounded-lg
                 hover:bg-blue-50 dark:hover:bg-gray-600 focus:outline-none focus:ring-2
                 focus:ring-blue-500 focus:ring-offset-2 dark:focus:ring-offset-gray-800
                 transition-colors duration-200 font-medium">
          Zarejestruj się, aby dołączyć
        </button>
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UnauthenticatedActionsComponent {
  /**
   * Emits when user clicks login button
   */
  login = output<void>();

  /**
   * Emits when user clicks register button
   */
  register = output<void>();
}
