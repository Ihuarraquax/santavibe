# View Implementation Plan: Login

## 1. Overview

The Login View provides authentication functionality for existing users to access the SantaVibe application. This view implements a secure login form with email and password fields, handles authentication via JWT tokens, and redirects users to their dashboard upon successful login. The implementation prioritizes security (no email enumeration), usability (password visibility toggle, clear error messages), and mobile responsiveness.

**Key Objectives:**
- Authenticate users with email and password
- Store JWT token for session management
- Redirect to dashboard or return URL after successful login
- Provide clear, secure error messages
- Ensure mobile-optimized user experience

## 2. View Routing

**Path:** `/login`

**Route Configuration:**
```typescript
{
  path: 'login',
  component: LoginViewComponent,
  canActivate: [guestGuard] // Redirect to dashboard if already authenticated
}
```

**Query Parameters:**
- `returnUrl` (optional): URL to redirect after successful login

## 3. Component Structure

```
LoginViewComponent (Smart Component - Page Container)
├── LoginFormComponent (Presentational Component)
│   ├── Email Input (native with Flowbite styling)
│   ├── Password Input with Toggle (native with Flowbite styling)
│   └── Submit Button (Flowbite button)
├── ErrorAlertComponent (Presentational Component)
└── PageFooterComponent (Shared Component)
    └── GDPR/Privacy Links
```

**Component Hierarchy:**
- **LoginViewComponent**: Container component managing state, API calls, and navigation
- **LoginFormComponent**: Presentational component for form UI and user input
- **ErrorAlertComponent**: Reusable component for displaying error messages
- **PageFooterComponent**: Shared footer with legal/privacy links

## 4. Component Details

### 4.1 LoginViewComponent (Smart Component)

**Component Description:**
Main container component that orchestrates the login flow. Manages authentication state, handles API communication via AuthService, processes form submission, displays errors, and handles post-login navigation.

**Main Elements:**
- Container div with responsive layout (centered card on desktop, full-width on mobile)
- App logo/branding header
- LoginFormComponent
- ErrorAlertComponent (conditionally rendered)
- Link to registration page
- PageFooterComponent

**Handled Interactions:**
- `onFormSubmit(formData: LoginFormData)`: Validates form, calls AuthService.login(), handles success/error
- `onNavigateToRegister()`: Navigates to registration page
- Component initialization: Checks if user already authenticated, extracts returnUrl from query params

**Validation:**
- Check if user is already authenticated on component init (redirect if true)
- Verify returnUrl is a valid internal path (prevent open redirect)
- Ensure form data is present before API call

**Types:**
- `LoginFormData`: Form data structure
- `LoginRequest`: API request DTO
- `LoginResponse`: API response DTO
- `AuthUser`: Authenticated user model

**Props:**
None (route component)

**Injected Services:**
- `AuthService`: For authentication operations
- `Router`: For navigation
- `ActivatedRoute`: For accessing query parameters

**State (Signals):**
- `isLoading = signal<boolean>(false)`: Tracks form submission state
- `error = signal<string | null>(null)`: Stores current error message
- `returnUrl = signal<string>('/dashboard')`: Destination after successful login

### 4.2 LoginFormComponent (Presentational Component)

**Component Description:**
Presentational component that renders the login form with email and password fields. Implements form validation using Angular Reactive Forms, password visibility toggle, and emits form data to parent component on submission.

**Main Elements:**
- HTML `<form>` element with formGroup directive
- Email input field (type="email", formControlName="email")
  - Label: "Email address"
  - Flowbite input styling
  - Error message display area (shown on invalid + touched)
- Password input field with toggle button
  - Label: "Password"
  - Type toggles between "password" and "text"
  - Flowbite input styling
  - Show/hide icon button (eye icon)
  - Error message display area
- Submit button (Flowbite primary button)
  - Text: "Sign in"
  - Disabled when form invalid or loading
  - Shows loading spinner when submitting
- "Forgot password?" link (optional, can be disabled for MVP)

**Handled Interactions:**
- `onSubmit()`: Validates form, emits formSubmit event with LoginFormData
- `togglePasswordVisibility()`: Toggles showPassword signal
- Email/password input changes: Updates form state, clears errors on typing

**Validation (Detailed):**

**Email Field:**
- **Required**: Must not be empty
  - Error: "Email address is required"
  - Shown: On blur if touched and empty, or on submit attempt
- **Email Format**: Must match valid email pattern
  - Error: "Please enter a valid email address"
  - Shown: On blur if touched and invalid format, or on submit attempt
- **Max Length**: 254 characters (standard email max length)
  - Error: "Email address is too long"

**Password Field:**
- **Required**: Must not be empty
  - Error: "Password is required"
  - Shown: On blur if touched and empty, or on submit attempt
- **Min Length**: No minimum enforced at login (authentication validates correctness)
- **Max Length**: 128 characters (reasonable upper bound)
  - Error: "Password is too long"

