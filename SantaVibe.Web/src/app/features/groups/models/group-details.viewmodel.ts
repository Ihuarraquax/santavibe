/**
 * View models for the Group Details view.
 * These models transform API DTOs into view-friendly structures with computed properties.
 */

/**
 * Participant view model with computed properties for display.
 */
export interface ParticipantViewModel {
  userId: string;
  firstName: string;
  lastName: string;
  fullName: string; // Computed: `${firstName} ${lastName}`
  joinedAt: Date; // Converted from ISO string
  hasWishlist: boolean;
  isOrganizer: boolean;
  canRemove: boolean; // Computed: !isOrganizer (cannot remove organizer)
}

/**
 * Draw validation view model with computed helper property.
 */
export interface DrawValidationViewModel {
  isValid: boolean;
  errors: string[];
  hasErrors: boolean; // Computed: errors.length > 0
}

/**
 * Assignment view model with computed recipient full name.
 */
export interface AssignmentViewModel {
  recipientId: string;
  recipientFirstName: string;
  recipientLastName: string;
  recipientFullName: string; // Computed: `${recipientFirstName} ${recipientLastName}`
  hasWishlist: boolean;
  budget: number; // From parent group data
}

/**
 * Exclusion rule view model with full names for display.
 */
export interface ExclusionRuleViewModel {
  ruleId: string; // UUID
  user1Id: string;
  user1Name: string; // Full name
  user2Id: string;
  user2Name: string; // Full name
}

/**
 * AI-generated gift suggestion.
 */
export interface GiftSuggestion {
  category: string; // e.g., "Książki", "Elektronika", "Dekoracje"
  itemName: string; // e.g., "Słuchawki bezprzewodowe"
  description: string; // Brief description of the gift
  approximatePrice: number; // Price in PLN
}

/**
 * Main group details view model.
 * Conditionally contains pre-draw or post-draw specific fields.
 */
export interface GroupDetailsViewModel {
  // Base information (always present)
  groupId: string;
  name: string;
  organizerName: string;
  isOrganizer: boolean;
  participantCount: number;
  createdAt: Date; // Converted from ISO string

  // State flags
  drawCompleted: boolean;

  // Conditional fields based on draw state
  budget: number | null; // null before draw, number after
  drawCompletedAt: Date | null; // null before draw, Date after

  // Pre-draw specific fields (only when drawCompleted = false)
  participants?: ParticipantViewModel[];
  exclusionRuleCount?: number;
  canDraw?: boolean;
  drawValidation?: DrawValidationViewModel;

  // Post-draw specific fields (only when drawCompleted = true)
  myAssignment?: AssignmentViewModel;
}
