# View Implementation Plan: Registration View

## 1. Overview

The Registration View enables new users to create an account for the SantaVibe Secret Santa application. Users provide email, password, first name, last name, and GDPR consent. The view includes real-time password strength feedback, on-blur field validation, and comprehensive error handling. Upon successful registration, users are automatically logged in and redirected to the groups list or their intended destination.

## 2. View Routing

**Path:** `/register`

**Authentication Required:** No (public route)

**Access Control:** Redirect to `/groups` if user is already authenticated

## 3. Component Structure

```
RegistrationView (route component)
│
└── RegistrationFormComponent
    ├── EmailInputField
    ├── PasswordInputField
    │   └── PasswordStrengthIndicatorComponent
    ├── FirstNameInputField
    ├── LastNameInputField
    ├── GdprConsentCheckbox
    ├── ErrorAlertComponent
    └── SubmitButton
    └── LoginLinkComponent
```

## 4. Component Details

### 4.1 RegistrationView (Route Component)

**Component Description:**
Container component for the registration page. Handles routing logic and ensures unauthenticated access only.

**Main Elements:**
- Page wrapper with Tailwind styling
- RegistrationFormComponent as the main content
- Background styling and branding elements

**Handled Interactions:**
- None (delegates to child components)

**Handled Validation:**
- None (delegates to child components)

**Types:**
- None specific to this component

**Props:**
- None (route component)

---

### 4.2 RegistrationFormComponent

**Component Description:**
Main form component that manages registration state, validation, and API interaction. Orchestrates all child components and handles form submission flow.

**Main Elements:**
- `<form>` element with novalidate attribute
- EmailInputField component
- PasswordInputField component with PasswordStrengthIndicatorComponent
- FirstNameInputField component
- LastNameInputField component
- GdprConsentCheckbox component
- ErrorAlertComponent for API errors
- Submit button with loading state
- Link to login page

**Handled Interactions:**
- Form submission (prevent default, validate, call API)
- Field blur events (trigger validation for touched fields)
- Password input changes (update strength indicator)
- GDPR checkbox toggle
- Navigate to login page

**Handled Validation:**

1. **Email Field:**
   - Required: Field must not be empty
   - Format: Must match email regex `/^[^\s@]+@[^\s@]+\.[^\s@]+$/`
   - Max length: 256 characters
   - Error messages:
     - "Email is required"
     - "Please enter a valid email address"
     - "Email must be less than 256 characters"

2. **Password Field:**
   - Required: Field must not be empty
   - Min length: 8 characters
   - Must contain uppercase letter: `/[A-Z]/`
   - Must contain lowercase letter: `/[a-z]/`
   - Must contain digit: `/[0-9]/`
   - Must contain special character: `/[!@#$%^&*(),.?":{}|<>]/`
   - Error messages:
     - "Password is required"
     - "Must be at least 8 characters"
     - "Must contain at least one uppercase letter"
     - "Must contain at least one lowercase letter"
     - "Must contain at least one digit"
     - "Must contain at least one special character"

3. **First Name Field:**
   - Required: Field must not be empty
   - Max length: 100 characters
   - Trim whitespace before validation
   - Error messages:
     - "First name is required"
     - "First name must be less than 100 characters"

4. **Last Name Field:**
   - Required: Field must not be empty
   - Max length: 100 characters
   - Trim whitespace before validation
   - Error messages:
     - "Last name is required"
     - "Last name must be less than 100 characters"

5. **GDPR Consent:**
   - Required: Checkbox must be checked (true)
   - Error messages:
     - "You must accept the terms and conditions to register"

**Types:**
- `RegistrationFormData`
- `FormValidationErrors`
- `RegisterRequest` (DTO)
- `RegisterResponse` (DTO)
- `ApiError`

**Props:**
- None (standalone component)

---

### 4.3 PasswordStrengthIndicatorComponent

**Component Description:**
Visual component that displays real-time password strength feedback based on password requirements. Shows individual checks for each requirement and an overall strength level.

**Main Elements:**
- Container div with strength level indicator (color-coded bar or badge)
- List of requirement checks with icons (checkmark or X):
  - Minimum 8 characters
  - Uppercase letter
  - Lowercase letter
  - Digit
  - Special character
