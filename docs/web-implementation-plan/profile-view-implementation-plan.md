# Profile View Implementation Plan

## 1. Overview

The Profile View allows authenticated users to view and edit their personal profile information. It displays the user's first name, last name, email address, account creation date, and last login date. Users can edit their first and last names, but the email address is read-only. The view also provides a "Change Password" button that initiates a password reset flow via email. This is a minimal MVP implementation with a clean, simple card-based layout.

## 2. View Routing

**Path**: `/profile`

**Route Configuration**:
```typescript
{
  path: 'profile',
  component: ProfileComponent,
  canActivate: [AuthGuard]
}
```

**Authentication**: Required (protected by AuthGuard)

## 3. Component Structure

```
ProfileComponent (Smart/Container Component)
├── LoadingSpinnerComponent (shown during initial data load)
└── ProfileInfoComponent (Presentational Component)
    ├── Profile Header Section
    ├── Profile Information Card
    │   ├── Read-only fields (when not editing)
    │   │   ├── First Name display
    │   │   ├── Last Name display
    │   │   ├── Email display
    │   │   ├── Created At display
    │   │   └── Last Login display
    │   ├── Editable fields (when in edit mode)
    │   │   ├── First Name input with validation
    │   │   └── Last Name input with validation
    │   └── Action Buttons Section
    │       ├── Edit Button (visible when not editing)
    │       ├── Save Button (visible when editing)
    │       ├── Cancel Button (visible when editing)
    │       └── Change Password Button
    └── ErrorAlertComponent (shown when errors occur)
```

## 4. Component Details

### ProfileComponent (Smart Component)

**Component Description**:
The ProfileComponent is the main container component for the profile view. It manages the application state, handles API calls through the ProfileService, and orchestrates the interaction between child components. It is responsible for fetching profile data on initialization, managing edit mode state, handling save operations, and triggering the password reset flow.

**Main HTML Elements and Child Components**:
- Root container with Tailwind CSS classes for layout and responsiveness
- `<app-loading-spinner>` (conditionally rendered with @if when isLoading is true)
- `<app-profile-info>` (conditionally rendered with @if when profile data is loaded)
  - Receives profile data, editing state, and saving state as inputs
  - Emits save, cancel, and changePassword events

**Handled Events**:
1. **Component Initialization** (`ngOnInit`):
   - Trigger profile data fetch
   - Set isLoading to true
   - Handle fetch success/failure

2. **Save Profile Event** (from ProfileInfoComponent):
   - Receive updated firstName and lastName
   - Validate data client-side
   - Call ProfileService.updateProfile()
   - Handle success (update profile signal, show toast, exit edit mode)
   - Handle errors (show error messages)

3. **Cancel Edit Event** (from ProfileInfoComponent):
   - Set isEditing to false
   - Discard any unsaved changes

4. **Change Password Event** (from ProfileInfoComponent):
   - Call ProfileService or AuthService to initiate password reset
   - Show success toast with instructions
   - Handle errors

**Validation Conditions**: None directly (delegates to ProfileInfoComponent)

**Types**:
- `GetProfileResponse` (DTO from API)
- `UpdateProfileRequest` (DTO to API)
- `UpdateProfileResponse` (DTO from API)
- `ProfileViewModel` (for display)

**Props**: None (top-level component)

**State Management** (Signals):
- `profile: WritableSignal<ProfileViewModel | null>` - Current profile data
- `isLoading: WritableSignal<boolean>` - Initial loading state
- `isEditing: WritableSignal<boolean>` - Edit mode toggle
- `isSaving: WritableSignal<boolean>` - Saving operation state
- `error: WritableSignal<string | null>` - Error message storage

---

### ProfileInfoComponent (Presentational Component)

**Component Description**:
The ProfileInfoComponent is a presentational component responsible for displaying profile information and handling user interactions. It renders profile fields in either read-only or edit mode based on the `isEditing` input. It manages local form state for the editable fields (firstName, lastName) and performs client-side validation before emitting save events to the parent component.

**Main HTML Elements and Child Components**:
- Profile card container (Flowbite card styling with Tailwind CSS)
- Profile header with title "Profile" or "Edit Profile"
- Information section:
  - **Read-only mode**:
    - Label and value pairs for all fields
    - `<p>` elements for displaying firstName, lastName, email
    - `<p>` elements for formatted dates (createdAt, lastLoginAt)
  - **Edit mode**:
    - `<input>` elements for firstName and lastName (with validation)
    - `<p>` elements for email (read-only)
    - `<p>` elements for dates (read-only)
    - Validation error messages (`<span>` elements below each input)
- Action buttons section:
  - "Edit" button (shown when not editing)
  - "Save" button with loading spinner (shown when editing, disabled when saving)
  - "Cancel" button (shown when editing)
  - "Change Password" button (always shown)
- `<app-error-alert>` (conditionally shown if validation or other errors exist)

**Handled Events**:
1. **Edit Button Click**:
   - Emit internal signal to switch to edit mode
   - Initialize form with current profile values
   - Focus on first editable field

2. **Save Button Click**:
   - Validate firstName (required, max 100 chars)
   - Validate lastName (required, max 100 chars)
   - If valid: emit `save` event with UpdateProfileRequest
   - If invalid: show validation errors, prevent save

3. **Cancel Button Click**:
   - Discard form changes
   - Emit `cancel` event to parent
   - Reset form to original profile values

4. **Change Password Button Click**:
   - Emit `changePassword` event to parent
   - Parent handles password reset API call

5. **Input Field Blur Events**:
   - Validate specific field on blur
   - Show field-level error messages immediately

6. **Input Field Input Events**:
   - Clear validation error when user starts typing
   - Update form model signal

**Validation Conditions** (Detailed):
1. **firstName Validation**:
   - **Required**: Field cannot be empty or whitespace only
     - Error message: "First name is required" (Polish: "Imię jest wymagane")
   - **Maximum Length**: Cannot exceed 100 characters
     - Error message: "First name must be 100 characters or less" (Polish: "Imię może mieć maksymalnie 100 znaków")
   - **Validation Timing**: On blur and on save attempt

2. **lastName Validation**:
   - **Required**: Field cannot be empty or whitespace only
     - Error message: "Last name is required" (Polish: "Nazwisko jest wymagane")
   - **Maximum Length**: Cannot exceed 100 characters
     - Error message: "Last name must be 100 characters or less" (Polish: "Nazwisko może mieć maksymalnie 100 znaków")
   - **Validation Timing**: On blur and on save attempt

3. **Form-Level Validation**:
   - All field validations must pass before emitting save event
   - If any field is invalid, prevent save and show all errors
   - Display summary error if needed

**Types**:
- `ProfileViewModel` (input from parent)
- `UpdateProfileRequest` (for emitting save event)
- `ProfileFormModel` (internal form state)

**Props** (Component Interface):
```typescript
@Component({
  selector: 'app-profile-info',
  // ...
})
export class ProfileInfoComponent {
  // Inputs
  profile = input.required<ProfileViewModel>();
  isEditing = input.required<boolean>();
  isSaving = input<boolean>(false);

  // Outputs
  save = output<UpdateProfileRequest>();
  cancel = output<void>();
  changePassword = output<void>();
}
```

---

### LoadingSpinnerComponent (Shared Component)

**Component Description**:
A reusable component that displays a loading spinner with an optional message. Used to indicate that data is being fetched or an operation is in progress.

**Main HTML Elements**:
- Spinner icon (SVG or CSS animation)
- Optional message text

**Handled Events**: None (purely presentational)

**Validation Conditions**: None

**Types**: None specific to this component

**Props**:
```typescript
@Component({
  selector: 'app-loading-spinner',
  // ...
})
export class LoadingSpinnerComponent {
  message = input<string>();
}
```

---

### ErrorAlertComponent (Shared Component)

**Component Description**:
A reusable component for displaying error messages in an alert banner format. Can be used for inline errors within the profile view.

**Main HTML Elements**:
- Alert container with error styling (red border, background)
- Error icon
- Error message text
- Optional dismiss button

