/**
 * View model for displaying a group card.
 * Transforms API DTO data into display-ready format.
 */
export interface GroupCardViewModel {
  /** Group identifier for navigation */
  groupId: string;

  /** Group name to display */
  name: string;

  /** Organizer's full name */
  organizerName: string;

  /** Number of participants */
  participantCount: number;

  /** Formatted budget string (e.g., "100,00 PLN") or null */
  budget: string | null;

  /** Draw status enum value */
  drawStatus: DrawStatus;

  /** Localized status label for display */
  drawStatusLabel: string;

  /** Badge color based on status */
  drawStatusColor: BadgeColor;

  /** Formatted join date (e.g., "15 paź 2025") */
  joinedDate: string;

  /** Formatted draw completion date or null */
  drawCompletedDate: string | null;

  /** Whether current user is the organizer */
  isOrganizer: boolean;
}

/**
 * Enum representing the draw status of a group.
 */
export enum DrawStatus {
  /** Draw has not been performed yet */
  Pending = 'pending',

  /** Draw has been completed */
  Completed = 'completed'
}

/**
 * Type for badge color variants (maps to Flowbite badge colors).
 */
export type BadgeColor = 'success' | 'warning' | 'info' | 'error';

/**
 * Type for skeleton loader variants.
 */
export type SkeletonType = 'card' | 'list' | 'table';
