# View Implementation Plan: Group Create View

## 1. Overview

The Group Create View is a simple, focused interface that allows authenticated users to create a new Secret Santa group. The view consists of a single-field form where users enter a group name. Upon successful creation, the user becomes both the organizer and the first participant of the group, and the system generates a unique shareable invitation link. The user is then redirected to the newly created group's details page with a success notification.

**Key Features:**
- Single-field form with group name input
- Real-time client-side validation
- Auto-focus on the group name field
- Loading state during API call
- Error handling with user-friendly messages
- Browser-native unsaved changes warning
- Automatic redirect to group details page after success
- Success toast notification with instructions

## 2. View Routing

**Route Path:** `/groups/create`

**Route Configuration:**
```typescript
{
  path: 'groups/create',
  component: GroupCreateComponent,
  canActivate: [AuthGuard],
  canDeactivate: [UnsavedChangesGuard]
}
```

**Guards:**
- `AuthGuard`: Ensures user is authenticated before accessing the view
- `UnsavedChangesGuard`: Prompts user with browser-native confirmation if navigating away with unsaved changes

## 3. Component Structure

```
GroupCreateComponent (Smart Container with inline form)
├── <form [formGroup]="form"> (Reactive Form)
│   ├── <div> form field container
│   │   ├── <label> for group name
│   │   ├── <input formControlName="groupName"> (auto-focused)
│   │   └── <span> validation error message
│   ├── <p> helper text
│   └── <button type="submit"> Create Group
├── ErrorAlertComponent (Conditional - displayed when error exists)
└── LoadingSpinnerComponent (Conditional - displayed during API call)
```

**Component Hierarchy:**
1. **GroupCreateComponent** (Container/Smart Component)
   - Manages form state using Reactive Forms (FormControl)
   - Uses signals only for non-form state (loading, errors)
   - Handles API integration via GroupService
   - Orchestrates navigation and toast notifications
   - Contains form UI directly (no separate form component needed)
   - Contains ErrorAlertComponent, LoadingSpinnerComponent

**Note:** The form is simple enough (single field) that a separate presentational form component is unnecessary. The form logic will be managed directly in GroupCreateComponent using Angular Reactive Forms.

2. **ErrorAlertComponent** (Shared Component)
   - Displays error messages
   - Provides dismiss functionality

3. **LoadingSpinnerComponent** (Shared Component)
   - Shows loading indicator during API calls
   - Optionally displays loading message

## 4. Component Details

### 4.1 GroupCreateComponent (Smart/Container Component)

**Component Description:**
The main container component responsible for managing the group creation flow. It uses Angular Reactive Forms (FormControl) for form state management and Signals only for non-form state (loading, errors). It coordinates form submission, API calls, navigation, and user notifications through integration with GroupService, ToastService, and Router.

**Main HTML Elements and Child Components:**
```html
<div class="min-h-screen bg-gray-50 py-8 px-4 sm:px-6 lg:px-8">
  <div class="max-w-md mx-auto">
    <!-- Header -->
    <div class="text-center mb-8">
      <h1 class="text-3xl font-bold text-gray-900">Create New Group</h1>
      <p class="mt-2 text-sm text-gray-600">
        Start a new Secret Santa gift exchange
      </p>
    </div>

    <!-- Error Alert -->
    @if (error()) {
      <app-error-alert
        [message]="error()!"
        (dismiss)="clearError()"
        class="mb-4">
      </app-error-alert>
    }

    <!-- Loading State -->
    @if (isLoading()) {
      <div class="bg-white rounded-lg shadow-md p-8">
        <app-loading-spinner
          message="Creating group...">
        </app-loading-spinner>
      </div>
    }

    <!-- Form -->
    @if (!isLoading()) {
      <div class="bg-white rounded-lg shadow-md p-6 sm:p-8">
        <form [formGroup]="form" (ngSubmit)="onSubmit()" novalidate class="space-y-6">
          <!-- Group Name Field -->
          <div class="form-field">
            <label for="groupName" class="block text-sm font-medium text-gray-700 mb-2">
              Group Name <span class="text-red-600">*</span>
            </label>

            <input
              #groupNameInput
              type="text"
              id="groupName"
              formControlName="groupName"
              [class.border-red-500]="groupNameControl.invalid && groupNameControl.touched"
              [class.border-gray-300]="groupNameControl.valid || !groupNameControl.touched"
              maxlength="200"
              class="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 disabled:bg-gray-100 disabled:cursor-not-allowed transition-colors"
              placeholder="e.g., Family Secret Santa 2025"
              autocomplete="off"
            />

            @if (groupNameControl.invalid && groupNameControl.touched) {
              <span class="text-red-600 text-sm mt-1 block flex items-center">
                <svg class="h-4 w-4 mr-1" fill="currentColor" viewBox="0 0 20 20">
                  <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clip-rule="evenodd" />
                </svg>
                {{ getErrorMessage() }}
              </span>
            }

            <!-- Character counter -->
            <span class="text-gray-500 text-xs mt-1 block">
              {{ groupNameControl.value?.length || 0 }} / 200 characters
            </span>
          </div>

          <!-- Helper Text -->
          <div class="bg-blue-50 border border-blue-200 rounded-lg p-3">
            <p class="text-sm text-blue-800 flex items-start">
              <svg class="h-5 w-5 mr-2 flex-shrink-0 text-blue-600" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
              </svg>
              You'll be automatically added as a participant and organizer
            </p>
          </div>

          <!-- Submit Button -->
          <button
            type="submit"
            [disabled]="form.invalid"
            class="w-full bg-blue-600 text-white font-medium py-3 px-4 rounded-lg hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:bg-gray-400 disabled:cursor-not-allowed transition-colors"
          >
            Create Group
          </button>
        </form>
      </div>
    }

    <!-- Back Link -->
    <div class="mt-6 text-center">
      <a
        routerLink="/groups"
        class="text-sm text-blue-600 hover:text-blue-800 hover:underline">
        ← Back to My Groups
      </a>
    </div>
  </div>
</div>
```

**Handled Events:**
- `(ngSubmit)="onSubmit()"`: Form submission, triggers API call
- `(dismiss)="clearError()"`: Clears error message when user dismisses error alert
- Navigation events handled internally via Router service

**Validation Conditions:**
- **Required**: Group name cannot be empty (Validators.required)
- **Min Length**: Minimum 3 characters (Validators.minLength(3))
- **Max Length**: Maximum 200 characters (Validators.maxLength(200))
- Form validation handled by Angular Reactive Forms validators
- Handles server-side validation errors from API response

**Types:**
- `CreateGroupRequest`: Request DTO for API call
- `CreateGroupResponse`: Response DTO from API call

**Props:**
None (route component, no inputs)