**Handled Events**:
- Dismiss button click (if dismissible)

**Validation Conditions**: None

**Types**: None specific to this component

**Props**:
```typescript
@Component({
  selector: 'app-error-alert',
  // ...
})
export class ErrorAlertComponent {
  message = input.required<string>();
  dismissible = input<boolean>(false);
  dismiss = output<void>();
}
```

---

## 5. Types

### API Data Transfer Objects (DTOs)

#### GetProfileResponse
```typescript
/**
 * Response from GET /api/profile
 * Contains the complete user profile information
 */
interface GetProfileResponse {
  /** Unique identifier for the user (UUID format) */
  userId: string;

  /** User's email address (read-only) */
  email: string;

  /** User's first name */
  firstName: string;

  /** User's last name */
  lastName: string;

  /** ISO 8601 date string indicating when the account was created */
  createdAt: string;

  /** ISO 8601 date string indicating the last successful login */
  lastLoginAt: string;
}
```

#### UpdateProfileRequest
```typescript
/**
 * Request body for PUT /api/profile
 * Contains only the editable profile fields
 */
interface UpdateProfileRequest {
  /** Updated first name (required, max 100 characters) */
  firstName: string;

  /** Updated last name (required, max 100 characters) */
  lastName: string;
}
```

#### UpdateProfileResponse
```typescript
/**
 * Response from PUT /api/profile
 * Returns the updated profile (same structure as GetProfileResponse)
 */
type UpdateProfileResponse = GetProfileResponse;
```

---

### View Models

#### ProfileViewModel
```typescript
/**
 * View model for displaying profile information in the UI
 * Includes formatted dates for display in Polish locale
 */
interface ProfileViewModel {
  /** Unique identifier for the user */
  userId: string;

  /** User's email address (always read-only in UI) */
  email: string;

  /** User's first name (editable) */
  firstName: string;

  /** User's last name (editable) */
  lastName: string;

  /** Formatted creation date for display (e.g., "1 stycznia 2025, 10:00") */
  createdAt: string;

  /** Formatted last login date for display */
  lastLoginAt: string;

  /** Raw Date object for createdAt (for potential future use) */
  createdAtRaw: Date;

  /** Raw Date object for lastLoginAt (for potential future use) */
  lastLoginAtRaw: Date;
}
```

**Mapping Function**:
```typescript
function mapToProfileViewModel(dto: GetProfileResponse): ProfileViewModel {
  return {
    userId: dto.userId,
    email: dto.email,
    firstName: dto.firstName,
    lastName: dto.lastName,
    createdAt: formatDateInPolish(dto.createdAt),
    lastLoginAt: formatDateInPolish(dto.lastLoginAt),
    createdAtRaw: new Date(dto.createdAt),
    lastLoginAtRaw: new Date(dto.lastLoginAt)
  };
}

/**
 * Format ISO date string to Polish locale format
 * Example output: "1 stycznia 2025, 10:00"
 */
function formatDateInPolish(isoString: string): string {
  const date = new Date(isoString);
  return format(date, "d MMMM yyyy, HH:mm", { locale: pl });
}
```

#### ProfileFormModel
```typescript
/**
 * Form model for managing editable profile fields
 * Used internally in ProfileInfoComponent during edit mode
 */
interface ProfileFormModel {
  /** First name field value */
  firstName: string;

  /** Last name field value */
  lastName: string;
}
```

#### ValidationErrors
```typescript
/**
 * Type for storing field-level validation errors
 * Key is the field name, value is the error message
 */
type ValidationErrors = Record<string, string>;

/**
 * Example:
 * {
 *   firstName: "First name is required",
 *   lastName: "Last name must be 100 characters or less"
 * }
 */
```

---

## 6. State Management

### Service-Level State (ProfileService)

The ProfileService manages API calls but does not maintain shared state since profile data is only used in the ProfileComponent. The service provides methods that return Observables for API operations.

**ProfileService Interface**:
```typescript
@Injectable({ providedIn: 'root' })
export class ProfileService {
  private http = inject(HttpClient);
  private readonly apiUrl = '/api/profile';

  /**
   * Fetch the current user's profile
   * @returns Observable of GetProfileResponse
   */
  getProfile(): Observable<GetProfileResponse> {
    return this.http.get<GetProfileResponse>(this.apiUrl);
  }

  /**
   * Update the current user's profile
   * @param request - Object containing firstName and lastName
   * @returns Observable of UpdateProfileResponse
   */
  updateProfile(request: UpdateProfileRequest): Observable<UpdateProfileResponse> {
    return this.http.put<UpdateProfileResponse>(this.apiUrl, request);
  }
}
```

**Note on Password Reset**: The password reset functionality will likely be part of the AuthService or a separate endpoint. Check if `POST /api/auth/forgot-password` or similar exists. If not, coordinate with backend to implement this endpoint.

---

### Component-Level State (ProfileComponent Signals)

The ProfileComponent manages local state using Angular signals for reactive updates with OnPush change detection strategy.

**State Signals**:

1. **profile: WritableSignal<ProfileViewModel | null>**
   - Purpose: Stores the current user's profile data
   - Initial value: `null`
   - Updated after successful API fetch or update
   - Used by template to pass data to ProfileInfoComponent

2. **isLoading: WritableSignal<boolean>**
   - Purpose: Indicates initial profile data is being loaded
   - Initial value: `true`
   - Set to `false` after API response (success or error)
   - Controls visibility of LoadingSpinnerComponent

3. **isEditing: WritableSignal<boolean>**
   - Purpose: Tracks whether the user is in edit mode
   - Initial value: `false`
   - Set to `true` when user clicks "Edit" button
   - Set to `false` on save success or cancel
   - Passed to ProfileInfoComponent to control UI rendering

4. **isSaving: WritableSignal<boolean>**
   - Purpose: Indicates a save operation is in progress
   - Initial value: `false`
   - Set to `true` when save API call starts
   - Set to `false` when API call completes (success or error)
   - Passed to ProfileInfoComponent to disable form during save

5. **error: WritableSignal<string | null>**
   - Purpose: Stores error messages for display
   - Initial value: `null`
   - Set when API calls fail
   - Cleared on successful operations
   - Displayed via ErrorAlertComponent

**Example Implementation**:
```typescript
@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [LoadingSpinnerComponent, ProfileInfoComponent, ErrorAlertComponent]
})
export class ProfileComponent implements OnInit {
  private profileService = inject(ProfileService);
  private toastService = inject(ToastService);

  // State signals
  protected profile = signal<ProfileViewModel | null>(null);
  protected isLoading = signal<boolean>(true);
  protected isEditing = signal<boolean>(false);
  protected isSaving = signal<boolean>(false);
  protected error = signal<string | null>(null);

  ngOnInit() {
    this.loadProfile();
  }

  private loadProfile() {
    // Implementation in section 11
  }

  protected onSave(request: UpdateProfileRequest) {
    // Implementation in section 11
  }

  protected onCancel() {
    this.isEditing.set(false);
  }

  protected onChangePassword() {
    // Implementation in section 11
  }
}
```

---

### Form State (ProfileInfoComponent)

The ProfileInfoComponent manages local form state for editable fields.

**Form State Signals**:

1. **formModel: WritableSignal<ProfileFormModel>**
   - Purpose: Stores form field values during editing
   - Initialized with current profile values when entering edit mode
   - Updated as user types in input fields
   - Reset on cancel

2. **validationErrors: WritableSignal<ValidationErrors>**
   - Purpose: Stores field-level validation error messages
   - Updated on field blur and form submission
   - Cleared when user corrects a field
   - Displayed below each input field

3. **isFormValid: Signal<boolean>** (computed)
   - Purpose: Indicates whether the form passes all validation
   - Computed from validationErrors signal
   - Used to disable/enable save button
   - Prevents invalid submissions

---

## 7. API Integration

### Endpoint 1: Get Profile

**HTTP Method**: GET
**Endpoint**: `/api/profile`
**Authentication**: Required (JWT Bearer token in Authorization header)

