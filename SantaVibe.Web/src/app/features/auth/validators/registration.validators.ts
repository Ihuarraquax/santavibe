import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';
import { ValidationService } from '../services/validation.service';

/**
 * Custom validators for registration form fields.
 * Wraps ValidationService methods to work with Angular Reactive Forms.
 */

/**
 * Email validator that uses ValidationService.
 */
export function emailValidator(validationService: ValidationService): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const errors = validationService.validateEmail(control.value || '');
    return errors.length > 0 ? { email: errors } : null;
  };
}

/**
 * Password validator that uses ValidationService.
 */
export function passwordValidator(validationService: ValidationService): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const errors = validationService.validatePassword(control.value || '');
    return errors.length > 0 ? { password: errors } : null;
  };
}

/**
 * First name validator that uses ValidationService.
 */
export function firstNameValidator(validationService: ValidationService): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const errors = validationService.validateFirstName(control.value || '');
    return errors.length > 0 ? { firstName: errors } : null;
  };
}

/**
 * Last name validator that uses ValidationService.
 */
export function lastNameValidator(validationService: ValidationService): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const errors = validationService.validateLastName(control.value || '');
    return errors.length > 0 ? { lastName: errors } : null;
  };
}

/**
 * GDPR consent validator that uses ValidationService.
 */
export function gdprConsentValidator(validationService: ValidationService): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const errors = validationService.validateGdprConsent(control.value || false);
    return errors.length > 0 ? { gdprConsent: errors } : null;
  };
}
