/**
 * Type definitions for the registration feature.
 * These types define the structure of data used in the registration flow.
 */

/**
 * Internal form state before submission.
 * Tracks current values of all form fields.
 */
export interface RegistrationFormData {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  gdprConsent: boolean;
}

/**
 * Track validation state for individual fields.
 * Includes whether the user has interacted with the field.
 */
export interface FieldValidationState {
  isValid: boolean;
  isTouched: boolean;
  errors: string[];
}

/**
 * Store all field-level validation errors in a structured format.
 * Each field can have multiple validation error messages.
 */
export interface FormValidationErrors {
  email?: string[];
  password?: string[];
  firstName?: string[];
  lastName?: string[];
  gdprConsent?: string[];
}

/**
 * Password strength levels enum.
 */
export enum PasswordStrength {
  WEAK = 'weak',
  MEDIUM = 'medium',
  STRONG = 'strong'
}

/**
 * Track password strength requirements.
 * Includes overall strength level, score percentage, and individual requirement checks.
 */
export interface PasswordStrengthResult {
  strength: PasswordStrength;
  score: number; // 0-100 percentage
  feedback: string; // Polish feedback text with friendly tone
  meetsRequirements: boolean; // All requirements satisfied
  requirements: {
    minLength: boolean;
    hasUppercase: boolean;
    hasLowercase: boolean;
    hasDigit: boolean;
    hasSpecialChar: boolean;
  };
}

/**
 * Structure for API error responses.
 * `details` contains field-specific validation errors from the backend.
 */
export interface ApiErrorResponse {
  error: string;
  message?: string;
  details?: Record<string, string[]>; // Field-mapped errors
  statusCode?: number;
}

/**
 * Used by ErrorAlertComponent to display different types of alerts.
 */
export interface ErrorMessage {
  message: string;
  severity: 'error' | 'warning' | 'info';
}
