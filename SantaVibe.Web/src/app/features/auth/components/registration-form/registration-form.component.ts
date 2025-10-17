import { Component, signal, computed, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormInputComponent } from '../form-input/form-input.component';
import { PasswordStrengthIndicatorComponent } from '../password-strength-indicator/password-strength-indicator.component';
import { GdprConsentCheckboxComponent } from '../gdpr-consent-checkbox/gdpr-consent-checkbox.component';
import { ErrorAlertComponent } from '../error-alert/error-alert.component';
import { ValidationService } from '../../services/validation.service';
import { AuthService } from '../../services/auth.service';
import { RegistrationFormData, FormValidationErrors } from '../../models/registration.types';
import { RegisterRequest } from '@api/model/register-request';

/**
 * Main registration form component that orchestrates all form fields and handles submission.
 * Manages form state, validation, and API integration.
 */
@Component({
  selector: 'app-registration-form',
  templateUrl: './registration-form.component.html',
  styleUrls: ['./registration-form.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    FormInputComponent,
    PasswordStrengthIndicatorComponent,
    GdprConsentCheckboxComponent,
    ErrorAlertComponent
  ]
})
export class RegistrationFormComponent {
  private validationService = inject(ValidationService);
  private authService = inject(AuthService);
  private router = inject(Router);

  /**
   * Form data signals for each field.
   */
  email = signal<string>('');
  password = signal<string>('');
  firstName = signal<string>('');
  lastName = signal<string>('');
  gdprConsent = signal<boolean>(false);

  /**
   * Touched state signals for each field.
   */
  emailTouched = signal<boolean>(false);
  passwordTouched = signal<boolean>(false);
  firstNameTouched = signal<boolean>(false);
  lastNameTouched = signal<boolean>(false);
  gdprConsentTouched = signal<boolean>(false);

  /**
   * Loading state signal.
   */
  isLoading = signal<boolean>(false);

  /**
   * General API error message signal.
   */
  apiError = signal<string | null>(null);

  /**
   * Success message signal.
   */
  successMessage = signal<string | null>(null);

  /**
   * Computed validation errors for each field.
   */
  emailErrors = computed(() => this.validationService.validateEmail(this.email()));
  passwordErrors = computed(() => this.validationService.validatePassword(this.password()));
  firstNameErrors = computed(() => this.validationService.validateFirstName(this.firstName()));
  lastNameErrors = computed(() => this.validationService.validateLastName(this.lastName()));
  gdprConsentErrors = computed(() => this.validationService.validateGdprConsent(this.gdprConsent()));

  /**
   * Computed overall form validity.
   */
  isFormValid = computed(() => {
    return (
      this.emailErrors().length === 0 &&
      this.passwordErrors().length === 0 &&
      this.firstNameErrors().length === 0 &&
      this.lastNameErrors().length === 0 &&
      this.gdprConsentErrors().length === 0
    );
  });

  /**
   * Handles email field blur event.
   */
  onEmailBlur(): void {
    this.emailTouched.set(true);
  }

  /**
   * Handles password field blur event.
   */
  onPasswordBlur(): void {
    this.passwordTouched.set(true);
  }

  /**
   * Handles first name field blur event.
   */
  onFirstNameBlur(): void {
    this.firstNameTouched.set(true);
  }

  /**
   * Handles last name field blur event.
   */
  onLastNameBlur(): void {
    this.lastNameTouched.set(true);
  }

  /**
   * Handles GDPR consent field blur event.
   */
  onGdprConsentBlur(): void {
    this.gdprConsentTouched.set(true);
  }

  /**
   * Handles form submission.
   * Validates all fields, calls API, and handles response.
   */
  onSubmit(): void {
    // Mark all fields as touched
    this.emailTouched.set(true);
    this.passwordTouched.set(true);
    this.firstNameTouched.set(true);
    this.lastNameTouched.set(true);
    this.gdprConsentTouched.set(true);

    // Clear previous messages
    this.apiError.set(null);
    this.successMessage.set(null);

    // Check if form is valid
    if (!this.isFormValid()) {
      this.apiError.set('Proszę poprawić błędy w formularzu przed wysłaniem');
      return;
    }

    // Prepare registration request
    const request: RegisterRequest = {
      email: this.email(),
      password: this.password(),
      firstName: this.firstName(),
      lastName: this.lastName(),
      gdprConsent: this.gdprConsent()
    };

    // Set loading state
    this.isLoading.set(true);

    // Call API
    this.authService.register(request).subscribe({
      next: (response) => {
        this.isLoading.set(false);
        this.successMessage.set('Rejestracja zakończona pomyślnie! Przekierowywanie...');

        // Redirect to dashboard after 2 seconds
        setTimeout(() => {
          this.router.navigate(['/dashboard']);
        }, 2000);
      },
      error: (error) => {
        this.isLoading.set(false);

        // Handle API errors
        if (error.details) {
          // Map API errors to form fields
          const formErrors = this.authService.mapApiErrorsToFormErrors(error);

          // Set field-specific errors (this would require refactoring to support backend errors)
          // For now, just display a general error message
          this.apiError.set(error.message || 'Wystąpił błąd podczas rejestracji');
        } else {
          this.apiError.set(error.message || 'Wystąpił błąd podczas rejestracji');
        }
      }
    });
  }

  /**
   * Dismisses the API error message.
   */
  dismissApiError(): void {
    this.apiError.set(null);
  }

  /**
   * Dismisses the success message.
   */
  dismissSuccessMessage(): void {
    this.successMessage.set(null);
  }

  /**
   * Navigates to the login page.
   */
  navigateToLogin(): void {
    this.router.navigate(['/login']);
  }
}