- Visual strength meter (weak/fair/good/strong)

**Handled Interactions:**
- Receives password value from parent
- Updates display reactively based on password changes

**Handled Validation:**
- Performs all password requirement checks
- Calculates overall strength level
- Does NOT prevent form submission (informational only)

**Types:**
- `PasswordStrengthResult`

**Props:**
- `password: Signal<string>` - Current password value to analyze

---

### 4.4 ErrorAlertComponent

**Component Description:**
Reusable component for displaying error messages (validation errors, API errors). Supports dismissible alerts and different severity levels.

**Main Elements:**
- Alert container with Flowbite alert styling
- Error icon
- Error message text
- Dismiss button (optional)

**Handled Interactions:**
- Dismiss button click (emits close event)

**Handled Validation:**
- None (display component only)

**Types:**
- `ErrorMessage` type with message and severity

**Props:**
- `error: Signal<string | null>` - Error message to display
- `dismissible: boolean` - Whether error can be dismissed (default: true)
- `severity: 'error' | 'warning' | 'info'` - Alert type (default: 'error')

---

### 4.5 Individual Input Field Components

**Component Description:**
Reusable text input components with integrated validation display. Each handles its own validation state presentation.

**Main Elements:**
- Label element
- Input element with Tailwind styling
- Error message container (shown when field is invalid and touched)
- Helper text (optional)

**Handled Interactions:**
- Input change (emit value)
- Blur event (emit touched state)

**Handled Validation:**
- Display validation errors passed from parent
- Show/hide errors based on touched state

**Types:**
- Field-specific value type (string)

**Props:**
- `value: Signal<string>` - Current field value
- `errors: Signal<string[]>` - Validation errors for this field
- `touched: Signal<boolean>` - Whether field has been blurred
- `label: string` - Field label text
- `type: string` - Input type (text, email, password)
- `placeholder: string` - Placeholder text
- `required: boolean` - Whether field is required

---

### 4.6 GdprConsentCheckbox

**Component Description:**
Checkbox component for GDPR consent with link to terms and conditions.

**Main Elements:**
- Checkbox input
- Label with embedded link to terms
- Error message container

**Handled Interactions:**
- Checkbox toggle (emit value change)
- Terms link click (open in new tab)

**Handled Validation:**
- Display error if not checked on submit attempt

**Types:**
- `boolean` for checked state

**Props:**
- `checked: Signal<boolean>` - Current checkbox state
- `error: Signal<string | null>` - Validation error message
- `termsUrl: string` - URL to terms and conditions page

## 5. Types

### 5.1 DTOs (from API)

**RegisterRequest:**
```typescript
export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  gdprConsent: boolean; // Must be true
}
```

**RegisterResponse:**
```typescript
export interface RegisterResponse {
  userId: string | null;
  email: string | null;
  firstName: string | null;
  lastName: string | null;
  token: string | null;
  expiresAt?: string;
}
```

### 5.2 ViewModels (New Types)

**RegistrationFormData:**
```typescript
interface RegistrationFormData {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  gdprConsent: boolean;
}
```
*Purpose:* Internal form state before submission. Tracks current values of all form fields.

**FieldValidationState:**
```typescript
interface FieldValidationState {
  isValid: boolean;
  isTouched: boolean;
  errors: string[];
}
```
*Purpose:* Track validation state for individual fields, including whether the user has interacted with the field.

**FormValidationErrors:**
```typescript
interface FormValidationErrors {
  email?: string[];
  password?: string[];
  firstName?: string[];
  lastName?: string[];
  gdprConsent?: string[];
}
```
*Purpose:* Store all field-level validation errors in a structured format. Each field can have multiple validation error messages.

**PasswordStrengthResult:**
```typescript
interface PasswordStrengthResult {
  level: 'weak' | 'fair' | 'good' | 'strong';
  checks: {
    hasMinLength: boolean;
    hasUppercase: boolean;
    hasLowercase: boolean;
    hasDigit: boolean;
    hasSpecialChar: boolean;
  };
}
```
*Purpose:* Track password strength requirements. `level` represents overall strength, while `checks` shows which individual requirements are met.