**Form-Level:**
- Form is invalid if any field is invalid
- Submit button disabled when: `form.invalid || isLoading()`
- Mark all fields as touched on submit attempt to show all errors

**Types:**
- `LoginFormData`: `{ email: string; password: string }`
- `FormFieldError`: `{ field: 'email' | 'password'; message: string }`

**Props (Component Interface):**
```typescript
@Input() isLoading: boolean = false; // From parent, disables form during submission
@Output() formSubmit = new EventEmitter<LoginFormData>(); // Emits validated form data
```

**Template-Driven State (Signals):**
- `showPassword = signal<boolean>(false)`: Controls password input type

**Form Definition:**
```typescript
loginForm = this.formBuilder.group({
  email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
  password: ['', [Validators.required, Validators.maxLength(128)]]
});
```

### 4.3 ErrorAlertComponent (Presentational Component)

**Component Description:**
Reusable component for displaying error messages with consistent styling. Shows dismissible alert box with error icon and message. Used throughout the application for error feedback.

**Main Elements:**
- Alert container (Flowbite alert styling)
- Error icon (from Flowbite or Heroicons)
- Error message text
- Dismiss button (X icon)

**Handled Interactions:**
- `onDismiss()`: Emits dismiss event to parent

**Validation:**
None (display only)

**Types:**
- `AlertType`: `'error' | 'warning' | 'info' | 'success'`

**Props (Component Interface):**
```typescript
@Input() message: string = ''; // Error message to display
@Input() type: AlertType = 'error'; // Alert variant
@Input() dismissible: boolean = true; // Show dismiss button
@Output() dismiss = new EventEmitter<void>(); // Emitted when dismissed
```

### 4.4 PageFooterComponent (Shared Component)

**Component Description:**
Shared footer component displaying GDPR compliance and privacy policy links. Used across authentication pages and potentially other views.

**Main Elements:**
- Footer container (fixed or static at bottom)
- Navigation links:
  - "Privacy Policy" → `/privacy`
  - "Terms of Service" → `/terms`
  - Copyright notice

**Handled Interactions:**
- Link clicks: Navigate to respective pages using Angular Router

**Validation:**
None

**Types:**
None (static content)

**Props:**
None (static component)

## 5. Types

### 5.1 API Types (Already Defined)

**LoginRequest** (from `login-request.ts`):
```typescript
export interface LoginRequest {
  email: string;      // User's email address
  password: string;   // User's password (plain text, sent over HTTPS)
}
```

**LoginResponse** (from `login-response.ts`):
```typescript
export interface LoginResponse {
  userId: string | null;      // Unique user identifier (UUID)
  email: string | null;       // User's email address
  firstName: string | null;   // User's first name
  lastName: string | null;    // User's last name
  token: string | null;       // JWT authentication token
  expiresAt?: string;         // Token expiration timestamp (ISO 8601)
}
```

### 5.2 Custom ViewModels

**LoginFormData** (Form state interface):
```typescript
export interface LoginFormData {
  email: string;      // Email input value
  password: string;   // Password input value
}
```
**Usage**: Data structure for form values, emitted by LoginFormComponent to parent.

**AuthUser** (Authenticated user model):
```typescript
export interface AuthUser {
  userId: string;      // User's unique identifier
  email: string;       // User's email
  firstName: string;   // User's first name
  lastName: string;    // User's last name
  token: string;       // JWT token for API requests
  expiresAt: Date;     // Token expiration as Date object
}
```
**Usage**: Stored in AuthService after successful login. Used throughout app for authentication state.

**FormFieldError** (Validation error structure):
```typescript
export interface FormFieldError {
  field: 'email' | 'password' | 'general';  // Field identifier
  message: string;                          // Error message to display
}
```
**Usage**: Structure for displaying field-specific validation errors.

**ApiErrorResponse** (API error structure):
```typescript
export interface ApiErrorResponse {
  error: string;      // Error code (e.g., "InvalidCredentials")
  message: string;    // Human-readable error message
  statusCode?: number; // HTTP status code
}
```
**Usage**: Structure for parsing API error responses.

**AlertType** (Alert variant enum):
```typescript
export type AlertType = 'error' | 'warning' | 'info' | 'success';
```
**Usage**: Defines alert styling variants for ErrorAlertComponent.

## 6. State Management

### 6.1 Component-Level State (LoginViewComponent)

**Using Angular Signals:**

```typescript
// Loading state for form submission
isLoading = signal<boolean>(false);

// Current error message (null when no error)
error = signal<string | null>(null);

// Destination URL after successful login
returnUrl = signal<string>('/dashboard');
```

**State Updates:**
- `isLoading`: Set to `true` on form submit, `false` on API response (success or error)
- `error`: Set to error message on API failure, cleared on new submit attempt
- `returnUrl`: Extracted from query params on component init

### 6.2 Application-Level State (AuthService)