**Request**:
- No request body
- JWT token automatically added by AuthInterceptor

**Response Type**: `GetProfileResponse`

**Success Response (200 OK)**:
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "firstName": "Jan",
  "lastName": "Kowalski",
  "createdAt": "2025-10-01T10:00:00Z",
  "lastLoginAt": "2025-10-15T08:30:00Z"
}
```

**Error Responses**:
- `401 Unauthorized`: Token missing, invalid, or expired
  - Handled by ErrorInterceptor → redirect to login

**Frontend Integration**:
```typescript
// In ProfileService
getProfile(): Observable<GetProfileResponse> {
  return this.http.get<GetProfileResponse>(this.apiUrl);
}

// In ProfileComponent
private loadProfile() {
  this.isLoading.set(true);
  this.error.set(null);

  this.profileService.getProfile()
    .pipe(takeUntilDestroyed(this.destroyRef))
    .subscribe({
      next: (response) => {
        const viewModel = mapToProfileViewModel(response);
        this.profile.set(viewModel);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.error.set('Unable to load profile. Please try again.');
        this.isLoading.set(false);
        console.error('Profile load error:', err);
      }
    });
}
```

---

### Endpoint 2: Update Profile

**HTTP Method**: PUT
**Endpoint**: `/api/profile`
**Authentication**: Required (JWT Bearer token)

**Request Type**: `UpdateProfileRequest`

**Request Body**:
```json
{
  "firstName": "Jan",
  "lastName": "Nowak"
}
```

**Request Validation** (API-side):
- `firstName`: Required, max 100 characters
- `lastName`: Required, max 100 characters

**Response Type**: `UpdateProfileResponse`

**Success Response (200 OK)**:
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "firstName": "Jan",
  "lastName": "Nowak",
  "createdAt": "2025-10-01T10:00:00Z",
  "lastLoginAt": "2025-10-15T08:30:00Z"
}
```

**Error Responses**:
- `400 Bad Request`: Validation failed
  ```json
  {
    "error": "ValidationError",
    "details": {
      "firstName": ["First name is required"],
      "lastName": ["Last name must be 100 characters or less"]
    }
  }
  ```
- `401 Unauthorized`: Token missing, invalid, or expired

**Frontend Integration**:
```typescript
// In ProfileService
updateProfile(request: UpdateProfileRequest): Observable<UpdateProfileResponse> {
  return this.http.put<UpdateProfileResponse>(this.apiUrl, request);
}

// In ProfileComponent
protected onSave(request: UpdateProfileRequest) {
  this.isSaving.set(true);
  this.error.set(null);

  this.profileService.updateProfile(request)
    .pipe(takeUntilDestroyed(this.destroyRef))
    .subscribe({
      next: (response) => {
        const viewModel = mapToProfileViewModel(response);
        this.profile.set(viewModel);
        this.isSaving.set(false);
        this.isEditing.set(false);
        this.toastService.showSuccess('Profile updated successfully');
      },
      error: (err) => {
        this.isSaving.set(false);
        if (err.status === 400) {
          // Handle validation errors - pass to ProfileInfoComponent
          this.error.set('Validation failed. Please check your inputs.');
        } else {
          this.error.set('Unable to update profile. Please try again.');
        }
        console.error('Profile update error:', err);
      }
    });
}
```

---

### Endpoint 3: Initiate Password Reset (Assumed)

**Note**: This endpoint may not exist yet. Coordinate with backend team to confirm endpoint details or implement if needed.

**HTTP Method**: POST (assumed)
**Endpoint**: `/api/auth/forgot-password` (assumed)
**Authentication**: May or may not require JWT (depends on implementation)

**Request Body** (possible structure):
```json
{
  "email": "user@example.com"
}
```

Or, if using JWT, the email can be inferred from the token and no body is needed.

**Response** (assumed):
```json
{
  "message": "Password reset email sent"
}
```

**Frontend Integration** (placeholder):
```typescript
// In AuthService or ProfileService
initiatePasswordReset(): Observable<void> {
  return this.http.post<void>('/api/auth/forgot-password', {});
}

// In ProfileComponent
protected onChangePassword() {
  this.authService.initiatePasswordReset()
    .pipe(takeUntilDestroyed(this.destroyRef))
    .subscribe({
      next: () => {
        this.toastService.showSuccess('Password reset link sent to your email');
      },
      error: (err) => {
        this.toastService.showError('Unable to send password reset email. Please try again later.');
        console.error('Password reset error:', err);
      }
    });
}
```

---

## 8. User Interactions

### Interaction 1: View Profile (Initial Load)

**User Action**: User navigates to `/profile`

**System Flow**:
1. Router activates ProfileComponent
2. AuthGuard verifies authentication
   - If not authenticated: Redirect to `/login`
   - If authenticated: Allow navigation
3. ProfileComponent `ngOnInit` is called
4. `loadProfile()` method is invoked
5. Set `isLoading` signal to `true`
6. Template renders LoadingSpinnerComponent
7. API call to `GET /api/profile` is made
8. On success:
   - Transform `GetProfileResponse` to `ProfileViewModel` (format dates)
   - Set `profile` signal with transformed data
   - Set `isLoading` to `false`
   - Template renders ProfileInfoComponent with profile data
9. On error:
   - Set `error` signal with user-friendly message
   - Set `isLoading` to `false`
   - Template shows error alert

**Expected Outcome**: User sees their profile information displayed in a clean card layout with all fields visible and read-only.

---

### Interaction 2: Enter Edit Mode

**User Action**: User clicks "Edit" button

**System Flow**:
1. ProfileInfoComponent emits internal edit signal
2. Component initializes `formModel` signal with current profile values:
   ```typescript
   this.formModel.set({
     firstName: this.profile().firstName,
     lastName: this.profile().lastName
   });
   ```
3. Set internal `isEditMode` signal to `true`
4. Template re-renders:
   - Replace read-only text with input fields for firstName and lastName
   - Show "Save" and "Cancel" buttons
   - Hide "Edit" button
5. Focus automatically moves to firstName input field

**Expected Outcome**: User sees input fields for firstName and lastName with current values, ready for editing. Email and date fields remain read-only.

---

### Interaction 3: Edit Fields

**User Action**: User modifies firstName or lastName in input fields

**System Flow**:
1. User types in input field
2. Input event fires
3. Update `formModel` signal with new value
4. Clear any existing validation error for that field
5. Template updates reactively (OnPush + signals)

**Expected Outcome**: Input fields update with user's typing, validation errors (if any) are cleared for the field being edited.

---

### Interaction 4: Field Blur Validation

**User Action**: User moves focus away from an input field (blur event)

**System Flow**:
1. Blur event fires on input field
2. Validate the specific field:
   - **firstName**: Check required and max length
   - **lastName**: Check required and max length
3. If invalid:
   - Add error message to `validationErrors` signal
   - Template displays error message below field
   - Apply red border to input field
4. If valid:
   - Remove any existing error for that field from `validationErrors`
   - Remove red border

**Expected Outcome**: User sees immediate feedback if a field is invalid (error message and red border), or confirmation if valid (error cleared).

---

### Interaction 5: Save Profile

**User Action**: User clicks "Save" button

**System Flow**:
1. Click event triggers `onSaveClick()` method
2. Perform full form validation:
   - Validate firstName (required, max 100 chars)
   - Validate lastName (required, max 100 chars)
3. If validation fails:
   - Update `validationErrors` signal with all errors
   - Template displays all validation errors
   - Prevent API call
   - Focus on first invalid field
4. If validation passes:
   - Create `UpdateProfileRequest` object from `formModel`
   - Emit `save` event to ProfileComponent
   - ProfileComponent receives event
   - Set `isSaving` signal to `true`
   - Disable form inputs and save button (via `isSaving` input)
   - Show loading spinner on save button
   - Call `ProfileService.updateProfile(request)`
