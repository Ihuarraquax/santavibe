# View Implementation Plan: Groups List View

## 1. Overview

The Groups List View serves as the main landing page after user authentication. It displays all Secret Santa groups where the authenticated user is a participant. The view provides a comprehensive overview of each group including draw status, participant count, budget information, and key dates. Users can navigate to group details, create new groups, and manually refresh the list to see the latest data.

## 2. View Routing

**Path**: `/groups`

**Route Configuration**:
```typescript
{
  path: 'groups',
  component: GroupsListComponent,
  canActivate: [AuthGuard],
  title: 'My Groups - SantaVibe'
}
```

**Guard**: `AuthGuard` - Ensures user is authenticated before accessing the view. Redirects to `/login` if not authenticated.

## 3. Component Structure

```
GroupsListComponent (Smart/Container Component)
├── Header Section
│   ├── Page Title ("Moje Grupy")
│   └── Refresh Button
│       └── LoadingSpinnerComponent (conditional, during refresh)
├── Main Content Section
│   ├── Loading State (@if isLoading && groups().length === 0)
│   │   └── SkeletonLoaderComponent × 3
│   ├── Groups Display (@if groups().length > 0)
│   │   └── Grid Container (responsive)
│   │       └── @for (group of groups())
│   │           └── GroupCardComponent
│   └── Empty State (@if !isLoading() && groups().length === 0)
│       └── EmptyStateComponent
└── Footer Section
    └── Create Group Button (Floating Action Button or fixed button)
```

**Layout Strategy**:
- Mobile (<768px): Single column, full-width cards, stacked layout
- Tablet (768px-1024px): 2-column grid
- Desktop (>1024px): 3-column grid with max-width container (1200px)

## 4. Component Details

### GroupsListComponent (Container Component)

**Component Description**:
The main container component responsible for fetching and displaying the user's groups. It manages loading states, handles errors, orchestrates child components, and provides navigation to group details and group creation. Uses Angular Signals for reactive state management and OnPush change detection strategy.

**Main HTML Elements**:
```html
<div class="container mx-auto px-4 py-6 max-w-7xl">
  <!-- Header -->
  <header class="mb-6 flex justify-between items-center">
    <h1 class="text-2xl font-bold text-gray-900">Moje Grupy</h1>
    <div class="flex items-center gap-4">
      @if (lastUpdated()) {
        <span class="text-sm text-gray-500">
          Ostatnia aktualizacja: {{ formatRelativeTime(lastUpdated()!) }}
        </span>
      }
      <button
        (click)="handleRefresh()"
        [disabled]="isRefreshing()"
        class="btn-secondary">
        @if (isRefreshing()) {
          <LoadingSpinnerComponent size="sm" />
        } @else {
          <svg><!-- Refresh icon --></svg>
        }
        Odśwież
      </button>
    </div>
  </header>

  <!-- Error Alert -->
  @if (error()) {
    <ErrorAlertComponent
      [message]="error()!"
      (retry)="handleRefresh()"
      (dismiss)="clearError()" />
  }

  <!-- Loading Skeleton -->
  @if (isLoading() && groups().length === 0) {
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      @for (item of [1, 2, 3]; track item) {
        <SkeletonLoaderComponent type="card" />
      }
    </div>
  }

  <!-- Groups Grid -->
  @if (groups().length > 0) {
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      @for (group of groupViewModels(); track group.groupId) {
        <GroupCardComponent
          [group]="group"
          (click)="navigateToGroup(group.groupId)" />
      }
    </div>
  }

  <!-- Empty State -->
  @if (!isLoading() && groups().length === 0 && !error()) {
    <EmptyStateComponent
      message="Nie jesteś jeszcze członkiem żadnej grupy."
      description="Stwórz nową grupę lub poproś organizatora o link zaproszenia."
      actionLabel="Utwórz Pierwszą Grupę"
      (action)="navigateToCreate()" />
  }

  <!-- Create Group FAB -->
  <button
    (click)="navigateToCreate()"
    class="fixed bottom-6 right-6 btn-primary rounded-full w-14 h-14 shadow-lg">
    <svg><!-- Plus icon --></svg>
  </button>
</div>
```

**Handled Interactions**:
- Component initialization: Fetch groups on `ngOnInit`
- Refresh button click: Reload groups data
- Group card click: Navigate to group details (`/groups/:groupId`)
- Create group button click: Navigate to group creation (`/groups/create`)
- Empty state action: Navigate to group creation
- Error retry: Retry fetching groups

**Handled Validation**:
None (this is a read-only display view with no form validation)

**Types Required**:
- `GetUserGroupsResponse` (DTO from API)
- `GroupDto` (DTO from API)
- `GroupCardViewModel` (transformed for display)
- `GroupsListState` (component state interface)

**Props**:
None (this is a routed component, not a child component)

