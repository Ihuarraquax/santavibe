# Registration View Implementation Plan

## 1. Overview

The Registration View is a public (unauthenticated) page that allows new users to create an account in the SantaVibe application. The view uses **Angular Reactive Forms** for robust form management and validation, with email/password authentication, personal information fields (first name, last name), GDPR consent checkbox, and real-time password strength feedback. Upon successful registration, users are automatically logged in and redirected to their intended destination or the groups list.

**Key Features:**
- Angular Reactive Forms with comprehensive validation
- Email and password registration with strong password requirements
- Real-time password strength indicator
- On-blur and real-time field validation with inline error messages
- GDPR consent requirement with link to terms
- Auto-login after successful registration
- Redirect to intended destination or groups list
- Mobile-first responsive design
- Polish language support with friendly, Christmas-themed tone

**Technical Approach:**
- Reactive Forms (FormGroup, FormControl, Validators)
- Signals for UI state and derived state from FormGroup
- OnPush change detection strategy
- Standalone components (no NgModules)
- `inject()` function for dependency injection
- Native control flow (`@if`, `@for`, `@switch`)

## 2. View Routing

**Path:** `/register`

**Route Configuration:**
```typescript
{
  path: 'register',
  component: RegistrationComponent,
  canActivate: [unauthenticatedGuard] // Redirect if already logged in
}
```

**Query Parameters:**
- `returnUrl` (optional): Destination URL after successful registration (e.g., `/invite/{token}`)

## 3. Component Structure

```
RegistrationComponent (smart container)
├── Page Header
│   ├── App Logo
│   ├── Page Title ("Dołącz do magii Świąt! 🎄")
│   └── Subtitle/Description
├── RegistrationFormComponent (main form container)
│   ├── Email Input Field (formControlName="email")
│   │   └── ErrorAlertComponent (conditional)
│   ├── Password Input Field (formControlName="password")
│   │   ├── Show/Hide Toggle Button
│   │   ├── PasswordStrengthIndicatorComponent
│   │   └── ErrorAlertComponent (conditional)
│   ├── First Name Input Field (formControlName="firstName")
│   │   └── ErrorAlertComponent (conditional)
│   ├── Last Name Input Field (formControlName="lastName")
│   │   └── ErrorAlertComponent (conditional)
│   ├── GDPR Consent Checkbox (formControlName="gdprConsent")
│   │   ├── Label with Link to Terms
│   │   └── ErrorAlertComponent (conditional)
│   ├── Submit Button (with loading state)
│   └── ErrorAlertComponent (form-level errors, conditional)
├── Navigation Links
│   └── Link to Login Page ("Masz już konto? Zaloguj się")
└── Footer Links (Terms, Privacy Policy)
```

## 4. Component Details

### 4.1 RegistrationComponent (Smart Container)

**Purpose:** Main container component that manages the registration page layout and orchestrates the registration flow.

**Main Elements:**
- Page header with branding and title
- RegistrationFormComponent (child component)
- Navigation links to login page
- Footer with legal links

**Handled Events:**
- Registration success from RegistrationFormComponent
- Navigation to login page

**Validation:** None (delegated to child components)

**Types:**
- `RegisterRequest` (DTO)
- `RegisterResponse` (DTO)

**Component Configuration:**
```typescript
@Component({
  selector: 'app-registration',
  imports: [RegistrationFormComponent, RouterLink],
  templateUrl: './registration.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RegistrationComponent {
  private router = inject(Router);

  navigateToLogin(): void {
    this.router.navigate(['/login']);
  }
}
```

### 4.2 RegistrationFormComponent (Smart Form Component)

**Purpose:** Manages registration form state using Angular Reactive Forms, validation, submission, and user interactions. Handles API integration for user registration.

**Main Elements:**
- FormGroup with FormControls for each field
- Email input (type="email", autofocus)
- Password input (type="password" or "text" based on visibility toggle)
- Password visibility toggle button
- PasswordStrengthIndicatorComponent
- First name input (type="text")
- Last name input (type="text")
- GDPR consent checkbox with linked label
- Submit button with loading state
- ErrorAlertComponent for form-level errors
- ErrorAlertComponent for each field (inline errors)

**Handled Events:**
1. Form value changes → Reactive Forms automatically updates
2. `blur` on each field → Mark as touched, trigger validation display
3. `click` on password visibility toggle → Toggle showPassword signal
4. `submit` on form → Validate and call API if valid
5. API success → Auto-login and redirect
6. API error → Map errors to form controls