**ApiError:**
```typescript
interface ApiError {
  error: string;
  message: string;
  details?: Record<string, string[]>;
}
```
*Purpose:* Structure for API error responses. `details` contains field-specific validation errors from the backend.

**ErrorMessage:**
```typescript
interface ErrorMessage {
  message: string;
  severity: 'error' | 'warning' | 'info';
}
```
*Purpose:* Used by ErrorAlertComponent to display different types of alerts.

## 6. State Management

### 6.1 Component State (Signals)

The RegistrationFormComponent manages state using Angular signals:

**formData - WritableSignal<RegistrationFormData>**
- Initial value:
  ```typescript
  {
    email: '',
    password: '',
    firstName: '',
    lastName: '',
    gdprConsent: false
  }
  ```
- Updated on: Input change events
- Purpose: Store current form input values

**validationErrors - WritableSignal<FormValidationErrors>**
- Initial value: `{}`
- Updated on: Field blur, form submission, API errors
- Purpose: Store field-level validation errors

**passwordStrength - WritableSignal<PasswordStrengthResult>**
- Initial value:
  ```typescript
  {
    level: 'weak',
    checks: {
      hasMinLength: false,
      hasUppercase: false,
      hasLowercase: false,
      hasDigit: false,
      hasSpecialChar: false
    }
  }
  ```
- Updated on: Password input changes
- Purpose: Track password strength in real-time

**isSubmitting - WritableSignal<boolean>**
- Initial value: `false`
- Updated on: Form submission start/end
- Purpose: Track form submission state for loading indicators and button disabling

**apiError - WritableSignal<string | null>**
- Initial value: `null`
- Updated on: API error responses
- Purpose: Store API-level errors (like "Email already exists")

**touchedFields - WritableSignal<Set<string>>**
- Initial value: `new Set<string>()`
- Updated on: Field blur events
- Purpose: Track which fields have been blurred to control validation error display

### 6.2 Computed Signals

**isFormValid - computed()**
```typescript
const isFormValid = computed(() => {
  const data = formData();
  const errors = validationErrors();

  // All fields must have values
  const hasAllFields = data.email && data.password &&
    data.firstName && data.lastName && data.gdprConsent;

  // No validation errors
  const hasNoErrors = Object.keys(errors).length === 0;

  return hasAllFields && hasNoErrors;
});
```
- Purpose: Determine if entire form is valid for submit button state
- Used by: Submit button disabled state

### 6.3 Services

**AuthService** (injected via `inject()`)
- Methods:
  - `register(request: RegisterRequest): Observable<RegisterResponse>`
  - `saveAuthToken(token: string, expiresAt: string): void`
  - `setCurrentUser(user: { userId: string, email: string, firstName: string, lastName: string }): void`
- Purpose: Handle registration API calls and authentication state management

**Router** (injected via `inject()`)
- Purpose: Navigate to groups page or intended destination after successful registration

**ValidationService** (optional utility service)
- Methods:
  - `validateEmail(email: string): string[]`
  - `validatePassword(password: string): string[]`
  - `validateRequired(value: string, fieldName: string): string[]`
  - `validateMaxLength(value: string, maxLength: number, fieldName: string): string[]`
  - `calculatePasswordStrength(password: string): PasswordStrengthResult`
- Purpose: Centralize validation logic for reusability

## 7. API Integration

### 7.1 Endpoint Details

**Endpoint:** `POST /api/auth/register`

**Request Type:** `RegisterRequest`
```typescript
{
  email: string,
  password: string,
  firstName: string,
  lastName: string,
  gdprConsent: boolean
}
```

**Response Type:** `RegisterResponse` (on success - 201)
```typescript
{
  userId: string | null,
  email: string | null,
  firstName: string | null,
  lastName: string | null,
  token: string | null,
  expiresAt?: string
}
```

**Error Response Types:**
- 400 Bad Request: `ApiError` with validation details
- 409 Conflict: `ApiError` for email already exists
- 500 Server Error: Generic error

### 7.2 API Call Implementation