**Component Class**:
```typescript
@Component({
  selector: 'app-groups-list',
  standalone: true,
  imports: [
    CommonModule,
    GroupCardComponent,
    SkeletonLoaderComponent,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    ErrorAlertComponent
  ],
  templateUrl: './groups-list.component.html',
  styleUrls: ['./groups-list.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class GroupsListComponent implements OnInit {
  // Injected services
  private groupService = inject(GroupService);
  private router = inject(Router);
  private errorHandlingService = inject(ErrorHandlingService);

  // Local state signals
  isLoading = signal<boolean>(true);
  isRefreshing = signal<boolean>(false);
  error = signal<string | null>(null);
  lastUpdated = signal<Date | null>(null);

  // Computed from service
  groups = computed(() => this.groupService.groups());

  // View models
  groupViewModels = computed(() =>
    this.groups().map(group => this.transformToViewModel(group))
  );

  async ngOnInit(): Promise<void> {
    await this.loadGroups();
  }

  private async loadGroups(): Promise<void> {
    try {
      this.isLoading.set(true);
      this.error.set(null);
      await this.groupService.fetchGroups();
      this.lastUpdated.set(new Date());
    } catch (err) {
      this.error.set(this.errorHandlingService.mapError(err));
    } finally {
      this.isLoading.set(false);
    }
  }

  async handleRefresh(): Promise<void> {
    try {
      this.isRefreshing.set(true);
      this.error.set(null);
      await this.groupService.fetchGroups();
      this.lastUpdated.set(new Date());
    } catch (err) {
      this.error.set(this.errorHandlingService.mapError(err));
    } finally {
      this.isRefreshing.set(false);
    }
  }

  navigateToGroup(groupId: string): void {
    this.router.navigate(['/groups', groupId]);
  }

  navigateToCreate(): void {
    this.router.navigate(['/groups/create']);
  }

  clearError(): void {
    this.error.set(null);
  }

  private transformToViewModel(dto: GroupDto): GroupCardViewModel {
    return {
      groupId: dto.groupId,
      name: dto.name,
      organizerName: dto.organizerName,
      participantCount: dto.participantCount,
      budget: dto.budget ? this.formatBudget(dto.budget) : null,
      drawStatus: dto.drawCompleted ? DrawStatus.Completed : DrawStatus.Pending,
      drawStatusLabel: dto.drawCompleted ? 'Losowanie zakończone' : 'Oczekuje na losowanie',
      drawStatusColor: dto.drawCompleted ? 'success' : 'warning',
      joinedDate: this.formatDate(dto.joinedAt),
      drawCompletedDate: dto.drawCompletedAt ? this.formatDate(dto.drawCompletedAt) : null,
      isOrganizer: dto.isOrganizer
    };
  }

  formatBudget(budget: number): string {
    return new Intl.NumberFormat('pl-PL', {
      style: 'currency',
      currency: 'PLN'
    }).format(budget);
  }

  formatDate(dateString: string): string {
    return format(new Date(dateString), 'd MMM yyyy', { locale: pl });
  }

  formatRelativeTime(date: Date): string {
    return formatDistance(date, new Date(), {
      addSuffix: true,
      locale: pl
    });
  }
}
```

---

### GroupCardComponent (Presentational Component)

**Component Description**:
A reusable presentational component that displays a single group's summary information in a card format. The card is clickable and shows key group details including name, organizer, participant count, budget, and draw status. The component uses Flowbite card styling with Tailwind CSS.

**Main HTML Elements**:
```html
<div
  class="block p-6 bg-white border border-gray-200 rounded-lg shadow hover:bg-gray-50 cursor-pointer transition-colors"
  role="button"
  tabindex="0"
  (click)="handleClick()"
  (keydown.enter)="handleClick()"
  (keydown.space)="handleClick()">

  <!-- Header -->
  <div class="mb-4">
    <h3 class="text-xl font-semibold text-gray-900 mb-2">
      {{ group.name }}
    </h3>
    <div class="flex items-center gap-2">
      <span
        class="text-xs font-medium px-2.5 py-0.5 rounded"
        [ngClass]="getBadgeClasses(group.drawStatusColor)">
        {{ group.drawStatusLabel }}
      </span>
      @if (group.isOrganizer) {
        <span class="text-xs font-medium px-2.5 py-0.5 rounded bg-blue-100 text-blue-800">
          Organizator
        </span>
      }
    </div>
  </div>

  <!-- Details -->
  <div class="space-y-2 text-sm text-gray-600">
    <div class="flex items-center gap-2">
      <svg class="w-4 h-4"><!-- User icon --></svg>
      <span>Organizator: <strong>{{ group.organizerName }}</strong></span>
    </div>

    <div class="flex items-center gap-2">
      <svg class="w-4 h-4"><!-- Users icon --></svg>
      <span>Uczestnicy: <strong>{{ group.participantCount }}</strong></span>
    </div>

    @if (group.budget) {
      <div class="flex items-center gap-2">
        <svg class="w-4 h-4"><!-- Currency icon --></svg>
        <span>Budżet: <strong>{{ group.budget }}</strong></span>
      </div>
    }

    <div class="flex items-center gap-2">
      <svg class="w-4 h-4"><!-- Calendar icon --></svg>
      <span>Dołączono: {{ group.joinedDate }}</span>
    </div>

    @if (group.drawCompletedDate) {
      <div class="flex items-center gap-2">
        <svg class="w-4 h-4"><!-- Check icon --></svg>
        <span>Losowanie: {{ group.drawCompletedDate }}</span>
      </div>
    }
  </div>
</div>
```

**Handled Interactions**:
- Card click: Emits click event (handled by parent)
- Keyboard navigation: Enter/Space key support for accessibility

**Handled Validation**:
None (display only)

**Types Required**:
- `GroupCardViewModel` (input)
- `BadgeColor` (for styling)

**Props (Component Interface)**:
```typescript
@Component({
  selector: 'app-group-card',
  standalone: true,
  imports: [CommonModule, NgClass],
  templateUrl: './group-card.component.html',
  styleUrls: ['./group-card.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class GroupCardComponent {
  // Input
  group = input.required<GroupCardViewModel>();

  // Output
  cardClick = output<string>(); // Emits groupId

  handleClick(): void {
    this.cardClick.emit(this.group().groupId);
  }

  getBadgeClasses(color: BadgeColor): string {
    const classes = {
      success: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300',
      warning: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-300',
      info: 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-300',
      error: 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-300'
    };
    return classes[color] || classes.info;
  }
}
```

---

### SkeletonLoaderComponent (Shared Component)

**Component Description**:
A reusable component that displays animated skeleton placeholders during initial data loading. Provides visual feedback to users that content is being loaded. Supports different skeleton types for various layouts.

