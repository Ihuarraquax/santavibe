/**
 * Profile-related type definitions for the Profile feature.
 * These types extend the API DTOs with view-specific models.
 */

// Re-export API types for convenience
export type {
  GetProfileResponse,
  UpdateProfileRequest,
  UpdateProfileResponse
} from '../../../../api/model/models';

/**
 * View model for displaying profile information in the UI
 * Includes formatted dates for display in Polish locale
 */
export interface ProfileViewModel {
  /** Unique identifier for the user */
  userId: string;

  /** User's email address (always read-only in UI) */
  email: string;

  /** User's first name (editable) */
  firstName: string;

  /** User's last name (editable) */
  lastName: string;

  /** Formatted creation date for display (e.g., "1 stycznia 2025, 10:00") */
  createdAt: string;

  /** Formatted last login date for display */
  lastLoginAt: string;

  /** Raw Date object for createdAt (for potential future use) */
  createdAtRaw: Date;

  /** Raw Date object for lastLoginAt (for potential future use) */
  lastLoginAtRaw: Date;
}

/**
 * Form model for managing editable profile fields
 * Used internally in ProfileInfoComponent during edit mode
 */
export interface ProfileFormModel {
  /** First name field value */
  firstName: string;

  /** Last name field value */
  lastName: string;
}

/**
 * Type for storing field-level validation errors
 * Key is the field name, value is the error message
 *
 * Example:
 * {
 *   firstName: "Imię jest wymagane",
 *   lastName: "Nazwisko może mieć maksymalnie 100 znaków"
 * }
 */
export type ValidationErrors = Record<string, string>;
