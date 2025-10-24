/**
 * Type definitions for the Invitation feature
 */

import {
  GetInvitationDetailsResponse,
  AcceptInvitationRequest,
  AcceptInvitationResponse
} from '@api/model/models';

/**
 * View state for InvitationComponent
 * Manages loading, data, error, and joining states
 */
export interface InvitationViewState {
  /** Whether invitation details are being loaded */
  loading: boolean;

  /** Loaded invitation details (null if not loaded or error) */
  invitation: GetInvitationDetailsResponse | null;

  /** Error state (null if no error) */
  error: InvitationError | null;

  /** Whether join request is in progress */
  isJoining: boolean;
}

/**
 * Error state for invitation view
 * Categorizes errors for appropriate UI messaging
 */
export interface InvitationError {
  /**
   * Error type category
   * - invalid: 404, token not found or malformed
   * - expired: 410, draw already completed
   * - already-joined: 409, user already in group
   * - network: Connection or timeout error
   * - unknown: Unexpected server error
   */
  type: 'invalid' | 'expired' | 'already-joined' | 'network' | 'unknown';

  /** User-friendly error message in Polish */
  message: string;
}

/**
 * Form data for joining a group
 * Emitted by JoinGroupFormComponent on submission
 */
export interface JoinGroupFormData {
  /** Optional budget suggestion in PLN */
  budgetSuggestion: number | null;
}

// Re-export API types for convenience
export type InvitationDetailsDto = GetInvitationDetailsResponse;
export type AcceptInvitationRequestDto = AcceptInvitationRequest;
export type AcceptInvitationResponseDto = AcceptInvitationResponse;