**Component Class Structure:**
```typescript
@Component({
  selector: 'app-group-create',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    ErrorAlertComponent,
    LoadingSpinnerComponent
  ],
  templateUrl: './group-create.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class GroupCreateComponent implements AfterViewInit {
  // Injected Services
  private groupService = inject(GroupService);
  private toastService = inject(ToastService);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  // View Children
  @ViewChild('groupNameInput') groupNameInput?: ElementRef<HTMLInputElement>;

  // Reactive Form
  form = this.fb.group({
    groupName: ['', [
      Validators.required,
      Validators.minLength(3),
      Validators.maxLength(200)
    ]]
  });

  // Convenience getter for form control
  get groupNameControl(): FormControl {
    return this.form.get('groupName') as FormControl;
  }

  // State Signals (only for non-form state)
  isLoading = signal<boolean>(false);
  error = signal<string | null>(null);

  // Lifecycle Hooks
  ngAfterViewInit(): void {
    // Auto-focus on group name input
    afterNextRender(() => {
      this.groupNameInput?.nativeElement.focus();
    });
  }

  // Methods
  onSubmit(): void {
    if (this.form.invalid) {
      this.groupNameControl.markAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.error.set(null);

    const request: CreateGroupRequest = {
      name: this.groupNameControl.value.trim()
    };

    this.groupService.createGroup(request).subscribe({
      next: (response: CreateGroupResponse) => {
        this.isLoading.set(false);
        this.form.markAsPristine();

        this.toastService.showSuccess(
          'Group created! Copy invitation link to invite participants'
        );

        this.router.navigate(['/groups', response.groupId]);
      },
      error: (error: any) => {
        this.isLoading.set(false);
        const errorMessage = this.parseErrorMessage(error);
        this.error.set(errorMessage);
      }
    });
  }

  clearError(): void {
    this.error.set(null);
  }

  getErrorMessage(): string {
    const control = this.groupNameControl;

    if (control.hasError('required')) {
      return 'Group name is required';
    }

    if (control.hasError('minlength')) {
      return 'Group name must be at least 3 characters';
    }

    if (control.hasError('maxlength')) {
      return 'Group name cannot exceed 200 characters';
    }

    return '';
  }

  canDeactivate(): boolean {
    if (this.form.pristine) {
      return true;
    }

    return window.confirm(
      'You have unsaved changes. Are you sure you want to leave?'
    );
  }

  private parseErrorMessage(error: any): string {
    // Error parsing logic (same as before)
    if (error.status === 0) {
      return 'Unable to connect to server. Please check your internet connection.';
    }

    if (error.status === 400 && error.error?.details?.name) {
      return error.error.details.name[0];
    }

    if (error.status === 400) {
      return 'Validation error. Please check your input.';
    }

    if (error.status === 429) {
      return 'Too many requests. Please wait a moment and try again.';
    }

    if (error.status >= 500) {
      return 'An unexpected error occurred. Please try again later.';
    }

    if (error.error?.message) {
      return error.error.message;
    }

    return 'Unable to create group. Please try again.';
  }
}
```

---

### 4.2 ErrorAlertComponent (Shared Component)

**Component Description:**
A reusable presentational component that displays error messages in a styled alert box. It includes a dismiss button and uses Tailwind CSS for styling. The component can be used throughout the application for consistent error messaging.

**Main HTML Elements:**
```html
<div class="bg-red-50 border border-red-400 text-red-800 px-4 py-3 rounded relative mb-4" role="alert">
  <div class="flex items-start">
    <div class="flex-shrink-0">
      <!-- Error Icon -->
      <svg class="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
        <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
      </svg>
    </div>

    <div class="ml-3 flex-1">
      <p class="text-sm">{{ message() }}</p>
    </div>

    @if (dismissible()) {
      <div class="ml-auto pl-3">
        <button
          type="button"
          (click)="onDismiss()"
          class="inline-flex bg-red-50 rounded-md p-1.5 text-red-500 hover:bg-red-100 focus:outline-none focus:ring-2 focus:ring-red-600"
        >
          <span class="sr-only">Dismiss</span>
          <!-- Close Icon -->
          <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
          </svg>
        </button>
      </div>
    }
  </div>
</div>
```

**Handled Events:**
- `(click)="onDismiss()"`: Emits dismiss event when close button clicked

**Validation Conditions:**
None (presentational only)

**Types:**
None (uses primitive types)

**Props (Component Interface):**
```typescript
// Inputs
message = input.required<string>(); // Error message to display
dismissible = input<boolean>(true); // Whether to show dismiss button

// Outputs
dismiss = output<void>(); // Emits when user dismisses the alert
```

---

### 4.3 LoadingSpinnerComponent (Shared Component)

**Component Description:**
A reusable presentational component that displays a loading spinner with an optional message. Used to indicate ongoing asynchronous operations.

**Main HTML Elements:**
```html
<div class="flex flex-col items-center justify-center p-8">
  <!-- Spinner -->
  <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>

  @if (message()) {
    <p class="mt-4 text-gray-600 text-sm">{{ message() }}</p>
  }
</div>
```

**Handled Events:**
None

**Validation Conditions:**
None (presentational only)

**Types:**
None (uses primitive types)

**Props (Component Interface):**
```typescript
// Inputs
message = input<string>(''); // Optional loading message
```

---

## 5. Types

### 5.1 Request/Response DTOs (API Contract)

#### CreateGroupRequest
```typescript
/**
 * Request DTO for creating a new group
 * Sent to: POST /api/groups
 */
export interface CreateGroupRequest {
  /** Group name - Required, min 3 chars, max 200 chars */
  name: string;
}
```

#### CreateGroupResponse
```typescript
/**
 * Response DTO from successful group creation
 * Received from: POST /api/groups
 */
export interface CreateGroupResponse {
  /** Unique identifier for the created group (UUID format) */
  groupId: string;

  /** Name of the group */
  name: string;

  /** User ID of the group organizer (UUID format) */
  organizerId: string;

  /** Full name of the organizer */
  organizerName: string;

  /** Unique invitation token (UUID format) */
  invitationToken: string;

  /** Full invitation URL that can be shared with participants */
  invitationLink: string;

  /** Current number of participants (will be 1 after creation - the organizer) */
  participantCount: number;

  /** Budget for the group (null until set by organizer) */
  budget: number | null;

  /** Whether the Secret Santa draw has been completed (always false on creation) */
  drawCompleted: boolean;

  /** Timestamp when the group was created (ISO 8601 format) */
  createdAt: string;
}
```

### 5.2 ViewModels (Frontend-Specific Types)

**Note:** With Reactive Forms, we don't need custom interfaces for form state. The form state is managed by Angular's `FormGroup` and `FormControl` classes, which provide built-in properties like `value`, `valid`, `pristine`, `touched`, etc.

The only custom state we need is for non-form concerns (loading, errors), which we manage with signals directly without needing an interface.

### 5.3 Error Response Types

#### ApiErrorResponse
```typescript
/**
 * Standard error response structure from the API
 * Used for handling validation and server errors
 */
export interface ApiErrorResponse {
  /** Error type/code */
  error: string;

  /** Human-readable error message */
  message: string;

  /** Field-specific validation errors (for 400 Bad Request) */
  details?: {
    [fieldName: string]: string[];
  };
}
```

---

## 6. State Management

### 6.1 State Architecture

The Group Create View uses a **hybrid approach** combining Angular Reactive Forms for form state and Signals for non-form state:

**Form State:** Managed by Angular Reactive Forms (`FormGroup` and `FormControl`)
- Group name value
- Validation state
- Pristine/dirty status
- Touched status

**Non-Form State:** Managed by Angular Signals
- Loading state
- Error messages

This approach leverages Angular's built-in form capabilities while using signals for simpler reactive updates for non-form concerns.

### 6.2 State Location

**GroupCreateComponent:**
- All state is local to this component
- No shared state needed beyond this component
- Form state managed by `FormGroup`
- Non-form state managed by signals

### 6.3 Form State (Reactive Forms)

```typescript
export class GroupCreateComponent {
  // Reactive Form
  form = this.fb.group({
    groupName: ['', [
      Validators.required,
      Validators.minLength(3),
      Validators.maxLength(200)
    ]]
  });

  // Convenience getter
  get groupNameControl(): FormControl {
    return this.form.get('groupName') as FormControl;
  }
}
```

**Form Properties (Built-in):**
- `form.value.groupName`: Current value of group name
- `form.valid`: Whether form passes all validations
- `form.invalid`: Whether form has validation errors
- `form.pristine`: Whether form hasn't been modified
- `form.dirty`: Whether form has been modified
- `groupNameControl.touched`: Whether field has been focused and blurred
- `groupNameControl.errors`: Object containing validation errors

### 6.4 Non-Form State (Signals)

```typescript
export class GroupCreateComponent {
  // Signals for non-form state
  isLoading = signal<boolean>(false);    // API call in progress
  error = signal<string | null>(null);   // Error message to display
}
```

### 6.5 State Transitions

#### Initial State (Page Load)
```typescript
// Form State
form.value = { groupName: '' }
form.valid = false
form.pristine = true
groupNameControl.touched = false

// Non-Form State
isLoading = false
error = null
```

