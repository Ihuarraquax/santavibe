import { Component, input, ChangeDetectionStrategy } from '@angular/core';
import { SkeletonType } from '../../../features/groups/models/group.types';

/**
 * Reusable component that displays animated skeleton placeholders during initial data loading.
 * Provides visual feedback to users that content is being loaded.
 * Supports different skeleton types for various layouts.
 */
@Component({
  selector: 'app-skeleton-loader',
  template: `
    @if (type() === 'card') {
      <div class="animate-pulse p-6 bg-white border border-gray-200 rounded-lg shadow">
        <!-- Header skeleton -->
        <div class="mb-4">
          <div class="h-6 bg-gray-200 rounded w-3/4 mb-2"></div>
          <div class="h-5 bg-gray-200 rounded w-1/4"></div>
        </div>

        <!-- Details skeleton -->
        <div class="space-y-2">
          <div class="h-4 bg-gray-200 rounded w-full"></div>
          <div class="h-4 bg-gray-200 rounded w-5/6"></div>
          <div class="h-4 bg-gray-200 rounded w-4/6"></div>
          <div class="h-4 bg-gray-200 rounded w-3/6"></div>
        </div>
      </div>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SkeletonLoaderComponent {
  type = input<SkeletonType>('card');
}