5. On API success (200 OK):
   - Transform `UpdateProfileResponse` to `ProfileViewModel`
   - Update `profile` signal with new data
   - Set `isSaving` to `false`
   - Set `isEditing` to `false`
   - Exit edit mode (template shows read-only view)
   - Show success toast: "Profile updated successfully" (Polish: "Profil zaktualizowany pomyślnie")
6. On API error (400 Bad Request):
   - Parse validation errors from response
   - Map errors to `validationErrors` signal
   - Set `isSaving` to `false`
   - Remain in edit mode
   - Display field-level error messages
7. On API error (other):
   - Set `error` signal with generic message
   - Set `isSaving` to `false`
   - Show error toast
   - Remain in edit mode

**Expected Outcome**:
- Success: Profile data is updated, user exits edit mode, sees updated values, and receives success feedback
- Validation error: User sees specific error messages and can correct inputs
- Other error: User sees error message and can retry

---

### Interaction 6: Cancel Edit

**User Action**: User clicks "Cancel" button

**System Flow**:
1. Click event triggers `onCancelClick()` method
2. Discard changes to `formModel`
3. Clear `validationErrors` signal
4. Emit `cancel` event to ProfileComponent
5. ProfileComponent receives event
6. Set `isEditing` signal to `false`
7. Template re-renders:
   - Show read-only view with original profile values
   - Show "Edit" button
   - Hide "Save" and "Cancel" buttons

**Expected Outcome**: User exits edit mode without saving changes. Profile displays original values.

---

### Interaction 7: Change Password

**User Action**: User clicks "Change Password" button

**System Flow**:
1. Click event triggers `onChangePasswordClick()` method
2. ProfileInfoComponent emits `changePassword` event to ProfileComponent
3. ProfileComponent receives event
4. Optionally show confirmation dialog: "A password reset link will be sent to your email. Continue?" (Polish: "Link do resetowania hasła zostanie wysłany na Twój email. Kontynuować?")
5. If user confirms (or no dialog):
   - Call `AuthService.initiatePasswordReset()` or similar method
   - Show loading indicator (optional, if operation takes time)
6. On API success:
   - Show success toast: "Password reset link sent to your email. Please check your inbox." (Polish: "Link do resetowania hasła wysłany na Twój email. Sprawdź swoją skrzynkę.")
7. On API error:
   - Show error toast: "Unable to send password reset email. Please try again later." (Polish: "Nie udało się wysłać emaila z linkiem do resetowania hasła. Spróbuj ponownie później.")

**Expected Outcome**: User receives confirmation that a password reset email has been sent. They can check their email and complete the reset process via the link. No change to the profile view itself.

---

### Interaction 8: Session Expiry During Use

**User Action**: User is viewing or editing profile when JWT token expires

**System Flow**:
1. User triggers an API call (load profile or save profile)
2. API returns `401 Unauthorized`
3. ErrorInterceptor catches the 401 response
4. Store current route (`/profile`) as return URL
5. Clear authentication state in AuthService
6. Redirect user to `/login`
7. Show toast: "Your session has expired. Please log in again." (Polish: "Twoja sesja wygasła. Zaloguj się ponownie.")
8. User logs in
9. After successful login, redirect to stored return URL (`/profile`)
10. Profile loads normally

**Expected Outcome**: User is gracefully redirected to login, can re-authenticate, and returns to the profile view without losing context.

---

### Interaction 9: Handle Network Error

**User Action**: User attempts to load or save profile, but network is unavailable

**System Flow**:
1. API call fails due to network error
2. Error handler in ProfileComponent catches error
3. Set `error` signal with message: "Unable to load profile. Please check your connection and try again." (for load) or "Unable to save profile. Please try again." (for save)
4. Set `isLoading` or `isSaving` to `false`
5. Template displays ErrorAlertComponent with error message
6. Provide "Retry" button (optional) to re-attempt the operation

**Expected Outcome**: User sees clear error message indicating the issue and can retry when connection is restored.

---

## 9. Conditions and Validation

### Condition 1: Authentication Required

**What**: User must be authenticated to access the profile view

**Components Affected**: ProfileComponent (route level)

**Verification Method**:
- AuthGuard on route checks `AuthService.isAuthenticated()` signal
- If false, redirect to `/login` and store return URL
- If true, allow navigation to ProfileComponent

**Interface Impact**:
- If not authenticated: User cannot reach profile view, sees login page
- If authenticated: User can access profile view

---

### Condition 2: firstName Validation - Required

**What**: firstName field cannot be empty or contain only whitespace

**Components Affected**: ProfileInfoComponent

**Verification Method**:
```typescript
function validateFirstNameRequired(value: string): string | null {
  if (!value || value.trim().length === 0) {
    return 'First name is required'; // Polish: 'Imię jest wymagane'
  }
  return null;
}
```

**Validation Timing**: On blur and on save attempt

**Interface Impact**:
- If invalid: Red border on input, error message below field, save button disabled (or save prevented)
- If valid: No error indication, normal input styling

---

### Condition 3: firstName Validation - Maximum Length

**What**: firstName cannot exceed 100 characters

**Components Affected**: ProfileInfoComponent

**Verification Method**:
```typescript
function validateFirstNameLength(value: string): string | null {
  if (value.length > 100) {
    return 'First name must be 100 characters or less';
    // Polish: 'Imię może mieć maksymalnie 100 znaków'
  }
  return null;
}
```

**Validation Timing**: On blur and on save attempt

**Interface Impact**:
- If invalid: Red border on input, error message below field, save button disabled (or save prevented)
- If valid: No error indication, normal input styling
- Optional: Show character count when approaching limit (e.g., "95/100")

---

### Condition 4: lastName Validation - Required

**What**: lastName field cannot be empty or contain only whitespace

**Components Affected**: ProfileInfoComponent

**Verification Method**:
```typescript
function validateLastNameRequired(value: string): string | null {
  if (!value || value.trim().length === 0) {
    return 'Last name is required'; // Polish: 'Nazwisko jest wymagane'
  }
  return null;
}
```

**Validation Timing**: On blur and on save attempt

**Interface Impact**:
- If invalid: Red border on input, error message below field, save button disabled (or save prevented)
- If valid: No error indication, normal input styling

---

### Condition 5: lastName Validation - Maximum Length

**What**: lastName cannot exceed 100 characters

**Components Affected**: ProfileInfoComponent

**Verification Method**:
```typescript
function validateLastNameLength(value: string): string | null {
  if (value.length > 100) {
    return 'Last name must be 100 characters or less';
    // Polish: 'Nazwisko może mieć maksymalnie 100 znaków'
  }
  return null;
}
```

**Validation Timing**: On blur and on save attempt

**Interface Impact**:
- If invalid: Red border on input, error message below field, save button disabled (or save prevented)
- If valid: No error indication, normal input styling

---

### Condition 6: Email is Read-Only

**What**: Email address cannot be edited by the user (per MVP requirements)

**Components Affected**: ProfileInfoComponent

**Verification Method**:
- Email field is rendered as read-only text (not an input field)
- No validation needed since it's not editable

**Interface Impact**:
- Email is always displayed as static text with a "Read-only" or disabled appearance
- Email is never included in update requests
- No edit controls for email field

---

### Condition 7: Form-Level Validation Before Save

**What**: All field validations must pass before submitting update request

**Components Affected**: ProfileInfoComponent

**Verification Method**:
```typescript
function validateForm(formModel: ProfileFormModel): ValidationErrors {
  const errors: ValidationErrors = {};

  // Validate firstName
  const firstNameRequiredError = validateFirstNameRequired(formModel.firstName);
  const firstNameLengthError = validateFirstNameLength(formModel.firstName);
  if (firstNameRequiredError) errors.firstName = firstNameRequiredError;
  else if (firstNameLengthError) errors.firstName = firstNameLengthError;

  // Validate lastName
  const lastNameRequiredError = validateLastNameRequired(formModel.lastName);
  const lastNameLengthError = validateLastNameLength(formModel.lastName);
  if (lastNameRequiredError) errors.lastName = lastNameRequiredError;
  else if (lastNameLengthError) errors.lastName = lastNameLengthError;

  return errors;
}

function isFormValid(errors: ValidationErrors): boolean {
  return Object.keys(errors).length === 0;
}
```