**Validation Conditions (Detailed):**

1. **Email Field:**
   - Required: Must not be empty
   - Format: Must match email pattern
   - Validators: `[Validators.required, Validators.email]`
   - Custom async validator for uniqueness (optional, can be server-side only)
   - Error messages:
     - `required`: "Email jest wymagany ✉️"
     - `email`: "Nieprawidłowy format email"
     - Server error (409): "Ten email jest już zarejestrowany"

2. **Password Field:**
   - Required: Must not be empty
   - Custom validator for password complexity
   - Validators: `[Validators.required, Validators.minLength(8), passwordStrengthValidator]`
   - Requirements checked by custom validator:
     - Minimum 8 characters
     - At least one uppercase letter (A-Z)
     - At least one lowercase letter (a-z)
     - At least one digit (0-9)
     - At least one special character
   - Error messages:
     - `required`: "Hasło jest wymagane 🔒"
     - `minlength`: "Hasło musi mieć co najmniej 8 znaków"
     - `passwordStrength`: Custom error messages for each requirement

3. **First Name Field:**
   - Required: Must not be empty
   - Max length: 100 characters
   - Validators: `[Validators.required, Validators.maxLength(100)]`
   - Error messages:
     - `required`: "Imię jest wymagane"
     - `maxlength`: "Imię nie może przekraczać 100 znaków"

4. **Last Name Field:**
   - Required: Must not be empty
   - Max length: 100 characters
   - Validators: `[Validators.required, Validators.maxLength(100)]`
   - Error messages:
     - `required`: "Nazwisko jest wymagane"
     - `maxlength`: "Nazwisko nie może przekraczać 100 znaków"

5. **GDPR Consent Checkbox:**
   - Required: Must be checked (true)
   - Validators: `[Validators.requiredTrue]`
   - Error messages:
     - `required`: "Musisz zaakceptować regulamin, aby kontynuować 📜"

**Types:**
- `RegisterRequest` (DTO) - API request
- `RegisterResponse` (DTO) - API response
- `PasswordStrengthResult` (ViewModel) - Password strength data
- `ApiErrorResponse` (ViewModel) - API error structure

**Component Configuration:**
```typescript
@Component({
  selector: 'app-registration-form',
  imports: [
    ReactiveFormsModule,
    PasswordStrengthIndicatorComponent,
    ErrorAlertComponent,
    RouterLink
  ],
  templateUrl: './registration-form.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RegistrationFormComponent {
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private fb = inject(FormBuilder);

  // Reactive Form
  registrationForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8), passwordStrengthValidator()]],
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    gdprConsent: [false, [Validators.requiredTrue]]
  });

  // UI state signals
  showPassword = signal(false);
  isSubmitting = signal(false);
  formLevelError = signal<string | null>(null);

  // Computed signals from FormGroup
  passwordStrength = computed(() =>
    this.calculatePasswordStrength(this.registrationForm.get('password')?.value || '')
  );

  canSubmit = computed(() =>
    this.registrationForm.valid && !this.isSubmitting()
  );

  // Return URL from query params
  returnUrl = signal('/groups');

  constructor() {
    // Read returnUrl from query params
    const params = this.route.snapshot.queryParams;
    if (params['returnUrl']) {
      this.returnUrl.set(params['returnUrl']);
    }
  }

  // Helper methods for template
  getControl(name: string): FormControl {
    return this.registrationForm.get(name) as FormControl;
  }

  hasError(controlName: string, errorType: string): boolean {
    const control = this.getControl(controlName);
    return control.hasError(errorType) && (control.dirty || control.touched);
  }

  getErrorMessage(controlName: string): string {
    // Return appropriate error message based on validation errors
  }
}
```

**Outputs:**
```typescript
// Using output() function
registrationSuccess = output<RegisterResponse>();
```

### 4.3 PasswordStrengthIndicatorComponent (Presentational)

**Purpose:** Displays a visual indicator of password strength with color-coded feedback and textual description in Polish with friendly tone.

**Main Elements:**
- Progress bar or strength meter (visual representation)
- Strength label (Słabe/Średnie/Silne)
- Requirements checklist with emojis
- Friendly Christmas-themed messages

**Handled Events:** None (presentational only)

**Validation:** None (receives validation result as input)