**Main HTML Elements**:
```html
@if (type === 'card') {
  <div class="animate-pulse p-6 bg-white border border-gray-200 rounded-lg shadow">
    <!-- Header skeleton -->
    <div class="mb-4">
      <div class="h-6 bg-gray-200 rounded w-3/4 mb-2"></div>
      <div class="h-5 bg-gray-200 rounded w-1/4"></div>
    </div>

    <!-- Details skeleton -->
    <div class="space-y-2">
      <div class="h-4 bg-gray-200 rounded w-full"></div>
      <div class="h-4 bg-gray-200 rounded w-5/6"></div>
      <div class="h-4 bg-gray-200 rounded w-4/6"></div>
      <div class="h-4 bg-gray-200 rounded w-3/6"></div>
    </div>
  </div>
}
```

**Handled Interactions**: None

**Handled Validation**: None

**Types Required**:
- `SkeletonType` = `'card' | 'list' | 'table'`

**Props**:
```typescript
@Component({
  selector: 'app-skeleton-loader',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './skeleton-loader.component.html',
  styleUrls: ['./skeleton-loader.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SkeletonLoaderComponent {
  type = input<SkeletonType>('card');
}
```

---

### LoadingSpinnerComponent (Shared Component)

**Component Description**:
A simple spinner component for displaying loading states during operations. Used in buttons or inline contexts where skeleton screens aren't appropriate.

**Main HTML Elements**:
```html
<svg
  class="animate-spin"
  [ngClass]="getSizeClasses()"
  xmlns="http://www.w3.org/2000/svg"
  fill="none"
  viewBox="0 0 24 24">
  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
</svg>
@if (message()) {
  <span class="ml-2">{{ message() }}</span>
}
```

**Handled Interactions**: None

**Handled Validation**: None

**Types Required**: None (uses primitive types)

**Props**:
```typescript
@Component({
  selector: 'app-loading-spinner',
  standalone: true,
  imports: [CommonModule, NgClass],
  templateUrl: './loading-spinner.component.html',
  styleUrls: ['./loading-spinner.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoadingSpinnerComponent {
  size = input<'sm' | 'md' | 'lg'>('md');
  message = input<string>();

  getSizeClasses(): string {
    const sizes = {
      sm: 'w-4 h-4',
      md: 'w-8 h-8',
      lg: 'w-12 h-12'
    };
    return sizes[this.size()];
  }
}
```

---

### EmptyStateComponent (Shared Component)

**Component Description**:
Displays a friendly message and call-to-action when the user has no groups. Provides clear guidance on what actions the user can take.

**Main HTML Elements**:
```html
<div class="flex flex-col items-center justify-center py-12 px-4 text-center">
  <!-- Illustration/Icon -->
  <svg class="w-24 h-24 text-gray-400 mb-4">
    <!-- Empty state illustration -->
  </svg>

  <!-- Message -->
  <h3 class="text-xl font-semibold text-gray-900 mb-2">
    {{ message }}
  </h3>

  @if (description()) {
    <p class="text-gray-600 mb-6 max-w-md">
      {{ description() }}
    </p>
  }

  <!-- Action Button -->
  @if (actionLabel()) {
    <button
      (click)="handleAction()"
      class="btn-primary">
      @if (actionIcon()) {
        <svg class="w-5 h-5 mr-2"><!-- Icon --></svg>
      }
      {{ actionLabel() }}
    </button>
  }
</div>
```

**Handled Interactions**:
- Action button click: Emits action event

**Handled Validation**: None

**Types Required**: None (uses primitive types)

**Props**:
```typescript
@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './empty-state.component.html',
  styleUrls: ['./empty-state.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EmptyStateComponent {
  message = input.required<string>();
  description = input<string>();
  actionLabel = input<string>();
  actionIcon = input<string>();

  action = output<void>();

  handleAction(): void {
    this.action.emit();
  }
}
```

---

### ErrorAlertComponent (Shared Component)

**Component Description**:
Displays error messages with retry and dismiss options. Provides user-friendly error feedback with actionable buttons.

**Main HTML Elements**:
```html
<div class="flex p-4 mb-4 text-red-800 border border-red-300 rounded-lg bg-red-50" role="alert">
  <svg class="flex-shrink-0 w-5 h-5" fill="currentColor"><!-- Error icon --></svg>
  <div class="ml-3 text-sm font-medium flex-grow">
    {{ message }}
  </div>
  <div class="flex gap-2">
    <button
      (click)="handleRetry()"
      class="text-red-800 bg-transparent border border-red-800 hover:bg-red-900 hover:text-white focus:ring-4 focus:outline-none focus:ring-red-300 font-medium rounded-lg text-xs px-3 py-1.5 text-center">
      Spróbuj ponownie
    </button>
    <button
      type="button"
      (click)="handleDismiss()"
      class="ml-auto -mx-1.5 -my-1.5 bg-red-50 text-red-500 rounded-lg focus:ring-2 focus:ring-red-400 p-1.5 hover:bg-red-200 inline-flex h-8 w-8">
      <svg class="w-5 h-5"><!-- Close icon --></svg>
    </button>
  </div>
</div>
```

**Handled Interactions**:
- Retry button click: Emits retry event
- Dismiss button click: Emits dismiss event

**Handled Validation**: None

**Types Required**: None

**Props**:
```typescript
@Component({
  selector: 'app-error-alert',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './error-alert.component.html',
  styleUrls: ['./error-alert.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ErrorAlertComponent {
  message = input.required<string>();

  retry = output<void>();
  dismiss = output<void>();

  handleRetry(): void {
    this.retry.emit();
  }

  handleDismiss(): void {
    this.dismiss.emit();
  }
}
```

## 5. Types

### API DTOs (from Backend)

**GetUserGroupsResponse**
```typescript
/**
 * API response for GET /api/groups endpoint
 */
export interface GetUserGroupsResponse {
  /** Array of groups where the user is a participant */
  groups: GroupDto[];

  /** Total number of groups returned */
  totalCount: number;
}
```

