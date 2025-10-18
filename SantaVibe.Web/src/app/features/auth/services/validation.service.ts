import { Injectable } from '@angular/core';
import { PasswordStrengthResult, PasswordStrength } from '../models/registration.types';

/**
 * Centralized validation service for reusable validation logic.
 * Provides methods for validating form fields and calculating password strength.
 */
@Injectable({
  providedIn: 'root'
})
export class ValidationService {
  /**
   * Email validation regex pattern.
   */
  private readonly emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

  /**
   * Password requirement regex patterns.
   */
  private readonly passwordRegex = {
    uppercase: /[A-Z]/,
    lowercase: /[a-z]/,
    digit: /[0-9]/,
    specialChar: /[!@#$%^&*(),.?":{}|<>]/
  };

  /**
   * Validates email field.
   * @param email - Email string to validate
   * @returns Array of error messages (empty if valid)
   */
  validateEmail(email: string): string[] {
    const errors: string[] = [];

    if (!email || email.trim().length === 0) {
      errors.push('Email jest wymagany');
      return errors;
    }

    if (!this.emailRegex.test(email)) {
      errors.push('Proszę podać prawidłowy adres email');
    }

    if (email.length > 256) {
      errors.push('Email musi mieć mniej niż 256 znaków');
    }

    return errors;
  }

  /**
   * Validates password field.
   * @param password - Password string to validate
   * @returns Array of error messages (empty if valid)
   */
  validatePassword(password: string): string[] {
    const errors: string[] = [];

    if (!password || password.length === 0) {
      errors.push('Hasło jest wymagane');
      return errors;
    }

    if (password.length < 8) {
      errors.push('Hasło musi mieć co najmniej 8 znaków');
    }

    if (!this.passwordRegex.uppercase.test(password)) {
      errors.push('Hasło musi zawierać co najmniej jedną wielką literę');
    }

    if (!this.passwordRegex.lowercase.test(password)) {
      errors.push('Hasło musi zawierać co najmniej jedną małą literę');
    }

    if (!this.passwordRegex.digit.test(password)) {
      errors.push('Hasło musi zawierać co najmniej jedną cyfrę');
    }

    if (!this.passwordRegex.specialChar.test(password)) {
      errors.push('Hasło musi zawierać co najmniej jeden znak specjalny');
    }

    return errors;
  }

  /**
   * Validates required field (generic).
   * @param value - Field value to validate
   * @param fieldName - Name of the field (for error message)
   * @returns Array of error messages (empty if valid)
   */
  validateRequired(value: string, fieldName: string): string[] {
    const errors: string[] = [];

    if (!value || value.trim().length === 0) {
      errors.push(`${fieldName} jest wymagane`);
    }

    return errors;
  }

  /**
   * Validates maximum length of a field.
   * @param value - Field value to validate
   * @param maxLength - Maximum allowed length
   * @param fieldName - Name of the field (for error message)
   * @returns Array of error messages (empty if valid)
   */
  validateMaxLength(value: string, maxLength: number, fieldName: string): string[] {
    const errors: string[] = [];

    if (value && value.trim().length > maxLength) {
      errors.push(`${fieldName} musi mieć mniej niż ${maxLength} znaków`);
    }

    return errors;
  }

  /**
   * Calculates password strength based on requirements met.
   * @param password - Password string to analyze
   * @returns PasswordStrengthResult with strength, score, feedback, and individual checks
   */
  calculatePasswordStrength(password: string): PasswordStrengthResult {
    if (!password) {
      return {
        strength: PasswordStrength.WEAK,
        score: 0,
        feedback: 'Wprowadź hasło',
        meetsRequirements: false,
        requirements: {
          minLength: false,
          hasUppercase: false,
          hasLowercase: false,
          hasDigit: false,
          hasSpecialChar: false
        }
      };
    }

    const requirements = {
      minLength: password.length >= 8,
      hasUppercase: this.passwordRegex.uppercase.test(password),
      hasLowercase: this.passwordRegex.lowercase.test(password),
      hasDigit: this.passwordRegex.digit.test(password),
      hasSpecialChar: this.passwordRegex.specialChar.test(password)
    };

    // Count how many requirements are met
    const satisfiedCount = Object.values(requirements).filter(Boolean).length;
    const score = (satisfiedCount / 5) * 100;

    let strength: PasswordStrength;
    let feedback: string;

    if (score < 60) {
      strength = PasswordStrength.WEAK;
      feedback = 'Słabe hasło 😟 Dodaj więcej znaków!';
    } else if (score < 100) {
      strength = PasswordStrength.MEDIUM;
      feedback = 'Niezłe hasło 😊 Jeszcze trochę!';
    } else {
      strength = PasswordStrength.STRONG;
      feedback = 'Świetne hasło! 🎉 Twoje dane są bezpieczne!';
    }

    return {
      strength,
      score,
      feedback,
      meetsRequirements: satisfiedCount === 5,
      requirements
    };
  }

  /**
   * Validates first name field.
   * @param firstName - First name to validate
   * @returns Array of error messages (empty if valid)
   */
  validateFirstName(firstName: string): string[] {
    const errors: string[] = [];

    const requiredErrors = this.validateRequired(firstName, 'Imię');
    if (requiredErrors.length > 0) {
      return requiredErrors;
    }

    const maxLengthErrors = this.validateMaxLength(firstName, 100, 'Imię');
    errors.push(...maxLengthErrors);

    return errors;
  }

  /**
   * Validates last name field.
   * @param lastName - Last name to validate
   * @returns Array of error messages (empty if valid)
   */
  validateLastName(lastName: string): string[] {
    const errors: string[] = [];

    const requiredErrors = this.validateRequired(lastName, 'Nazwisko');
    if (requiredErrors.length > 0) {
      return requiredErrors;
    }

    const maxLengthErrors = this.validateMaxLength(lastName, 100, 'Nazwisko');
    errors.push(...maxLengthErrors);

    return errors;
  }

  /**
   * Validates GDPR consent checkbox.
   * @param gdprConsent - Boolean indicating if consent is given
   * @returns Array of error messages (empty if valid)
   */
  validateGdprConsent(gdprConsent: boolean): string[] {
    const errors: string[] = [];

    if (!gdprConsent) {
      errors.push('Musisz zaakceptować regulamin, aby się zarejestrować');
    }

    return errors;
  }
}
