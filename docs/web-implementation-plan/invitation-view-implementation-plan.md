# View Implementation Plan: Invitation View

## 1. Overview

The Invitation View (`/invite/{token}`) is a dual-purpose public view that displays Secret Santa group information to potential participants and handles the join flow. The view has two distinct states:

1. **Unauthenticated State**: Displays group details (name, organizer, participant count) with call-to-action buttons prompting users to login or register
2. **Authenticated State**: Shows the same group details plus a join form with an optional budget suggestion field

The view serves as the entry point for users receiving invitation links and manages token persistence during the registration flow for seamless joining after authentication.

## 2. View Routing

**Path**: `/invite/:token`

**Route Configuration**:
```typescript
{
  path: 'invite/:token',
  component: InvitationComponent,
  // No auth guard - public view
  // Authenticated users see join form, unauthenticated see CTAs
}
```

**Path Parameters**:
- `token` (string): UUID invitation token from the shareable link

**Query Parameters** (optional):
- `returnUrl` (string): Used when returning from login/registration flow

## 3. Component Structure

```
InvitationComponent (Smart/Container)
├── LoadingSpinnerComponent (conditional: loading state)
├── ErrorAlertComponent (conditional: error state)
└── Content (conditional: success state)
    ├── InvitationDetailsCardComponent
    │   └── Group information display
    └── (Conditional based on authentication state)
        ├── [Unauthenticated]
        │   └── UnauthenticatedActionsComponent
        │       ├── "Login to Join" button
        │       └── "Register to Join" button
        └── [Authenticated]
            └── JoinGroupFormComponent
                ├── BudgetSuggestionInputComponent
                └── "Join Group" button
```

## 4. Component Details

### 4.1 InvitationComponent (Smart/Container Component)

**Component Description**:
Main container component that manages the invitation view state, orchestrates API calls, handles authentication state detection, and conditionally renders child components based on loading, error, and authentication states.

**File Location**: `src/app/features/invitations/invitation.component.ts`

**Main Elements**:
```html
<div class="container mx-auto p-4 max-w-2xl">
  @if (state().loading) {
    <app-loading-spinner message="Loading invitation details..." />
  }

  @if (state().error) {
    <app-error-alert
      [error]="state().error"
      [showActions]="true"
      (retry)="loadInvitation()" />
  }

  @if (state().invitation && !state().error) {
    <div class="space-y-6">
      <app-invitation-details-card
        [invitation]="state().invitation!" />

      @if (!isAuthenticated()) {
        <app-unauthenticated-actions
          (login)="navigateToLogin()"
          (register)="navigateToRegister()" />
      } @else {
        <app-join-group-form
          [isJoining]="state().isJoining"
          [groupName]="state().invitation!.groupName"
          (submit)="handleJoinGroup($event)" />
      }
    </div>
  }
</div>
```

**Handled Events**:
- `ngOnInit`: Load invitation details, check authentication state, detect return from auth flow
- `retry`: Reload invitation details after error
- `navigateToLogin`: Store token and navigate to login page
- `navigateToRegister`: Store token and navigate to registration page
- `handleJoinGroup`: Submit join request with optional budget suggestion

**Validation Conditions**:
- Token format: Must be valid UUID (validated by route)
- Invitation validity: `isValid === true` from API response
- Draw status: `drawCompleted === false` to allow joining
- Authentication state: Required for joining (not for viewing)

**Types**:
- `InvitationDetailsDto` (API response)
- `InvitationViewState` (component state)
- `InvitationError` (error state)
- `JoinGroupFormData` (form submission)

**Props**: None (route-based component)

**State Management**:
```typescript
// Component state signal
private readonly state = signal<InvitationViewState>({
  loading: true,
  invitation: null,
  error: null,
  isJoining: false
});

// Computed authentication state from service
readonly isAuthenticated = computed(() =>
  this.authService.isAuthenticated()
);

// Expose read-only state
readonly invitationState = this.state.asReadonly();
```

**Dependencies** (via inject()):
- `InvitationService`: API calls for invitation details and joining
- `AuthService`: Check authentication state
- `Router`: Navigation
- `ActivatedRoute`: Extract token parameter
- `ToastService`: Show success/error notifications

---

### 4.2 InvitationDetailsCardComponent (Presentational)

**Component Description**:
Displays group information from the invitation in a card format. Pure presentational component that receives invitation data as input and renders it in a visually appealing, mobile-responsive card.

**File Location**: `src/app/features/invitations/components/invitation-details-card.component.ts`

**Main Elements**:
```html
<div class="bg-white rounded-lg shadow-md p-6 border border-gray-200">
  <h1 class="text-2xl font-bold text-gray-900 mb-4">
    {{ invitation.groupName }}
  </h1>

  <div class="space-y-3">
    <div class="flex items-center gap-2">
      <svg class="w-5 h-5 text-gray-500"><!-- User icon --></svg>
      <span class="text-gray-700">
        <span class="font-medium">Organizator:</span>
        {{ invitation.organizerName }}
      </span>
    </div>

    <div class="flex items-center gap-2">
      <svg class="w-5 h-5 text-gray-500"><!-- Users icon --></svg>
      <span class="text-gray-700">
        <span class="font-medium">Uczestnicy:</span>
        {{ invitation.participantCount }}
      </span>
    </div>
  </div>

  <div class="mt-4 p-4 bg-blue-50 rounded-lg border border-blue-200">
    <p class="text-sm text-blue-800">
      Zostałeś zaproszony do wzięcia udziału w Tajemniczym Mikołaju!
    </p>
  </div>
</div>
```

**Handled Events**: None (pure display component)

**Validation Conditions**: None (displays data as-is)

**Types**:
- `InvitationDetailsDto` (input)

**Props**:
```typescript
export class InvitationDetailsCardComponent {
  invitation = input.required<InvitationDetailsDto>();
}
```

---

### 4.3 UnauthenticatedActionsComponent (Presentational)

**Component Description**:
Displays call-to-action buttons for unauthenticated users to either login or register. Emits navigation events to parent component for handling.

**File Location**: `src/app/features/invitations/components/unauthenticated-actions.component.ts`