**GroupDto**
```typescript
/**
 * Represents a single group in the user's group list (API response)
 */
export interface GroupDto {
  /** Unique identifier for the group (UUID) */
  groupId: string;

  /** Name of the group */
  name: string;

  /** UUID of the user who created/organizes the group */
  organizerId: string;

  /** Full name of the organizer */
  organizerName: string;

  /** Whether the current authenticated user is the organizer */
  isOrganizer: boolean;

  /** Number of participants in the group */
  participantCount: number;

  /** Budget set for the group in PLN (null if not yet set) */
  budget: number | null;

  /** Whether the Secret Santa draw has been completed */
  drawCompleted: boolean;

  /** ISO 8601 date string when the user joined the group */
  joinedAt: string;

  /** ISO 8601 date string when draw was completed (null if not completed) */
  drawCompletedAt: string | null;
}
```

### View Models (for Frontend Display)

**GroupCardViewModel**
```typescript
/**
 * View model for displaying a group card
 * Transforms API DTO data into display-ready format
 */
export interface GroupCardViewModel {
  /** Group identifier for navigation */
  groupId: string;

  /** Group name to display */
  name: string;

  /** Organizer's full name */
  organizerName: string;

  /** Number of participants */
  participantCount: number;

  /** Formatted budget string (e.g., "100,00 PLN") or null */
  budget: string | null;

  /** Draw status enum value */
  drawStatus: DrawStatus;

  /** Localized status label for display */
  drawStatusLabel: string;

  /** Badge color based on status */
  drawStatusColor: BadgeColor;

  /** Formatted join date (e.g., "15 paź 2025") */
  joinedDate: string;

  /** Formatted draw completion date or null */
  drawCompletedDate: string | null;

  /** Whether current user is the organizer */
  isOrganizer: boolean;
}
```

**DrawStatus (Enum)**
```typescript
/**
 * Enum representing the draw status of a group
 */
export enum DrawStatus {
  /** Draw has not been performed yet */
  Pending = 'pending',

  /** Draw has been completed */
  Completed = 'completed'
}
```

**BadgeColor (Type)**
```typescript
/**
 * Type for badge color variants (maps to Flowbite badge colors)
 */
export type BadgeColor = 'success' | 'warning' | 'info' | 'error';
```

**SkeletonType (Type)**
```typescript
/**
 * Type for skeleton loader variants
 */
export type SkeletonType = 'card' | 'list' | 'table';
```

### API Request Types

**GetUserGroupsQueryParams**
```typescript
/**
 * Query parameters for GET /api/groups endpoint
 */
export interface GetUserGroupsQueryParams {
  /** Whether to include groups where draw is completed (default: true) */
  includeCompleted?: boolean;
}
```

## 6. State Management

### Service-Level State (GroupService)

The `GroupService` manages shared state for groups across the application using Angular Signals.

**GroupService Implementation**:
```typescript
import { Injectable, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class GroupService {
  private http = inject(HttpClient);
  private readonly API_BASE = '/api/groups';

  // State signals
  private groupsSignal = signal<GroupDto[]>([]);

  // Readonly accessor
  groups = this.groupsSignal.asReadonly();

  /**
   * Fetch all groups for the authenticated user
   * Updates the groups signal with the response
   */
  async fetchGroups(includeCompleted: boolean = true): Promise<GetUserGroupsResponse> {
    const params = new HttpParams().set('includeCompleted', includeCompleted);

    const response = await firstValueFrom(
      this.http.get<GetUserGroupsResponse>(this.API_BASE, { params })
    );

    this.groupsSignal.set(response.groups);
    return response;
  }

  /**
   * Clear groups state (used on logout)
   */
  clearGroups(): void {
    this.groupsSignal.set([]);
  }
}
```

### Component-Level State (GroupsListComponent)

The component maintains local UI state using signals:

```typescript
// Loading states
isLoading = signal<boolean>(true);      // Initial page load
isRefreshing = signal<boolean>(false);  // Manual refresh operation

// Error state
error = signal<string | null>(null);

// Timestamp for last update
lastUpdated = signal<Date | null>(null);

// Computed state from service
groups = computed(() => this.groupService.groups());

// Transformed view models
groupViewModels = computed(() =>
  this.groups().map(group => this.transformToViewModel(group))
);
```

### State Flow

1. **Initial Load**:
   - `ngOnInit()` → `loadGroups()` → `groupService.fetchGroups()`
   - `groupService` updates `groupsSignal`
   - Component's `groups()` computed signal reacts
   - `groupViewModels()` transforms data
   - Template re-renders with new data

2. **Manual Refresh**:
   - User clicks refresh → `handleRefresh()`
   - `isRefreshing` set to `true`
   - `groupService.fetchGroups()` called
   - Service updates `groupsSignal`
   - Component signals react
   - `isRefreshing` set to `false`
   - `lastUpdated` updated with current time

3. **Error Handling**:
   - API call fails
   - Catch block sets `error` signal
   - Template shows error alert
   - User can retry or dismiss

## 7. API Integration

### Endpoint Details

**Endpoint**: `GET /api/groups`

**Authentication**: Required - JWT Bearer token (automatically added by AuthInterceptor)

**Query Parameters**:
- `includeCompleted` (optional): `boolean`, default `true`

### Request Example

```typescript
// In GroupService
async fetchGroups(includeCompleted: boolean = true): Promise<GetUserGroupsResponse> {
  const params = new HttpParams().set('includeCompleted', includeCompleted.toString());

  const response = await firstValueFrom(
    this.http.get<GetUserGroupsResponse>('/api/groups', { params })
  );

  this.groupsSignal.set(response.groups);
  return response;
}
```