#### State During User Input
```typescript
// Form State (automatically updated by Angular)
form.value = { groupName: 'My Fam' }  // Updated as user types
form.valid = true  // If >= 3 chars
form.pristine = false  // Changed after first input
form.dirty = true

// Non-Form State (unchanged)
isLoading = false
error = null
```

#### State During API Call
```typescript
// Form State (unchanged during submission)
form.value = { groupName: 'My Family Secret Santa' }
form.valid = true
form.dirty = true

// Non-Form State
isLoading = true  // Set when onSubmit() is called
error = null  // Cleared before API call
```

#### State After Error
```typescript
// Form State (unchanged)
form.value = { groupName: 'My Family Secret Santa' }
form.valid = true
form.dirty = true  // Remains true so user can retry

// Non-Form State
isLoading = false  // Set to false after error
error = 'Unable to create group. Please try again.'
```

#### State After Success
Component navigates away, so state is destroyed.

### 6.6 State Management Benefits

**Why This Approach:**

1. **Leverages Framework Features:** Reactive Forms provide built-in validation, dirty/pristine tracking, and error handling
2. **Less Boilerplate:** No need to manually manage form state with signals
3. **Type Safety:** FormControl provides type checking for form values
4. **Simplicity:** Signals used only where needed (loading, errors)
5. **Idiomatic Angular:** Follows Angular best practices for form handling

### 6.7 Services (No Custom Hooks)

Angular uses **Services** instead of React-style custom hooks. For this view:

**GroupService** (Injected Service):
- Provides `createGroup(request: CreateGroupRequest)` method
- Returns `Observable<CreateGroupResponse>`
- Handles HTTP communication with the API

**ToastService** (Injected Service):
- Provides `showSuccess(message: string)` method
- Manages toast notifications globally

**Router** (Angular Service):
- Provides navigation functionality
- `navigate(['/groups', groupId])` for redirection

### 6.8 State Persistence

No state persistence required. The form is simple enough that if a user navigates away, they can easily re-enter the group name. The unsaved changes warning (checking `form.pristine`) provides sufficient protection against accidental navigation.

---

## 7. API Integration

### 7.1 Service Integration

The GroupCreateComponent integrates with the backend API through the **GroupService**, which encapsulates all HTTP communication related to groups.

### 7.2 GroupService Method

**Location:** `src/app/services/group.service.ts`

```typescript
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';
import { CreateGroupRequest, CreateGroupResponse } from '@models/group.models';

@Injectable({
  providedIn: 'root'
})
export class GroupService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/groups`;

  /**
   * Creates a new Secret Santa group
   * @param request - The group creation request containing the group name
   * @returns Observable of the created group details
   */
  createGroup(request: CreateGroupRequest): Observable<CreateGroupResponse> {
    return this.http.post<CreateGroupResponse>(this.apiUrl, request);
  }
}
```

### 7.3 Request Configuration

**HTTP Method:** `POST`

**Endpoint:** `/api/groups`

**Request Headers:**
- `Content-Type: application/json`
- `Authorization: Bearer {jwt-token}` (automatically added by AuthInterceptor)

**Request Body Type:** `CreateGroupRequest`
```typescript
{
  name: string  // Group name (min 3, max 200 chars)
}
```

**Example Request:**
```json
{
  "name": "Family Secret Santa 2025"
}
```

### 7.4 Response Handling

**Success Response Type:** `CreateGroupResponse`

**Status Code:** `201 Created`

**Response Body:**
```typescript
{
  groupId: string;           // "7c9e6679-7425-40de-944b-e07fc1f90ae7"
  name: string;              // "Family Secret Santa 2025"
  organizerId: string;       // "550e8400-e29b-41d4-a716-446655440000"
  organizerName: string;     // "Jan Kowalski"
  invitationToken: string;   // "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d"
  invitationLink: string;    // "https://santavibe.com/invite/a1b2c3d4..."
  participantCount: number;  // 1
  budget: number | null;     // null
  drawCompleted: boolean;    // false
  createdAt: string;         // "2025-10-15T10:00:00Z"
}
```

### 7.5 Error Response Handling

**Error Response Type:** `ApiErrorResponse`

#### 400 Bad Request (Validation Error)
```json
{
  "error": "ValidationError",
  "message": "Validation failed",
  "details": {
    "name": ["Group name must be at least 3 characters"]
  }
}
```

**Handling:**
- Parse `details` object to extract field-specific errors
- Display validation error for the `name` field
- Keep form data intact for user correction

#### 401 Unauthorized (Authentication Error)
```json
{
  "error": "Unauthorized",
  "message": "Invalid or expired token"
}
```

**Handling:**
- Caught by ErrorInterceptor globally
- User redirected to `/login`
- Current URL stored as return URL
- Toast message: "Your session has expired. Please log in again."

#### 429 Too Many Requests (Rate Limiting)
```json
{
  "error": "TooManyRequests",
  "message": "Rate limit exceeded. Please try again later."
}
```

**Handling:**
- Display error message to user
- Suggest waiting before retrying

#### 500 Internal Server Error
```json
{
  "error": "InternalServerError",
  "message": "An unexpected error occurred"
}
```

**Handling:**
- Display generic error message
- Suggest user try again or contact support

### 7.6 Component Integration Pattern

```typescript
export class GroupCreateComponent {
  private groupService = inject(GroupService);
  private toastService = inject(ToastService);
  private router = inject(Router);

  isLoading = signal<boolean>(false);
  error = signal<string | null>(null);

  onFormSubmit(groupName: string): void {
    // Set loading state
    this.isLoading.set(true);
    this.error.set(null);

    // Prepare request
    const request: CreateGroupRequest = {
      name: groupName
    };

    // Make API call
    this.groupService.createGroup(request)
      .subscribe({
        next: (response: CreateGroupResponse) => {
          // Success handling
          this.isLoading.set(false);

          // Show success toast
          this.toastService.showSuccess(
            'Group created! Copy invitation link to invite participants'
          );

          // Navigate to group details
          this.router.navigate(['/groups', response.groupId]);
        },
        error: (error: any) => {
          // Error handling
          this.isLoading.set(false);

          // Parse and display error message
          const errorMessage = this.parseErrorMessage(error);
          this.error.set(errorMessage);
        }
      });
  }

  private parseErrorMessage(error: any): string {
    // Handle HttpErrorResponse
    if (error.status === 400 && error.error?.details?.name) {
      return error.error.details.name[0];
    }

    if (error.error?.message) {
      return error.error.message;
    }

    // Generic fallback
    return 'Unable to create group. Please try again.';
  }
}
```

### 7.7 Interceptors

**AuthInterceptor:**
- Automatically adds JWT token to request headers
- No manual token handling required in component

**ErrorInterceptor:**
- Catches 401 errors globally
- Handles authentication failures
- Redirects to login when necessary

---

## 8. User Interactions

### 8.1 Interaction Flow Diagram

```
User Lands on Page
       ↓
Page Loads → Auto-focus on Input Field
       ↓
User Types Group Name → Real-time Validation → Show/Hide Errors
       ↓
User Clicks "Create Group"
       ↓
Form Valid?
  ├─ No → Show Validation Errors
  └─ Yes → Submit to API
             ↓
          Loading State → Disable Form, Show Spinner
             ↓
          API Response?
            ├─ Success → Navigate to Group Details + Show Toast
            └─ Error → Show Error Alert, Re-enable Form
