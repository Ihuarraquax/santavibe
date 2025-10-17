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
 * Track password strength requirements.
 * `level` represents overall strength, while `checks` shows which individual requirements are met.
 */
export interface PasswordStrengthResult {
  level: 'weak' | 'fair' | 'good' | 'strong';
  checks: {
    hasMinLength: boolean;
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
export interface ApiError {
  error: string;
  message: string;
  details?: Record<string, string[]>;
}

/**
 * Used by ErrorAlertComponent to display different types of alerts.
 */
export interface ErrorMessage {
  message: string;
  severity: 'error' | 'warning' | 'info';
}