**HTTP Request**:
```
GET /api/groups?includeCompleted=true
Headers:
  Authorization: Bearer <jwt-token>
  Accept: application/json
```

### Response Handling

**Success Response (200 OK)**:
```json
{
  "groups": [
    {
      "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
      "name": "Family Secret Santa 2025",
      "organizerId": "550e8400-e29b-41d4-a716-446655440000",
      "organizerName": "Jan Kowalski",
      "isOrganizer": true,
      "participantCount": 5,
      "budget": 100.00,
      "drawCompleted": false,
      "joinedAt": "2025-10-15T10:00:00Z",
      "drawCompletedAt": null
    }
  ],
  "totalCount": 1
}
```

**Error Responses**:

1. **401 Unauthorized**: Invalid or missing token
   - Handling: ErrorInterceptor catches → AuthService.logout() → Redirect to `/login`
   - User Feedback: Toast "Your session has expired. Please log in again."

2. **500 Server Error**: Internal server error
   - Handling: Catch in service → Set error signal
   - User Feedback: Error alert "Something went wrong. Please try again later."

3. **Network Error**: No connection
   - Handling: Catch in service → Set error signal
   - User Feedback: Error alert "Unable to load groups. Please check your connection."

### HTTP Interceptors

**AuthInterceptor**: Automatically adds JWT token to requests
```typescript
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const token = authService.token();

  if (token) {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  return next(req);
};
```

**ErrorInterceptor**: Handles global error responses
```typescript
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        authService.logout();
        router.navigate(['/login'], {
          queryParams: { returnUrl: router.url }
        });
      }
      return throwError(() => error);
    })
  );
};
```

## 8. User Interactions

### 1. Initial Page Load

**User Action**: User navigates to `/groups` after logging in

**System Response**:
1. AuthGuard verifies authentication
2. Component renders with loading state (`isLoading = true`)
3. Skeleton loaders display (3 card skeletons)
4. `ngOnInit()` triggers `loadGroups()`
5. API call to `GET /api/groups`
6. Response received and transformed to view models
7. Skeleton loaders replaced with actual group cards (or empty state)
8. `isLoading` set to `false`
9. `lastUpdated` timestamp set

**Expected Outcome**: User sees their groups displayed in a grid layout (or empty state with CTA if no groups)

---

### 2. Click on Group Card

**User Action**: User clicks on a group card

**System Response**:
1. `GroupCardComponent` emits `cardClick` event with `groupId`
2. `GroupsListComponent` receives event via `(click)` handler
3. `navigateToGroup(groupId)` method called
4. Router navigates to `/groups/:groupId`
5. User sees group details view

**Expected Outcome**: User navigates to the detailed view of the selected group

---

### 3. Manual Refresh

**User Action**: User clicks the "Odśwież" (Refresh) button

**System Response**:
1. `handleRefresh()` method called
2. `isRefreshing` signal set to `true`
3. Refresh button shows loading spinner
4. `groupService.fetchGroups()` called
5. API request made to `GET /api/groups`
6. Response received and state updated
7. `lastUpdated` timestamp updated to current time
8. `isRefreshing` set to `false`
9. Button returns to normal state
10. Template re-renders with updated data

**Expected Outcome**: User sees updated group list with new "Last updated" timestamp

---

### 4. Create New Group (via button)

**User Action**: User clicks "Utwórz Pierwszą Grupę" or FAB button

**System Response**:
1. `navigateToCreate()` method called
2. Router navigates to `/groups/create`
3. User sees group creation form

**Expected Outcome**: User navigates to group creation view

---

### 5. Error Retry

**User Action**: User clicks "Spróbuj ponownie" (Try again) on error alert

**System Response**:
1. `ErrorAlertComponent` emits `retry` event
2. `handleRefresh()` called in parent component
3. Error cleared (`error.set(null)`)
4. New API request initiated
5. If successful, groups displayed
6. If fails again, error shown again

**Expected Outcome**: System attempts to reload groups; user sees either groups or error message

---

### 6. Error Dismissal

**User Action**: User clicks dismiss (X) button on error alert

**System Response**:
1. `ErrorAlertComponent` emits `dismiss` event
2. `clearError()` called in parent component
3. `error` signal set to `null`
4. Error alert removed from view

**Expected Outcome**: Error alert disappears; user can continue using the app

---

### 7. Keyboard Navigation on Group Card

**User Action**: User focuses on group card and presses Enter or Space

**System Response**:
1. `(keydown.enter)` or `(keydown.space)` event handler triggered
2. `handleClick()` method called
3. Navigation to group details occurs (same as mouse click)

**Expected Outcome**: Accessible navigation for keyboard users

## 9. Conditions and Validation

### Authentication State

**Condition**: User must be authenticated to access the view

**Verification**:
- **Where**: Route guard (`AuthGuard`)
- **How**: Check if valid JWT token exists in AuthService
- **Effect on UI**:
  - If authenticated: View loads normally
  - If not authenticated: Redirect to `/login` with `returnUrl` query parameter

**Implementation**:
```typescript
export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  authService.storeReturnUrl(state.url);
  return router.createUrlTree(['/login']);
};
```

---

### Empty Groups List

**Condition**: User has no groups (empty array from API)

**Verification**:
- **Where**: `GroupsListComponent` template
- **How**: Check `groups().length === 0 && !isLoading() && !error()`
- **Effect on UI**: Display `EmptyStateComponent` instead of grid

**Template Logic**:
```html
@if (!isLoading() && groups().length === 0 && !error()) {
  <EmptyStateComponent
    message="Nie jesteś jeszcze członkiem żadnej grupy."
    description="Stwórz nową grupę lub poproś organizatora o link zaproszenia."
    actionLabel="Utwórz Pierwszą Grupę"
    (action)="navigateToCreate()" />
}
```

---

### Loading State

**Condition**: Data is being fetched for the first time