**Injectable Service with Signals:**

```typescript
@Injectable({ providedIn: 'root' })
export class AuthService {
  // Current authenticated user (null when not logged in)
  private currentUserSignal = signal<AuthUser | null>(null);

  // Public read-only signal
  currentUser = this.currentUserSignal.asReadonly();

  // Computed authentication status
  isAuthenticated = computed(() => this.currentUser() !== null);

  // Computed token expiration check
  isTokenExpired = computed(() => {
    const user = this.currentUser();
    if (!user) return true;
    return new Date() >= user.expiresAt;
  });
}
```

**State Persistence:**
- JWT token stored in `localStorage` with key `'auth_token'`
- User data stored in `localStorage` with key `'auth_user'`
- Token automatically loaded on app initialization
- Token included in HTTP requests via HttpInterceptor

**State Management Flow:**
1. User submits login form
2. LoginViewComponent calls `AuthService.login()`
3. AuthService makes API call
4. On success: Store token in localStorage, update `currentUserSignal`, return success
5. On error: Return error Observable
6. LoginViewComponent handles response, updates local state, navigates on success

### 6.3 Form State (LoginFormComponent)

**Using Angular Reactive Forms:**

```typescript
loginForm = this.formBuilder.group({
  email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
  password: ['', [Validators.required, Validators.maxLength(128)]]
});
```

**Form State Properties:**
- `loginForm.valid`: True when all validators pass
- `loginForm.invalid`: True when any validator fails
- `loginForm.touched`: True when any field has been touched
- `loginForm.dirty`: True when any field value has changed
- `loginForm.get('email')?.errors`: Email field validation errors
- `loginForm.get('password')?.errors`: Password field validation errors

**Local Component State (Signals):**
```typescript
showPassword = signal<boolean>(false); // Password visibility toggle
```

## 7. API Integration

### 7.1 Endpoint Details

**Endpoint:** `POST /api/auth/login`

**Authentication:** None required (public endpoint)

**Request Type:** `LoginRequest`
```typescript
{
  email: string;
  password: string;
}
```

**Response Type (Success):** `LoginResponse`
```typescript
{
  userId: string | null;
  email: string | null;
  firstName: string | null;
  lastName: string | null;
  token: string | null;
  expiresAt?: string;
}
```

**Error Responses:**
- **401 Unauthorized**: Invalid credentials
  ```typescript
  { error: "InvalidCredentials", message: "Invalid email or password" }
  ```
- **429 Too Many Requests**: Rate limiting
  ```typescript
  { error: "RateLimitExceeded", message: "Too many login attempts" }
  ```
- **500 Internal Server Error**: Server error
  ```typescript
  { error: "InternalServerError", message: "An error occurred" }
  ```

### 7.2 AuthService Implementation

**Service Method:**
```typescript
login(request: LoginRequest): Observable<LoginResponse> {
  return this.http.post<LoginResponse>('/api/auth/login', request)
    .pipe(
      tap(response => {
        if (response.token) {
          // Store token and user data
          this.storeToken(response.token);
          this.updateCurrentUser(response);
        }
      }),
      catchError(error => {
        // Transform error to user-friendly message
        const errorMessage = this.handleLoginError(error);
        return throwError(() => new Error(errorMessage));
      })
    );
}

private storeToken(token: string): void {
  localStorage.setItem('auth_token', token);
}

private updateCurrentUser(response: LoginResponse): void {
  if (response.userId && response.email && response.firstName && response.lastName && response.token) {
    const user: AuthUser = {
      userId: response.userId,
      email: response.email,
      firstName: response.firstName,
      lastName: response.lastName,
      token: response.token,
      expiresAt: response.expiresAt ? new Date(response.expiresAt) : new Date(Date.now() + 24 * 60 * 60 * 1000)
    };
    localStorage.setItem('auth_user', JSON.stringify(user));
    this.currentUserSignal.set(user);
  }
}

private handleLoginError(error: any): string {
  if (error.status === 401) {
    return 'Invalid email or password';
  } else if (error.status === 429) {
    return 'Too many login attempts. Please try again later.';
  } else if (error.status === 0) {
    return 'Connection error. Please check your internet connection.';
  } else {
    return 'An error occurred. Please try again later.';
  }
}
```

### 7.3 HTTP Interceptor for Token

**Purpose:** Automatically attach JWT token to API requests

```typescript
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = localStorage.getItem('auth_token');

  if (token) {
    const cloned = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
    return next(cloned);
  }

  return next(req);
};
```

### 7.4 Integration Flow in LoginViewComponent

```typescript
onFormSubmit(formData: LoginFormData): void {
  // Clear previous errors
  this.error.set(null);

  // Set loading state
  this.isLoading.set(true);

  // Prepare request
  const request: LoginRequest = {
    email: formData.email.toLowerCase().trim(), // Normalize email
    password: formData.password
  };

  // Call AuthService
  this.authService.login(request).subscribe({
    next: (response) => {
      // Success: Navigate to return URL or dashboard
      this.isLoading.set(false);
      this.router.navigate([this.returnUrl()]);
    },
    error: (error) => {
      // Error: Display error message
      this.isLoading.set(false);
      this.error.set(error.message);
    }
  });
}
```