**Validation Timing**: Before emitting save event to parent

**Interface Impact**:
- If form is invalid:
  - Display all validation errors
  - Prevent save event emission
  - Optionally disable save button
  - Focus on first invalid field
- If form is valid:
  - Clear all validation errors
  - Emit save event
  - Proceed with API call

---

### Condition 8: Disable Form During Save Operation

**What**: Prevent user interaction with form while save operation is in progress

**Components Affected**: ProfileInfoComponent

**Verification Method**: Check `isSaving` input prop

**Interface Impact**:
- If `isSaving` is true:
  - Disable all input fields (add `disabled` attribute)
  - Disable save button
  - Show loading spinner on save button
  - Disable cancel button (optional, or allow cancel to abort)
- If `isSaving` is false:
  - Enable all input fields
  - Enable save button (if form is valid)
  - Hide loading spinner
  - Enable cancel button

---

## 10. Error Handling

### Error Scenario 1: Network Error on Load

**Scenario**: User navigates to `/profile`, but network is unavailable or API is unreachable

**Handling**:
1. API call fails with network error
2. Catch error in ProfileComponent's `loadProfile()` method
3. Set `error` signal: "Unable to load profile. Please check your connection and try again."
4. Set `isLoading` to `false`
5. Template displays ErrorAlertComponent with error message
6. Optionally provide "Retry" button to call `loadProfile()` again

**User Impact**: User sees error message and can retry when connection is restored

**Implementation**:
```typescript
error: (err) => {
  this.error.set('Unable to load profile. Please check your connection and try again.');
  this.isLoading.set(false);
  console.error('Profile load error:', err);
}
```

---

### Error Scenario 2: 401 Unauthorized (Session Expired)

**Scenario**: User's JWT token expires while viewing or editing profile

**Handling**:
1. API call returns `401 Unauthorized`
2. ErrorInterceptor catches the 401 response globally
3. Store current route (`/profile`) in session storage
4. Clear authentication state in AuthService
5. Navigate to `/login`
6. Show toast: "Your session has expired. Please log in again."
7. After successful login, redirect to stored return URL (`/profile`)

**User Impact**: User is redirected to login page, logs in, and returns to profile view

**Implementation**: Handled by ErrorInterceptor (no component-level code needed)

---

### Error Scenario 3: 400 Bad Request (Validation Error from API)

**Scenario**: Client-side validation passes, but API returns validation errors (e.g., concurrent modification, additional server-side rules)

**Handling**:
1. API call returns `400 Bad Request` with error details
2. Parse error response:
   ```json
   {
     "error": "ValidationError",
     "details": {
       "firstName": ["First name contains invalid characters"],
       "lastName": ["Last name is too long"]
     }
   }
   ```
3. Map errors to `validationErrors` signal or pass to ProfileInfoComponent
4. Display field-level errors in UI
5. Set `isSaving` to `false`
6. Keep form in edit mode
7. Allow user to correct and resubmit

**User Impact**: User sees specific error messages for each field and can correct them

**Implementation**:
```typescript
error: (err) => {
  this.isSaving.set(false);
  if (err.status === 400 && err.error?.details) {
    // Pass validation errors to child component or set in error signal
    this.error.set('Validation failed. Please check your inputs.');
    // Optionally: emit errors to ProfileInfoComponent to display field-level errors
  } else {
    this.error.set('Unable to update profile. Please try again.');
  }
  console.error('Profile update error:', err);
}
```

---

### Error Scenario 4: Client-Side Validation Failure

**Scenario**: User tries to save with invalid data (empty fields, too long values)

**Handling**:
1. User clicks "Save" button
2. `onSaveClick()` method performs validation
3. Validation fails (e.g., firstName is empty)
4. Update `validationErrors` signal with specific errors
5. Template displays error messages below fields
6. Apply red border to invalid fields
7. Prevent API call (do not emit save event)
8. Focus on first invalid field
9. User corrects errors and tries again

**User Impact**: User sees immediate feedback about what's wrong and can fix it without making an API call

**Implementation**:
```typescript
protected onSaveClick() {
  const errors = this.validateForm();
  this.validationErrors.set(errors);

  if (!this.isFormValid()) {
    // Focus on first invalid field
    this.focusFirstInvalidField();
    return; // Prevent save
  }

  // If valid, emit save event
  const request: UpdateProfileRequest = {
    firstName: this.formModel().firstName.trim(),
    lastName: this.formModel().lastName.trim()
  };
  this.save.emit(request);
}
```

---

### Error Scenario 5: Password Reset Email Send Failure

**Scenario**: User clicks "Change Password", but email cannot be sent (server error, email service down)

**Handling**:
1. User clicks "Change Password" button
2. API call to initiate password reset fails
3. Catch error in ProfileComponent's `onChangePassword()` method
4. Show error toast: "Unable to send password reset email. Please try again later."
5. Log error to console for debugging
6. User can retry by clicking "Change Password" again

**User Impact**: User is informed of the issue and can try again later

**Implementation**:
```typescript
protected onChangePassword() {
  this.authService.initiatePasswordReset()
    .pipe(takeUntilDestroyed(this.destroyRef))
    .subscribe({
      next: () => {
        this.toastService.showSuccess('Password reset link sent to your email');
      },
      error: (err) => {
        this.toastService.showError('Unable to send password reset email. Please try again later.');
        console.error('Password reset error:', err);
      }
    });
}
```

---

### Error Scenario 6: 500 Internal Server Error

**Scenario**: Server encounters an unexpected error while processing request

**Handling**:
1. API call returns `500 Internal Server Error`
2. Catch error in ProfileComponent
3. Set `error` signal: "An unexpected error occurred. Please try again later."
4. Set `isLoading` or `isSaving` to `false`
5. Display error in ErrorAlertComponent or toast
6. Log error details to console
7. Optionally provide "Retry" button

**User Impact**: User is informed of a server issue and can retry later

**Implementation**:
```typescript
error: (err) => {
  if (err.status >= 500) {
    this.error.set('An unexpected error occurred. Please try again later.');
  } else {
    this.error.set('Unable to update profile. Please try again.');
  }
  this.isSaving.set(false);
  console.error('Profile update error:', err);
}
```

---

### Error Scenario 7: Concurrent Modification (Optional)

**Scenario**: Profile is modified in another session, causing a conflict when saving

**Handling** (if API implements conflict detection):
1. API returns `409 Conflict`
2. Show error: "Your profile was modified elsewhere. Please refresh and try again."
3. Provide "Refresh" button to reload profile
4. User's current changes are lost (warn user)
5. After refresh, user can make changes again

**User Impact**: User is aware of the conflict and can refresh to get the latest data

**Implementation** (if needed):
```typescript
if (err.status === 409) {
  this.error.set('Your profile was modified elsewhere. Please refresh and try again.');
  // Optionally auto-reload profile
  this.loadProfile();
}
```

---

### General Error Handling Best Practices

