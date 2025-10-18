import { Component, ChangeDetectionStrategy, input, output, signal, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { LoginFormData } from '../../models/login.types';

/**
 * Presentational component for the login form.
 * Handles form validation, password visibility toggle, and emits form data to parent.
 */
@Component({
  selector: 'app-login-form',
  templateUrl: './login-form.component.html',
  styleUrls: ['./login-form.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, CommonModule]
})
export class LoginFormComponent {
  private formBuilder = inject(FormBuilder);

  /**
   * Loading state from parent component to disable form during submission.
   */
  isLoading = input<boolean>(false);

  /**
   * Emits validated form data when user submits the form.
   */
  formSubmit = output<LoginFormData>();

  /**
   * Controls password field visibility (show/hide toggle).
   */
  showPassword = signal<boolean>(false);

  /**
   * Reactive form for login with email and password fields.
   */
  loginForm = this.formBuilder.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
    password: ['', [Validators.required, Validators.maxLength(128)]]
  });

  /**
   * Handles form submission.
   * Validates form and emits data to parent if valid.
   */
  onSubmit(): void {
    // Mark all fields as touched to show validation errors
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    // Emit validated form data
    const formData: LoginFormData = {
      email: this.loginForm.value.email!,
      password: this.loginForm.value.password!
    };

    this.formSubmit.emit(formData);
  }

  /**
   * Toggles password visibility between masked and visible.
   */
  togglePasswordVisibility(): void {
    this.showPassword.update(v => !v);
  }

  /**
   * Gets validation error message for email field.
   * Returns null if field is valid or not touched.
   */
  get emailErrors(): string | null {
    const control = this.loginForm.get('email');
    if (!control?.touched || !control?.errors) return null;

    if (control.errors['required']) return 'Adres email jest wymagany';
    if (control.errors['email']) return 'Podaj prawidłowy adres email';
    if (control.errors['maxlength']) return 'Adres email jest za długi';

    return null;
  }

  /**
   * Gets validation error message for password field.
   * Returns null if field is valid or not touched.
   */
  get passwordErrors(): string | null {
    const control = this.loginForm.get('password');
    if (!control?.touched || !control?.errors) return null;

    if (control.errors['required']) return 'Hasło jest wymagane';
    if (control.errors['maxlength']) return 'Hasło jest za długie';

    return null;
  }

  /**
   * Checks if email field should show error styling.
   */
  get emailHasError(): boolean {
    const control = this.loginForm.get('email');
    return !!(control?.invalid && control?.touched);
  }

  /**
   * Checks if password field should show error styling.
   */
  get passwordHasError(): boolean {
    const control = this.loginForm.get('password');
    return !!(control?.invalid && control?.touched);
  }
}