**Main Elements**:
```html
<div class="bg-white rounded-lg shadow-md p-6 border border-gray-200">
  <h2 class="text-lg font-semibold text-gray-900 mb-4">
    Dołącz do grupy
  </h2>

  <p class="text-gray-600 mb-6">
    Aby dołączyć do tej grupy, musisz się zalogować lub zarejestrować.
  </p>

  <div class="space-y-3">
    <button
      type="button"
      (click)="login.emit()"
      class="w-full px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700
             focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2
             transition-colors duration-200 font-medium">
      Zaloguj się, aby dołączyć
    </button>

    <button
      type="button"
      (click)="register.emit()"
      class="w-full px-4 py-2 bg-white text-blue-600 border-2 border-blue-600 rounded-lg
             hover:bg-blue-50 focus:outline-none focus:ring-2 focus:ring-blue-500
             focus:ring-offset-2 transition-colors duration-200 font-medium">
      Zarejestruj się, aby dołączyć
    </button>
  </div>
</div>
```

**Handled Events**:
- `click` on login button → emit `login` event
- `click` on register button → emit `register` event

**Validation Conditions**: None

**Types**: None (no data inputs)

**Props**:
```typescript
export class UnauthenticatedActionsComponent {
  login = output<void>();
  register = output<void>();
}
```

---

### 4.4 JoinGroupFormComponent (Presentational)

**Component Description**:
Form component for joining a group with optional budget suggestion. Handles form validation, displays loading state during submission, and emits join event with form data.

**File Location**: `src/app/features/invitations/components/join-group-form.component.ts`

**Main Elements**:
```html
<div class="bg-white rounded-lg shadow-md p-6 border border-gray-200">
  <h2 class="text-lg font-semibold text-gray-900 mb-4">
    Dołącz do grupy: {{ groupName }}
  </h2>

  <form [formGroup]="joinForm" (ngSubmit)="handleSubmit()" class="space-y-6">
    <app-budget-suggestion-input
      [control]="joinForm.controls.budgetSuggestion"
      [disabled]="isJoining" />

    <div class="bg-gray-50 rounded-lg p-4 border border-gray-200">
      <p class="text-sm text-gray-600">
        Sugestia budżetu jest opcjonalna. Organizator może wykorzystać ją do ustalenia
        finalnego budżetu dla całej grupy.
      </p>
    </div>

    <button
      type="submit"
      [disabled]="isJoining || joinForm.invalid"
      class="w-full px-4 py-3 bg-green-600 text-white rounded-lg hover:bg-green-700
             disabled:bg-gray-400 disabled:cursor-not-allowed focus:outline-none
             focus:ring-2 focus:ring-green-500 focus:ring-offset-2
             transition-colors duration-200 font-medium flex items-center justify-center gap-2">
      @if (isJoining) {
        <svg class="animate-spin h-5 w-5 text-white"><!-- Spinner icon --></svg>
        <span>Dołączanie...</span>
      } @else {
        <span>Dołącz do grupy</span>
      }
    </button>
  </form>
</div>
```

**Handled Events**:
- `ngOnInit`: Initialize form with validators
- `submit`: Validate form and emit join event with data
- Form value changes: Real-time validation feedback

**Validation Conditions**:
- Budget suggestion (if provided):
  - Must be positive number
  - Must be between 0.01 and 99999999.99
  - Maximum 2 decimal places
  - Optional (can be null/empty)

**Types**:
- `JoinGroupFormData` (output event data)

**Props**:
```typescript
export class JoinGroupFormComponent {
  groupName = input.required<string>();
  isJoining = input.required<boolean>();
  submit = output<JoinGroupFormData>();

  // Internal form management
  private readonly fb = inject(FormBuilder);
  readonly joinForm = this.fb.group({
    budgetSuggestion: [null as number | null, [
      Validators.min(0.01),
      Validators.max(99999999.99),
      this.decimalPlacesValidator(2)
    ]]
  });
}
```

---

### 4.5 BudgetSuggestionInputComponent (Presentational)

**Component Description**:
Reusable input component for budget suggestion with PLN currency label, validation feedback, and proper number formatting. Integrates with Angular Reactive Forms.

**File Location**: `src/app/shared/components/budget-suggestion-input.component.ts`

**Main Elements**:
```html
<div class="space-y-2">
  <label for="budget-suggestion" class="block text-sm font-medium text-gray-700">
    Sugestia budżetu (opcjonalnie)
  </label>

  <div class="relative">
    <input
      id="budget-suggestion"
      type="number"
      step="0.01"
      min="0.01"
      [formControl]="control"
      [disabled]="disabled"
      placeholder="np. 50.00"
      class="block w-full rounded-lg border-gray-300 pr-12
             focus:border-blue-500 focus:ring-blue-500
             disabled:bg-gray-100 disabled:cursor-not-allowed
             @if(control.invalid && control.touched) { border-red-300 }">

    <div class="absolute inset-y-0 right-0 pr-3 flex items-center pointer-events-none">
      <span class="text-gray-500 sm:text-sm">PLN</span>
    </div>
  </div>

  @if (control.invalid && control.touched) {
    <p class="text-sm text-red-600">
      @if (control.hasError('min')) {
        Budżet musi być większy niż 0.01 PLN
      }
      @if (control.hasError('max')) {
        Budżet nie może przekraczać 99,999,999.99 PLN
      }
      @if (control.hasError('decimalPlaces')) {
        Budżet może mieć maksymalnie 2 miejsca po przecinku
      }
    </p>
  }
</div>
```

**Handled Events**:
- `input`: Update form control value
- `blur`: Trigger validation feedback

**Validation Conditions**:
- Minimum value: 0.01
- Maximum value: 99999999.99
- Maximum decimal places: 2
- Optional (can be empty)

**Types**:
- `FormControl<number | null>` (input control)

**Props**:
```typescript
export class BudgetSuggestionInputComponent {
  control = input.required<FormControl<number | null>>();
  disabled = input<boolean>(false);
}
```

---

### 4.6 ErrorAlertComponent (Shared Component)

**Component Description**:
Reusable error display component that shows user-friendly error messages based on error type, with optional action buttons (retry, navigation).

