import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * Custom validator to check maximum decimal places for a number input.
 * @param maxPlaces Maximum allowed decimal places
 * @returns Validator function
 */
export function decimalPlacesValidator(maxPlaces: number): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value && control.value !== 0) {
      return null; // Allow empty values
    }

    const stringValue = control.value.toString();
    const decimalIndex = stringValue.indexOf('.');

    if (decimalIndex === -1) {
      return null; // No decimal places
    }

    const decimalPlaces = stringValue.length - decimalIndex - 1;

    return decimalPlaces <= maxPlaces
      ? null
      : { decimalPlaces: { max: maxPlaces, actual: decimalPlaces } };
  };
}
