import { Component, input, ChangeDetectionStrategy } from '@angular/core';
import { InvitationDetailsDto } from '../../models/invitation.types';

/**
 * Displays group information from the invitation in a card format.
 * Pure presentational component that receives invitation data as input
 * and renders it in a visually appealing, mobile-responsive card.
 */
@Component({
  selector: 'app-invitation-details-card',
  template: `
    <div class="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 border border-gray-200 dark:border-gray-700">
      <h1 class="text-2xl font-bold text-gray-900 dark:text-white mb-4">
        {{ invitation().groupName }}
      </h1>

      <div class="space-y-3">
        <div class="flex items-center gap-2">
          <svg class="w-5 h-5 text-gray-500 dark:text-gray-400" fill="currentColor" viewBox="0 0 20 20">
            <path fill-rule="evenodd" d="M10 9a3 3 0 100-6 3 3 0 000 6zm-7 9a7 7 0 1114 0H3z" clip-rule="evenodd"/>
          </svg>
          <span class="text-gray-700 dark:text-gray-300">
            <span class="font-medium">Organizator:</span>
            {{ invitation().organizerName }}
          </span>
        </div>

        <div class="flex items-center gap-2">
          <svg class="w-5 h-5 text-gray-500 dark:text-gray-400" fill="currentColor" viewBox="0 0 20 20">
            <path d="M9 6a3 3 0 11-6 0 3 3 0 016 0zM17 6a3 3 0 11-6 0 3 3 0 016 0zM12.93 17c.046-.327.07-.66.07-1a6.97 6.97 0 00-1.5-4.33A5 5 0 0119 16v1h-6.07zM6 11a5 5 0 015 5v1H1v-1a5 5 0 015-5z"/>
          </svg>
          <span class="text-gray-700 dark:text-gray-300">
            <span class="font-medium">Uczestnicy:</span>
            {{ invitation().participantCount }}
          </span>
        </div>
      </div>

      <div class="mt-4 p-4 bg-blue-50 dark:bg-blue-900/20 rounded-lg border border-blue-200 dark:border-blue-800">
        <p class="text-sm text-blue-800 dark:text-blue-200">
          🎅 Zostałeś zaproszony do wzięcia udziału w Tajemniczym Mikołaju!
        </p>
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class InvitationDetailsCardComponent {
  /**
   * Invitation details to display
   */
  invitation = input.required<InvitationDetailsDto>();
}