**File Location**: `src/app/shared/components/error-alert.component.ts`

**Main Elements**:
```html
<div class="bg-red-50 border border-red-200 rounded-lg p-6">
  <div class="flex items-start gap-3">
    <svg class="w-6 h-6 text-red-600 flex-shrink-0"><!-- Error icon --></svg>

    <div class="flex-1">
      <h3 class="text-lg font-semibold text-red-900 mb-2">
        {{ getErrorTitle() }}
      </h3>

      <p class="text-red-800 mb-4">
        {{ error.message }}
      </p>

      @if (showActions) {
        <div class="flex flex-wrap gap-3">
          @if (error.type === 'network') {
            <button
              type="button"
              (click)="retry.emit()"
              class="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700">
              Spróbuj ponownie
            </button>
          }

          @if (error.type === 'invalid' || error.type === 'expired') {
            <a
              routerLink="/groups/create"
              class="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700">
              Stwórz własną grupę
            </a>
            <a
              routerLink="/groups"
              class="px-4 py-2 bg-white text-gray-700 border border-gray-300 rounded-lg hover:bg-gray-50">
              Moje grupy
            </a>
          }
        </div>
      }
    </div>
  </div>
</div>
```

**Handled Events**:
- `click` on retry button → emit `retry` event

**Validation Conditions**: None

**Types**:
- `InvitationError` (input)

**Props**:
```typescript
export class ErrorAlertComponent {
  error = input.required<InvitationError>();
  showActions = input<boolean>(true);
  retry = output<void>();
}
```

---

## 5. Types

### 5.1 API Response DTOs

#### InvitationDetailsDto
Maps directly to `GetInvitationDetailsResponse` from backend.

```typescript
/**
 * Response DTO for invitation details
 * Maps to: GetInvitationDetailsResponse from backend
 */
export interface InvitationDetailsDto {
  /** Invitation token (UUID) */
  invitationToken: string;

  /** Group unique identifier */
  groupId: string;

  /** Name of the Secret Santa group */
  groupName: string;

  /** Full name of the group organizer */
  organizerName: string;

  /** Current number of participants in the group */
  participantCount: number;

  /** Whether the draw has been completed */
  drawCompleted: boolean;

  /** Whether the invitation is valid and can be used */
  isValid: boolean;
}
```

#### AcceptInvitationRequestDto
Maps to `AcceptInvitationRequest` from backend.

```typescript
/**
 * Request DTO for accepting an invitation
 * Maps to: AcceptInvitationRequest from backend
 */
export interface AcceptInvitationRequestDto {
  /** Optional budget suggestion in PLN (0.01 - 99999999.99) */
  budgetSuggestion?: number;
}
```

#### AcceptInvitationResponseDto
Maps to `AcceptInvitationResponse` from backend.

```typescript
/**
 * Response DTO for successful invitation acceptance
 * Maps to: AcceptInvitationResponse from backend
 */
export interface AcceptInvitationResponseDto {
  /** Group unique identifier */
  groupId: string;

  /** Name of the joined group */
  groupName: string;

  /** Full name of group organizer */
  organizerName: string;

  /** Updated participant count after join */
  participantCount: number;

  /** Final budget in PLN (null if not set by organizer) */
  budget: number | null;

  /** Whether draw has been completed */
  drawCompleted: boolean;

  /** Timestamp when user joined the group (ISO 8601 string) */
  joinedAt: string;
}
```

---

### 5.2 View Models

#### InvitationViewState
Component state representing the entire view state.

```typescript
/**
 * View state for InvitationComponent
 * Manages loading, data, error, and joining states
 */
export interface InvitationViewState {
  /** Whether invitation details are being loaded */
  loading: boolean;

  /** Loaded invitation details (null if not loaded or error) */
  invitation: InvitationDetailsDto | null;

  /** Error state (null if no error) */
  error: InvitationError | null;

  /** Whether join request is in progress */
  isJoining: boolean;
}
```

#### InvitationError
Error state with categorized error types for specific UI handling.

```typescript
/**
 * Error state for invitation view
 * Categorizes errors for appropriate UI messaging
 */
export interface InvitationError {
  /**
   * Error type category
   * - invalid: 404, token not found or malformed
   * - expired: 410, draw already completed
   * - already-joined: 409, user already in group
   * - network: Connection or timeout error
   * - unknown: Unexpected server error
   */
  type: 'invalid' | 'expired' | 'already-joined' | 'network' | 'unknown';

  /** User-friendly error message in Polish */
  message: string;
}
```

#### JoinGroupFormData
Form data emitted from JoinGroupFormComponent.

```typescript
/**
 * Form data for joining a group
 * Emitted by JoinGroupFormComponent on submission
 */
export interface JoinGroupFormData {
  /** Optional budget suggestion in PLN */
  budgetSuggestion: number | null;
}
```

---

### 5.3 Error Response Type

#### ErrorResponse
Standard API error response structure.

```typescript
/**
 * Standard API error response
 * Used by backend for error responses
 */
export interface ErrorResponse {
  /** Error code/identifier */
  error: string;

  /** Human-readable error message */
  message: string;
}
```

---

## 6. State Management

### 6.1 Component-Level State (Signals)

The Invitation View uses **component-level signals** for local state management, as the invitation data is specific to the current view and not shared across the application.

#### Primary State Signal

```typescript
export class InvitationComponent implements OnInit {
  // Writable signal for internal state management
  private readonly state = signal<InvitationViewState>({
    loading: true,
    invitation: null,
    error: null,
    isJoining: false
  });

  // Expose read-only state to template
  readonly invitationState = this.state.asReadonly();

  // Computed values from state
  readonly loading = computed(() => this.state().loading);
  readonly invitation = computed(() => this.state().invitation);
  readonly error = computed(() => this.state().error);
  readonly isJoining = computed(() => this.state().isJoining);
}
```

#### Authentication State (From Service)

Authentication state is managed by `AuthService` and consumed as a computed signal:

```typescript
export class InvitationComponent implements OnInit {
  private readonly authService = inject(AuthService);

  // Computed from AuthService signal
  readonly isAuthenticated = computed(() =>
    this.authService.isAuthenticated()
  );
}
```