**Verification**:
- **Where**: `GroupsListComponent` template
- **How**: Check `isLoading() && groups().length === 0`
- **Effect on UI**: Display skeleton loaders instead of content

**Template Logic**:
```html
@if (isLoading() && groups().length === 0) {
  <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
    @for (item of [1, 2, 3]; track item) {
      <SkeletonLoaderComponent type="card" />
    }
  </div>
}
```

---

### Refreshing State

**Condition**: User manually refreshed data

**Verification**:
- **Where**: `GroupsListComponent` refresh button
- **How**: Check `isRefreshing()` signal
- **Effect on UI**:
  - Button shows spinner instead of icon
  - Button is disabled during refresh

**Template Logic**:
```html
<button
  (click)="handleRefresh()"
  [disabled]="isRefreshing()"
  class="btn-secondary">
  @if (isRefreshing()) {
    <LoadingSpinnerComponent size="sm" />
  } @else {
    <svg><!-- Refresh icon --></svg>
  }
  Odśwież
</button>
```

---

### Error State

**Condition**: API request failed

**Verification**:
- **Where**: `GroupsListComponent` template
- **How**: Check `error() !== null`
- **Effect on UI**: Display error alert banner with retry/dismiss options

**Template Logic**:
```html
@if (error()) {
  <ErrorAlertComponent
    [message]="error()!"
    (retry)="handleRefresh()"
    (dismiss)="clearError()" />
}
```

---

### Budget Display

**Condition**: Group has budget set

**Verification**:
- **Where**: `GroupCardComponent` template
- **How**: Check `group.budget !== null`
- **Effect on UI**: Show budget row or hide it

**Template Logic**:
```html
@if (group.budget) {
  <div class="flex items-center gap-2">
    <svg class="w-4 h-4"><!-- Currency icon --></svg>
    <span>Budżet: <strong>{{ group.budget }}</strong></span>
  </div>
}
```

---

### Draw Completed Date Display

**Condition**: Draw has been completed for the group

**Verification**:
- **Where**: `GroupCardComponent` template
- **How**: Check `group.drawCompletedDate !== null`
- **Effect on UI**: Show draw completion date or hide row

**Template Logic**:
```html
@if (group.drawCompletedDate) {
  <div class="flex items-center gap-2">
    <svg class="w-4 h-4"><!-- Check icon --></svg>
    <span>Losowanie: {{ group.drawCompletedDate }}</span>
  </div>
}
```

---

### Organizer Badge Display

**Condition**: Current user is the organizer of the group

**Verification**:
- **Where**: `GroupCardComponent` template
- **How**: Check `group.isOrganizer === true`
- **Effect on UI**: Display "Organizator" badge

**Template Logic**:
```html
@if (group.isOrganizer) {
  <span class="text-xs font-medium px-2.5 py-0.5 rounded bg-blue-100 text-blue-800">
    Organizator
  </span>
}
```

---

### Draw Status Badge Styling

**Condition**: Draw status determines badge color

**Verification**:
- **Where**: `GroupCardComponent`
- **How**: Check `group.drawCompleted` boolean
- **Effect on UI**: Apply different badge classes
  - `drawCompleted = true` → Green "success" badge
  - `drawCompleted = false` → Yellow "warning" badge

**Implementation**:
```typescript
// In transformToViewModel()
drawStatusColor: dto.drawCompleted ? 'success' : 'warning'
```

## 10. Error Handling

### 1. Authentication Errors (401 Unauthorized)

**Scenario**: JWT token is invalid, expired, or missing

**Handling**:
1. `ErrorInterceptor` catches 401 response
2. `AuthService.logout()` called (clears token, user data)
3. Current URL stored as `returnUrl`
4. Navigate to `/login?returnUrl=/groups`
5. Toast notification: "Your session has expired. Please log in again."

**User Experience**: Seamless redirect to login with ability to return after re-authentication

---

### 2. Network Errors

**Scenario**: User is offline or server is unreachable

**Detection**: HTTP request times out or network error thrown

**Handling**:
```typescript
catch (err) {
  if (err instanceof HttpErrorResponse) {
    if (err.status === 0) {
      // Network error
      this.error.set('Brak połączenia z internetem. Sprawdź swoje połączenie i spróbuj ponownie.');
    }
  }
}
```

**User Experience**:
- Error alert displayed
- Retry button available
- Dismissible alert

---

### 3. Server Errors (500, 502, 503)

**Scenario**: Server-side error or maintenance

**Handling**:
```typescript
catch (err) {
  if (err instanceof HttpErrorResponse) {
    if (err.status >= 500) {
      this.error.set('Wystąpił błąd serwera. Spróbuj ponownie później.');
    }
  }
}
```