```typescript
// In RegistrationFormComponent
async onSubmit() {
  // Mark all fields as touched
  this.touchedFields.set(new Set(['email', 'password', 'firstName', 'lastName', 'gdprConsent']));

  // Validate all fields
  const errors = this.validateAllFields();
  if (Object.keys(errors).length > 0) {
    this.validationErrors.set(errors);
    return;
  }

  // Prepare request
  const request: RegisterRequest = {
    email: this.formData().email.trim(),
    password: this.formData().password,
    firstName: this.formData().firstName.trim(),
    lastName: this.formData().lastName.trim(),
    gdprConsent: this.formData().gdprConsent
  };

  // Submit
  this.isSubmitting.set(true);
  this.apiError.set(null);

  this.authService.register(request).subscribe({
    next: (response: RegisterResponse) => {
      // Save authentication token
      if (response.token && response.expiresAt) {
        this.authService.saveAuthToken(response.token, response.expiresAt);
      }

      // Set current user
      if (response.userId && response.email && response.firstName && response.lastName) {
        this.authService.setCurrentUser({
          userId: response.userId,
          email: response.email,
          firstName: response.firstName,
          lastName: response.lastName
        });
      }

      // Navigate to groups or intended destination
      this.router.navigate(['/groups']);
    },
    error: (error: HttpErrorResponse) => {
      this.isSubmitting.set(false);

      if (error.status === 400) {
        // Validation error - parse and display field errors
        const apiError = error.error as ApiError;
        if (apiError.details) {
          this.validationErrors.set(apiError.details as FormValidationErrors);
        } else {
          this.apiError.set(apiError.message || 'Validation failed. Please check your inputs.');
        }
      } else if (error.status === 409) {
        // Email already exists
        this.apiError.set('An account with this email already exists. Please log in or use a different email.');
      } else {
        // Generic error
        this.apiError.set('Unable to create account. Please try again later.');
      }
    },
    complete: () => {
      this.isSubmitting.set(false);
    }
  });
}
```

## 8. User Interactions

### 8.1 Email Field Interaction

**User Action:** Types in email field
- **On Input:** Update `formData.email` signal
- **On Blur:**
  - Add 'email' to `touchedFields`
  - Validate email format and requirements
  - Update `validationErrors.email` if invalid
  - Display errors below field if invalid

**Validation Triggered:**
- Email format check
- Required check
- Max length check (256 characters)

---

### 8.2 Password Field Interaction

**User Action:** Types in password field
- **On Input:**
  - Update `formData.password` signal
  - Calculate and update `passwordStrength` signal in real-time
  - Update PasswordStrengthIndicatorComponent display
- **On Blur:**
  - Add 'password' to `touchedFields`
  - Validate all password requirements
  - Update `validationErrors.password` if invalid
  - Display errors below field if invalid

**Validation Triggered:**
- Min 8 characters
- Has uppercase letter
- Has lowercase letter
- Has digit
- Has special character

---

### 8.3 First Name Field Interaction

**User Action:** Types in first name field
- **On Input:** Update `formData.firstName` signal
- **On Blur:**
  - Add 'firstName' to `touchedFields`
  - Trim whitespace
  - Validate required and max length
  - Update `validationErrors.firstName` if invalid
  - Display errors below field if invalid

**Validation Triggered:**
- Required check
- Max length check (100 characters)

---

### 8.4 Last Name Field Interaction

**User Action:** Types in last name field
- **On Input:** Update `formData.lastName` signal
- **On Blur:**
  - Add 'lastName' to `touchedFields`
  - Trim whitespace
  - Validate required and max length
  - Update `validationErrors.lastName` if invalid
  - Display errors below field if invalid

**Validation Triggered:**
- Required check
- Max length check (100 characters)

---

### 8.5 GDPR Checkbox Interaction

**User Action:** Clicks GDPR consent checkbox
- **On Change:**
  - Toggle `formData.gdprConsent` signal
  - Clear `validationErrors.gdprConsent` if now checked
  - Update submit button enabled state (via `isFormValid` computed signal)

**User Action:** Clicks terms link
- **On Click:**
  - Open terms page in new tab (target="_blank")
  - Maintain form data (no navigation in current tab)

**Validation Triggered:**
- Must be checked (true) for form submission

---

### 8.6 Form Submission Interaction