### 6.2 State Transitions

#### Initial Load Flow
```typescript
ngOnInit() {
  const token = this.route.snapshot.params['token'];
  this.loadInvitation(token);
}

private loadInvitation(token: string): void {
  // Set loading state
  this.state.update(s => ({ ...s, loading: true, error: null }));

  // Fetch invitation details
  this.invitationService.getInvitationDetails(token)
    .pipe(takeUntilDestroyed(this.destroyRef))
    .subscribe({
      next: (invitation) => {
        this.state.update(s => ({
          ...s,
          loading: false,
          invitation,
          error: null
        }));
      },
      error: (err) => {
        this.state.update(s => ({
          ...s,
          loading: false,
          invitation: null,
          error: this.mapError(err)
        }));
      }
    });
}
```

#### Join Group Flow
```typescript
handleJoinGroup(formData: JoinGroupFormData): void {
  const token = this.state().invitation!.invitationToken;

  // Set joining state
  this.state.update(s => ({ ...s, isJoining: true }));

  // Call API
  this.invitationService.acceptInvitation(token, formData.budgetSuggestion)
    .pipe(takeUntilDestroyed(this.destroyRef))
    .subscribe({
      next: (response) => {
        // Show success toast
        this.toastService.success('Pomyślnie dołączono do grupy!');

        // Navigate to group details
        this.router.navigate(['/groups', response.groupId]);
      },
      error: (err) => {
        this.state.update(s => ({
          ...s,
          isJoining: false,
          error: this.mapError(err)
        }));
      }
    });
}
```

### 6.3 Token Persistence

For seamless join after registration, the invitation token is stored in session storage:

```typescript
// Before navigating to login/register
navigateToLogin(): void {
  const token = this.route.snapshot.params['token'];
  sessionStorage.setItem('pendingInvitationToken', token);
  this.router.navigate(['/login'], {
    queryParams: { returnUrl: `/invite/${token}` }
  });
}

// On component init, check for stored token
ngOnInit() {
  const token = this.route.snapshot.params['token'];
  const storedToken = sessionStorage.getItem('pendingInvitationToken');

  if (storedToken === token && this.isAuthenticated()) {
    // User just authenticated, auto-show join form
    sessionStorage.removeItem('pendingInvitationToken');
  }

  this.loadInvitation(token);
}
```

### 6.4 No Custom Hook Required

Unlike React, Angular doesn't use hooks. The state management is handled through:
- **Component signals** for local state
- **Service signals** for shared authentication state
- **RxJS** for async API operations (converted to signal updates)
- **Computed signals** for derived values

---

## 7. API Integration

### 7.1 InvitationService

Create a new service at `src/app/core/services/invitation.service.ts`:

```typescript
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '@env/environment';

@Injectable({
  providedIn: 'root'
})
export class InvitationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/invitations`;

  /**
   * Get invitation details by token (public endpoint)
   * @param token Invitation token (UUID)
   * @returns Observable<InvitationDetailsDto>
   */
  getInvitationDetails(token: string): Observable<InvitationDetailsDto> {
    return this.http.get<InvitationDetailsDto>(`${this.baseUrl}/${token}`)
      .pipe(catchError(this.handleError));
  }

  /**
   * Accept invitation and join group (requires authentication)
   * @param token Invitation token (UUID)
   * @param budgetSuggestion Optional budget suggestion in PLN
   * @returns Observable<AcceptInvitationResponseDto>
   */
  acceptInvitation(
    token: string,
    budgetSuggestion?: number
  ): Observable<AcceptInvitationResponseDto> {
    const request: AcceptInvitationRequestDto = {
      budgetSuggestion: budgetSuggestion ?? undefined
    };

    return this.http.post<AcceptInvitationResponseDto>(
      `${this.baseUrl}/${token}/accept`,
      request
    ).pipe(catchError(this.handleError));
  }

  /**
   * Store invitation token for use after authentication
   */
  storeInvitationToken(token: string): void {
    sessionStorage.setItem('pendingInvitationToken', token);
  }

  /**
   * Retrieve stored invitation token
   */
  getStoredInvitationToken(): string | null {
    return sessionStorage.getItem('pendingInvitationToken');
  }

  /**
   * Clear stored invitation token
   */
  clearStoredInvitationToken(): void {
    sessionStorage.removeItem('pendingInvitationToken');
  }

  /**
   * Handle HTTP errors
   */
  private handleError(error: HttpErrorResponse): Observable<never> {
    // Let the component handle the error mapping
    return throwError(() => error);
  }
}
```

### 7.2 API Endpoints

#### Endpoint 1: Get Invitation Details

**Method**: GET
**Path**: `/api/invitations/{token}`
**Authentication**: None (public endpoint)
**Request Type**: None (path parameter only)
**Response Type**: `InvitationDetailsDto`

**Request Example**:
```typescript
this.invitationService.getInvitationDetails('a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d')
```

**Success Response (200)**:
```json
{
  "invitationToken": "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "groupName": "Family Secret Santa 2025",
  "organizerName": "Jan Kowalski",
  "participantCount": 5,
  "drawCompleted": false,
  "isValid": true
}
```

**Error Responses**:
- **404 Not Found**: Invalid token
  ```json
  {
    "error": "InvalidInvitation",
    "message": "This invitation link is invalid or has expired"
  }
  ```
- **410 Gone**: Draw already completed
  ```json
  {
    "error": "InvitationExpired",
    "message": "This group has already completed the draw and is no longer accepting participants"
  }
  ```

---

#### Endpoint 2: Accept Invitation

**Method**: POST
**Path**: `/api/invitations/{token}/accept`
**Authentication**: Required (JWT token in Authorization header)
**Request Type**: `AcceptInvitationRequestDto`
**Response Type**: `AcceptInvitationResponseDto`

**Request Example**:
```typescript
this.invitationService.acceptInvitation(
  'a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d',
  50.00  // Optional budget suggestion
)
```

**Request Body**:
```json
{
  "budgetSuggestion": 50.00
}
```

**Success Response (201 Created)**:
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "groupName": "Family Secret Santa 2025",
  "organizerName": "Jan Kowalski",
  "participantCount": 6,
  "budget": null,
  "drawCompleted": false,
  "joinedAt": "2025-01-15T14:30:00Z"
}
```