1. **Always log errors to console** for debugging (include stack trace if available)
2. **Show user-friendly messages** (don't expose technical details)
3. **Provide actionable feedback** (e.g., "Please try again", "Check your connection")
4. **Distinguish between error types** (network, validation, server, auth)
5. **Use toasts for transient feedback** (success, minor errors)
6. **Use inline alerts for persistent issues** (load failures, critical errors)
7. **Maintain UI state appropriately** (don't leave spinners running, re-enable forms)
8. **Consider retry mechanisms** for transient failures (network issues, timeouts)

---

## 11. Implementation Steps

### Step 1: Set Up Routing

**Action**: Configure the route for the Profile View

**Tasks**:
1. Open the routing configuration file (e.g., `app.routes.ts`)
2. Add a new route for the profile view:
   ```typescript
   {
     path: 'profile',
     component: ProfileComponent,
     canActivate: [AuthGuard]
   }
   ```
3. Ensure `AuthGuard` is imported and checks authentication state
4. Verify the route is accessible at `/profile` when logged in

---

### Step 2: Create Type Definitions

**Action**: Define all DTOs and ViewModels in a shared types file

**Tasks**:
1. Create file: `src/app/features/profile/models/profile.types.ts`
2. Define all interfaces:
   ```typescript
   export interface GetProfileResponse {
     userId: string;
     email: string;
     firstName: string;
     lastName: string;
     createdAt: string;
     lastLoginAt: string;
   }

   export interface UpdateProfileRequest {
     firstName: string;
     lastName: string;
   }

   export type UpdateProfileResponse = GetProfileResponse;

   export interface ProfileViewModel {
     userId: string;
     email: string;
     firstName: string;
     lastName: string;
     createdAt: string;
     lastLoginAt: string;
     createdAtRaw: Date;
     lastLoginAtRaw: Date;
   }

   export interface ProfileFormModel {
     firstName: string;
     lastName: string;
   }

   export type ValidationErrors = Record<string, string>;
   ```
3. Verify types are exported correctly

---

### Step 3: Create ProfileService

**Action**: Implement service for API calls

**Tasks**:
1. Create file: `src/app/features/profile/services/profile.service.ts`
2. Generate service: `ng generate service features/profile/services/profile` (if using CLI)
3. Implement service:
   ```typescript
   import { Injectable, inject } from '@angular/core';
   import { HttpClient } from '@angular/common/http';
   import { Observable } from 'rxjs';
   import { GetProfileResponse, UpdateProfileRequest, UpdateProfileResponse } from '../models/profile.types';

   @Injectable({ providedIn: 'root' })
   export class ProfileService {
     private http = inject(HttpClient);
     private readonly apiUrl = '/api/profile';

     getProfile(): Observable<GetProfileResponse> {
       return this.http.get<GetProfileResponse>(this.apiUrl);
     }

     updateProfile(request: UpdateProfileRequest): Observable<UpdateProfileResponse> {
       return this.http.put<UpdateProfileResponse>(this.apiUrl, request);
     }
   }
   ```
4. Test service methods (optional unit tests)

---

### Step 4: Create Date Formatting Utility

**Action**: Implement helper function to format dates in Polish locale

**Tasks**:
1. Install date-fns if not already installed: `npm install date-fns`
2. Create file: `src/app/shared/utils/date-format.util.ts`
3. Implement formatting function:
   ```typescript
   import { format } from 'date-fns';
   import { pl } from 'date-fns/locale';

   export function formatDateInPolish(isoString: string): string {
     const date = new Date(isoString);
     return format(date, 'd MMMM yyyy, HH:mm', { locale: pl });
   }
   ```
4. Test with sample dates

---

### Step 5: Create ViewModel Mapper

**Action**: Implement function to transform DTOs to ViewModels

**Tasks**:
1. Create file: `src/app/features/profile/utils/profile.mapper.ts`
2. Implement mapper:
   ```typescript
   import { GetProfileResponse, ProfileViewModel } from '../models/profile.types';
   import { formatDateInPolish } from '../../../shared/utils/date-format.util';

   export function mapToProfileViewModel(dto: GetProfileResponse): ProfileViewModel {
     return {
       userId: dto.userId,
       email: dto.email,
       firstName: dto.firstName,
       lastName: dto.lastName,
       createdAt: formatDateInPolish(dto.createdAt),
       lastLoginAt: formatDateInPolish(dto.lastLoginAt),
       createdAtRaw: new Date(dto.createdAt),
       lastLoginAtRaw: new Date(dto.lastLoginAt)
     };
   }
   ```
3. Test mapper with sample data

---

### Step 6: Create ProfileComponent (Container)

**Action**: Implement the smart component that manages state and API calls

**Tasks**:
1. Generate component: `ng generate component features/profile/profile --standalone`
2. Set up component structure:
   ```typescript
   import { Component, OnInit, signal, inject, ChangeDetectionStrategy } from '@angular/core';
   import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
   import { ProfileService } from './services/profile.service';
   import { ProfileViewModel, UpdateProfileRequest } from './models/profile.types';
   import { mapToProfileViewModel } from './utils/profile.mapper';

   @Component({
     selector: 'app-profile',
     templateUrl: './profile.component.html',
     styleUrls: ['./profile.component.css'],
     changeDetection: ChangeDetectionStrategy.OnPush,
     standalone: true,
     imports: [
       // Will add child components later
     ]
   })
   export class ProfileComponent implements OnInit {
     private profileService = inject(ProfileService);
     private destroyRef = inject(DestroyRef);

     protected profile = signal<ProfileViewModel | null>(null);
     protected isLoading = signal<boolean>(true);
     protected isEditing = signal<boolean>(false);
     protected isSaving = signal<boolean>(false);
     protected error = signal<string | null>(null);

     ngOnInit() {
       this.loadProfile();
     }

     private loadProfile() {
       // Implementation in next step
     }

     protected onSave(request: UpdateProfileRequest) {
       // Implementation in next step
     }

     protected onCancel() {
       this.isEditing.set(false);
     }

     protected onChangePassword() {
       // Implementation in next step
     }
   }
   ```

---

### Step 7: Implement ProfileComponent Methods

**Action**: Complete the implementation of API call methods

**Tasks**:
1. Implement `loadProfile()`:
   ```typescript
   private loadProfile() {
     this.isLoading.set(true);
     this.error.set(null);

     this.profileService.getProfile()
       .pipe(takeUntilDestroyed(this.destroyRef))
       .subscribe({
         next: (response) => {
           const viewModel = mapToProfileViewModel(response);
           this.profile.set(viewModel);
           this.isLoading.set(false);
         },
         error: (err) => {
           this.error.set('Unable to load profile. Please try again.');
           this.isLoading.set(false);
           console.error('Profile load error:', err);
         }
       });
   }
   ```

2. Implement `onSave()`:
   ```typescript
   protected onSave(request: UpdateProfileRequest) {
     this.isSaving.set(true);
     this.error.set(null);

     this.profileService.updateProfile(request)
       .pipe(takeUntilDestroyed(this.destroyRef))
       .subscribe({
         next: (response) => {
           const viewModel = mapToProfileViewModel(response);
           this.profile.set(viewModel);
           this.isSaving.set(false);
           this.isEditing.set(false);
           // Show success toast (implement toast service)
         },
         error: (err) => {
           this.isSaving.set(false);
           if (err.status === 400) {
             this.error.set('Validation failed. Please check your inputs.');
           } else {
             this.error.set('Unable to update profile. Please try again.');
           }
           console.error('Profile update error:', err);
         }
       });
   }
   ```

3. Implement `onChangePassword()`:
   ```typescript
   protected onChangePassword() {
     // Placeholder - implement when auth endpoint is available
     console.log('Change password clicked');
     // this.authService.initiatePasswordReset()...
   }
   ```

---

### Step 8: Create ProfileInfoComponent (Presentational)

**Action**: Implement the presentational component for displaying and editing profile

**Tasks**:
1. Generate component: `ng generate component features/profile/components/profile-info --standalone`
2. Set up component structure:
   ```typescript
   import { Component, input, output, signal, computed, ChangeDetectionStrategy } from '@angular/core';
   import { ProfileViewModel, UpdateProfileRequest, ProfileFormModel, ValidationErrors } from '../../models/profile.types';

   @Component({
     selector: 'app-profile-info',
     templateUrl: './profile-info.component.html',
     styleUrls: ['./profile-info.component.css'],
     changeDetection: ChangeDetectionStrategy.OnPush,
     standalone: true,
     imports: [
       // Will add form and UI components
     ]
   })
   export class ProfileInfoComponent {
     // Inputs
     profile = input.required<ProfileViewModel>();
     isEditing = input.required<boolean>();
     isSaving = input<boolean>(false);

     // Outputs
     save = output<UpdateProfileRequest>();
     cancel = output<void>();
     changePassword = output<void>();

     // Local state
     protected formModel = signal<ProfileFormModel>({ firstName: '', lastName: '' });
     protected validationErrors = signal<ValidationErrors>({});
     protected isFormValid = computed(() => Object.keys(this.validationErrors()).length === 0);

     // Methods will be implemented in next step
   }
   ```

---

### Step 9: Implement ProfileInfoComponent Validation

**Action**: Add validation logic for form fields

**Tasks**:
1. Add validation methods:
   ```typescript
   private validateFirstName(value: string): string | null {
     if (!value || value.trim().length === 0) {
       return 'First name is required';
     }
     if (value.length > 100) {
       return 'First name must be 100 characters or less';
     }
     return null;
   }

   private validateLastName(value: string): string | null {
     if (!value || value.trim().length === 0) {
       return 'Last name is required';
     }
     if (value.length > 100) {
       return 'Last name must be 100 characters or less';
     }
     return null;
   }

   private validateForm(): ValidationErrors {
     const errors: ValidationErrors = {};
     const form = this.formModel();

     const firstNameError = this.validateFirstName(form.firstName);
     if (firstNameError) errors.firstName = firstNameError;

     const lastNameError = this.validateLastName(form.lastName);
     if (lastNameError) errors.lastName = lastNameError;

     return errors;
   }

   protected onFieldBlur(fieldName: 'firstName' | 'lastName') {
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
   ```

2. Add interaction methods:
   ```typescript
   protected onEditClick() {
     this.formModel.set({
       firstName: this.profile().firstName,
       lastName: this.profile().lastName
     });
     this.validationErrors.set({});
     // Trigger edit mode in parent (or handle via internal signal if managing locally)
   }

   protected onSaveClick() {
     const errors = this.validateForm();
     this.validationErrors.set(errors);

     if (Object.keys(errors).length > 0) {
       return; // Prevent save if invalid
     }

     const request: UpdateProfileRequest = {
       firstName: this.formModel().firstName.trim(),
       lastName: this.formModel().lastName.trim()
     };

     this.save.emit(request);
   }

   protected onCancelClick() {
     this.validationErrors.set({});
     this.cancel.emit();
   }

   protected onChangePasswordClick() {
     this.changePassword.emit();
   }

   protected updateField(fieldName: 'firstName' | 'lastName', value: string) {
     const updated = { ...this.formModel(), [fieldName]: value };
     this.formModel.set(updated);

     // Clear error when user starts typing
     const errors = { ...this.validationErrors() };
     delete errors[fieldName];
     this.validationErrors.set(errors);
   }
   ```

---

### Step 10: Create ProfileInfoComponent Template

**Action**: Build the HTML template with Tailwind CSS and Flowbite styling

**Tasks**:
1. Create the template in `profile-info.component.html`:
   ```html
   <div class="max-w-2xl mx-auto p-4">
     <!-- Profile Card -->
     <div class="bg-white rounded-lg shadow-md p-6">
       <!-- Header -->
       <h2 class="text-2xl font-bold mb-6">
         @if (isEditing()) {
           Edit Profile
         } @else {
           Profile
         }
       </h2>

       <!-- Read-only Mode -->
       @if (!isEditing()) {
         <div class="space-y-4">
           <!-- First Name -->
           <div>
             <label class="block text-sm font-medium text-gray-700 mb-1">First Name</label>
             <p class="text-gray-900">{{ profile().firstName }}</p>
           </div>

           <!-- Last Name -->
           <div>
             <label class="block text-sm font-medium text-gray-700 mb-1">Last Name</label>
             <p class="text-gray-900">{{ profile().lastName }}</p>
           </div>

           <!-- Email (always read-only) -->
           <div>
             <label class="block text-sm font-medium text-gray-700 mb-1">Email</label>
             <p class="text-gray-900">{{ profile().email }}</p>
           </div>

           <!-- Created At -->
           <div>
             <label class="block text-sm font-medium text-gray-700 mb-1">Member Since</label>
             <p class="text-gray-900">{{ profile().createdAt }}</p>
           </div>

           <!-- Last Login -->
           <div>
             <label class="block text-sm font-medium text-gray-700 mb-1">Last Login</label>
             <p class="text-gray-900">{{ profile().lastLoginAt }}</p>
           </div>
         </div>
       }

       <!-- Edit Mode -->
       @if (isEditing()) {
         <div class="space-y-4">
           <!-- First Name Input -->
           <div>
             <label for="firstName" class="block text-sm font-medium text-gray-700 mb-1">
               First Name <span class="text-red-600">*</span>
             </label>
             <input
               type="text"
               id="firstName"
               [value]="formModel().firstName"
               (input)="updateField('firstName', $any($event.target).value)"
               (blur)="onFieldBlur('firstName')"
               [disabled]="isSaving()"
               [class.border-red-600]="validationErrors()['firstName']"
               class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
             />
             @if (validationErrors()['firstName']) {
               <span class="text-sm text-red-600 mt-1">{{ validationErrors()['firstName'] }}</span>
             }
           </div>

           <!-- Last Name Input -->
           <div>
             <label for="lastName" class="block text-sm font-medium text-gray-700 mb-1">
               Last Name <span class="text-red-600">*</span>
             </label>
             <input
               type="text"
               id="lastName"
               [value]="formModel().lastName"
               (input)="updateField('lastName', $any($event.target).value)"
               (blur)="onFieldBlur('lastName')"
               [disabled]="isSaving()"
               [class.border-red-600]="validationErrors()['lastName']"
               class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
             />
             @if (validationErrors()['lastName']) {
               <span class="text-sm text-red-600 mt-1">{{ validationErrors()['lastName'] }}</span>
             }
           </div>

           <!-- Email (read-only even in edit mode) -->
           <div>
             <label class="block text-sm font-medium text-gray-700 mb-1">Email</label>
             <p class="text-gray-500">{{ profile().email }}</p>
             <span class="text-xs text-gray-500">Email cannot be changed</span>
           </div>
         </div>
       }

       <!-- Action Buttons -->
       <div class="mt-6 flex gap-3">
         @if (!isEditing()) {
           <button
             (click)="onEditClick()"
             class="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500"
           >
             Edit
           </button>
         }

         @if (isEditing()) {
           <button
             (click)="onSaveClick()"
             [disabled]="isSaving()"
             class="px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 disabled:opacity-50 disabled:cursor-not-allowed"
           >
             @if (isSaving()) {
               <span>Saving...</span>
             } @else {
               <span>Save</span>
             }
           </button>

           <button
             (click)="onCancelClick()"
             [disabled]="isSaving()"
             class="px-4 py-2 bg-gray-300 text-gray-700 rounded-md hover:bg-gray-400 focus:outline-none focus:ring-2 focus:ring-gray-500 disabled:opacity-50"
           >
             Cancel
           </button>
         }

         <button
           (click)="onChangePasswordClick()"
           class="px-4 py-2 bg-yellow-500 text-white rounded-md hover:bg-yellow-600 focus:outline-none focus:ring-2 focus:ring-yellow-400"
         >
           Change Password
         </button>
       </div>
     </div>
   </div>
   ```

2. Adjust Tailwind classes for responsive design and accessibility

---

### Step 11: Create ProfileComponent Template

**Action**: Build the container template that uses child components

**Tasks**:
1. Create the template in `profile.component.html`:
   ```html
   <div class="container mx-auto py-8">
     @if (isLoading()) {
       <app-loading-spinner message="Loading profile..." />
     }

     @if (!isLoading() && profile()) {
       <app-profile-info
         [profile]="profile()!"
         [isEditing]="isEditing()"
         [isSaving]="isSaving()"
         (save)="onSave($event)"
         (cancel)="onCancel()"
         (changePassword)="onChangePassword()"
       />
     }

     @if (error()) {
       <app-error-alert
         [message]="error()!"
         [dismissible]="true"
         (dismiss)="error.set(null)"
       />
     }
   </div>
   ```

2. Import child components in ProfileComponent:
   ```typescript
   imports: [
     LoadingSpinnerComponent,
     ProfileInfoComponent,
     ErrorAlertComponent
   ]
   ```

---

### Step 12: Implement Shared Components (if not already existing)

**Action**: Create or verify existence of LoadingSpinnerComponent and ErrorAlertComponent

**Tasks**:
1. **LoadingSpinnerComponent**:
   - If not exists, generate: `ng generate component shared/components/loading-spinner --standalone`
   - Implement simple spinner with optional message prop

2. **ErrorAlertComponent**:
   - If not exists, generate: `ng generate component shared/components/error-alert --standalone`
   - Implement alert banner with error message and optional dismiss button

---

### Step 13: Add Navigation Link to Profile

**Action**: Ensure users can navigate to the profile view

**Tasks**:
1. Open the navigation component (header/menu)
2. Add a link to `/profile`:
   ```html
   <a routerLink="/profile" class="nav-link">Profile</a>
   ```
3. Verify the link appears in navigation menu
4. Test navigation works correctly

---

### Step 14: Integrate with Toast Service

**Action**: Add success/error toast notifications

**Tasks**:
1. Verify ToastService exists or create one
2. Inject ToastService in ProfileComponent:
   ```typescript
   private toastService = inject(ToastService);
   ```
3. Show success toast after profile update:
   ```typescript
   this.toastService.showSuccess('Profile updated successfully');
   ```
4. Show error toasts for failures as needed
5. Implement toast for password reset email

---

### Step 15: Implement Password Reset Flow

**Action**: Complete the password reset integration

**Tasks**:
1. Check if password reset endpoint exists (e.g., `POST /api/auth/forgot-password`)
2. If exists:
   - Add method to AuthService:
     ```typescript
     initiatePasswordReset(): Observable<void> {
       return this.http.post<void>('/api/auth/forgot-password', {});
     }
     ```
3. Update ProfileComponent's `onChangePassword()`:
   ```typescript
   protected onChangePassword() {
     this.authService.initiatePasswordReset()
       .pipe(takeUntilDestroyed(this.destroyRef))
       .subscribe({
         next: () => {
           this.toastService.showSuccess('Password reset link sent to your email');
         },
         error: (err) => {
           this.toastService.showError('Unable to send password reset email');
           console.error('Password reset error:', err);
         }
       });
   }
   ```
4. If endpoint doesn't exist, coordinate with backend team to implement it

---

### Step 16: Add Polish Translations (i18n preparation)

**Action**: Replace hardcoded English text with Polish (MVP is Polish-only)

**Tasks**:
1. Update all user-facing text to Polish:
   - "Profile" → "Profil"
   - "Edit Profile" → "Edytuj profil"
   - "First Name" → "Imię"
   - "Last Name" → "Nazwisko"
   - "Email" → "Email"
   - "Member Since" → "Członek od"
   - "Last Login" → "Ostatnie logowanie"
   - "Edit" → "Edytuj"
   - "Save" → "Zapisz"
   - "Cancel" → "Anuluj"
   - "Change Password" → "Zmień hasło"
   - Error messages in Polish (as defined in validation section)
2. Prepare structure for future i18n (consider using `@angular/localize` or similar)

---

### Step 17: Style and Responsive Design

**Action**: Ensure the view is fully responsive and follows design guidelines

**Tasks**:
1. Test profile view on mobile, tablet, and desktop screen sizes
2. Adjust Tailwind breakpoints as needed:
   - Mobile: Full-width cards, single column
   - Tablet: Max-width container
   - Desktop: Centered layout with max-width
3. Ensure touch targets are at least 44x44px
4. Test button spacing and form layout
5. Verify Flowbite components are styled correctly

---

### Step 18: Add Unit Tests

**Action**: Write tests for components and services

**Tasks**:
1. **ProfileService Tests**:
   - Test `getProfile()` returns expected response
   - Test `updateProfile()` sends correct request
   - Mock HttpClient

2. **ProfileComponent Tests**:
   - Test component initializes and loads profile
   - Test save flow updates profile
   - Test cancel exits edit mode
   - Test error handling

3. **ProfileInfoComponent Tests**:
   - Test validation logic
   - Test form submission
   - Test event emissions

4. Run tests: `ng test`

---

### Step 19: Manual Testing

**Action**: Thoroughly test the profile view end-to-end

**Test Cases**:
1. Navigate to `/profile` while logged in → Profile loads successfully
2. Navigate to `/profile` while logged out → Redirected to login
3. Click "Edit" → Enter edit mode with input fields
4. Modify firstName and lastName → Values update in form
5. Click "Save" with valid data → Profile updates, exits edit mode, shows success toast
6. Click "Save" with empty firstName → Validation error shown, save prevented
7. Click "Save" with lastName > 100 chars → Validation error shown
8. Click "Cancel" during edit → Edit mode exited, changes discarded
9. Click "Change Password" → Success toast shown (or error if endpoint not ready)
10. Refresh page during edit → Changes discarded (expected behavior)
11. Test on mobile device → Layout is responsive, buttons are accessible
12. Test session expiry → Redirected to login, return to profile after login

---

### Step 20: Integration Testing with Backend

**Action**: Verify integration with actual backend API

**Tasks**:
1. Ensure backend is running locally or in test environment
2. Test GET `/api/profile` returns correct data
3. Test PUT `/api/profile` updates data correctly
4. Test validation errors from API are handled
5. Test 401 errors redirect to login
6. Test password reset endpoint (if available)
7. Fix any integration issues

---

### Step 21: Code Review and Refinement

**Action**: Review code for quality, consistency, and best practices

**Tasks**:
1. Review component structure and organization
2. Ensure all types are correctly defined and used
3. Check for proper error handling
4. Verify signals and computed values are used correctly
5. Ensure OnPush change detection works properly
6. Check for unused imports or code
7. Verify code follows Angular style guide
8. Address any linting errors or warnings

---

### Step 22: Documentation

**Action**: Document the profile feature for future developers

**Tasks**:
1. Add comments to complex logic
2. Document any assumptions or decisions
3. Update project README if needed
4. Document any known issues or future improvements
5. Add inline JSDoc comments for public methods

---

### Step 23: Accessibility (A11y) Review

**Action**: Ensure the profile view is accessible

**Tasks**:
1. Test keyboard navigation (Tab, Enter, Escape)
2. Verify focus states are visible
3. Ensure error messages are associated with form fields (aria-describedby)
4. Add aria-labels where needed
5. Test with screen reader (optional but recommended)
6. Verify color contrast meets WCAG standards
7. Ensure buttons have descriptive text

---

### Step 24: Performance Optimization

**Action**: Optimize performance where applicable

**Tasks**:
1. Verify OnPush change detection is working (check with Angular DevTools)
2. Ensure signals are used efficiently
3. Check for unnecessary API calls (e.g., multiple fetches on navigation)
4. Verify form doesn't cause excessive re-renders
5. Consider debouncing validation on input (optional for MVP)

---

### Step 25: Final Review and Deployment Preparation

**Action**: Prepare the feature for production deployment

**Tasks**:
1. Run full test suite: `ng test`
2. Run linting: `ng lint`
3. Build production bundle: `ng build --configuration production`
4. Verify bundle size is acceptable
5. Test production build locally
6. Create pull request with detailed description
7. Request code review from team
8. Address feedback and merge

---

## Summary

This implementation plan provides a comprehensive guide for building the Profile View in the SantaVibe application. The view allows users to view and edit their profile information with proper validation, error handling, and responsive design. Key features include:

- **Authentication**: Protected route accessible only to logged-in users
- **Data Display**: Shows firstName, lastName, email, createdAt, lastLoginAt
- **Editing**: Allows editing firstName and lastName with client-side validation
- **Password Reset**: Triggers email-based password reset flow
- **Responsive Design**: Works on mobile, tablet, and desktop devices
- **Error Handling**: Comprehensive error handling for various scenarios
- **State Management**: Uses Angular signals for reactive state
- **Type Safety**: Fully typed with interfaces for DTOs and ViewModels

Follow the implementation steps sequentially to build the feature incrementally, testing at each stage to ensure correctness and quality.