```

### 8.2 Detailed User Interactions

#### Interaction 1: Page Load and Initial Focus

**User Action:** User navigates to `/groups/create`

**System Response:**
1. GroupCreateComponent renders
2. AuthGuard verifies authentication (redirects to login if not authenticated)
3. Form renders with empty group name field
4. Group name input field receives automatic focus
5. Helper text is visible
6. Create button is disabled (form invalid)
7. No error messages shown

**Visual State:**
- Empty form
- Cursor blinking in group name field
- Create button disabled (grayed out)
- Helper text: "You'll be automatically added as a participant and organizer"

---

#### Interaction 2: User Types in Group Name Field

**User Action:** User types characters into the group name field

**System Response (Real-time as user types):**

**Scenario A: 0-2 characters entered**
- `groupName` signal updates with each keystroke
- `isDirty` signal set to `true` (on first keystroke)
- Form remains invalid (`canSubmit` computed signal returns `false`)
- Create button remains disabled
- If field has been touched: Show error "Group name must be at least 3 characters"
- Character counter updates: "2 / 200 characters"

**Scenario B: 3-200 characters entered**
- `groupName` signal updates with each keystroke
- Form becomes valid (`canSubmit` computed signal returns `true`)
- Create button becomes enabled (blue, clickable)
- No error messages shown
- Character counter updates: "15 / 200 characters"

**Scenario C: Attempting to exceed 200 characters**
- `maxlength="200"` attribute prevents typing beyond 200 chars
- Character counter shows: "200 / 200 characters"
- Form remains valid (at exactly 200 chars)

---

#### Interaction 3: User Focuses Out of Field (Blur Event)

**User Action:** User clicks outside the group name field or tabs away

**System Response:**
- `touched` signal set to `true`
- Validation errors now displayed if field is invalid
- If group name is empty or < 3 chars: Show appropriate error message
- Error messages persist until user corrects the input

---

#### Interaction 4: User Attempts to Submit Invalid Form

**User Action:** User clicks "Create Group" button when form is invalid (< 3 chars or empty)

**System Response:**
1. `touched` signal set to `true` (if not already)
2. Form submission prevented (`onSubmit` method validates first)
3. Validation errors displayed below input field
4. No API call made
5. Form remains enabled
6. User can continue editing

**Visual State:**
- Red border around input field
- Error message displayed in red text below field
- Create button remains disabled

---

#### Interaction 5: User Submits Valid Form

**User Action:** User clicks "Create Group" button with valid group name (3-200 chars)

**System Response:**

**Phase 1: Submission Start**
1. `isLoading` signal set to `true`
2. `error` signal cleared (set to `null`)
3. Form disabled (input field and button)
4. LoadingSpinnerComponent rendered
5. GroupCreateFormComponent hidden
6. API request sent via GroupService

**Visual State During Loading:**
- Form disappears
- Loading spinner displayed with message "Creating group..."
- User cannot interact with form

**Phase 2: Success Response**
1. API returns 201 Created with `CreateGroupResponse`
2. `isLoading` signal set to `false`
3. Extract `groupId` from response
4. Call `toastService.showSuccess()` with message:
   "Group created! Copy invitation link to invite participants"
5. Navigate to `/groups/{groupId}` using Router
6. User sees group details page with success toast

**Visual State After Success:**
- User redirected to new page (group details)
- Success toast notification visible (typically top-right corner)
- Group details page loads with invitation link prominently displayed

**Phase 3: Error Response**
1. API returns error (400, 429, 500, etc.)
2. `isLoading` signal set to `false`
3. Error message parsed from response
4. `error` signal set with user-friendly message
5. LoadingSpinnerComponent hidden
6. GroupCreateFormComponent re-rendered
7. ErrorAlertComponent displayed above form

**Visual State After Error:**
- Form re-enabled
- Error alert displayed at top with specific message
- Form data preserved (user doesn't lose their input)
- User can dismiss error and retry
- Create button re-enabled

---

#### Interaction 6: User Dismisses Error Alert

**User Action:** User clicks the "X" (dismiss) button on the ErrorAlertComponent

**System Response:**
1. `clearError()` method called
2. `error` signal set to `null`
3. ErrorAlertComponent removed from DOM

**Visual State:**
- Error alert disappears
- Form remains visible and enabled
- User can retry submission

---

#### Interaction 7: User Attempts to Navigate Away with Unsaved Changes

**User Action:** User tries to navigate away (browser back, clicks link, closes tab) when `isDirty` is `true`

**System Response:**
1. `UnsavedChangesGuard` intercepts navigation attempt
2. Calls `canDeactivate()` method on GroupCreateComponent
3. If `isDirty` is `true`:
   - Browser-native confirmation dialog appears
   - Message: "You have unsaved changes. Are you sure you want to leave?"
4. User chooses:
   - **OK/Leave**: Navigation proceeds, form data lost
   - **Cancel/Stay**: Navigation cancelled, user remains on form

**Visual State:**
- Native browser confirmation dialog (modal)
- User must make a choice to proceed

---

### 8.3 Keyboard Interactions

**Tab Navigation:**
- Tab order: Group name field → Create button
- Shift+Tab: Reverse order

**Enter Key:**
- Pressing Enter in group name field: Submits form (if valid)
- Pressing Enter on Create button: Submits form

**Escape Key:**
- On error alert: Dismisses the alert (if implemented)

---

### 8.4 Touch/Mobile Interactions

**Auto-focus Behavior:**
- On mobile, auto-focus may trigger keyboard to open automatically
- Consider disabling auto-focus on small screens if this is disruptive

**Touch Targets:**
- All interactive elements (button, input, dismiss button) minimum 44x44px
- Adequate spacing between elements

**Virtual Keyboard:**
- Input field should remain visible when keyboard opens
- Consider using `scrollIntoView()` if field becomes obscured

---

## 9. Conditions and Validation

### 9.1 Client-Side Validation Conditions

The Group Create View implements comprehensive client-side validation to ensure data quality and provide immediate feedback before making API calls. All validation is performed in real-time or on blur/submit events.

#### Condition 1: Required Field Validation

**Validation Rule:** Group name must not be empty or consist only of whitespace.

**Validation Logic:**
```typescript
const isRequired = (value: string): boolean => {
  return value.trim().length > 0;
};
```

**When Checked:**
- On blur (after user focuses out of field)
- On form submission attempt

**Error Message:** "Group name is required"

**Visual Feedback:**
- Red border on input field
- Error message displayed below field in red text
- Create button remains disabled

**Affected Components:**
- `GroupCreateFormComponent`: Performs validation
- Submit button: Disabled when condition fails

**Impact on Interface:**
- Form cannot be submitted
- User cannot proceed until condition is met

---

#### Condition 2: Minimum Length Validation

**Validation Rule:** Group name must be at least 3 characters (after trimming whitespace).

**Validation Logic:**
```typescript
const meetsMinLength = (value: string): boolean => {
  return value.trim().length >= 3;
};
```

**When Checked:**
- Real-time as user types (if field has been touched)
- On blur
- On form submission attempt

**Error Message:** "Group name must be at least 3 characters"

**Visual Feedback:**
- Red border on input field
- Error message displayed below field
- Create button disabled
- Character counter shows current length

**Affected Components:**
- `GroupCreateFormComponent`: Performs validation and displays error
- Submit button: Disabled when condition fails

**Impact on Interface:**
- Form cannot be submitted
- Immediate feedback as user types
- Submit button state changes as condition is met/unmet

---

#### Condition 3: Maximum Length Validation

**Validation Rule:** Group name must not exceed 200 characters.

**Validation Logic:**
```typescript
const meetsMaxLength = (value: string): boolean => {
  return value.length <= 200;
};
```

**When Checked:**
- Real-time as user types
- Prevented by `maxlength="200"` HTML attribute

**Error Message:** "Group name cannot exceed 200 characters"

**Note:** This error should rarely/never be shown due to `maxlength` attribute preventing input beyond 200 characters. Included for completeness and in case paste operations bypass the attribute.

**Visual Feedback:**
- Character counter shows "200 / 200 characters"
- Input field prevents further typing

**Affected Components:**
- `GroupCreateFormComponent`: Input field with `maxlength` attribute
- Character counter display

**Impact on Interface:**
- User cannot type beyond 200 characters
- Form remains valid at exactly 200 characters

---

#### Condition 4: Combined Validation (Form Validity)

**Validation Rule:** All individual validations must pass for form to be valid.

**Validation Logic:**
```typescript
const isFormValid = computed(() => {
  const name = groupNameValue().trim();
  return name.length >= 3 && name.length <= 200;
});
```

**When Checked:**
- Continuously via computed signal
- Updated automatically when `groupName` changes

**Affected Components:**
- Submit button: Enabled/disabled based on `isFormValid()`
- Form: Cannot be submitted when invalid

**Impact on Interface:**
- Submit button visual state (enabled/disabled)
- Form submission allowed/prevented

---

### 9.2 Server-Side Validation

While client-side validation provides immediate feedback, server-side validation is the authoritative source of truth. The API may reject requests that pass client-side validation if additional server-side rules exist.

#### Handling Server Validation Errors

**API Response (400 Bad Request):**
```json
{
  "error": "ValidationError",
  "message": "Validation failed",
  "details": {
    "name": ["Group name must be at least 3 characters"]
  }
}
```

**Processing:**
1. Parse `error.error.details` object
2. Extract field-specific error message
3. Display in ErrorAlertComponent above form
4. Keep form enabled for user correction

**Visual Feedback:**
- Error alert displayed at top of form
- Specific validation error message shown
- Form remains enabled
- User can correct and resubmit

---

### 9.3 Authentication Validation

**Condition:** User must be authenticated to access the view.

**Validation:** Performed by `AuthGuard` on route

**When Checked:** Before component renders (route guard)

**Handling Failure:**
- User redirected to `/login`
- Current URL (`/groups/create`) stored as return URL
- After successful login, user redirected back to create group page

**Affected Components:**
- Route configuration
- Not visible to GroupCreateComponent (handled at router level)

---

### 9.4 Unsaved Changes Validation

**Condition:** User has entered data but not submitted (`isDirty === true`).

**Validation:** Performed by `UnsavedChangesGuard` when user attempts to navigate away

**When Checked:** On navigation attempt (route guard)

**Handling:**
```typescript
canDeactivate(): boolean {
  if (!this.isDirty()) {
    return true; // Allow navigation
  }

  // Show browser-native confirmation
  return window.confirm(
    'You have unsaved changes. Are you sure you want to leave?'
  );
}
```

**Visual Feedback:**
- Browser-native confirmation dialog
- User chooses to stay or leave

**Affected Components:**
- GroupCreateComponent: Implements `canDeactivate()` method
- Router: Respects guard decision

---

### 9.5 Validation State Summary

| Condition | When Checked | Error Message | Button State | Can Submit |
|-----------|--------------|---------------|--------------|------------|
| Empty field | On blur, submit | "Group name is required" | Disabled | No |
| < 3 chars | Real-time, blur, submit | "Group name must be at least 3 characters" | Disabled | No |
| > 200 chars | Real-time (prevented) | "Group name cannot exceed 200 characters" | Disabled | No |
| 3-200 chars | Continuous | None | Enabled | Yes |
| During API call | N/A | None | Disabled | No |
| Unsaved changes | On navigation | Browser confirm dialog | N/A | N/A |

---

## 10. Error Handling

### 10.1 Error Handling Strategy

The Group Create View implements a comprehensive error handling strategy with user-friendly messages, appropriate recovery actions, and consistent error display patterns. All errors are caught, logged, and presented to the user in a clear, actionable manner.

---

### 10.2 Client-Side Errors

#### Error Type 1: Validation Errors

**Scenario:** User input fails client-side validation

**Examples:**
- Empty group name
- Group name too short (< 3 characters)
- Group name too long (> 200 characters)

**Handling:**
- Errors displayed inline below the input field
- Validation runs in real-time or on blur
- Form submission prevented
- No API call made

**User Experience:**
- Immediate feedback
- Clear, specific error messages
- Ability to correct and retry instantly

**Recovery Action:**
- User corrects input based on error message
- Validation re-runs automatically
- Error disappears when condition is met

---

#### Error Type 2: Network Errors

**Scenario:** Network connection fails or is unavailable

**Error Detection:**
```typescript
error: (error: any) => {
  if (error.status === 0) {
    // Network error
    this.error.set('Unable to connect to server. Please check your internet connection.');
  }
}
```

**User Message:** "Unable to connect to server. Please check your internet connection."

**Visual Display:**
- ErrorAlertComponent displayed above form
- Dismissible alert
- Form remains enabled

**Recovery Action:**
- User checks internet connection
- Dismisses error alert
- Retries form submission

---

### 10.3 API Errors

#### Error Type 3: Bad Request (400)

**Scenario:** Server-side validation fails

**API Response:**
```json
{
  "error": "ValidationError",
  "message": "Validation failed",
  "details": {
    "name": ["Group name must be at least 3 characters"]
  }
}
```

**Handling:**
```typescript
private parseErrorMessage(error: any): string {
  if (error.status === 400 && error.error?.details?.name) {
    // Extract field-specific error
    return error.error.details.name[0];
  }
  return 'Validation error. Please check your input.';
}
```

**User Message:** Specific field error (e.g., "Group name must be at least 3 characters") or generic validation message

**Visual Display:**
- ErrorAlertComponent above form
- Specific validation error highlighted

**Recovery Action:**
- User reviews error message
- Corrects input according to server requirements
- Resubmits form

---

#### Error Type 4: Unauthorized (401)

**Scenario:** JWT token is missing, invalid, or expired

**API Response:**
```json
{
  "error": "Unauthorized",
  "message": "Invalid or expired token"
}
```

**Handling:**
- Caught by `ErrorInterceptor` globally
- Not handled directly in component
- Automatic redirect to `/login`
- Current URL stored as return URL

**User Message (Toast):** "Your session has expired. Please log in again."

**Visual Display:**
- User redirected to login page
- Toast notification shown

**Recovery Action:**
- User logs in again
- After successful login, redirected back to `/groups/create`
- User can retry group creation

**No Component-Level Handling Required:** ErrorInterceptor handles this globally

---

#### Error Type 5: Rate Limiting (429)

**Scenario:** User has made too many requests in a short time

**API Response:**
```json
{
  "error": "TooManyRequests",
  "message": "Rate limit exceeded. Please try again later."
}
```

**Handling:**
```typescript
if (error.status === 429) {
  this.error.set('Too many requests. Please wait a moment and try again.');
}
```

**User Message:** "Too many requests. Please wait a moment and try again."

**Visual Display:**
- ErrorAlertComponent above form
- Dismissible alert

**Recovery Action:**
- User waits briefly
- Dismisses error
- Retries submission

---

#### Error Type 6: Internal Server Error (500)

**Scenario:** Unexpected server error occurs

**API Response:**
```json
{
  "error": "InternalServerError",
  "message": "An unexpected error occurred"
}
```

**Handling:**
```typescript
if (error.status === 500) {
  this.error.set('An unexpected error occurred. Please try again later.');
}
```

**User Message:** "An unexpected error occurred. Please try again later."

**Visual Display:**
- ErrorAlertComponent above form
- Dismissible alert

**Recovery Action:**
- User retries immediately
- If error persists, user waits and retries later
- User may contact support if problem continues

---

### 10.4 Error Display Components

#### ErrorAlertComponent Usage

**Location:** Displayed above GroupCreateFormComponent

**Conditional Rendering:**
```html
@if (error()) {
  <app-error-alert
    [message]="error()"
    (dismiss)="clearError()">
  </app-error-alert>
}
```

**Characteristics:**
- Red background (bg-red-50)
- Red border (border-red-400)
- Error icon
- Dismissible (X button)
- Accessible (role="alert")

---

### 10.5 Error Message Mapping

**Centralized Error Parsing:**
```typescript
private parseErrorMessage(error: any): string {
  // Network error
  if (error.status === 0) {
    return 'Unable to connect to server. Please check your internet connection.';
  }

  // Validation error with field details
  if (error.status === 400 && error.error?.details?.name) {
    return error.error.details.name[0];
  }

  // Generic validation error
  if (error.status === 400) {
    return 'Validation error. Please check your input.';
  }

  // Rate limiting
  if (error.status === 429) {
    return 'Too many requests. Please wait a moment and try again.';
  }

  // Server error
  if (error.status >= 500) {
    return 'An unexpected error occurred. Please try again later.';
  }

  // Extract message from error object
  if (error.error?.message) {
    return error.error.message;
  }

  // Generic fallback
  return 'Unable to create group. Please try again.';
}
```

---

### 10.6 Error Recovery Patterns

#### Pattern 1: Inline Correction
**Applicable To:** Validation errors
**User Action:** Corrects input, error disappears automatically

#### Pattern 2: Dismiss and Retry
**Applicable To:** API errors, network errors
**User Action:** Dismisses error alert, modifies input if needed, resubmits form

#### Pattern 3: Wait and Retry
**Applicable To:** Rate limiting, temporary server issues
**User Action:** Waits briefly, resubmits form

#### Pattern 4: Re-authentication
**Applicable To:** 401 Unauthorized
**User Action:** Logs in again, returns to form, resubmits

---

### 10.7 Error Logging

**Development Environment:**
```typescript
error: (error: any) => {
  console.error('Group creation failed:', error);
  // Set user-friendly error message
  this.setError(this.parseErrorMessage(error));
}
```

**Production Environment:**
- Consider integrating error tracking service (e.g., Sentry)
- Log errors with context (user ID, timestamp, request payload)
- Monitor error rates for system health

---

### 10.8 Error Prevention Strategies

1. **Client-Side Validation:** Catches most errors before API call
2. **Loading State:** Prevents duplicate submissions
3. **Disabled Form During Submission:** Prevents user interference
4. **Authentication Guard:** Prevents unauthorized access attempts
5. **Unsaved Changes Warning:** Prevents accidental data loss

---

## 11. Implementation Steps

Follow these steps in order to implement the Group Create View. Each step builds upon the previous ones.

---

### Step 1: Set Up Project Structure and Models

**Objective:** Create necessary directories and define TypeScript interfaces for type safety.

**Actions:**

1.1. Create directory structure:
```bash
src/app/
├── features/
│   └── groups/
│       └── components/
│           └── group-create/
│               ├── group-create.component.ts
│               ├── group-create.component.html
│               └── group-create.component.spec.ts
├── shared/
│   └── components/
│       ├── error-alert/
│       │   ├── error-alert.component.ts
│       │   ├── error-alert.component.html
│       │   └── error-alert.component.spec.ts
│       └── loading-spinner/
│           ├── loading-spinner.component.ts
│           ├── loading-spinner.component.html
│           └── loading-spinner.component.spec.ts
├── core/
│   ├── models/
│   │   └── group.models.ts
│   ├── services/
│   │   ├── group.service.ts
│   │   └── toast.service.ts
│   └── guards/
│       ├── auth.guard.ts
│       └── unsaved-changes.guard.ts
```

**Note:** We no longer need a separate `GroupCreateFormComponent` since the form is managed directly in `GroupCreateComponent` using Reactive Forms.

1.2. Create `group.models.ts` with type definitions:
```typescript
// src/app/core/models/group.models.ts