**Error Responses**:
- **400 Bad Request**: Invalid budget value
- **401 Unauthorized**: Not authenticated
- **404 Not Found**: Invalid token
- **409 Conflict**: Already joined
- **410 Gone**: Draw already completed

### 7.3 HTTP Interceptors

**AuthInterceptor**: Automatically adds JWT token to requests (already exists).

**ErrorInterceptor**: Should be configured to NOT intercept 401 on public endpoints.

---

## 8. User Interactions

### 8.1 Interaction Flow: Unauthenticated User Views Invitation

**User Action**: User clicks invitation link `/invite/{token}` (not logged in)

**System Response**:
1. Route loads `InvitationComponent`
2. Component shows `LoadingSpinnerComponent`
3. Component calls `invitationService.getInvitationDetails(token)`
4. On success:
   - Update state with invitation data
   - Hide loading spinner
   - Display `InvitationDetailsCardComponent` with group info
   - Display `UnauthenticatedActionsComponent` with login/register CTAs
5. On error (404/410):
   - Update state with error
   - Display `ErrorAlertComponent` with appropriate message and action buttons

**Expected Outcome**: User sees group details and is prompted to login or register.

---

### 8.2 Interaction Flow: User Clicks "Login to Join"

**User Action**: Click "Zaloguj się, aby dołączyć" button

**System Response**:
1. `UnauthenticatedActionsComponent` emits `login` event
2. `InvitationComponent` handles event:
   - Stores token in session storage via `invitationService.storeInvitationToken(token)`
   - Navigates to `/login` with query param `returnUrl=/invite/{token}`

**Expected Outcome**: User is redirected to login page, with return URL set.

---

### 8.3 Interaction Flow: User Clicks "Register to Join"

**User Action**: Click "Zarejestruj się, aby dołączyć" button

**System Response**:
1. `UnauthenticatedActionsComponent` emits `register` event
2. `InvitationComponent` handles event:
   - Stores token in session storage
   - Navigates to `/register` with query param `returnUrl=/invite/{token}`

**Expected Outcome**: User is redirected to registration page, with return URL set.

---

### 8.4 Interaction Flow: User Returns After Authentication

**User Action**: User completes login/registration and is redirected back to `/invite/{token}`

**System Response**:
1. Component initializes
2. Detects authenticated state via `authService.isAuthenticated()`
3. Retrieves stored token from session storage (if exists)
4. Clears stored token
5. Loads invitation details
6. Displays `InvitationDetailsCardComponent` (same as before)
7. Displays `JoinGroupFormComponent` (instead of auth CTAs)

**Expected Outcome**: User sees join form with optional budget field.

---

### 8.5 Interaction Flow: User Enters Budget Suggestion

**User Action**: Types budget value in input field (e.g., "50.00")

**System Response**:
1. `BudgetSuggestionInputComponent` updates form control value
2. Form validators run automatically:
   - Check minimum (0.01)
   - Check maximum (99999999.99)
   - Check decimal places (max 2)
3. Display validation errors if invalid (below input field)
4. Update submit button disabled state

**Expected Outcome**: User receives immediate validation feedback.

---

### 8.6 Interaction Flow: User Submits Join Request

**User Action**: Click "Dołącz do grupy" button

**System Response**:
1. `JoinGroupFormComponent` validates form
2. If valid:
   - Emits `submit` event with form data
   - `InvitationComponent` handles event:
     - Updates state: `isJoining = true`
     - Disables submit button (via `isJoining` prop)
     - Shows loading spinner in button
     - Calls `invitationService.acceptInvitation(token, budgetSuggestion)`
3. On success:
   - Shows success toast: "Pomyślnie dołączono do grupy!"
   - Navigates to `/groups/{groupId}`
4. On error (409, 410, etc.):
   - Updates state: `isJoining = false`, sets error
   - Displays `ErrorAlertComponent` with error message

**Expected Outcome**: User joins group and is redirected to group details page.

---

### 8.7 Interaction Flow: User Encounters Invalid Token

**User Action**: User clicks invitation link with invalid token

**System Response**:
1. Component loads and calls API
2. API returns 404 with `InvalidInvitation` error
3. Component maps error to `InvitationError`:
   - `type: 'invalid'`
   - `message: "To zaproszenie jest nieprawidłowe lub wygasło"`
4. Displays `ErrorAlertComponent` with:
   - Error message
   - "Stwórz własną grupę" button → `/groups/create`
   - "Moje grupy" button → `/groups`

**Expected Outcome**: User sees friendly error message with navigation options.

---

### 8.8 Interaction Flow: User Tries to Join After Draw

**User Action**: User clicks invitation link for group with completed draw

**System Response**:
1. Component loads and calls API
2. API returns 410 with `InvitationExpired` error
3. Component maps error to `InvitationError`:
   - `type: 'expired'`
   - `message: "Ta grupa zakończyła już losowanie i nie przyjmuje nowych uczestników"`
4. Displays `ErrorAlertComponent` with message and navigation options

**Expected Outcome**: User understands group is closed and has alternative actions.

---

### 8.9 Interaction Flow: User Already in Group

**User Action**: User (authenticated) tries to join group they're already in

**System Response**:
1. User sees join form (since authenticated)
2. User clicks "Dołącz do grupy"
3. API returns 409 Conflict
4. Component maps error to `InvitationError`:
   - `type: 'already-joined'`
   - `message: "Jesteś już członkiem tej grupy"`
5. Shows toast notification
6. Redirects to `/groups/{groupId}`

**Expected Outcome**: User is informed and redirected to group page.

---

## 9. Conditions and Validation

### 9.1 Token Validation

**Condition**: Token must be valid UUID format
**Verified by**: Angular router (`:token` parameter accepts any string, UUID validation happens server-side)
**Component Verification**: None (rely on API response)
**UI Impact**:
- Valid token → Display invitation details
- Invalid token → Display error alert with "invalid invitation" message

---