## 8. User Interactions

### 8.1 Page Load

**Interaction:** User navigates to `/login`

**Behavior:**
1. Check if user is already authenticated (`authService.isAuthenticated()`)
2. If authenticated and token not expired: Redirect to `/dashboard`
3. If not authenticated: Display login form
4. Extract `returnUrl` from query params (default to `/dashboard`)
5. Auto-focus email input field (using `@ViewChild` and `nativeElement.focus()`)

**Implementation:**
```typescript
ngOnInit(): void {
  // Check authentication
  if (this.authService.isAuthenticated() && !this.authService.isTokenExpired()) {
    this.router.navigate(['/dashboard']);
    return;
  }

  // Extract return URL
  const returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/dashboard';
  this.returnUrl.set(returnUrl);
}

ngAfterViewInit(): void {
  // Auto-focus email field
  this.emailInput()?.nativeElement.focus();
}
```

### 8.2 Email Input

**Interaction:** User types in email field

**Behavior:**
1. Clear any previous error messages (`this.error.set(null)`)
2. Update form control value
3. On blur: Validate email format
4. Display validation error if invalid and touched

**Validation Display:**
- Show error below input field
- Error styling: Red border, red text
- Error shown when: Field is touched and invalid

### 8.3 Password Input

**Interaction:** User types in password field

**Behavior:**
1. Clear any previous error messages
2. Update form control value
3. Mask characters (type="password" by default)
4. On blur: Validate required
5. Display validation error if invalid and touched

### 8.4 Show/Hide Password Toggle

**Interaction:** User clicks eye icon button next to password field

**Behavior:**
1. Toggle `showPassword` signal value
2. Update password input type:
   - `showPassword = true`: type="text" (password visible)
   - `showPassword = false`: type="password" (password masked)
3. Update icon:
   - `showPassword = true`: Show "eye-slash" icon
   - `showPassword = false`: Show "eye" icon
4. Maintain cursor position in input field

**Accessibility:**
- Button has `aria-label="Toggle password visibility"`
- Button has `type="button"` (prevent form submission)

### 8.5 Form Submission

**Interaction:** User clicks "Sign in" button or presses Enter in form

**Behavior:**
1. Validate form (mark all fields as touched to show errors)
2. If form invalid: Stop submission, display field errors
3. If form valid:
   - Set `isLoading` to `true`
   - Disable form inputs and submit button
   - Clear previous error message
   - Normalize email (lowercase, trim whitespace)
   - Call `AuthService.login()` with form data
   - Show loading spinner on submit button

**Success Response:**
1. Store JWT token in localStorage
2. Update `currentUser` signal in AuthService
3. Set `isLoading` to `false`
4. Navigate to `returnUrl` (or `/dashboard`)

**Error Response:**
1. Set `isLoading` to `false`
2. Re-enable form
3. Display error message via ErrorAlertComponent
4. Keep email field populated, clear password field
5. Focus password field for retry

### 8.6 Navigate to Registration

**Interaction:** User clicks "Don't have an account? Sign up" link

**Behavior:**
1. Navigate to `/register` using Angular Router
2. Preserve `returnUrl` query parameter if present

**Implementation:**
```typescript
onNavigateToRegister(): void {
  const returnUrl = this.returnUrl();
  this.router.navigate(['/register'], {
    queryParams: returnUrl !== '/dashboard' ? { returnUrl } : {}
  });
}
```

### 8.7 Forgot Password Link

**Interaction:** User clicks "Forgot password?" link

**Behavior:**
- **MVP**: Display "Coming soon" message or tooltip
- **Future**: Navigate to `/forgot-password` page

### 8.8 Error Alert Dismissal

**Interaction:** User clicks X button on error alert

**Behavior:**
1. Clear error message (`this.error.set(null)`)
2. Hide ErrorAlertComponent
3. Re-enable form for retry

## 9. Conditions and Validation

### 9.1 Form Field Validation

#### Email Field Conditions

**Required Validation:**
- **Condition**: Email field is not empty
- **Component**: LoginFormComponent
- **When Checked**: On blur and on submit attempt
- **Error Message**: "Email address is required"
- **UI Effect**: Red border on input, error text below field, submit button disabled

**Email Format Validation:**
- **Condition**: Email matches valid email pattern (standard Angular email validator)
- **Component**: LoginFormComponent
- **When Checked**: On blur and on submit attempt
- **Error Message**: "Please enter a valid email address"
- **UI Effect**: Red border on input, error text below field, submit button disabled