**User Experience**:
- Generic error message (don't expose technical details)
- Retry option
- Dismissible alert

---

### 4. Malformed API Response

**Scenario**: API returns unexpected data structure

**Detection**: TypeScript type mismatch or undefined properties

**Handling**:
```typescript
async fetchGroups(): Promise<GetUserGroupsResponse> {
  try {
    const response = await firstValueFrom(
      this.http.get<GetUserGroupsResponse>('/api/groups')
    );

    // Validate response structure
    if (!response || !Array.isArray(response.groups)) {
      throw new Error('Invalid response format');
    }

    this.groupsSignal.set(response.groups);
    return response;
  } catch (err) {
    console.error('Failed to fetch groups:', err);
    throw new Error('Unable to load groups');
  }
}
```

**User Experience**: Generic error message with retry option

---

### 5. Empty State (Not an Error)

**Scenario**: User legitimately has no groups

**Handling**: Display `EmptyStateComponent` with helpful message and CTA

**User Experience**:
- Friendly message explaining the situation
- Clear call-to-action to create first group
- No error styling (not an error condition)

---

### 6. Partial Failures

**Scenario**: Some group data is malformed but request succeeds

**Handling**:
```typescript
transformToViewModel(dto: GroupDto): GroupCardViewModel {
  try {
    return {
      groupId: dto.groupId,
      name: dto.name || 'Unnamed Group',
      organizerName: dto.organizerName || 'Unknown',
      participantCount: dto.participantCount || 0,
      budget: dto.budget ? this.formatBudget(dto.budget) : null,
      // ... rest of transformation with safe defaults
    };
  } catch (err) {
    console.error('Failed to transform group:', dto, err);
    // Return a fallback view model or skip this group
    return null;
  }
}

// Filter out null results
groupViewModels = computed(() =>
  this.groups()
    .map(group => this.transformToViewModel(group))
    .filter((vm): vm is GroupCardViewModel => vm !== null)
);
```

**User Experience**: Groups that can be displayed are shown; malformed ones are skipped silently (logged for debugging)

---

### 7. Date Formatting Errors

**Scenario**: Invalid date string from API

**Handling**:
```typescript
formatDate(dateString: string | null | undefined): string {
  if (!dateString) return '';

  try {
    const date = new Date(dateString);
    if (isNaN(date.getTime())) {
      return 'Invalid date';
    }
    return format(date, 'd MMM yyyy', { locale: pl });
  } catch (err) {
    console.error('Failed to format date:', dateString, err);
    return 'Invalid date';
  }
}
```

**User Experience**: Graceful fallback to "Invalid date" or empty string

---

### 8. Currency Formatting Errors

**Scenario**: Invalid budget value

**Handling**:
```typescript
formatBudget(budget: number | null | undefined): string | null {
  if (budget == null || isNaN(budget)) return null;

  try {
    return new Intl.NumberFormat('pl-PL', {
      style: 'currency',
      currency: 'PLN'
    }).format(budget);
  } catch (err) {
    console.error('Failed to format budget:', budget, err);
    return `${budget} PLN`; // Fallback
  }
}
```

**User Experience**: Fallback to simple format if Intl fails

---

### Error Service Integration

**ErrorHandlingService**:
```typescript
@Injectable({ providedIn: 'root' })
export class ErrorHandlingService {
  mapError(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      switch (error.status) {
        case 0:
          return 'Brak połączenia z internetem. Sprawdź swoje połączenie.';
        case 401:
          return 'Sesja wygasła. Zaloguj się ponownie.';
        case 403:
          return 'Nie masz uprawnień do wykonania tej operacji.';
        case 404:
          return 'Nie znaleziono żądanych danych.';
        case 500:
        case 502:
        case 503:
          return 'Wystąpił błąd serwera. Spróbuj ponownie później.';
        default:
          return 'Wystąpił nieoczekiwany błąd. Spróbuj ponownie.';
      }
    }

    if (error instanceof Error) {
      return error.message;
    }

    return 'Wystąpił nieoczekiwany błąd.';
  }
}
```

## 11. Implementation Steps

### Step 1: Create Type Definitions

**File**: `src/app/models/group.types.ts`

1. Create API DTO interfaces:
   - `GetUserGroupsResponse`
   - `GroupDto`
   - `GetUserGroupsQueryParams`

2. Create View Model interfaces:
   - `GroupCardViewModel`
   - `DrawStatus` enum
   - `BadgeColor` type
   - `SkeletonType` type

3. Add JSDoc comments for all types

**Verification**: Types compile without errors, used by IDE autocomplete

---

### Step 2: Create GroupService

**File**: `src/app/services/group.service.ts`

1. Implement service with:
   - `groupsSignal` signal (private)
   - `groups` readonly accessor
   - `fetchGroups()` async method
   - `clearGroups()` method

2. Add HTTP client injection

3. Implement error handling

4. Add unit tests for service methods

**Verification**: Service can fetch groups from API, update signals correctly

---

### Step 3: Create Shared Components

**Files**:
- `src/app/shared/components/loading-spinner/`
- `src/app/shared/components/skeleton-loader/`
- `src/app/shared/components/empty-state/`
- `src/app/shared/components/error-alert/`

For each component:
1. Generate component with Angular CLI
2. Implement template with Tailwind/Flowbite styling
3. Define inputs using `input()` function
4. Define outputs using `output()` function
5. Add OnPush change detection
6. Create component tests

**Verification**: Components render correctly in isolation, accept props, emit events

---

### Step 4: Create GroupCardComponent

**File**: `src/app/features/groups/components/group-card/`

1. Generate component
2. Define `GroupCardViewModel` input
3. Implement template with:
   - Group name header
   - Status badges (draw status, organizer)
   - Details section (organizer, participants, budget, dates)
   - Hover/focus states
   - Keyboard navigation support

4. Implement `getBadgeClasses()` method
5. Add click and keyboard event handlers
6. Add OnPush change detection
7. Create component tests

**Verification**: Card displays all group information correctly, responds to interactions

---

### Step 5: Create GroupsListComponent

**File**: `src/app/features/groups/groups-list/`

1. Generate component
2. Inject services (GroupService, Router, ErrorHandlingService)
3. Create state signals:
   - `isLoading`
   - `isRefreshing`
   - `error`
   - `lastUpdated`

4. Create computed signals:
   - `groups` (from service)
   - `groupViewModels` (transformed)

5. Implement `ngOnInit()` with initial load
6. Implement `handleRefresh()` method
7. Implement navigation methods:
   - `navigateToGroup()`
   - `navigateToCreate()`

8. Implement transformation method:
   - `transformToViewModel()`

9. Implement formatting utilities:
   - `formatBudget()`
   - `formatDate()`
   - `formatRelativeTime()`

10. Add OnPush change detection

**Verification**: Component loads groups, handles all states correctly

---

### Step 6: Create GroupsListComponent Template

**File**: `groups-list.component.html`

1. Create page structure:
   - Container with max-width
   - Header section (title, last updated, refresh button)
   - Content section (conditional rendering)

2. Implement conditional sections:
   - Error alert (`@if error()`)
   - Skeleton loaders (`@if isLoading()`)
   - Groups grid (`@if groups().length > 0`)
   - Empty state (`@if !isLoading() && groups().length === 0`)

3. Add Floating Action Button for creating groups

4. Implement responsive grid classes:
   - Mobile: `grid-cols-1`
   - Tablet: `md:grid-cols-2`
   - Desktop: `lg:grid-cols-3`

**Verification**: Template renders correctly for all states (loading, empty, populated, error)

---

### Step 7: Add Styling

**File**: `groups-list.component.css` (if needed for custom styles)

1. Add any component-specific styles not covered by Tailwind
2. Ensure responsive layout works on all screen sizes
3. Test hover/focus states
4. Test accessibility (keyboard navigation, screen readers)

**Verification**: View looks good on mobile, tablet, and desktop; meets accessibility standards

---

### Step 8: Configure Routing

**File**: `src/app/app.routes.ts`

1. Add route configuration:
```typescript
{
  path: 'groups',
  component: GroupsListComponent,
  canActivate: [authGuard],
  title: 'Moje Grupy - SantaVibe'
}
```

2. Ensure AuthGuard is properly configured

**Verification**: Route accessible only when authenticated, redirects to login otherwise

---

### Step 9: Implement AuthGuard (if not exists)

**File**: `src/app/guards/auth.guard.ts`

1. Create functional guard using `CanActivateFn`
2. Inject AuthService and Router
3. Check authentication state
4. Store return URL if not authenticated
5. Return boolean or UrlTree

**Verification**: Guard prevents unauthorized access, stores return URL correctly

---

### Step 10: Set Up HTTP Interceptors

**Files**:
- `src/app/interceptors/auth.interceptor.ts`
- `src/app/interceptors/error.interceptor.ts`

1. Create `authInterceptor` to add JWT token
2. Create `errorInterceptor` to handle global errors
3. Register interceptors in app configuration

**Verification**: All API requests include auth token, 401 errors trigger logout

---

### Step 11: Implement Date/Currency Utilities

**File**: `src/app/utils/formatters.ts` or in component

1. Install date-fns: `npm install date-fns`
2. Import Polish locale: `import { pl } from 'date-fns/locale'`
3. Implement date formatting functions
4. Implement currency formatting functions
5. Add error handling for invalid inputs

**Verification**: Dates display in Polish format, currency displays as PLN

---

### Step 12: Write Unit Tests

**Files**:
- `group.service.spec.ts`
- `groups-list.component.spec.ts`
- `group-card.component.spec.ts`
- All shared component spec files

1. Test GroupService:
   - Successful API calls
   - Error handling
   - Signal updates

2. Test GroupsListComponent:
   - Initial load
   - Refresh functionality
   - Navigation
   - Error states
   - Empty states

3. Test GroupCardComponent:
   - Rendering with different props
   - Click handlers
   - Badge color logic

4. Test shared components:
   - Input/output behavior
   - Rendering

**Verification**: All tests pass with good coverage (>80%)

---

### Step 13: Integration Testing

1. Test complete user flows:
   - Login → Groups list load
   - Empty state → Create group navigation
   - Group card click → Group details navigation
   - Refresh button → Data reload
   - Error → Retry

2. Test responsive behavior on different screen sizes

3. Test keyboard navigation and accessibility

**Verification**: All user flows work end-to-end

---

### Step 14: Performance Optimization

1. Verify OnPush change detection is working
2. Check for unnecessary re-renders
3. Optimize computed signals if needed
4. Add trackBy functions to `@for` loops if needed

**Verification**: View performs smoothly, no performance warnings

---

### Step 15: Accessibility Audit

1. Run Lighthouse accessibility audit
2. Test with screen reader
3. Verify keyboard navigation
4. Check color contrast ratios
5. Add ARIA labels where needed

**Verification**: Accessibility score >90, works with screen readers

---

### Step 16: Polish Language Review

1. Review all UI text in Polish
2. Ensure proper grammar and spelling
3. Verify date/currency formatting for Polish locale
4. Test with Polish users if possible

**Verification**: All text is grammatically correct Polish

---

### Step 17: Final QA and Bug Fixes

1. Test all error scenarios
2. Test edge cases (very long group names, many participants, etc.)
3. Test on different browsers (Chrome, Firefox, Safari, Edge)
4. Fix any discovered bugs

**Verification**: No critical bugs, works across browsers

---

### Step 18: Documentation

1. Add JSDoc comments to all public methods
2. Update component README if needed
3. Document any complex logic
4. Create or update user documentation

**Verification**: Code is well-documented, easy for other developers to understand

---

### Step 19: Code Review

1. Self-review code for consistency
2. Check adherence to coding standards
3. Request peer code review
4. Address review feedback

**Verification**: Code passes review, meets team standards

---

### Step 20: Deployment Preparation

1. Build production bundle: `npm run build`
2. Test production build locally
3. Verify no console errors or warnings
4. Prepare deployment checklist

**Verification**: Production build works correctly

---

## Summary

This implementation plan provides a comprehensive guide for building the Groups List View. The view follows Angular 20 best practices using standalone components, signals for state management, and OnPush change detection. It integrates with the backend API, handles all error scenarios, provides excellent UX with loading states and empty states, and is fully accessible and responsive.

Key implementation priorities:
1. Type safety throughout (TypeScript interfaces)
2. Reactive state management (Angular Signals)
3. Error resilience (comprehensive error handling)
4. Accessibility (keyboard navigation, ARIA labels)
5. Performance (OnPush, computed signals)
6. User experience (loading states, helpful messages)

The component architecture is modular and maintainable, with clear separation between container (smart) and presentational (dumb) components. All shared components are reusable across the application.