### 9.2 Invitation Validity

**Condition**: `isValid === true` in API response
**Verified by**: API
**Component Verification**: Check `invitation.isValid` from response
**UI Impact**:
- If `isValid === false` → Treat as invalid invitation (show error)
- Currently not explicitly used in backend response flow (404 returned instead)

---

### 9.3 Draw Completion Status

**Condition**: `drawCompleted === false` to allow joining
**Verified by**: API returns 410 if draw completed
**Component Verification**:
- Check HTTP status code (410)
- Map to `expired` error type
**UI Impact**:
- `drawCompleted === true` → Display "group closed" error
- Disable join functionality entirely

---

### 9.4 Authentication State

**Condition**: User must be authenticated to join
**Verified by**:
- Component: `authService.isAuthenticated()`
- API: JWT token presence (401 if missing)
**Component Verification**:
```typescript
readonly isAuthenticated = computed(() =>
  this.authService.isAuthenticated()
);
```
**UI Impact**:
- Not authenticated → Show auth CTAs
- Authenticated → Show join form

---

### 9.5 Budget Suggestion Validation

**Condition**: If provided, must be valid number (0.01 - 99999999.99, max 2 decimals)
**Verified by**:
- Component: Angular form validators
- API: Server-side validation with `[Range]` attribute
**Component Verification**:
```typescript
budgetSuggestion: [null, [
  Validators.min(0.01),
  Validators.max(99999999.99),
  decimalPlacesValidator(2)
]]
```
**UI Impact**:
- Invalid → Show inline error message below input
- Invalid → Disable submit button
- Valid or empty → Enable submit button

**Custom Validator**:
```typescript
function decimalPlacesValidator(maxPlaces: number): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) return null; // Allow empty

    const stringValue = control.value.toString();
    const decimalIndex = stringValue.indexOf('.');

    if (decimalIndex === -1) return null; // No decimals

    const decimalPlaces = stringValue.length - decimalIndex - 1;
    return decimalPlaces <= maxPlaces
      ? null
      : { decimalPlaces: { max: maxPlaces, actual: decimalPlaces } };
  };
}
```

---

### 9.6 Duplicate Join Prevention

**Condition**: User cannot join same group twice
**Verified by**: API returns 409 Conflict
**Component Verification**: None (rely on API error)
**UI Impact**:
- On 409 error → Show toast notification
- Redirect to group details page

---

### 9.7 Form Submission State

**Condition**: Prevent multiple simultaneous join requests
**Verified by**: Component state `isJoining`
**Component Verification**:
```typescript
readonly isJoining = computed(() => this.state().isJoining);
```
**UI Impact**:
- `isJoining === true`:
  - Disable submit button
  - Show loading spinner in button
  - Disable budget input field
- `isJoining === false`:
  - Enable form (if valid)

---

## 10. Error Handling

### 10.1 Error Mapping Function

Create a centralized error mapping function in `InvitationComponent`:

```typescript
private mapError(error: HttpErrorResponse): InvitationError {
  // Network/connection errors
  if (error.status === 0) {
    return {
      type: 'network',
      message: 'Nie można połączyć się z serwerem. Sprawdź połączenie internetowe.'
    };
  }

  // Map HTTP status codes to error types
  switch (error.status) {
    case 404:
      return {
        type: 'invalid',
        message: 'To zaproszenie jest nieprawidłowe lub wygasło.'
      };

    case 409:
      return {
        type: 'already-joined',
        message: 'Jesteś już członkiem tej grupy.'
      };

    case 410:
      return {
        type: 'expired',
        message: 'Ta grupa zakończyła już losowanie i nie przyjmuje nowych uczestników.'
      };

    case 401:
      // Should not happen on GET invitation details (public)
      // Can happen on POST accept invitation if token expired
      return {
        type: 'unknown',
        message: 'Twoja sesja wygasła. Zaloguj się ponownie.'
      };

    case 400:
      // Validation error from API
      return {
        type: 'unknown',
        message: error.error?.message || 'Nieprawidłowe dane. Sprawdź formularz.'
      };

    default:
      return {
        type: 'unknown',
        message: 'Wystąpił nieoczekiwany błąd. Spróbuj ponownie później.'
      };
  }
}
```

### 10.2 Error Scenarios

#### Scenario 1: Invalid Token (404)
- **Trigger**: Token doesn't exist or is malformed
- **API Response**: 404 with `InvalidInvitation` error
- **Error Type**: `invalid`
- **UI Display**:
  - Error alert with message
  - "Stwórz własną grupę" button
  - "Moje grupy" button
- **User Action**: Navigate to alternative pages

---

#### Scenario 2: Draw Completed (410)
- **Trigger**: Group has already executed draw
- **API Response**: 410 with `InvitationExpired` error
- **Error Type**: `expired`
- **UI Display**:
  - Error alert with specific message
  - Navigation options to create group or view groups
- **User Action**: Understand group is closed, navigate elsewhere

---

#### Scenario 3: Already Joined (409)
- **Trigger**: User tries to join group they're already in
- **API Response**: 409 Conflict
- **Error Type**: `already-joined`
- **UI Display**:
  - Toast notification: "Jesteś już członkiem tej grupy"
  - Automatic redirect to group details
- **User Action**: None (automatic redirect)

---

#### Scenario 4: Network Error
- **Trigger**: No internet connection or API unreachable
- **API Response**: HTTP status 0 or timeout
- **Error Type**: `network`
- **UI Display**:
  - Error alert with network message
  - "Spróbuj ponownie" button
- **User Action**: Click retry button to reload

---

#### Scenario 5: Budget Validation Error (Client-Side)
- **Trigger**: Invalid budget format (negative, too many decimals, etc.)
- **Validation**: Angular form validators
- **UI Display**:
  - Inline error message below input
  - Red border on input field
  - Disabled submit button
- **User Action**: Correct budget value

---

#### Scenario 6: Budget Validation Error (Server-Side)
- **Trigger**: Client-side validation bypassed or server has different rules
- **API Response**: 400 Bad Request
- **Error Type**: `unknown`
- **UI Display**:
  - Error alert or inline error with API message
- **User Action**: Correct budget value and retry