**User Action:** Clicks Submit button
- **Pre-submission:**
  - Check if already submitting (prevent double submit)
  - Mark all fields as touched
  - Run complete form validation
  - If validation fails:
    - Update `validationErrors` with all errors
    - Scroll to first error
    - Do not call API
- **Submission:**
  - Set `isSubmitting` to true (shows loading spinner, disables button)
  - Clear previous `apiError`
  - Prepare `RegisterRequest` from `formData`
  - Call `authService.register()`
- **Success Response:**
  - Save JWT token via `authService.saveAuthToken()`
  - Update auth state via `authService.setCurrentUser()`
  - Show success notification (optional toast)
  - Navigate to `/groups` or intended destination
- **Error Response:**
  - Set `isSubmitting` to false
  - Handle based on status code:
    - 400: Parse field errors, update `validationErrors`
    - 409: Set `apiError` to "Email already exists" message
    - Other: Set `apiError` to generic error message
  - Keep user on registration page to fix errors

---

### 8.7 Navigation Interactions

**User Action:** Clicks "Already have an account? Log in" link
- **On Click:**
  - Navigate to `/login` route
  - Form data is lost (no persistence in MVP)

## 9. Conditions and Validation

### 9.1 Field-Level Validation Rules

#### Email Field
**Conditions Checked:**
1. Required: `email.trim().length > 0`
2. Valid format: `/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)`
3. Max length: `email.length <= 256`

**When Validated:**
- On blur after first interaction
- On form submission

**UI State Impact:**
- Invalid: Red border, error messages displayed below field
- Valid: Default border, no error messages
- Untouched: Default state, no validation indicators

**Error Messages:**
- "Email is required" (if empty)
- "Please enter a valid email address" (if invalid format)
- "Email must be less than 256 characters" (if too long)

---

#### Password Field
**Conditions Checked:**
1. Required: `password.length > 0`
2. Min length: `password.length >= 8`
3. Has uppercase: `/[A-Z]/.test(password)`
4. Has lowercase: `/[a-z]/.test(password)`
5. Has digit: `/[0-9]/.test(password)`
6. Has special char: `/[!@#$%^&*(),.?":{}|<>]/.test(password)`

**When Validated:**
- Real-time strength check on every input
- Full validation on blur
- Full validation on form submission

**UI State Impact:**
- PasswordStrengthIndicatorComponent updates in real-time
- Each requirement shows checkmark (green) or X (red)
- Overall strength bar changes color (red → yellow → green)
- Invalid: Red border on blur, specific error messages
- Valid: Default border, all requirements met

**Error Messages:**
- "Password is required" (if empty)
- "Must be at least 8 characters"
- "Must contain at least one uppercase letter"
- "Must contain at least one lowercase letter"
- "Must contain at least one digit"
- "Must contain at least one special character"

---

#### First Name Field
**Conditions Checked:**
1. Required: `firstName.trim().length > 0`
2. Max length: `firstName.trim().length <= 100`

**When Validated:**
- On blur after first interaction
- On form submission

**UI State Impact:**
- Invalid: Red border, error messages displayed
- Valid: Default border, no error messages

**Error Messages:**
- "First name is required"
- "First name must be less than 100 characters"

---

#### Last Name Field
**Conditions Checked:**
1. Required: `lastName.trim().length > 0`
2. Max length: `lastName.trim().length <= 100`

**When Validated:**
- On blur after first interaction
- On form submission

**UI State Impact:**
- Invalid: Red border, error messages displayed
- Valid: Default border, no error messages

**Error Messages:**
- "Last name is required"
- "Last name must be less than 100 characters"

---

#### GDPR Consent Checkbox
**Conditions Checked:**
1. Must be true: `gdprConsent === true`

**When Validated:**
- On form submission only (not on blur)

**UI State Impact:**
- Submit button disabled if `!isFormValid` (which includes GDPR check)
- On submit attempt without consent: Red border around checkbox, error message
- When checked: Error clears, submit button enables (if other fields valid)

**Error Messages:**
- "You must accept the terms and conditions to register"

---

### 9.2 Form-Level Validation