**Types:**
- `PasswordStrengthResult` (input)

**Component Configuration:**
```typescript
@Component({
  selector: 'app-password-strength-indicator',
  imports: [],
  template: `
    @if (strengthResult()) {
      <div class="mt-2">
        <!-- Progress bar -->
        <div class="h-2 w-full bg-gray-200 rounded-full overflow-hidden">
          <div
            class="h-full transition-all duration-300"
            [class]="getStrengthColorClass()"
            [style.width.%]="strengthResult()!.score">
          </div>
        </div>

        <!-- Strength label -->
        <p class="text-sm mt-1" [class]="getStrengthTextClass()">
          {{ strengthResult()!.feedback }}
        </p>

        <!-- Requirements checklist -->
        <ul class="text-xs mt-2 space-y-1">
          <li [class]="strengthResult()!.requirements.minLength ? 'text-green-600' : 'text-gray-500'">
            {{ strengthResult()!.requirements.minLength ? '✅' : '⭕' }} Co najmniej 8 znaków
          </li>
          <li [class]="strengthResult()!.requirements.hasUppercase ? 'text-green-600' : 'text-gray-500'">
            {{ strengthResult()!.requirements.hasUppercase ? '✅' : '⭕' }} Wielka litera
          </li>
          <li [class]="strengthResult()!.requirements.hasLowercase ? 'text-green-600' : 'text-gray-500'">
            {{ strengthResult()!.requirements.hasLowercase ? '✅' : '⭕' }} Mała litera
          </li>
          <li [class]="strengthResult()!.requirements.hasDigit ? 'text-green-600' : 'text-gray-500'">
            {{ strengthResult()!.requirements.hasDigit ? '✅' : '⭕' }} Cyfra
          </li>
          <li [class]="strengthResult()!.requirements.hasSpecialChar ? 'text-green-600' : 'text-gray-500'">
            {{ strengthResult()!.requirements.hasSpecialChar ? '✅' : '⭕' }} Znak specjalny
          </li>
        </ul>
      </div>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PasswordStrengthIndicatorComponent {
  strengthResult = input.required<PasswordStrengthResult>();

  getStrengthColorClass(): string {
    const strength = this.strengthResult()?.strength;
    if (strength === PasswordStrength.WEAK) return 'bg-red-500';
    if (strength === PasswordStrength.MEDIUM) return 'bg-yellow-500';
    return 'bg-green-500';
  }

  getStrengthTextClass(): string {
    const strength = this.strengthResult()?.strength;
    if (strength === PasswordStrength.WEAK) return 'text-red-600';
    if (strength === PasswordStrength.MEDIUM) return 'text-yellow-600';
    return 'text-green-600';
  }
}
```

### 4.4 ErrorAlertComponent (Presentational)

**Purpose:** Displays validation errors inline (field-level) or as banners (form-level) with appropriate styling, icons, and friendly Polish messages.

**Main Elements:**
- Alert container with color-coded styling
- Error icon
- Error message text
- Optional dismiss button (for form-level errors)

**Handled Events:**
- `click` on dismiss button → Emit dismiss event (if dismissible)

**Validation:** None (displays provided errors)

**Types:**
- Error message string (input)

**Component Configuration:**
```typescript
@Component({
  selector: 'app-error-alert',
  imports: [],
  template: `
    <div
      [class]="type() === 'banner' ? 'p-4 mb-4 rounded-lg bg-red-50 border border-red-200' : 'mt-1 text-sm text-red-600'">
      @if (type() === 'banner') {
        <div class="flex items-start">
          <span class="text-red-400 mr-2">⚠️</span>
          <div class="flex-1">
            <p class="text-red-800">{{ message() }}</p>
          </div>
          @if (dismissible()) {
            <button
              type="button"
              (click)="dismiss.emit()"
              class="ml-auto text-red-400 hover:text-red-600">
              ✕
            </button>
          }
        </div>
      } @else {
        <p class="flex items-center">
          <span class="mr-1">⚠️</span>
          {{ message() }}
        </p>
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ErrorAlertComponent {
  message = input.required<string>();
  type = input<'inline' | 'banner'>('inline');
  dismissible = input(false);

  dismiss = output<void>();
}
```

## 5. Types

### 5.1 DTOs (From API)

```typescript
// API Request DTO
export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  gdprConsent?: boolean; // Should always be true when sent
}

// API Response DTO
export interface RegisterResponse {
  userId: string | null;
  email: string | null;
  firstName: string | null;
  lastName: string | null;
  token: string | null;
  expiresAt?: string;
}
```

### 5.2 ViewModels (Custom Types)

```typescript
// Password strength levels
export enum PasswordStrength {
  WEAK = 'weak',
  MEDIUM = 'medium',
  STRONG = 'strong'
}

// Password strength result
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

// API error response structure
export interface ApiErrorResponse {
  error: string;
  message?: string;
  details?: Record<string, string[]>; // Field-mapped errors
  statusCode?: number;
}
```

### 5.3 Custom Validators

```typescript
// Custom password strength validator
export function passwordStrengthValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value as string;

    if (!value) {
      return null; // Don't validate empty value (required validator handles this)
    }

    const errors: ValidationErrors = {};

    if (!/[A-Z]/.test(value)) {
      errors['missingUppercase'] = 'Hasło musi zawierać dużą literę';
    }

    if (!/[a-z]/.test(value)) {
      errors['missingLowercase'] = 'Hasło musi zawierać małą literę';
    }

    if (!/[0-9]/.test(value)) {
      errors['missingDigit'] = 'Hasło musi zawierać cyfrę';
    }

    if (!/[^A-Za-z0-9]/.test(value)) {
      errors['missingSpecialChar'] = 'Hasło musi zawierać znak specjalny (!@#$%^&*...)';
    }

    return Object.keys(errors).length > 0 ? errors : null;
  };
}
```

## 6. State Management

The Registration View uses **Angular Reactive Forms** for form state management and **Angular Signals** for UI state and derived values.

### 6.1 Form State (Reactive Forms)

```typescript
export class RegistrationFormComponent {
  private fb = inject(FormBuilder);

  // FormGroup manages all form state
  registrationForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [
      Validators.required,
      Validators.minLength(8),
      passwordStrengthValidator()
    ]],
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    gdprConsent: [false, [Validators.requiredTrue]]
  });
}
```

### 6.2 UI State (Signals)

```typescript
export class RegistrationFormComponent {
  // UI-only state
  showPassword = signal(false);
  isSubmitting = signal(false);
  formLevelError = signal<string | null>(null);
  returnUrl = signal('/groups');

  // Computed from FormGroup
  passwordStrength = computed(() => {
    const password = this.registrationForm.get('password')?.value || '';
    return this.calculatePasswordStrength(password);
  });

  canSubmit = computed(() =>
    this.registrationForm.valid && !this.isSubmitting()
  );
}
```

### 6.3 Service-Level State (AuthService)

```typescript
@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);

  // Authentication state (shared across app)
  currentUser = signal<User | null>(null);
  isAuthenticated = signal<boolean>(false);
  token = signal<string | null>(null);

  async register(data: RegisterRequest): Promise<RegisterResponse> {
    const response = await firstValueFrom(
      this.http.post<RegisterResponse>('/api/auth/register', data)
    );

    // Update auth state on success
    if (response.token) {
      this.token.set(response.token);
      this.currentUser.set({
        userId: response.userId!,
        email: response.email!,
        firstName: response.firstName!,
        lastName: response.lastName!
      });
      this.isAuthenticated.set(true);
      this.storeToken(response.token);
    }

    return response;
  }

  private storeToken(token: string): void {
    localStorage.setItem('auth_token', token);
  }
}
```

## 7. API Integration

### 7.1 Endpoint Details

**Endpoint:** `POST /api/auth/register`

**Request Type:** `RegisterRequest`
```typescript
{
  email: string;          // Required, valid email format
  password: string;       // Required, meets complexity requirements
  firstName: string;      // Required, max 100 chars
  lastName: string;       // Required, max 100 chars
  gdprConsent: boolean;   // Optional in DTO, but should be true
}
```

**Response Type:** `RegisterResponse`
```typescript
{
  userId: string | null;
  email: string | null;
  firstName: string | null;
  lastName: string | null;
  token: string | null;      // JWT token for authentication
  expiresAt?: string;        // Token expiration timestamp
}
```

**Success Status Codes:**
- `200 OK` or `201 Created` - Registration successful

**Error Status Codes:**
- `400 Bad Request` - Validation errors
- `409 Conflict` - Email already registered
- `429 Too Many Requests` - Rate limit exceeded
- `500 Internal Server Error` - Server error

### 7.2 Integration Implementation

```typescript
// In RegistrationFormComponent

async submitForm(): Promise<void> {
  // Mark all fields as touched to show validation errors
  this.registrationForm.markAllAsTouched();

  if (this.registrationForm.invalid) {
    return;
  }

  this.isSubmitting.set(true);
  this.formLevelError.set(null);

  try {
    const formValue = this.registrationForm.getRawValue();
    const request: RegisterRequest = {
      email: formValue.email!,
      password: formValue.password!,
      firstName: formValue.firstName!,
      lastName: formValue.lastName!,
      gdprConsent: formValue.gdprConsent!
    };

    const response = await this.authService.register(request);

    // Show success toast with Christmas theme
    this.toastService.show({
      message: 'Witaj w SantaVibe! 🎅 Twoje konto zostało utworzone!',
      type: 'success'
    });

    // Redirect to return URL or groups list
    await this.router.navigate([this.returnUrl()]);

  } catch (error) {
    this.handleApiError(error);
  } finally {
    this.isSubmitting.set(false);
  }
}

private handleApiError(error: any): void {
  const apiError = error as ApiErrorResponse;

  if (apiError.statusCode === 400 && apiError.details) {
    // Map field errors to form controls
    this.mapFieldErrors(apiError.details);
  } else if (apiError.statusCode === 409) {
    // Email already exists
    this.registrationForm.get('email')?.setErrors({
      serverError: 'Ten email jest już zarejestrowany'
    });
  } else if (apiError.statusCode === 429) {
    // Rate limit
    this.formLevelError.set('Zbyt wiele prób rejestracji. Spróbuj ponownie później. ⏰');
  } else {
    // Generic error
    this.formLevelError.set('Ups! Coś poszło nie tak. Spróbuj ponownie. 🎄');
  }
}

private mapFieldErrors(details: Record<string, string[]>): void {
  Object.entries(details).forEach(([field, errors]) => {
    const control = this.registrationForm.get(field);
    if (control) {
      control.setErrors({ serverError: errors[0] });
    }
  });
}
```

## 8. User Interactions

### 8.1 Field Interactions

| User Action | Reactive Forms Behavior | UI Feedback |
|-------------|-------------------------|-------------|
| Type in email field | FormControl value updates automatically | Value displayed in real-time |
| Leave email field | Control marked as touched | Validation errors shown if invalid |
| Type in password field | FormControl value updates, validators run | Strength indicator updates in real-time |
| Leave password field | Control marked as touched | Validation errors shown if invalid |
| Click show/hide password | `showPassword` signal toggles | Input type changes, icon changes |
| Type in firstName/lastName | FormControl value updates | Value displayed in real-time |
| Leave firstName/lastName | Control marked as touched | Validation errors shown if invalid |
| Check/uncheck GDPR | FormControl value updates | Checkbox state changes |
| Click submit button | `markAllAsTouched()` called, validation runs | If valid: loading state, if invalid: show all errors |

### 8.2 Form Submission Flow

1. **User clicks Submit**
2. `submitForm()` called
3. `markAllAsTouched()` marks all controls as touched
4. Validate form (`this.registrationForm.invalid` check)
5. If invalid:
   - All field errors displayed
   - Focus moved to first invalid field
   - Submit button remains enabled
6. If valid:
   - `isSubmitting` set to `true`
   - Submit button disabled
   - Loading spinner shown
   - API call made via `AuthService.register()`
7. **On Success:**
   - Token stored in AuthService
   - User state updated
   - Success toast shown (Christmas-themed)
   - Redirect to `returnUrl` or `/groups`
8. **On Error:**
   - `isSubmitting` set to `false`
   - Errors mapped to form controls or shown as form-level error
   - Error messages displayed
   - Submit button re-enabled

## 9. Conditions and Validation

### 9.1 Field-Level Validation with Reactive Forms

#### Email Field
**Validators:** `[Validators.required, Validators.email]`

**Error Messages:**
```typescript
getEmailErrorMessage(): string {
  const control = this.registrationForm.get('email');

  if (control?.hasError('required')) {
    return 'Email jest wymagany ✉️';
  }
  if (control?.hasError('email')) {
    return 'Nieprawidłowy format email';
  }
  if (control?.hasError('serverError')) {
    return control.getError('serverError');
  }
  return '';
}
```

#### Password Field
**Validators:** `[Validators.required, Validators.minLength(8), passwordStrengthValidator()]`

**Error Messages:**
```typescript
getPasswordErrorMessage(): string {
  const control = this.registrationForm.get('password');

  if (control?.hasError('required')) {
    return 'Hasło jest wymagane 🔒';
  }
  if (control?.hasError('minlength')) {
    return 'Hasło musi mieć co najmniej 8 znaków';
  }
  if (control?.hasError('missingUppercase')) {
    return 'Hasło musi zawierać dużą literę';
  }
  if (control?.hasError('missingLowercase')) {
    return 'Hasło musi zawierać małą literę';
  }
  if (control?.hasError('missingDigit')) {
    return 'Hasło musi zawierać cyfrę';
  }
  if (control?.hasError('missingSpecialChar')) {
    return 'Hasło musi zawierać znak specjalny';
  }
  return '';
}
```

#### First Name & Last Name Fields
**Validators:** `[Validators.required, Validators.maxLength(100)]`

**Error Messages:**
```typescript
getNameErrorMessage(field: 'firstName' | 'lastName'): string {
  const control = this.registrationForm.get(field);
  const label = field === 'firstName' ? 'Imię' : 'Nazwisko';

  if (control?.hasError('required')) {
    return `${label} jest wymagane`;
  }
  if (control?.hasError('maxlength')) {
    return `${label} nie może przekraczać 100 znaków`;
  }
  return '';
}
```

#### GDPR Consent
**Validators:** `[Validators.requiredTrue]`

**Error Message:**
```typescript
getGdprErrorMessage(): string {
  const control = this.registrationForm.get('gdprConsent');

  if (control?.hasError('required')) {
    return 'Musisz zaakceptować regulamin, aby kontynuować 📜';
  }
  return '';
}
```

### 9.2 Password Strength Calculation

```typescript
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
    hasUppercase: /[A-Z]/.test(password),
    hasLowercase: /[a-z]/.test(password),
    hasDigit: /[0-9]/.test(password),
    hasSpecialChar: /[^A-Za-z0-9]/.test(password)
  };

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
```

## 10. Error Handling

### 10.1 Template Error Display

**Field-Level Errors:**
```html
<!-- Email field -->
<div class="form-group">
  <label for="email" class="form-label">Email</label>
  <input
    id="email"
    type="email"
    formControlName="email"
    [class.border-red-500]="hasError('email', 'required') || hasError('email', 'email')"
    class="form-input"
    autofocus
  />
  @if (registrationForm.get('email')?.touched && registrationForm.get('email')?.invalid) {
    <app-error-alert
      [message]="getEmailErrorMessage()"
      type="inline"
    />
  }
</div>
```

**Form-Level Errors:**
```html
@if (formLevelError()) {
  <app-error-alert
    [message]="formLevelError()!"
    type="banner"
    [dismissible]="true"
    (dismiss)="formLevelError.set(null)"
  />
}
```

### 10.2 API Error Handling Strategies

#### 400 Bad Request - Validation Errors
```typescript
// Map server validation errors to form controls
private mapFieldErrors(details: Record<string, string[]>): void {
  Object.entries(details).forEach(([field, errors]) => {
    const control = this.registrationForm.get(field);
    if (control) {
      control.setErrors({ serverError: errors[0] });
      control.markAsTouched();
    }
  });
}
```

#### 409 Conflict - Email Already Exists
```typescript
this.registrationForm.get('email')?.setErrors({
  serverError: 'Ten email jest już zarejestrowany'
});
this.registrationForm.get('email')?.markAsTouched();
```

#### 429 Too Many Requests
```typescript
this.formLevelError.set('Zbyt wiele prób rejestracji. Poczekaj chwilę i spróbuj ponownie. ⏰');
```

#### 500 Internal Server Error
```typescript
this.formLevelError.set('Ups! Coś poszło nie tak po naszej stronie. Spróbuj ponownie za chwilę. 🎄');
```

## 11. Implementation Steps

### Step 1: Create Type Definitions
1. Create `src/app/features/auth/models/registration.models.ts`
2. Define all types:
   - `PasswordStrength` enum
   - `PasswordStrengthResult` interface
   - `ApiErrorResponse` interface
3. Import DTOs from generated API client:
   - `RegisterRequest`
   - `RegisterResponse`

### Step 2: Create Custom Validators
1. Create `src/app/features/auth/validators/password-strength.validator.ts`
2. Implement `passwordStrengthValidator()` function
3. Export validator for use in forms

### Step 3: Create Password Strength Utilities
1. Create `src/app/features/auth/utils/password-strength.utils.ts`
2. Implement `calculatePasswordStrength()` function
3. Export for use in components

### Step 4: Create AuthService
1. Create `src/app/core/services/auth.service.ts`
2. Add `@Injectable({ providedIn: 'root' })`
3. Use `inject()` for HttpClient
4. Implement authentication state signals:
   - `currentUser`
   - `isAuthenticated`
   - `token`
5. Implement `register()` method:
   - HTTP POST to `/api/auth/register`
   - Update state on success
   - Store token in localStorage
   - Return response or throw error
6. Implement token storage/retrieval methods
7. Implement error handling

### Step 5: Create Presentational Components

#### 5.1 ErrorAlertComponent
1. Create `src/app/shared/components/error-alert/error-alert.component.ts`
2. Set `changeDetection: ChangeDetectionStrategy.OnPush`
3. Do NOT set `standalone: true` (it's default in Angular 20)
4. Define inputs using `input()` function:
   - `message = input.required<string>()`
   - `type = input<'inline' | 'banner'>('inline')`
   - `dismissible = input(false)`
5. Define output using `output()` function:
   - `dismiss = output<void>()`
6. Implement inline template with Flowbite alert styling
7. Use `[class]` bindings (NOT `ngClass`)
8. Use `@if` for conditional rendering

#### 5.2 PasswordStrengthIndicatorComponent
1. Create `src/app/features/auth/components/password-strength-indicator/password-strength-indicator.component.ts`
2. Set `changeDetection: ChangeDetectionStrategy.OnPush`
3. Define input: `strengthResult = input.required<PasswordStrengthResult>()`
4. Implement inline template with:
   - Progress bar with `[style.width.%]` binding
   - Strength label with friendly Polish messages
   - Requirements checklist with emojis
5. Use `[class]` bindings for color-coding
6. Add helper methods for color classes

### Step 6: Create RegistrationFormComponent
1. Create `src/app/features/auth/components/registration-form/registration-form.component.ts`
2. Set `changeDetection: ChangeDetectionStrategy.OnPush`
3. Import required modules:
   - `ReactiveFormsModule`
   - `PasswordStrengthIndicatorComponent`
   - `ErrorAlertComponent`
   - `RouterLink`
4. Use `inject()` for all dependencies:
   - `AuthService`
   - `Router`
   - `ActivatedRoute`
   - `FormBuilder`
5. Create FormGroup with validators:
   ```typescript
   registrationForm = this.fb.group({
     email: ['', [Validators.required, Validators.email]],
     password: ['', [Validators.required, Validators.minLength(8), passwordStrengthValidator()]],
     firstName: ['', [Validators.required, Validators.maxLength(100)]],
     lastName: ['', [Validators.required, Validators.maxLength(100)]],
     gdprConsent: [false, [Validators.requiredTrue]]
   });
   ```
6. Define UI state signals:
   - `showPassword`
   - `isSubmitting`
   - `formLevelError`
   - `returnUrl`
7. Define computed signals:
   - `passwordStrength`
   - `canSubmit`
8. Implement methods:
   - `submitForm()`
   - `togglePasswordVisibility()`
   - `handleApiError()`
   - `mapFieldErrors()`
   - `getEmailErrorMessage()`, `getPasswordErrorMessage()`, etc.
   - `hasError(controlName, errorType)`
9. Define output:
   - `registrationSuccess = output<RegisterResponse>()`

### Step 7: Create RegistrationFormComponent Template
1. Create `registration-form.component.html`
2. Use `[formGroup]="registrationForm"` on form element
3. Use `formControlName` on each input
4. Implement structure:
   ```html
   <form [formGroup]="registrationForm" (ngSubmit)="submitForm()">
     <!-- Form-level error -->
     @if (formLevelError()) {
       <app-error-alert ... />
     }

     <!-- Email field -->
     <div class="form-group">
       <label for="email">Email</label>
       <input id="email" type="email" formControlName="email" autofocus />
       @if (registrationForm.get('email')?.touched && registrationForm.get('email')?.invalid) {
         <app-error-alert [message]="getEmailErrorMessage()" />
       }
     </div>

     <!-- Password field with toggle -->
     <div class="form-group">
       <label for="password">Hasło</label>
       <div class="relative">
         <input
           id="password"
           [type]="showPassword() ? 'text' : 'password'"
           formControlName="password"
         />
         <button
           type="button"
           (click)="showPassword.set(!showPassword())"
           class="absolute right-2 top-2">
           {{ showPassword() ? '👁️' : '👁️‍🗨️' }}
         </button>
       </div>
       <app-password-strength-indicator [strengthResult]="passwordStrength()" />
       @if (registrationForm.get('password')?.touched && registrationForm.get('password')?.invalid) {
         <app-error-alert [message]="getPasswordErrorMessage()" />
       }
     </div>

     <!-- First name, last name, GDPR similarly -->

     <!-- Submit button -->
     <button
       type="submit"
       [disabled]="!canSubmit()"
       class="btn btn-primary w-full">
       @if (isSubmitting()) {
         <span>Tworzenie konta... 🎅</span>
       } @else {
         <span>Utwórz konto 🎄</span>
       }
     </button>
   </form>
   ```
5. Use `@if` for all conditionals (NOT `*ngIf`)
6. Use `[class]` for dynamic classes (NOT `ngClass`)
7. Use `[style]` for dynamic styles (NOT `ngStyle`)
8. Apply Flowbite classes for styling

### Step 8: Create RegistrationComponent (Container)
1. Create `src/app/features/auth/pages/registration/registration.component.ts`
2. Set `changeDetection: ChangeDetectionStrategy.OnPush`
3. Import `RegistrationFormComponent` and `RouterLink`
4. Use `inject(Router)` for navigation
5. Create template:
   ```html
   <div class="min-h-screen flex items-center justify-center bg-gradient-to-br from-red-50 to-green-50">
     <div class="max-w-md w-full p-8 bg-white rounded-lg shadow-lg">
       <!-- Header -->
       <div class="text-center mb-8">
         <h1 class="text-3xl font-bold text-gray-800">Dołącz do magii Świąt! 🎄</h1>
         <p class="text-gray-600 mt-2">Stwórz konto i zacznij świąteczną zabawę</p>
       </div>

       <!-- Form -->
       <app-registration-form />

       <!-- Login link -->
       <p class="text-center mt-6 text-sm text-gray-600">
         Masz już konto?
         <a routerLink="/login" class="text-red-600 hover:text-red-700 font-medium">
           Zaloguj się
         </a>
       </p>

       <!-- Footer links -->
       <div class="text-center mt-8 text-xs text-gray-500">
         <a routerLink="/terms" class="hover:text-gray-700">Regulamin</a>
         <span class="mx-2">•</span>
         <a routerLink="/privacy" class="hover:text-gray-700">Polityka prywatności</a>
       </div>
     </div>
   </div>
   ```

### Step 9: Configure Routing
1. Update `src/app/app.routes.ts`
2. Add registration route:
   ```typescript
   export const routes: Routes = [
     {
       path: 'register',
       component: RegistrationComponent,
       canActivate: [unauthenticatedGuard]
     },
     // ... other routes
   ];
   ```
3. Create `unauthenticatedGuard` (functional guard):
   ```typescript
   export const unauthenticatedGuard: CanActivateFn = () => {
     const authService = inject(AuthService);
     const router = inject(Router);

     if (authService.isAuthenticated()) {
       return router.parseUrl('/groups');
     }
     return true;
   };
   ```

### Step 10: Style with Tailwind + Flowbite
1. Apply Flowbite form classes
2. Use mobile-first responsive design
3. Add validation state styling with `[class]` bindings
4. Add loading states
5. Ensure Christmas-themed colors (red, green, white)
6. Test on mobile devices

### Step 11: Testing
1. **Unit Tests:**
   - Test `passwordStrengthValidator()`
   - Test `calculatePasswordStrength()`
   - Test error message methods
2. **Component Tests:**
   - Test form validation
   - Test form submission
   - Test error display
   - Test API error handling
3. **Integration Tests:**
   - Test full registration flow
   - Test redirect logic
4. **Manual Testing:**
   - Test all validation scenarios
   - Test on mobile devices
   - Test keyboard navigation
   - Test accessibility

### Step 12: Accessibility
1. Add proper ARIA labels
2. Ensure keyboard navigation
3. Test with screen reader
4. Verify focus management
5. Check color contrast

### Step 13: Documentation
1. Add JSDoc comments
2. Document validators
3. Add README in auth feature folder

---

**End of Implementation Plan**