---

#### Scenario 7: Session Expired During Join
- **Trigger**: JWT token expired while user was on page
- **API Response**: 401 Unauthorized on POST request
- **Error Type**: `unknown`
- **UI Display**:
  - Toast: "Twoja sesja wygasła"
  - Redirect to login with return URL
- **User Action**: Login again (handled by error interceptor)

---

#### Scenario 8: Unexpected Server Error (500)
- **Trigger**: Internal server error
- **API Response**: 500 Internal Server Error
- **Error Type**: `unknown`
- **UI Display**:
  - Generic error message
  - Retry option if applicable
- **User Action**: Retry or contact support

---

### 10.3 Error Handling in Component

```typescript
export class InvitationComponent implements OnInit {
  // ... other code ...

  private loadInvitation(token: string): void {
    this.state.update(s => ({ ...s, loading: true, error: null }));

    this.invitationService.getInvitationDetails(token)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (invitation) => {
          this.state.update(s => ({
            ...s,
            loading: false,
            invitation,
            error: null
          }));
        },
        error: (httpError: HttpErrorResponse) => {
          const mappedError = this.mapError(httpError);
          this.state.update(s => ({
            ...s,
            loading: false,
            invitation: null,
            error: mappedError
          }));

          // Log for debugging
          console.error('Invitation load error:', httpError);
        }
      });
  }

  handleJoinGroup(formData: JoinGroupFormData): void {
    const token = this.state().invitation!.invitationToken;

    this.state.update(s => ({ ...s, isJoining: true }));

    this.invitationService.acceptInvitation(token, formData.budgetSuggestion)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.toastService.success('Pomyślnie dołączono do grupy!');
          this.invitationService.clearStoredInvitationToken();
          this.router.navigate(['/groups', response.groupId]);
        },
        error: (httpError: HttpErrorResponse) => {
          const mappedError = this.mapError(httpError);

          // Special handling for already-joined
          if (mappedError.type === 'already-joined') {
            this.toastService.info(mappedError.message);
            const groupId = this.state().invitation!.groupId;
            this.router.navigate(['/groups', groupId]);
          } else {
            // Show error in UI
            this.state.update(s => ({
              ...s,
              isJoining: false,
              error: mappedError
            }));
          }

          console.error('Join group error:', httpError);
        }
      });
  }
}
```

---

## 11. Implementation Steps

### Step 1: Create Type Definitions

**File**: `src/app/core/models/invitation.types.ts`

1. Create file and define all interfaces:
   - `InvitationDetailsDto`
   - `AcceptInvitationRequestDto`
   - `AcceptInvitationResponseDto`
   - `InvitationViewState`
   - `InvitationError`
   - `JoinGroupFormData`

**Estimated Time**: 15 minutes

---

### Step 2: Create InvitationService

**File**: `src/app/core/services/invitation.service.ts`

1. Generate service: `ng generate service core/services/invitation`
2. Implement methods:
   - `getInvitationDetails(token: string)`
   - `acceptInvitation(token: string, budgetSuggestion?: number)`
   - `storeInvitationToken(token: string)`
   - `getStoredInvitationToken()`
   - `clearStoredInvitationToken()`
3. Add error handling
4. Write unit tests

**Estimated Time**: 45 minutes

---

### Step 3: Create BudgetSuggestionInputComponent

**File**: `src/app/shared/components/budget-suggestion-input.component.ts`

1. Generate component: `ng generate component shared/components/budget-suggestion-input --standalone`
2. Define component with `input()` props for control and disabled state
3. Implement template with:
   - Number input with PLN label
   - Validation error messages
   - Tailwind styling
4. Create custom decimal places validator
5. Write unit tests

**Estimated Time**: 1 hour

---

### Step 4: Create ErrorAlertComponent

**File**: `src/app/shared/components/error-alert.component.ts`

1. Generate component: `ng generate component shared/components/error-alert --standalone`
2. Define component with `input()` for error and showActions
3. Implement template with:
   - Error icon and message display
   - Conditional action buttons based on error type
   - Tailwind styling
4. Implement `getErrorTitle()` method for dynamic titles
5. Write unit tests

**Estimated Time**: 45 minutes

---

### Step 5: Create InvitationDetailsCardComponent

**File**: `src/app/features/invitations/components/invitation-details-card.component.ts`

1. Create directory: `src/app/features/invitations/components`
2. Generate component: `ng generate component features/invitations/components/invitation-details-card --standalone`
3. Define component with `input()` for invitation details
4. Implement template with:
   - Group name heading
   - Organizer and participant info
   - Welcome message
   - Tailwind card styling
5. Write unit tests

**Estimated Time**: 30 minutes

---

### Step 6: Create UnauthenticatedActionsComponent

**File**: `src/app/features/invitations/components/unauthenticated-actions.component.ts`

1. Generate component: `ng generate component features/invitations/components/unauthenticated-actions --standalone`
2. Define component with `output()` events for login and register
3. Implement template with:
   - Login button (primary style)
   - Register button (secondary style)
   - Descriptive text
   - Tailwind styling
4. Write unit tests

**Estimated Time**: 30 minutes

---

### Step 7: Create JoinGroupFormComponent

**File**: `src/app/features/invitations/components/join-group-form.component.ts`

1. Generate component: `ng generate component features/invitations/components/join-group-form --standalone`
2. Define component with:
   - `input()` for groupName and isJoining
   - `output()` for submit event
3. Implement reactive form with FormBuilder
4. Add BudgetSuggestionInputComponent to template
5. Implement submit handler with validation
6. Add loading state to button
7. Write unit tests

**Estimated Time**: 1.5 hours

---

### Step 8: Create InvitationComponent (Container)

**File**: `src/app/features/invitations/invitation.component.ts`

1. Create directory: `src/app/features/invitations`
2. Generate component: `ng generate component features/invitations/invitation --standalone`
3. Implement component:
   - Define state signal (`InvitationViewState`)
   - Inject services (InvitationService, AuthService, Router, ActivatedRoute, ToastService)
   - Implement `ngOnInit()` to load invitation
   - Implement token extraction from route params
   - Implement error mapping function
   - Implement navigation methods (login, register)
   - Implement join group handler
   - Add token persistence logic