**Max Length Validation:**
- **Condition**: Email length ≤ 254 characters
- **Component**: LoginFormComponent
- **When Checked**: On input (prevents further typing)
- **Error Message**: "Email address is too long"
- **UI Effect**: Red border on input, error text below field

#### Password Field Conditions

**Required Validation:**
- **Condition**: Password field is not empty
- **Component**: LoginFormComponent
- **When Checked**: On blur and on submit attempt
- **Error Message**: "Password is required"
- **UI Effect**: Red border on input, error text below field, submit button disabled

**Max Length Validation:**
- **Condition**: Password length ≤ 128 characters
- **Component**: LoginFormComponent
- **When Checked**: On input (prevents further typing)
- **Error Message**: "Password is too long"
- **UI Effect**: Red border on input, error text below field

### 9.2 Form Submission Conditions

**Submit Button State:**
- **Enabled When**: `loginForm.valid && !isLoading()`
- **Disabled When**: `loginForm.invalid || isLoading()`
- **Component**: LoginFormComponent
- **UI Effect**:
  - Enabled: Primary blue button with hover effect
  - Disabled: Grayed out button with cursor-not-allowed

**Pre-Submission Validation:**
- **Condition**: All form fields pass validation
- **Component**: LoginViewComponent
- **Action**: If invalid, prevent API call and mark all fields as touched
- **UI Effect**: Show all validation errors simultaneously

### 9.3 Authentication Status Conditions

**Already Authenticated Check:**
- **Condition**: `authService.isAuthenticated() && !authService.isTokenExpired()`
- **Component**: LoginViewComponent
- **When Checked**: On component initialization
- **Action**: Redirect to dashboard if true
- **UI Effect**: User never sees login form

**Token Expiration Check:**
- **Condition**: Current date < token expiresAt date
- **Component**: AuthService (computed signal)
- **When Checked**: Continuously via computed signal
- **Action**: If expired, clear user state and redirect to login
- **UI Effect**: User redirected to login with session expired message

### 9.4 Return URL Validation

**Safe Redirect Validation:**
- **Condition**: `returnUrl` is a relative path (starts with `/`) and not external
- **Component**: LoginViewComponent
- **When Checked**: On component initialization when extracting query param
- **Action**: If invalid/external, default to `/dashboard`
- **Security**: Prevents open redirect vulnerability

### 9.5 API Response Conditions

**Success Condition:**
- **Status**: 200 OK
- **Required Fields**: userId, email, firstName, lastName, token (all non-null)
- **Component**: LoginViewComponent, AuthService
- **Action**: Store token, update user state, navigate to returnUrl
- **UI Effect**: Form disappears, user navigated to dashboard

**Invalid Credentials (401):**
- **Status**: 401 Unauthorized
- **Error Code**: "InvalidCredentials"
- **Component**: LoginViewComponent
- **Action**: Display generic error message
- **UI Effect**: ErrorAlertComponent shows "Invalid email or password", password field cleared

**Rate Limiting (429):**
- **Status**: 429 Too Many Requests
- **Component**: LoginViewComponent
- **Action**: Display rate limit message, temporarily disable form
- **UI Effect**: ErrorAlertComponent shows "Too many login attempts. Please try again later."

**Network Error:**
- **Status**: 0 (no response)
- **Component**: LoginViewComponent
- **Action**: Display connection error
- **UI Effect**: ErrorAlertComponent shows "Connection error. Please check your internet connection."

## 10. Error Handling

### 10.1 Client-Side Validation Errors

**Email Field Errors:**

1. **Empty Email**
   - **Trigger**: User leaves email field empty and attempts to submit or blurs field
   - **Message**: "Email address is required"
   - **Handling**: Display below email field, red border on input
   - **Recovery**: User enters email address

2. **Invalid Email Format**
   - **Trigger**: Email doesn't match valid pattern (e.g., "user@" or "user.com")
   - **Message**: "Please enter a valid email address"
   - **Handling**: Display below email field on blur, red border
   - **Recovery**: User corrects email format

3. **Email Too Long**
   - **Trigger**: Email exceeds 254 characters
   - **Message**: "Email address is too long"
   - **Handling**: Prevent additional typing, show error
   - **Recovery**: User shortens email

**Password Field Errors:**

1. **Empty Password**
   - **Trigger**: User leaves password field empty and attempts to submit or blurs field
   - **Message**: "Password is required"
   - **Handling**: Display below password field, red border on input
   - **Recovery**: User enters password

2. **Password Too Long**
   - **Trigger**: Password exceeds 128 characters
   - **Message**: "Password is too long"
   - **Handling**: Prevent additional typing
   - **Recovery**: User shortens password (unlikely scenario)

### 10.2 API Errors

