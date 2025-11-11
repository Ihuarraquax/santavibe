import {
  Component,
  input,
  output,
  signal,
  computed,
  ChangeDetectionStrategy,
  effect
} from '@angular/core';
import {
  ProfileViewModel,
  UpdateProfileRequest,
  ProfileFormModel,
  ValidationErrors
} from '../../models/profile.types';

/**
 * ProfileInfoComponent (presentational component)
 * Displays profile information and handles user interactions for editing
 */
@Component({
  selector: 'app-profile-info',
  templateUrl: './profile-info.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: []
})
export class ProfileInfoComponent {
  // Inputs
  profile = input.required<ProfileViewModel>();
  isEditing = input.required<boolean>();
  isSaving = input<boolean>(false);

  // Outputs
  edit = output<void>();
  save = output<UpdateProfileRequest>();
  cancelled = output<void>();
  changePassword = output<void>();

  // Local state
  protected formModel = signal<ProfileFormModel>({ firstName: '', lastName: '' });
  protected validationErrors = signal<ValidationErrors>({});
  protected isFormValid = computed(() => Object.keys(this.validationErrors()).length === 0);

  constructor() {
    // Initialize form when entering edit mode
    effect(() => {
      if (this.isEditing()) {
        this.formModel.set({
          firstName: this.profile().firstName,
          lastName: this.profile().lastName
        });
        this.validationErrors.set({});
      }
    });
  }

  /**
   * Validate firstName field
   * @returns Error message or null if valid
   */
  private validateFirstName(value: string): string | null {
    const trimmed = value.trim();

    if (!trimmed) {
      return 'Imię jest wymagane';
    }

    if (value.length > 100) {
      return 'Imię może mieć maksymalnie 100 znaków';
    }

    return null;
  }

  /**
   * Validate lastName field
   * @returns Error message or null if valid
   */
  private validateLastName(value: string): string | null {
    const trimmed = value.trim();

    if (!trimmed) {
      return 'Nazwisko jest wymagane';
    }

    if (value.length > 100) {
      return 'Nazwisko może mieć maksymalnie 100 znaków';
    }

    return null;
  }

  /**
   * Validate entire form
   * @returns ValidationErrors object with all errors
   */
  private validateForm(): ValidationErrors {
    const errors: ValidationErrors = {};
    const form = this.formModel();

    const firstNameError = this.validateFirstName(form.firstName);
    if (firstNameError) {
      errors['firstName'] = firstNameError;
    }

    const lastNameError = this.validateLastName(form.lastName);
    if (lastNameError) {
      errors['lastName'] = lastNameError;
    }

    return errors;
  }

  /**
   * Handle field blur event - validate single field
   */
  protected onFieldBlur(fieldName: 'firstName' | 'lastName'): void {
    const errors = { ...this.validationErrors() };
    const value = this.formModel()[fieldName];

    const error = fieldName === 'firstName'
      ? this.validateFirstName(value)
      : this.validateLastName(value);

    if (error) {
      errors[fieldName] = error;
    } else {
      delete errors[fieldName];
    }

    this.validationErrors.set(errors);
  }

  /**
   * Handle edit button click
   */
  protected onEditClick(): void {
    this.edit.emit();
  }

  /**
   * Handle save button click
   */
  protected onSaveClick(): void {
    // Validate entire form
    const errors = this.validateForm();
    this.validationErrors.set(errors);

    // Prevent save if validation fails
    if (Object.keys(errors).length > 0) {
      return;
    }

    // Emit save event with trimmed values
    const request: UpdateProfileRequest = {
      firstName: this.formModel().firstName.trim(),
      lastName: this.formModel().lastName.trim()
    };

    this.save.emit(request);
  }

  /**
   * Handle cancel button click
   */
  protected onCancelClick(): void {
    this.validationErrors.set({});
    this.cancelled.emit();
  }

  /**
   * Handle change password button click
   */
  protected onChangePasswordClick(): void {
    this.changePassword.emit();
  }

  /**
   * Update field value and clear its validation error
   */
  protected updateField(fieldName: 'firstName' | 'lastName', value: string): void {
    const updated = { ...this.formModel(), [fieldName]: value };
    this.formModel.set(updated);

    // Clear error when user starts typing
    const errors = { ...this.validationErrors() };
    delete errors[fieldName];
    this.validationErrors.set(errors);
  }
}