4. Implement template with conditional rendering:
   - Loading state → LoadingSpinnerComponent
   - Error state → ErrorAlertComponent
   - Success state → InvitationDetailsCardComponent + conditional actions
5. Write unit tests
6. Write integration tests

**Estimated Time**: 3 hours

---

### Step 9: Configure Routing

**File**: `src/app/app.routes.ts`

1. Add route configuration:
```typescript
{
  path: 'invite/:token',
  component: InvitationComponent,
  title: 'Zaproszenie do grupy'
}
```
2. Test navigation

**Estimated Time**: 15 minutes

---

### Step 10: Update AuthService (if needed)

**File**: `src/app/core/services/auth.service.ts`

1. Verify `isAuthenticated()` signal exists and is working
2. Ensure JWT token is included in HTTP requests via interceptor
3. Test authentication state detection

**Estimated Time**: 30 minutes

---

### Step 11: Configure HTTP Interceptors

**File**: `src/app/core/interceptors/auth.interceptor.ts`

1. Verify auth interceptor doesn't add JWT to public endpoints:
   - Skip `/api/invitations/{token}` GET requests
   - Include JWT for `/api/invitations/{token}/accept` POST requests
2. Test interceptor behavior

**Estimated Time**: 30 minutes

---

### Step 12: Implement Toast Service (if not exists)

**File**: `src/app/core/services/toast.service.ts`

1. Create toast service with methods:
   - `success(message: string)`
   - `error(message: string)`
   - `info(message: string)`
2. Implement toast notification component (if not exists)
3. Test toast display

**Estimated Time**: 1 hour (if new), 15 minutes (if exists)

---

### Step 13: Add Polish Translations

**File**: Component templates

1. Ensure all UI text is in Polish:
   - Button labels
   - Error messages
   - Helper text
   - Validation messages
2. Use consistent terminology

**Estimated Time**: 30 minutes

---

### Step 14: Responsive Design Testing

1. Test mobile layout (<640px):
   - Full-width cards
   - Stacked vertical layout
   - Touch-friendly buttons (min 44x44px)
2. Test tablet layout (640px - 1024px)
3. Test desktop layout (>1024px)
4. Fix any layout issues

**Estimated Time**: 1 hour

---

### Step 15: Manual Testing

1. **Test Flow 1: Unauthenticated User**
   - Access invitation link (not logged in)
   - Verify group details display
   - Verify auth CTAs display
   - Click login → verify navigation and token storage
   - Login → verify return to invitation
   - Verify join form displays

2. **Test Flow 2: Authenticated User**
   - Access invitation link (logged in)
   - Verify group details display
   - Verify join form displays immediately
   - Enter budget suggestion
   - Submit join
   - Verify success toast
   - Verify redirect to group details

3. **Test Flow 3: Invalid Token**
   - Access invitation with invalid token
   - Verify 404 error displays
   - Verify error message and action buttons

4. **Test Flow 4: Expired Invitation (Draw Completed)**
   - Access invitation for group with completed draw
   - Verify 410 error displays
   - Verify specific error message

5. **Test Flow 5: Already Joined**
   - Join a group
   - Access same invitation link again
   - Submit join
   - Verify 409 error handling
   - Verify redirect to group

6. **Test Flow 6: Validation Errors**
   - Enter invalid budget values:
     - Negative number
     - Too many decimals (e.g., 50.123)
     - Very large number
   - Verify inline validation errors
   - Verify submit button disabled

**Estimated Time**: 2 hours

---

### Step 16: Write Component Unit Tests

1. Test InvitationComponent:
   - Loading state
   - Error state mapping
   - Authentication state detection
   - Token persistence
   - Join flow
2. Test child components:
   - InvitationDetailsCardComponent
   - UnauthenticatedActionsComponent
   - JoinGroupFormComponent
   - BudgetSuggestionInputComponent
3. Test InvitationService:
   - API calls
   - Token storage methods

**Estimated Time**: 4 hours

---

### Step 17: Write Integration Tests

1. Test end-to-end invitation flow:
   - Unauthenticated → view → login → join
   - Authenticated → view → join
   - Error scenarios
2. Test with TestBed and HttpClientTestingModule

**Estimated Time**: 2 hours

---

### Step 18: Code Review and Refactoring

1. Review code for:
   - TypeScript best practices
   - Angular 20 patterns (signals, inject(), standalone)
   - Accessibility (ARIA labels, keyboard navigation)
   - Error handling completeness
2. Refactor as needed
3. Update documentation

**Estimated Time**: 1.5 hours

---

### Step 19: Final Testing and Bug Fixes

1. Run all unit tests: `ng test`
2. Run all e2e tests (if applicable)
3. Manual regression testing
4. Fix any discovered bugs
5. Test on multiple browsers (Chrome, Firefox, Safari)

**Estimated Time**: 2 hours

---

### Step 20: Documentation

1. Add JSDoc comments to service methods
2. Update README with invitation flow documentation
3. Document any special considerations
4. Create user guide for invitation feature (if needed)

**Estimated Time**: 1 hour

---

## Total Estimated Implementation Time

**Total**: ~24-26 hours (3-4 working days for one developer)

**Breakdown**:
- Component creation: ~8 hours
- Service implementation: ~2 hours
- Testing: ~8 hours
- Integration and bug fixes: ~4 hours
- Documentation and polish: ~2 hours
- Responsive design and accessibility: ~2 hours

---

## Additional Considerations

### Accessibility
- Add ARIA labels to buttons and inputs
- Ensure keyboard navigation works properly
- Test with screen readers
- Provide sufficient color contrast (WCAG AA)

### Performance
- Use OnPush change detection (already specified)
- Lazy load invitation feature module (if needed)
- Optimize bundle size

### Security
- Never expose sensitive data in invitation details
- Validate token server-side (already implemented)
- Use HTTPS only
- Sanitize all user inputs (handled by Angular)

### Future Enhancements
- QR code generation for invitation links
- Social sharing buttons
- Preview invitation as organizer
- Invitation expiration dates
- Invitation usage tracking