**401 Unauthorized - Invalid Credentials:**
- **Cause**: Incorrect email or password
- **Message**: "Invalid email or password"
- **Handling**:
  - Display in ErrorAlertComponent at top of form
  - Keep email field populated (don't clear)
  - Clear password field
  - Focus password field for easy retry
- **Security**: Generic message (no email enumeration)
- **Recovery**: User enters correct credentials

**429 Too Many Requests - Rate Limiting:**
- **Cause**: Too many failed login attempts from same IP
- **Message**: "Too many login attempts. Please try again later."
- **Handling**:
  - Display in ErrorAlertComponent
  - Disable form temporarily (disable inputs and submit button)
  - Optional: Show countdown timer if `Retry-After` header available
- **Recovery**: User waits and tries again later

**500 Internal Server Error:**
- **Cause**: Server-side error
- **Message**: "Something went wrong. Please try again later."
- **Handling**:
  - Display in ErrorAlertComponent
  - Log error details to console for debugging
  - Keep form enabled for retry
- **Recovery**: User retries, or contacts support if persistent

**0 Network Error:**
- **Cause**: No internet connection or server unreachable
- **Message**: "Connection error. Please check your internet connection."
- **Handling**:
  - Display in ErrorAlertComponent
  - Keep form enabled
  - Provide retry option
- **Recovery**: User checks connection and retries

**Unknown Errors:**
- **Cause**: Unexpected error (parsing failure, timeout, etc.)
- **Message**: "An unexpected error occurred. Please try again."
- **Handling**:
  - Display generic error message
  - Log full error to console
  - Keep form enabled
- **Recovery**: User retries or refreshes page

### 10.3 Token Storage Errors

**localStorage Unavailable:**
- **Cause**: Browser doesn't support localStorage or it's disabled
- **Message**: "Your browser settings prevent login. Please enable cookies and try again."
- **Handling**:
  - Detect localStorage availability before login attempt
  - Display error if unavailable
  - Log warning to console
- **Recovery**: User enables localStorage/cookies or uses different browser

**Token Storage Failure:**
- **Cause**: localStorage quota exceeded or write error
- **Message**: "Login successful but session could not be saved. Please try again."
- **Handling**:
  - Catch error during token storage
  - Display error message
  - Do not navigate to dashboard
  - Keep form enabled for retry
- **Recovery**: User clears browser storage or tries again

### 10.4 Navigation Errors

**Return URL Validation:**
- **Cause**: returnUrl query param is external or malicious
- **Handling**:
  - Validate returnUrl is relative path starting with `/`
  - If invalid: Default to `/dashboard`, log warning
  - Never redirect to external URLs (security)
- **Recovery**: Automatic (user redirected to safe default)

**Route Guard Failure:**
- **Cause**: User somehow reaches login while authenticated
- **Handling**:
  - Check authentication status in ngOnInit
  - Redirect authenticated users to dashboard
- **Recovery**: Automatic redirect

### 10.5 Error Handling Architecture

**Error Handling Service (Optional Enhancement):**
```typescript
@Injectable({ providedIn: 'root' })
export class ErrorHandlingService {
  handleApiError(error: HttpErrorResponse): string {
    // Centralized error message logic
    switch (error.status) {
      case 401: return 'Invalid email or password';
      case 429: return 'Too many login attempts. Please try again later.';
      case 0: return 'Connection error. Please check your internet connection.';
      default: return 'An error occurred. Please try again later.';
    }
  }

  logError(error: any, context: string): void {
    // Send to logging service (e.g., Sentry)
    console.error(`[${context}]`, error);
  }
}
```

**Error Display Strategy:**
- Field-level errors: Below input fields (validation)
- Form-level errors: Top of form in ErrorAlertComponent (API errors)
- All errors dismissible by user
- Errors cleared on new submission attempt or field input

## 11. Implementation Steps

### Step 1: Set Up Project Structure

1.1. Create directory structure:
```
src/app/features/auth/
├── pages/
│   └── login/
│       ├── login-view.component.ts
│       ├── login-view.component.html
│       └── login-view.component.css
├── components/
│   ├── login-form/
│   │   ├── login-form.component.ts
│   │   ├── login-form.component.html
│   │   └── login-form.component.css
│   └── error-alert/
│       ├── error-alert.component.ts
│       ├── error-alert.component.html
│       └── error-alert.component.css
├── services/
│   └── auth.service.ts
├── guards/
│   └── guest.guard.ts
├── interceptors/
│   └── auth.interceptor.ts
└── models/
    ├── auth-user.model.ts
    ├── login-form-data.model.ts
    └── api-error-response.model.ts
```

1.2. Create route configuration in app routing:
```typescript
{
  path: 'login',
  component: LoginViewComponent,
  canActivate: [guestGuard]
}
```

### Step 2: Create Type Definitions

2.1. Create `auth-user.model.ts`:
```typescript
export interface AuthUser {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  token: string;
  expiresAt: Date;
}
```

2.2. Create `login-form-data.model.ts`:
```typescript
export interface LoginFormData {
  email: string;
  password: string;
}
```

2.3. Create `api-error-response.model.ts`:
```typescript
export interface ApiErrorResponse {
  error: string;
  message: string;
  statusCode?: number;
}
```

### Step 3: Implement AuthService

3.1. Generate service: `ng generate service features/auth/services/auth`

3.2. Implement AuthService with signals:
```typescript
@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private currentUserSignal = signal<AuthUser | null>(null);

  currentUser = this.currentUserSignal.asReadonly();
  isAuthenticated = computed(() => this.currentUser() !== null);
  isTokenExpired = computed(() => {
    const user = this.currentUser();
    if (!user) return true;
    return new Date() >= user.expiresAt;
  });

  constructor() {
    this.loadUserFromStorage();
  }

  login(request: LoginRequest): Observable<LoginResponse> { /* ... */ }
  logout(): void { /* ... */ }
  private storeToken(token: string): void { /* ... */ }
  private loadUserFromStorage(): void { /* ... */ }
  getToken(): string | null { /* ... */ }
}
```

3.3. Implement all service methods following Section 7.2

### Step 4: Create HTTP Interceptor

4.1. Create `auth.interceptor.ts`:
```typescript
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = localStorage.getItem('auth_token');
  if (token) {
    req = req.clone({
      setHeaders: { Authorization: `Bearer ${token}` }
    });
  }
  return next(req);
};
```

4.2. Register interceptor in app.config.ts:
```typescript
export const appConfig: ApplicationConfig = {
  providers: [
    provideHttpClient(withInterceptors([authInterceptor]))
  ]
};
```

### Step 5: Create Route Guard

5.1. Create `guest.guard.ts`:
```typescript
export const guestGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated() && !authService.isTokenExpired()) {
    router.navigate(['/dashboard']);
    return false;
  }

  return true;
};
```

### Step 6: Implement ErrorAlertComponent

6.1. Generate component: `ng generate component features/auth/components/error-alert --standalone`

6.2. Implement component with Flowbite alert styling:
```typescript
@Component({
  selector: 'app-error-alert',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (message) {
      <div class="flex items-center p-4 mb-4 text-red-800 border border-red-300 rounded-lg bg-red-50" role="alert">
        <svg class="flex-shrink-0 w-4 h-4" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="currentColor" viewBox="0 0 20 20">
          <path d="M10 .5a9.5 9.5 0 1 0 9.5 9.5A9.51 9.51 0 0 0 10 .5ZM9.5 4a1.5 1.5 0 1 1 0 3 1.5 1.5 0 0 1 0-3ZM12 15H8a1 1 0 0 1 0-2h1v-3H8a1 1 0 0 1 0-2h2a1 1 0 0 1 1 1v4h1a1 1 0 0 1 0 2Z"/>
        </svg>
        <span class="sr-only">Error</span>
        <div class="ms-3 text-sm font-medium">{{ message }}</div>
        @if (dismissible) {
          <button type="button" (click)="onDismiss()" class="ms-auto -mx-1.5 -my-1.5 bg-red-50 text-red-500 rounded-lg focus:ring-2 focus:ring-red-400 p-1.5 hover:bg-red-200 inline-flex items-center justify-center h-8 w-8" aria-label="Close">
            <span class="sr-only">Close</span>
            <svg class="w-3 h-3" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 14 14">
              <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m1 1 6 6m0 0 6 6M7 7l6-6M7 7l-6 6"/>
            </svg>
          </button>
        }
      </div>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ErrorAlertComponent {
  message = input.required<string>();
  dismissible = input<boolean>(true);
  dismiss = output<void>();

  onDismiss(): void {
    this.dismiss.emit();
  }
}
```

### Step 7: Implement LoginFormComponent

7.1. Generate component: `ng generate component features/auth/components/login-form --standalone`

7.2. Implement component TypeScript:
```typescript
@Component({
  selector: 'app-login-form',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule],
  templateUrl: './login-form.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginFormComponent {
  private formBuilder = inject(FormBuilder);

  isLoading = input<boolean>(false);
  formSubmit = output<LoginFormData>();

  showPassword = signal<boolean>(false);

  loginForm = this.formBuilder.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
    password: ['', [Validators.required, Validators.maxLength(128)]]
  });

  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    const formData: LoginFormData = {
      email: this.loginForm.value.email!,
      password: this.loginForm.value.password!
    };

    this.formSubmit.emit(formData);
  }

  togglePasswordVisibility(): void {
    this.showPassword.update(v => !v);
  }

  get emailErrors(): string | null {
    const control = this.loginForm.get('email');
    if (!control?.touched || !control?.errors) return null;

    if (control.errors['required']) return 'Email address is required';
    if (control.errors['email']) return 'Please enter a valid email address';
    if (control.errors['maxlength']) return 'Email address is too long';

    return null;
  }

  get passwordErrors(): string | null {
    const control = this.loginForm.get('password');
    if (!control?.touched || !control?.errors) return null;

    if (control.errors['required']) return 'Password is required';
    if (control.errors['maxlength']) return 'Password is too long';

    return null;
  }
}
```

7.3. Implement component template with Flowbite styling (see detailed template in implementation)

### Step 8: Implement LoginViewComponent

8.1. Generate component: `ng generate component features/auth/pages/login --standalone`

8.2. Implement component TypeScript:
```typescript
@Component({
  selector: 'app-login-view',
  standalone: true,
  imports: [LoginFormComponent, ErrorAlertComponent, PageFooterComponent, RouterLink],
  templateUrl: './login-view.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginViewComponent implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  isLoading = signal<boolean>(false);
  error = signal<string | null>(null);
  returnUrl = signal<string>('/dashboard');

  ngOnInit(): void {
    // Redirect if already authenticated
    if (this.authService.isAuthenticated() && !this.authService.isTokenExpired()) {
      this.router.navigate(['/dashboard']);
      return;
    }

    // Extract return URL
    const returnUrl = this.route.snapshot.queryParams['returnUrl'];
    if (returnUrl && returnUrl.startsWith('/')) {
      this.returnUrl.set(returnUrl);
    }
  }

  onFormSubmit(formData: LoginFormData): void {
    this.error.set(null);
    this.isLoading.set(true);

    const request: LoginRequest = {
      email: formData.email.toLowerCase().trim(),
      password: formData.password
    };

    this.authService.login(request).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.router.navigate([this.returnUrl()]);
      },
      error: (error) => {
        this.isLoading.set(false);
        this.error.set(error.message);
      }
    });
  }

  onDismissError(): void {
    this.error.set(null);
  }

  onNavigateToRegister(): void {
    const returnUrl = this.returnUrl();
    this.router.navigate(['/register'], {
      queryParams: returnUrl !== '/dashboard' ? { returnUrl } : {}
    });
  }
}
```

8.3. Implement component template (see detailed template in implementation)

### Step 9: Create Page Layout and Styling

9.1. Implement responsive layout using Tailwind:
- Centered card on desktop (max-w-md)
- Full-width on mobile
- Proper spacing and padding
- Flowbite form component styling

9.2. Add Flowbite form styles for inputs, buttons, labels

9.3. Ensure mobile optimization with responsive utilities

### Step 10: Testing

10.1. **Unit Tests** for each component:
- LoginFormComponent: Test form validation, submission, password toggle
- LoginViewComponent: Test form submission handler, navigation, error handling
- AuthService: Test login method, token storage, state updates
- ErrorAlertComponent: Test message display and dismissal

10.2. **Integration Tests**:
- Test complete login flow from form submission to navigation
- Test error scenarios (401, 429, network errors)
- Test authentication state updates

10.3. **E2E Tests** (optional for MVP):
- Complete user journey: visit /login → enter credentials → submit → redirect to dashboard
- Test validation errors display
- Test error message display

### Step 11: Manual QA Checklist

11.1. **Functional Testing**:
- [ ] Can submit login form with valid credentials
- [ ] Successful login redirects to dashboard
- [ ] Token is stored in localStorage
- [ ] Invalid credentials show correct error message
- [ ] Form validation prevents submission with empty fields
- [ ] Email validation works correctly
- [ ] Password show/hide toggle works
- [ ] Already authenticated users are redirected away from login

11.2. **Security Testing**:
- [ ] Password is masked by default
- [ ] Error messages don't reveal if email exists
- [ ] Token is properly secured
- [ ] HTTPS is enforced
- [ ] No XSS vulnerabilities in error messages

11.3. **UX Testing**:
- [ ] Email field is auto-focused on page load
- [ ] Form is mobile-responsive
- [ ] Loading state is clearly indicated
- [ ] Error messages are clear and helpful
- [ ] Links to register and forgot password work

11.4. **Edge Cases**:
- [ ] Rate limiting error displays correctly
- [ ] Network errors are handled gracefully
- [ ] Return URL query parameter works
- [ ] Session expiration is handled correctly

### Step 12: Documentation and Deployment

12.1. Document component APIs and usage

12.2. Add JSDoc comments to complex methods

12.3. Update project README with authentication flow

12.4. Deploy to staging environment for testing

12.5. Conduct final review before production deployment

---

## Additional Notes

**Dependencies:**
- Angular 20
- Angular Reactive Forms
- Tailwind CSS 4.1
- Flowbite 3.1
- RxJS (for HTTP calls)

**Future Enhancements (Out of MVP Scope):**
- Social login (Google, Facebook)
- Two-factor authentication
- Remember me checkbox (extended session)
- Password strength indicator
- Forgot password functionality
- Email verification

**Performance Considerations:**
- Use OnPush change detection for optimal performance
- Lazy load auth module if needed
- Optimize bundle size by using standalone components

**Accessibility Considerations:**
- All form fields have associated labels
- Error messages are announced to screen readers
- Keyboard navigation is fully supported
- Focus management is properly handled
- Sufficient color contrast for all text
