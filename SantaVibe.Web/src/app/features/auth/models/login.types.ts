/**
 * Type definitions for the login feature.
 * API request/response types are imported from generated API client.
 */

/**
 * Internal form state before submission.
 * Tracks current values of login form fields.
 */
export interface LoginFormData {
  email: string;
  password: string;
}

/**
 * Authenticated user model stored in application state.
 * Used throughout app for authentication state.
 */
export interface AuthUser {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  token: string;
  expiresAt: Date; // Parsed Date object for easier comparison
}