**Overall Form Valid When:**
```typescript
computed(() => {
  const data = formData();
  const errors = validationErrors();

  return (
    data.email.trim() !== '' &&
    data.password !== '' &&
    data.firstName.trim() !== '' &&
    data.lastName.trim() !== '' &&
    data.gdprConsent === true &&
    Object.keys(errors).length === 0
  );
});
```

**UI State Impact:**
- Submit button enabled/disabled based on `isFormValid`
- Submit button shows loading spinner when `isSubmitting`
- Submit button text changes: "Create Account" → "Creating..." when submitting

---

### 9.3 API Validation

**Backend Re-validation:**
Even if client validation passes, backend performs same checks and may return:

**400 Bad Request - Validation Failed:**
```json
{
  "error": "ValidationError",
  "message": "Password does not meet security requirements",
  "details": {
    "password": ["Must contain at least one uppercase letter"]
  }
}
```
**Handling:** Parse `details` object and populate `validationErrors` signal with field-specific errors.

**409 Conflict - Email Already Exists:**
```json
{
  "error": "EmailAlreadyExists",
  "message": "An account with this email already exists"
}
```
**Handling:** Display error in ErrorAlertComponent at top of form. Suggest navigating to login page.

## 10. Error Handling

### 10.1 Client-Side Validation Errors

**Scenario:** User enters invalid data in any field

**Detection:**
- On blur for individual fields
- On submit for all fields

**Handling:**
1. Run field-specific validation functions
2. Populate `validationErrors` signal with error messages
3. Display errors below respective fields (only if field is touched)
4. Prevent form submission if any errors exist
5. Focus on first invalid field on submit attempt

**User Experience:**
- Errors appear immediately on blur for touched fields
- All errors shown on submit attempt
- Clear, specific error messages for each validation failure
- Red border around invalid fields

---

### 10.2 Email Already Exists (409 Conflict)

**Scenario:** User attempts to register with an email that's already in the system

**API Response:**
```json
{
  "error": "EmailAlreadyExists",
  "message": "An account with this email already exists"
}
```

**Handling:**
1. Set `apiError` signal to: "An account with this email already exists. Please log in or use a different email."
2. Display error in ErrorAlertComponent at top of form
3. Add dismissible action to close error
4. Optionally highlight email field
5. Provide link to login page

**User Experience:**
- Prominent error message at top of form
- Clear call-to-action: go to login or change email
- Error is dismissible
- Form remains filled (user can easily change email)

---

### 10.3 Server Validation Errors (400 Bad Request)

**Scenario:** Client validation passes but server validation fails (edge case)

**API Response:**
```json
{
  "error": "ValidationError",
  "message": "Validation failed",
  "details": {
    "email": ["Email format is invalid"],
    "password": ["Password is too weak"]
  }
}
```

**Handling:**
1. Parse `details` object from error response
2. Map API field names to component field names
3. Update `validationErrors` signal with server errors
4. Display errors below respective fields
5. Log discrepancy between client and server validation for debugging

**User Experience:**
- Field-level errors appear below inputs
- If no details available, show generic error at top
- User can correct issues and resubmit

---

### 10.4 Network Errors

**Scenario:** Request fails due to network connectivity issues

**Detection:** HttpClient error with status 0 or timeout

**Handling:**
1. Set `apiError` to: "Unable to connect. Please check your internet connection and try again."
2. Display error in ErrorAlertComponent
3. Allow user to retry submission
4. Log error for monitoring

**User Experience:**
- Clear error message about connectivity
- Form data preserved
- Submit button re-enabled for retry
- Consider showing "Retry" button

---

### 10.5 Server Errors (500 Internal Server Error)

**Scenario:** Backend encounters unexpected error

**API Response:**
```json
{
  "error": "InternalServerError",
  "message": "An unexpected error occurred"
}
```