export interface CreateGroupRequest {
  name: string;
}

export interface CreateGroupResponse {
  groupId: string;
  name: string;
  organizerId: string;
  organizerName: string;
  invitationToken: string;
  invitationLink: string;
  participantCount: number;
  budget: number | null;
  drawCompleted: boolean;
  createdAt: string;
}

export interface ApiErrorResponse {
  error: string;
  message: string;
  details?: {
    [fieldName: string]: string[];
  };
}
```

**Note:** We don't need `GroupCreateFormData` or `GroupCreateState` interfaces since Reactive Forms manage form state internally.

**Verification:**
- All directories exist
- `group.models.ts` compiles without errors
- Types can be imported in other files

---

### Step 2: Implement Shared Components

**Objective:** Create reusable ErrorAlertComponent and LoadingSpinnerComponent.

**Actions:**

2.1. **Implement LoadingSpinnerComponent** (simplest, no dependencies):

```typescript
// src/app/shared/components/loading-spinner/loading-spinner.component.ts
import { Component, ChangeDetectionStrategy, input } from '@angular/core';

@Component({
  selector: 'app-loading-spinner',
  standalone: true,
  templateUrl: './loading-spinner.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoadingSpinnerComponent {
  message = input<string>('');
}
```

```html
<!-- src/app/shared/components/loading-spinner/loading-spinner.component.html -->
<div class="flex flex-col items-center justify-center p-8">
  <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
  @if (message()) {
    <p class="mt-4 text-gray-600 text-sm">{{ message() }}</p>
  }
</div>
```

2.2. **Implement ErrorAlertComponent:**

```typescript
// src/app/shared/components/error-alert/error-alert.component.ts
import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';

@Component({
  selector: 'app-error-alert',
  standalone: true,
  templateUrl: './error-alert.component.html',
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

```html
<!-- src/app/shared/components/error-alert/error-alert.component.html -->
<div class="bg-red-50 border border-red-400 text-red-800 px-4 py-3 rounded relative mb-4" role="alert">
  <div class="flex items-start">
    <div class="flex-shrink-0">
      <svg class="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
        <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
      </svg>
    </div>

    <div class="ml-3 flex-1">
      <p class="text-sm">{{ message() }}</p>
    </div>

    @if (dismissible()) {
      <div class="ml-auto pl-3">
        <button
          type="button"
          (click)="onDismiss()"
          class="inline-flex bg-red-50 rounded-md p-1.5 text-red-500 hover:bg-red-100 focus:outline-none focus:ring-2 focus:ring-red-600"
          aria-label="Dismiss"
        >
          <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
          </svg>
        </button>
      </div>
    }
  </div>
</div>
```

**Verification:**
- Both components compile without errors
- Components can be imported and used in other components
- Tailwind classes render correctly

---

### Step 3: Implement GroupService

**Objective:** Create service for API communication.

**Actions:**

3.1. **Implement GroupService:**

```typescript
// src/app/core/services/group.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';
import { CreateGroupRequest, CreateGroupResponse } from '@models/group.models';

@Injectable({
  providedIn: 'root'
})
export class GroupService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/groups`;

  createGroup(request: CreateGroupRequest): Observable<CreateGroupResponse> {
    return this.http.post<CreateGroupResponse>(this.apiUrl, request);
  }
}
```

3.2. **Ensure environment configuration exists:**

```typescript
// src/environments/environment.ts
export const environment = {
  production: false,
  apiUrl: 'https://localhost:5001' // Adjust to your backend URL
};
```

**Verification:**
- GroupService compiles without errors
- Service can be injected into components
- HTTP module is provided in app configuration

---

### Step 4: Implement GroupCreateComponent (Container with Reactive Forms)

**Objective:** Create the main component with inline form using Angular Reactive Forms.

**Actions:**

**Note:** Use the component implementation from **Section 4.1** above, which includes:
- Reactive Forms setup with FormBuilder
- Auto-focus logic
- Validation with built-in validators
- Error handling
- API integration

Refer to the complete component code in Section 4.1 for the full implementation.

  // Outputs
  formSubmit = output<string>();

  // View Children
  @ViewChild('groupNameInput') groupNameInput!: ElementRef<HTMLInputElement>;

  // Internal State
  groupNameValue = signal<string>('');
  touched = signal<boolean>(false);

  // Computed State
  isValid = computed(() => {
    const name = this.groupNameValue().trim();
    return name.length >= 3 && name.length <= 200;
  });

  isFormValid = computed(() => this.isValid());

  showError = computed(() => this.touched() && !this.isValid());

  // Lifecycle
  ngAfterViewInit(): void {
    afterNextRender(() => {
      this.groupNameInput.nativeElement.focus();
    });
  }

  // Event Handlers
  onGroupNameChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.groupNameValue.set(input.value);
  }

  onTouched(): void {
    this.touched.set(true);
  }

  onSubmit(): void {
    this.touched.set(true);

    if (this.isValid()) {
      this.formSubmit.emit(this.groupNameValue().trim());
    }
  }

  getErrorMessage(): string {
    const name = this.groupNameValue().trim();

    if (name.length === 0) {
      return 'Group name is required';
    }

    if (name.length < 3) {
      return 'Group name must be at least 3 characters';
    }

    if (this.groupNameValue().length > 200) {
      return 'Group name cannot exceed 200 characters';
    }

    return '';
  }
}
```

4.2. **Implement component template:**

```html
<!-- src/app/features/groups/components/group-create-form/group-create-form.component.html -->
<form (ngSubmit)="onSubmit()" #formElement novalidate class="space-y-6">
  <!-- Group Name Field -->
  <div class="form-field">
    <label for="groupName" class="block text-sm font-medium text-gray-700 mb-2">
      Group Name <span class="text-red-600">*</span>
    </label>

    <input
      #groupNameInput
      type="text"
      id="groupName"
      name="groupName"
      [value]="groupNameValue()"
      (input)="onGroupNameChange($event)"
      (blur)="onTouched()"
      [disabled]="isSubmitting()"
      [class.border-red-500]="showError()"
      [class.border-gray-300]="!showError()"
      maxlength="200"
      class="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 disabled:bg-gray-100 disabled:cursor-not-allowed transition-colors"
      placeholder="e.g., Family Secret Santa 2025"
      autocomplete="off"
    />

    @if (showError()) {
      <span class="text-red-600 text-sm mt-1 block flex items-center">
        <svg class="h-4 w-4 mr-1" fill="currentColor" viewBox="0 0 20 20">
          <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clip-rule="evenodd" />
        </svg>
        {{ getErrorMessage() }}
      </span>
    }

    <!-- Character counter -->
    <span class="text-gray-500 text-xs mt-1 block">
      {{ groupNameValue().length }} / 200 characters
    </span>
  </div>

  <!-- Helper Text -->
  <div class="bg-blue-50 border border-blue-200 rounded-lg p-3">
    <p class="text-sm text-blue-800 flex items-start">
      <svg class="h-5 w-5 mr-2 flex-shrink-0 text-blue-600" fill="currentColor" viewBox="0 0 20 20">
        <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
      </svg>
      You'll be automatically added as a participant and organizer
    </p>
  </div>

  <!-- Submit Button -->
  <button
    type="submit"
    [disabled]="!isFormValid() || isSubmitting()"
    class="w-full bg-blue-600 text-white font-medium py-3 px-4 rounded-lg hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:bg-gray-400 disabled:cursor-not-allowed transition-colors"
  >
    @if (isSubmitting()) {
      <span class="flex items-center justify-center">
        <svg class="animate-spin -ml-1 mr-3 h-5 w-5 text-white" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
        Creating...
      </span>
    } @else {
      Create Group
    }
  </button>
</form>
```

**Verification:**
- Component compiles without errors
- Auto-focus works when component renders
- Validation errors display correctly
- Character counter updates in real-time
- Form submission emits correct event

---

### Step 5: Implement GroupCreateComponent (Container)

**Objective:** Create the smart component that orchestrates the entire view.

**Actions:**

5.1. **Implement component class:**

```typescript
// src/app/features/groups/components/group-create/group-create.component.ts
import { Component, ChangeDetectionStrategy, signal, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { GroupService } from '@core/services/group.service';
import { ToastService } from '@core/services/toast.service';
import { CreateGroupRequest, CreateGroupResponse } from '@core/models/group.models';
import { GroupCreateFormComponent } from '../group-create-form/group-create-form.component';
import { ErrorAlertComponent } from '@shared/components/error-alert/error-alert.component';
import { LoadingSpinnerComponent } from '@shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-group-create',
  standalone: true,
  imports: [
    GroupCreateFormComponent,
    ErrorAlertComponent,
    LoadingSpinnerComponent
  ],
  templateUrl: './group-create.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class GroupCreateComponent {
  // Services
  private groupService = inject(GroupService);
  private toastService = inject(ToastService);
  private router = inject(Router);

  // State
  groupName = signal<string>('');
  isLoading = signal<boolean>(false);
  error = signal<string | null>(null);
  isDirty = signal<boolean>(false);

  // Event Handlers
  onFormSubmit(groupName: string): void {
    this.isLoading.set(true);
    this.error.set(null);
    this.groupName.set(groupName);
    this.isDirty.set(true);

    const request: CreateGroupRequest = {
      name: groupName
    };

    this.groupService.createGroup(request).subscribe({
      next: (response: CreateGroupResponse) => {
        this.isLoading.set(false);
        this.isDirty.set(false);

        // Show success toast
        this.toastService.showSuccess(
          'Group created! Copy invitation link to invite participants'
        );

        // Navigate to group details
        this.router.navigate(['/groups', response.groupId]);
      },
      error: (error: any) => {
        this.isLoading.set(false);
        const errorMessage = this.parseErrorMessage(error);
        this.error.set(errorMessage);
      }
    });
  }

  clearError(): void {
    this.error.set(null);
  }

  canDeactivate(): boolean {
    if (!this.isDirty()) {
      return true;
    }

    return window.confirm(
      'You have unsaved changes. Are you sure you want to leave?'
    );
  }

  private parseErrorMessage(error: any): string {
    // Network error
    if (error.status === 0) {
      return 'Unable to connect to server. Please check your internet connection.';
    }

    // Validation error with field details
    if (error.status === 400 && error.error?.details?.name) {
      return error.error.details.name[0];
    }

    // Generic validation error
    if (error.status === 400) {
      return 'Validation error. Please check your input.';
    }

    // Rate limiting
    if (error.status === 429) {
      return 'Too many requests. Please wait a moment and try again.';
    }

    // Server error
    if (error.status >= 500) {
      return 'An unexpected error occurred. Please try again later.';
    }

    // Extract message from error object
    if (error.error?.message) {
      return error.error.message;
    }

    // Generic fallback
    return 'Unable to create group. Please try again.';
  }
}
```

5.2. **Implement component template:**

```html
<!-- src/app/features/groups/components/group-create/group-create.component.html -->
<div class="min-h-screen bg-gray-50 py-8 px-4 sm:px-6 lg:px-8">
  <div class="max-w-md mx-auto">
    <!-- Header -->
    <div class="text-center mb-8">
      <h1 class="text-3xl font-bold text-gray-900">Create New Group</h1>
      <p class="mt-2 text-sm text-gray-600">
        Start a new Secret Santa gift exchange
      </p>
    </div>

    <!-- Error Alert -->
    @if (error()) {
      <app-error-alert
        [message]="error()!"
        (dismiss)="clearError()"
        class="mb-4">
      </app-error-alert>
    }

    <!-- Loading State -->
    @if (isLoading()) {
      <div class="bg-white rounded-lg shadow-md p-8">
        <app-loading-spinner
          message="Creating group...">
        </app-loading-spinner>
      </div>
    }

    <!-- Form -->
    @if (!isLoading()) {
      <div class="bg-white rounded-lg shadow-md p-6 sm:p-8">
        <app-group-create-form
          [groupName]="groupName()"
          [isSubmitting]="isLoading()"
          (formSubmit)="onFormSubmit($event)">
        </app-group-create-form>
      </div>
    }

    <!-- Back Link -->
    <div class="mt-6 text-center">
      <a
        routerLink="/groups"
        class="text-sm text-blue-600 hover:text-blue-800 hover:underline">
        ← Back to My Groups
      </a>
    </div>
  </div>
</div>
```

**Verification:**
- Component compiles without errors
- Form submission triggers API call
- Loading state displays correctly
- Error handling works as expected

---

### Step 6: Implement Guards

**Objective:** Protect the route with authentication and unsaved changes guards.

**Actions:**

6.1. **Implement UnsavedChangesGuard:**

```typescript
// src/app/core/guards/unsaved-changes.guard.ts
import { CanDeactivateFn } from '@angular/router';

export interface CanComponentDeactivate {
  canDeactivate: () => boolean;
}

export const unsavedChangesGuard: CanDeactivateFn<CanComponentDeactivate> = (component) => {
  return component.canDeactivate ? component.canDeactivate() : true;
};
```

6.2. **Ensure AuthGuard exists** (should already be implemented for other routes):

```typescript
// src/app/core/guards/auth.guard.ts
import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '@core/services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  // Store return URL and redirect to login
  authService.storeReturnUrl(state.url);
  return router.createUrlTree(['/login']);
};
```

**Verification:**
- Guards compile without errors
- AuthGuard redirects unauthenticated users
- UnsavedChangesGuard prompts when navigating with unsaved changes

---

### Step 7: Configure Routing

**Objective:** Add the Group Create route to the application routing configuration.

**Actions:**

7.1. **Add route to routes configuration:**

```typescript
// src/app/app.routes.ts
import { Routes } from '@angular/router';
import { authGuard } from '@core/guards/auth.guard';
import { unsavedChangesGuard } from '@core/guards/unsaved-changes.guard';
import { GroupCreateComponent } from '@features/groups/components/group-create/group-create.component';

export const routes: Routes = [
  // ... other routes
  {
    path: 'groups/create',
    component: GroupCreateComponent,
    canActivate: [authGuard],
    canDeactivate: [unsavedChangesGuard]
  },
  // ... other routes
];
```

**Verification:**
- Route is accessible at `/groups/create`
- AuthGuard protects the route
- UnsavedChangesGuard works when navigating away

---

### Step 8: Implement ToastService (if not already exists)

**Objective:** Create service for displaying toast notifications.

**Actions:**

8.1. **Implement basic ToastService:**

```typescript
// src/app/core/services/toast.service.ts
import { Injectable, signal } from '@angular/core';

export interface Toast {
  id: number;
  message: string;
  type: 'success' | 'error' | 'info';
}

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  toasts = signal<Toast[]>([]);
  private nextId = 0;

  showSuccess(message: string): void {
    this.show(message, 'success');
  }

  showError(message: string): void {
    this.show(message, 'error');
  }

  showInfo(message: string): void {
    this.show(message, 'info');
  }

  private show(message: string, type: Toast['type']): void {
    const toast: Toast = {
      id: this.nextId++,
      message,
      type
    };

    this.toasts.update(toasts => [...toasts, toast]);

    // Auto-dismiss after 5 seconds
    setTimeout(() => this.dismiss(toast.id), 5000);
  }

  dismiss(id: number): void {
    this.toasts.update(toasts => toasts.filter(t => t.id !== id));
  }
}
```

8.2. **Create ToastContainerComponent** (to be added to app root):

```typescript
// src/app/shared/components/toast-container/toast-container.component.ts
import { Component, inject } from '@angular/core';
import { ToastService } from '@core/services/toast.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  template: `
    <div class="fixed top-4 right-4 z-50 space-y-2">
      @for (toast of toastService.toasts(); track toast.id) {
        <div
          [class]="getToastClass(toast.type)"
          class="px-4 py-3 rounded-lg shadow-lg max-w-sm animate-slide-in">
          <div class="flex items-start">
            <p class="text-sm flex-1">{{ toast.message }}</p>
            <button
              (click)="toastService.dismiss(toast.id)"
              class="ml-2 text-white hover:text-gray-200">
              <svg class="h-4 w-4" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
              </svg>
            </button>
          </div>
        </div>
      }
    </div>
  `
})
export class ToastContainerComponent {
  toastService = inject(ToastService);

  getToastClass(type: string): string {
    switch (type) {
      case 'success': return 'bg-green-600 text-white';
      case 'error': return 'bg-red-600 text-white';
      case 'info': return 'bg-blue-600 text-white';
      default: return 'bg-gray-600 text-white';
    }
  }
}
```

8.3. **Add ToastContainerComponent to app root:**

```typescript
// src/app/app.component.ts
import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastContainerComponent } from '@shared/components/toast-container/toast-container.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, ToastContainerComponent],
  template: `
    <app-toast-container />
    <router-outlet />
  `
})
export class AppComponent { }
```

**Verification:**
- ToastService works correctly
- Success toast displays after group creation
- Toasts auto-dismiss after 5 seconds

---

### Step 9: Add Navigation Link

**Objective:** Add a link to the Group Create page from the Groups List view.

**Actions:**

9.1. **Add "Create Group" button to Groups List:**

```html
<!-- In src/app/features/groups/components/groups-list/groups-list.component.html -->
<div class="flex justify-between items-center mb-6">
  <h1 class="text-2xl font-bold">My Groups</h1>
  <a
    routerLink="/groups/create"
    class="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 transition-colors">
    + Create New Group
  </a>
</div>
```

**Verification:**
- Link navigates to `/groups/create`
- Button is prominently displayed

---

### Step 10: Testing and Refinement

**Objective:** Test the complete flow and refine based on findings.

**Actions:**

10.1. **Manual Testing Checklist:**

- [ ] Navigate to `/groups/create` while unauthenticated → Redirected to login
- [ ] Navigate to `/groups/create` while authenticated → Page loads
- [ ] Group name field auto-focuses on page load
- [ ] Type 0-2 characters → Validation error shown after blur
- [ ] Type 3+ characters → No validation error, button enabled
- [ ] Submit form with valid name → Loading spinner shows
- [ ] Successful creation → Redirected to group details with toast
- [ ] API error → Error alert displayed, form re-enabled
- [ ] Navigate away with unsaved changes → Confirmation dialog shows
- [ ] Dismiss error alert → Error disappears
- [ ] Character counter updates in real-time
- [ ] Form disabled during submission

10.2. **Unit Tests** (example for GroupCreateFormComponent):

```typescript
// src/app/features/groups/components/group-create-form/group-create-form.component.spec.ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { GroupCreateFormComponent } from './group-create-form.component';

describe('GroupCreateFormComponent', () => {
  let component: GroupCreateFormComponent;
  let fixture: ComponentFixture<GroupCreateFormComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GroupCreateFormComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(GroupCreateFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should invalidate form with empty group name', () => {
    component.groupNameValue.set('');
    expect(component.isValid()).toBe(false);
  });

  it('should invalidate form with group name less than 3 characters', () => {
    component.groupNameValue.set('Ab');
    expect(component.isValid()).toBe(false);
  });

  it('should validate form with group name between 3 and 200 characters', () => {
    component.groupNameValue.set('Valid Group Name');
    expect(component.isValid()).toBe(true);
  });

  it('should emit formSubmit event on valid submission', () => {
    let emittedValue: string | undefined;
    component.formSubmit.subscribe((value: string) => {
      emittedValue = value;
    });

    component.groupNameValue.set('Test Group');
    component.onSubmit();

    expect(emittedValue).toBe('Test Group');
  });

  it('should not emit formSubmit event on invalid submission', () => {
    let emitted = false;
    component.formSubmit.subscribe(() => {
      emitted = true;
    });

    component.groupNameValue.set('Ab');
    component.onSubmit();

    expect(emitted).toBe(false);
  });
});
```


10.5. **Responsive Testing:**
- Test on mobile viewport (320px, 375px, 414px)
- Test on tablet viewport (768px, 1024px)
- Test on desktop viewport (1280px, 1920px)
- Verify touch targets are at least 44x44px

**Verification:**
- All tests pass
- View works correctly across all scenarios
- No console errors
- Performance is acceptable

---

### Step 11: Documentation and Cleanup

**Objective:** Document the implementation and clean up any temporary code.

**Actions:**

11.1. **Add code comments** where logic is complex

11.2. **Update project documentation:**
- Add view to application documentation
- Document any new patterns or conventions used

11.3. **Clean up:**
- Remove any console.log statements
- Remove unused imports
- Format code with Prettier
- Run linter and fix any issues

**Verification:**
- Code is clean and well-documented
- No linting errors
- All imports are used
- Code follows project conventions

---

## Implementation Complete!

After completing all 11 steps, the Group Create View will be fully functional and production-ready. The view will allow authenticated users to create new Secret Santa groups with proper validation, error handling, and user feedback.
