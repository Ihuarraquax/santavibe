import { Component, inject, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { AuthService } from '../../services/auth.service';
import { ValidationService } from '../../services/validation.service';
import { PasswordStrengthIndicatorComponent } from '../password-strength-indicator/password-strength-indicator.component';
import { ErrorAlertComponent } from '../../../../shared/components/error-alert/error-alert.component';
import { RegisterRequest } from '@api/model/register-request';
import { ErrorResponse } from '@api/model/error-response';

/**
 * Smart form component that manages registration form state using Angular Reactive Forms.
 * Handles validation, submission, and user interactions for the registration flow.
 */
@Component({
  selector: 'app-registration-form',
  templateUrl: './registration-form.component.html',
  styleUrls: ['./registration-form.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    PasswordStrengthIndicatorComponent,
    ErrorAlertComponent,
    RouterLink
  ]
})
export class RegistrationFormComponent {
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private fb = inject(FormBuilder);
  private validationService = inject(ValidationService);

  // Reactive Form with validators
  registrationForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    gdprConsent: [false, [Validators.requiredTrue]]
  });

  // UI state signals
  showPassword = signal(false);
  isSubmitting = signal(false);
  formLevelError = signal<string | null>(null);
  returnUrl = signal('/groups');

  // Convert form status to signal for reactivity
  formStatus = toSignal(this.registrationForm.statusChanges, {
    initialValue: this.registrationForm.status
  });

  // Computed signals
  passwordStrength = computed(() => {
    const password = this.registrationForm.get('password')?.value || '';
    return this.validationService.calculatePasswordStrength(password);
  });

  canSubmit = computed(() => {
    // Depend on formStatus signal to make this reactive
    const status = this.formStatus();
    return status === 'VALID' && !this.isSubmitting();
  });

  constructor() {
    // Read returnUrl from query params
    const params = this.route.snapshot.queryParams;
    if (params['returnUrl']) {
      this.returnUrl.set(params['returnUrl']);
    }
  }

  /**
   * Toggles password visibility.
   */
  togglePasswordVisibility(): void {
    this.showPassword.set(!this.showPassword());
  }

  /**
   * Handles form submission.
   */
  submitForm(): void {
    // Mark all fields as touched to show validation errors
    this.registrationForm.markAllAsTouched();

    if (this.registrationForm.invalid) {
      return;
    }

    this.isSubmitting.set(true);
    this.formLevelError.set(null);

    const formValue = this.registrationForm.getRawValue();
    const request: RegisterRequest = {
      email: formValue.email!,
      password: formValue.password!,
      firstName: formValue.firstName!,
      lastName: formValue.lastName!,
      gdprConsent: formValue.gdprConsent!
    };

    // Subscribe to the observable returned by register
    this.authService.register(request).subscribe({
      next: () => {
        // Registration successful
        this.isSubmitting.set(false);
        // Navigate to return URL or groups list
        this.router.navigate([this.returnUrl()]);
      },
      error: (error: ErrorResponse) => {
        this.handleApiError(error);
        this.isSubmitting.set(false);
      }
    });
  }

  /**
   * Handles API errors and maps them to form controls or displays form-level errors.
   */
  private handleApiError(error: ErrorResponse): void {
    if (error.details) {
      // Map field errors to form controls
      this.mapFieldErrors(error.details);
    } else if (error.error === 'EmailAlreadyExists' || error.message?.includes('email')) {
      // Email already exists
      const emailControl = this.registrationForm.get('email');
      if (emailControl) {
        emailControl.setErrors({
          serverError: 'Ten email jest już zarejestrowany'
        });
        emailControl.markAsTouched();
        emailControl.updateValueAndValidity({ onlySelf: false });
      }
    } else if (error.message?.includes('rate limit') || error.message?.includes('zbyt wiele')) {
      // Rate limit
      this.formLevelError.set('Zbyt wiele prób rejestracji. Spróbuj ponownie później. ⏰');
    } else {
      // Generic error
      this.formLevelError.set(error.message || 'Ups! Coś poszło nie tak. Spróbuj ponownie. 🎄');
    }
  }

  /**
   * Maps API field errors to form controls.
   */
  private mapFieldErrors(details: Record<string, string[]>): void {
    Object.entries(details).forEach(([field, errors]) => {
      // Normalize field name (lowercase first letter)
      const normalizedField = field.charAt(0).toLowerCase() + field.slice(1);
      const control = this.registrationForm.get(normalizedField);
      if (control && errors.length > 0) {
        control.setErrors({ serverError: errors[0] });
        control.markAsTouched();
        control.updateValueAndValidity({ onlySelf: false });
      }
    });
  }

  /**
   * Gets error message for email field.
   */
  getEmailErrorMessage(): string {
    const control = this.registrationForm.get('email');
    if (!control || !control.touched) return '';

    if (control.hasError('required')) {
      return 'Email jest wymagany ✉️';
    }

    if (control.hasError('email')) {
      return 'Nieprawidłowy format email';
    }

    if (control.hasError('serverError')) {
      return control.getError('serverError');
    }

    return '';
  }

  /**
   * Gets error message for password field.
   */
  getPasswordErrorMessage(): string {
    const control = this.registrationForm.get('password');
    if (!control || !control.touched) return '';

    if (control.hasError('required')) {
      return 'Hasło jest wymagane 🔒';
    }

    if (control.hasError('minlength')) {
      return 'Hasło musi mieć co najmniej 8 znaków';
    }

    if (control.hasError('missingUppercase')) {
      return 'Hasło musi zawierać wielką literę';
    }

    if (control.hasError('missingLowercase')) {
      return 'Hasło musi zawierać małą literę';
    }

    if (control.hasError('missingDigit')) {
      return 'Hasło musi zawierać cyfrę';
    }

    if (control.hasError('missingSpecialChar')) {
      return 'Hasło musi zawierać znak specjalny';
    }

    if (control.hasError('serverError')) {
      return control.getError('serverError');
    }

    return '';
  }

  /**
   * Gets error message for first name field.
   */
  getFirstNameErrorMessage(): string {
    const control = this.registrationForm.get('firstName');
    if (!control || !control.touched) return '';

    if (control.hasError('required')) {
      return 'Imię jest wymagane';
    }

    if (control.hasError('maxlength')) {
      return 'Imię nie może przekraczać 100 znaków';
    }

    if (control.hasError('serverError')) {
      return control.getError('serverError');
    }

    return '';
  }

  /**
   * Gets error message for last name field.
   */
  getLastNameErrorMessage(): string {
    const control = this.registrationForm.get('lastName');
    if (!control || !control.touched) return '';

    if (control.hasError('required')) {
      return 'Nazwisko jest wymagane';
    }

    if (control.hasError('maxlength')) {
      return 'Nazwisko nie może przekraczać 100 znaków';
    }

    if (control.hasError('serverError')) {
      return control.getError('serverError');
    }

    return '';
  }

  /**
   * Gets error message for GDPR consent field.
   */
  getGdprErrorMessage(): string {
    const control = this.registrationForm.get('gdprConsent');
    if (!control || !control.touched) return '';

    if (control.hasError('required')) {
      return 'Musisz zaakceptować regulamin, aby kontynuować 📜';
    }

    if (control.hasError('serverError')) {
      return control.getError('serverError');
    }

    return '';
  }

  /**
   * Checks if a field has an error and has been touched.
   */
  hasFieldError(fieldName: string): boolean {
    const control = this.registrationForm.get(fieldName);
    return !!(control && control.invalid && control.touched);
  }

  /**
   * Dismisses form-level error.
   */
  dismissFormError(): void {
    this.formLevelError.set(null);
  }
}