**Handling:**
1. Set `apiError` to: "Something went wrong. Please try again later."
2. Display generic error (don't expose technical details)
3. Log full error for debugging
4. Allow user to retry

**User Experience:**
- Generic, user-friendly error message
- Form data preserved
- Option to retry
- No technical jargon

---

### 10.6 Rate Limiting

**Scenario:** Too many registration attempts from same IP/client

**API Response:**
```json
{
  "error": "TooManyRequests",
  "message": "Too many registration attempts. Please try again in 5 minutes."
}
```

**Handling:**
1. Set `apiError` to message from API
2. Display error with retry timeframe
3. Optionally disable submit button for specified duration
4. Log event

**User Experience:**
- Clear explanation of rate limiting
- Specific time to wait before retry
- Form data preserved

---

### 10.7 Token Storage Failure

**Scenario:** Registration succeeds but JWT token can't be saved to localStorage (rare edge case)

**Detection:** Exception when calling `authService.saveAuthToken()`

**Handling:**
1. Log error for debugging
2. Still consider registration successful
3. Redirect to login page with message: "Account created successfully. Please log in."
4. Don't show error to user (account was created)

**User Experience:**
- Success message with login prompt
- Seamless redirect to login
- User can immediately log in with new credentials

---

### 10.8 Error Display Strategy

**Priority Order:**
1. API errors (top of form) - highest priority, most visible
2. Field validation errors (below fields) - contextual, field-specific
3. Password strength indicator - informational, real-time feedback

**Dismissal:**
- API errors: Dismissible via close button
- Field errors: Auto-dismiss when field becomes valid
- Errors clear on successful submission

**Accessibility:**
- Use ARIA attributes for error announcements
- Associate error messages with form fields using `aria-describedby`
- Ensure errors are keyboard-navigable

## 11. Implementation Steps

### Step 1: Create Type Definitions
1. Create `src/app/features/auth/models/registration.types.ts`
2. Define all ViewModels:
   - `RegistrationFormData`
   - `FormValidationErrors`
   - `PasswordStrengthResult`
   - `ApiError`
   - `ErrorMessage`
3. Export all types

### Step 2: Create Validation Service
1. Create `src/app/features/auth/services/validation.service.ts`
2. Implement validation methods:
   - `validateEmail(email: string): string[]`
   - `validatePassword(password: string): string[]`
   - `validateRequired(value: string, fieldName: string): string[]`
   - `validateMaxLength(value: string, max: number, fieldName: string): string[]`
   - `calculatePasswordStrength(password: string): PasswordStrengthResult`
3. Write unit tests for each validation method

### Step 3: Create ErrorAlertComponent
1. Generate component: `ng generate component features/auth/components/error-alert`
2. Implement with standalone component syntax
3. Add props: `error`, `dismissible`, `severity`
4. Style with Tailwind + Flowbite alert classes
5. Implement dismiss functionality with output event
6. Add unit tests

### Step 4: Create PasswordStrengthIndicatorComponent
1. Generate component: `ng generate component features/auth/components/password-strength-indicator`
2. Implement with standalone component syntax
3. Add prop: `password` signal
4. Create computed signal for strength calculation
5. Implement UI with colored bar and requirement checklist
6. Style with Tailwind (red/yellow/green color progression)
7. Add unit tests for strength calculation

### Step 5: Create Input Field Components
1. Generate base input component: `ng generate component shared/components/form-input`
2. Implement props: `value`, `errors`, `touched`, `label`, `type`, `placeholder`, `required`
3. Implement two-way binding with signals
4. Add error display logic (show only if touched)
5. Style with Tailwind + Flowbite form classes
6. Create variants for email, password, text inputs
7. Add unit tests

### Step 6: Create GdprConsentCheckbox Component
1. Generate component: `ng generate component features/auth/components/gdpr-consent`
2. Implement props: `checked`, `error`, `termsUrl`
3. Implement checkbox with label containing terms link
4. Handle link click (target="_blank")
5. Style with Tailwind
6. Add unit tests

### Step 7: Implement AuthService Methods
1. Open `src/app/core/services/auth.service.ts`
2. Implement `register(request: RegisterRequest): Observable<RegisterResponse>`
   - Use HttpClient to POST to `/api/auth/register`
   - Add proper headers
   - Handle response and errors
3. Implement `saveAuthToken(token: string, expiresAt: string): void`
   - Store in localStorage
   - Update auth state signal
4. Implement `setCurrentUser(user: UserInfo): void`
   - Update current user signal
5. Write unit tests with mocked HttpClient

### Step 8: Create RegistrationFormComponent
1. Generate component: `ng generate component features/auth/pages/registration-form`
2. Set up signals:
   - `formData = signal<RegistrationFormData>(...)`
   - `validationErrors = signal<FormValidationErrors>({})`
   - `passwordStrength = signal<PasswordStrengthResult>(...)`
   - `isSubmitting = signal<boolean>(false)`
   - `apiError = signal<string | null>(null)`
   - `touchedFields = signal<Set<string>>(new Set())`
3. Set up computed signal:
   - `isFormValid = computed(() => { ... })`
4. Inject services:
   - `authService = inject(AuthService)`
   - `router = inject(Router)`
   - `validationService = inject(ValidationService)`
5. Implement methods:
   - `onFieldChange(field: string, value: string): void`
   - `onFieldBlur(field: string): void`
   - `validateField(field: string): string[]`
   - `validateAllFields(): FormValidationErrors`
   - `onPasswordChange(password: string): void` (updates strength)
   - `onSubmit(): void` (main submission logic)
6. Build template with all child components
7. Wire up event handlers and data bindings
8. Add unit tests

### Step 9: Create RegistrationView Route Component
1. Generate component: `ng generate component features/auth/pages/registration`
2. Import and use RegistrationFormComponent
3. Add page-level styling and layout
4. Implement auth guard to redirect if already logged in
5. Add unit tests

### Step 10: Configure Routing
1. Open `src/app/app.routes.ts`
2. Add route: `{ path: 'register', component: RegistrationView }`
3. Ensure route is not protected by auth guard
4. Test navigation to `/register`

### Step 11: Implement Form Styling
1. Apply Tailwind utility classes for responsive layout
2. Use Flowbite component styles for:
   - Form inputs
   - Buttons
   - Alerts
   - Checkboxes
3. Ensure mobile responsiveness
4. Add focus states and accessibility attributes
5. Test on different screen sizes

### Step 12: Implement API Error Handling
1. In RegistrationFormComponent `onSubmit()` error callback:
   - Handle 400 errors: parse and display field errors
   - Handle 409 errors: display "Email exists" message
   - Handle 500 errors: display generic error
   - Handle network errors: display connectivity message
2. Test each error scenario with mock API responses
3. Verify error messages display correctly

### Step 13: Add Success Flow
1. In RegistrationFormComponent `onSubmit()` success callback:
   - Extract token and user info from response
   - Call `authService.saveAuthToken()`
   - Call `authService.setCurrentUser()`
   - Show success notification (optional toast)
   - Navigate to `/groups` or returnUrl if present
2. Test complete registration flow end-to-end

### Step 14: Add Accessibility Features
1. Add ARIA labels to all form inputs
2. Associate error messages with inputs using `aria-describedby`
3. Ensure keyboard navigation works for entire form
4. Add `aria-live` regions for dynamic error messages
5. Test with screen reader
6. Verify tab order is logical

### Step 15: Testing and Refinement
1. Write integration tests for complete registration flow
2. Test all validation scenarios:
   - Each field's validation rules
   - Form-level validation
   - API error responses
3. Test user interactions:
   - Typing in fields
   - Blur events
   - Submit with invalid/valid data
4. Test edge cases:
   - Email already exists
   - Network failure
   - Server error
5. Perform manual testing on different browsers
6. Verify mobile responsiveness
7. Address any bugs or UX issues

### Step 16: Documentation and Code Review
1. Add JSDoc comments to all public methods
2. Document component interfaces
3. Create usage examples for reusable components
4. Submit for code review
5. Address feedback
6. Merge to main branch

---

**Estimated Implementation Time:** 12-16 hours for experienced Angular developer

**Key Dependencies:**
- AuthService with HTTP client configured
- Routing module set up
- Tailwind CSS and Flowbite configured
- API endpoint `/api/auth/register` functional

**Testing Checklist:**
- [ ] All validation rules work correctly
- [ ] Password strength indicator updates in real-time
- [ ] Form submits successfully with valid data
- [ ] Errors display correctly for invalid data
- [ ] API errors are handled gracefully
- [ ] Auto-login works after registration
- [ ] Navigation to groups page succeeds
- [ ] Mobile responsive design works
- [ ] Keyboard navigation functional
- [ ] Screen reader accessible
