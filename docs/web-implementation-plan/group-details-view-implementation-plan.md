# View Implementation Plan: Group Details

## 1. Overview

The Group Details View serves as the central hub for all group-related activities in the SantaVibe application. It is a complex, conditionally-rendered view that adapts based on two primary factors: the user's role (organizer vs. regular participant) and the draw state (pre-draw vs. post-draw). This creates four distinct view states, each displaying relevant information and actions.

The view provides functionality for managing wishlists, budget suggestions, participant lists, exclusion rules, draw validation, and execution (pre-draw), as well as viewing assignments and generating AI gift suggestions (post-draw).

**Key Characteristics**:
- Single scrollable page with card-based sections
- Highly conditional rendering based on user role and draw state
- Mobile-first responsive design with Tailwind CSS
- Signal-based state management with Angular 20
- Real-time validation feedback for organizer actions
- No optimistic updates - always wait for API confirmation

## 2. View Routing

**Path**: `/groups/:groupId`

**Route Configuration**:
```typescript
{
  path: 'groups/:groupId',
  component: GroupDetailsComponent,
  canActivate: [AuthGuard]
}
```

**Route Guards**:
- `AuthGuard`: Ensures user is authenticated
- Component-level authorization check: Verifies user is a participant (403 handling)

**Route Parameters**:
- `groupId` (UUID): Extracted from route params to fetch group details

## 3. Component Structure

```
GroupDetailsComponent (Smart Container)
├── LoadingSpinnerComponent (initial load)
├── ErrorAlertComponent (error state)
├── Manual Refresh Button
│
├── GroupInfoCardComponent (always visible)
│
├── @if (isPreDraw)
│   ├── ParticipantListComponent
│   ├── WishlistEditorComponent
│   ├── BudgetSuggestionComponent
│   │
│   ├── @if (!isOrganizer)
│   │   └── Status Message: "Waiting for organizer to start the draw"
│   │
│   └── @if (isOrganizer)
│       ├── InvitationLinkCardComponent
│       │   └── CopyToClipboardButtonComponent
│       ├── BudgetManagementComponent
│       ├── ExclusionRulesComponent
│       ├── DrawValidationComponent
│       └── DrawExecutionComponent
│           ├── ConfirmDialogComponent
│           └── LoadingSpinnerComponent
│
└── @if (isPostDraw)
    ├── AssignmentCardComponent
    │   └── Recipient information
    ├── GiftSuggestionsComponent
    │   └── LoadingSpinnerComponent
    └── WishlistEditorComponent (editable with update notification)
```

**Component Hierarchy**:
- **1 Smart Component**: GroupDetailsComponent (orchestrates all logic and API calls)
- **11 Presentational Components**: Display data and emit events
- **4 Shared Components**: Reusable UI elements (spinner, toast, dialog, error alert)

## 4. Component Details

### 4.1 GroupDetailsComponent (Smart Container)

**Description**: Main container component that orchestrates the entire Group Details view. Fetches group data, manages all local state, handles API calls, and conditionally renders child components based on user role and draw state.

**Main Elements**:
- Header with group name and refresh button
- Conditional rendering sections based on `isPreDraw`, `isPostDraw`, `isOrganizer` flags
- Card-based layout with Tailwind CSS
- Loading and error states

**Child Components**:
- All components listed in Component Structure (section 3)

**Handled Events**:
- Component initialization: Fetch group details and related data
- Route param changes: Re-fetch data when groupId changes
- Manual refresh: User clicks refresh button
- Save wishlist: User saves wishlist content
- Save budget suggestion: User saves budget suggestion
- Copy invitation link: User copies link (organizer only)
- Add exclusion rule: User adds rule (organizer only)
- Delete exclusion rule: User deletes rule (organizer only)
- Remove participant: User removes participant (organizer only)
- Execute draw: User executes draw (organizer only)
- Generate gift suggestions: User requests AI suggestions (post-draw)
- Cancel gift suggestions: User cancels ongoing AI request (post-draw)

**Validation Conditions**:
- **Authorization**: User must be participant in group (API returns 403 if not)
- **Wishlist**: Content length <= 1000 characters
- **Budget Suggestion**: Amount > 0, max 2 decimal places
- **Final Budget** (draw execution): Amount > 0
- **Draw Execution**: `drawValidation.isValid` must be true
- **Exclusion Rules**: Validated by ExclusionRulesComponent (see section 4.7)

**Types**:
- `GroupDetailsViewModel` (component state)
- `GroupDetailsDto` (API response)
- `ParticipantViewModel`
- `DrawValidationViewModel`
- `AssignmentViewModel`
- `ExclusionRuleViewModel`
- `GiftSuggestion`

**Props**: None (root component, receives route params)

**Signals** (Component State):
```typescript
// Loading and error states
isLoading = signal<boolean>(true);
error = signal<string | null>(null);
isRefreshing = signal<boolean>(false);

// Group data
groupDetails = signal<GroupDetailsViewModel | null>(null);

// Pre-draw form states
myWishlist = signal<string>('');
myBudgetSuggestion = signal<number | null>(null);
isSavingWishlist = signal<boolean>(false);
isSavingBudget = signal<boolean>(false);

// Organizer states (pre-draw)
invitationLink = signal<string>('');
budgetSuggestions = signal<number[]>([]);
exclusionRules = signal<ExclusionRuleViewModel[]>([]);
finalBudget = signal<number | null>(null);
isExecutingDraw = signal<boolean>(false);

// Post-draw states
recipientWishlist = signal<string | null>(null);
giftSuggestions = signal<GiftSuggestion[]>([]);
isGeneratingSuggestions = signal<boolean>(false);

// Computed signals
isPreDraw = computed(() => !this.groupDetails()?.drawCompleted);
isPostDraw = computed(() => this.groupDetails()?.drawCompleted ?? false);
canExecuteDraw = computed(() =>
  this.groupDetails()?.drawValidation?.isValid ?? false
);
```

---

### 4.2 GroupInfoCardComponent (Presentational)

**Description**: Displays read-only summary information about the group. Shows group name, organizer name, participant count, and dates (created date, draw completion date if applicable).

**Main Elements**:
```html
<div class="card bg-white shadow rounded-lg p-6">
  <h1 class="text-2xl font-bold">{{ groupName }}</h1>
  <div class="mt-4 space-y-2">
    <div class="flex items-center">
      <span class="text-gray-600">Organizer:</span>
      <span class="ml-2 font-medium">{{ organizerName }}</span>
    </div>
    <div class="flex items-center">
      <span class="text-gray-600">Participants:</span>
      <span class="ml-2 font-medium">{{ participantCount }}</span>
    </div>
    <div class="flex items-center">
      <span class="text-gray-600">Created:</span>
      <span class="ml-2">{{ createdAt | date:'medium' }}</span>
    </div>
    @if (drawCompletedAt) {
      <div class="flex items-center">
        <span class="text-gray-600">Draw Completed:</span>
        <span class="ml-2">{{ drawCompletedAt | date:'medium' }}</span>
      </div>
    }
  </div>
</div>
```

**Handled Events**: None (purely presentational)

**Validation Conditions**: None

**Types**:
- Inputs use primitive types from `GroupDetailsViewModel`

**Props** (Input Signals):
```typescript
groupName = input.required<string>();
organizerName = input.required<string>();
participantCount = input.required<number>();
createdAt = input.required<Date>();
drawCompletedAt = input<Date | null>(null);
```

---

### 4.3 ParticipantListComponent (Presentational)

**Description**: Displays a list of all participants in the group, showing first name, last name, join date, and wishlist indicator. For organizers in pre-draw state, shows a "Remove" button for each participant (except the organizer).

**Main Elements**:
```html
<div class="card bg-white shadow rounded-lg p-6">
  <h2 class="text-xl font-semibold mb-4">Participants</h2>
  <div class="space-y-3">
    @for (participant of participants(); track participant.userId) {
      <div class="flex items-center justify-between p-3 border rounded">
        <div class="flex-1">
          <div class="font-medium">
            {{ participant.fullName }}
            @if (participant.isOrganizer) {
              <span class="ml-2 text-sm text-blue-600">(Organizer)</span>
            }
          </div>
          <div class="text-sm text-gray-600">
            Joined: {{ participant.joinedAt | date:'short' }}
          </div>
          <div class="text-sm">
            @if (participant.hasWishlist) {
              <span class="text-green-600">✓ Wishlist added</span>
            } @else {
              <span class="text-gray-400">No wishlist yet</span>
            }
          </div>
        </div>
        @if (canRemove() && participant.canRemove) {
          <button
            (click)="removeParticipant.emit(participant.userId)"
            class="btn btn-sm btn-danger">
            Remove
          </button>
        }
      </div>
    }
  </div>
</div>
```

**Handled Events**:
- `removeParticipant` (output): Emits userId when remove button clicked

**Validation Conditions**:
- Cannot remove organizer: `participant.canRemove` is false for organizer
- Remove button only visible when `canRemove` input is true (pre-draw + isOrganizer)

**Types**:
- `ParticipantViewModel[]`

**Props**:
```typescript
participants = input.required<ParticipantViewModel[]>();
canRemove = input<boolean>(false);
removeParticipant = output<string>(); // userId
```

---

### 4.4 WishlistEditorComponent (Presentational)

**Description**: Provides a textarea for users to create and edit their wishlist. Validates character count (max 1000), displays save button with loading state, and shows helper text. Used in both pre-draw and post-draw states (with different helper text in post-draw).

**Main Elements**:
```html
<div class="card bg-white shadow rounded-lg p-6">
  <h2 class="text-xl font-semibold mb-4">My Wishlist</h2>
  <textarea
    [(ngModel)]="wishlistContent"
    (ngModelChange)="onContentChange()"
    [disabled]="!isEditable() || isSaving()"
    class="w-full p-3 border rounded-lg"
    rows="6"
    maxlength="1000"
    placeholder="Add items to your wishlist...">
  </textarea>

  <div class="mt-2 flex items-center justify-between">
    <div class="text-sm text-gray-600">
      {{ characterCount() }} / 1000 characters
    </div>
    @if (validationError()) {
      <div class="text-sm text-red-600">{{ validationError() }}</div>
    }
  </div>

  @if (helperText()) {
    <div class="mt-2 text-sm text-gray-500">
      {{ helperText() }}
    </div>
  }

  <button
    (click)="save.emit(wishlistContent)"
    [disabled]="!isValid() || !isDirty() || isSaving()"
    class="mt-4 btn btn-primary">
    @if (isSaving()) {
      <LoadingSpinnerComponent size="small" />
    } @else {
      Save Wishlist
    }
  </button>
</div>
```

**Handled Events**:
- `save` (output): Emits wishlist content when save button clicked
- `ngModelChange` (internal): Tracks content changes for validation

**Validation Conditions**:
- **Character limit**: `content.length <= 1000`
- **Validation error**: Display if limit exceeded
- **Disable save**: If validation fails or content unchanged or already saving

**Types**:
- `string` for content

**Props**:
```typescript
initialContent = input<string>('');
isEditable = input<boolean>(true);
isSaving = input<boolean>(false);
helperText = input<string | null>(null);
save = output<string>(); // wishlist content

// Internal state
wishlistContent = signal<string>('');
characterCount = computed(() => this.wishlistContent().length);
isValid = computed(() => this.characterCount() <= 1000);
isDirty = computed(() => this.wishlistContent() !== this.initialContent());
validationError = computed(() =>
  this.isValid() ? null : 'Wishlist cannot exceed 1000 characters'
);
```

---

### 4.5 BudgetSuggestionComponent (Presentational)

**Description**: Allows users to input their budget suggestion (optional). Validates that the value is a positive number with max 2 decimal places. Displays save button with loading state.

**Main Elements**:
```html
<div class="card bg-white shadow rounded-lg p-6">
  <h2 class="text-xl font-semibold mb-4">My Budget Suggestion</h2>
  <p class="text-sm text-gray-600 mb-4">
    Suggest a budget amount (optional). The organizer will see anonymous suggestions.
  </p>

  <div class="flex items-center space-x-2">
    <input
      type="number"
      [(ngModel)]="budgetAmount"
      [disabled]="!isEditable() || isSaving()"
      class="w-32 p-2 border rounded"
      placeholder="0.00"
      min="0"
      step="0.01">
    <span class="text-gray-700">PLN</span>
  </div>

  @if (validationError()) {
    <div class="mt-2 text-sm text-red-600">{{ validationError() }}</div>
  }

  <button
    (click)="save.emit(budgetAmount())"
    [disabled]="!isValid() || !isDirty() || isSaving()"
    class="mt-4 btn btn-primary">
    @if (isSaving()) {
      <LoadingSpinnerComponent size="small" />
    } @else {
      Save Budget Suggestion
    }
  </button>
</div>
```

**Handled Events**:
- `save` (output): Emits budget amount when save button clicked

**Validation Conditions**:
- **Positive number**: `amount > 0`
- **Max 2 decimals**: Validated using regex or rounding check
- **Disable save**: If validation fails or content unchanged or already saving

**Types**:
- `number | null` for budget amount

**Props**:
```typescript
initialAmount = input<number | null>(null);
isEditable = input<boolean>(true);
isSaving = input<boolean>(false);
save = output<number | null>(); // budget amount

// Internal state
budgetAmount = signal<number | null>(null);
isValid = computed(() => {
  const amount = this.budgetAmount();
  if (amount === null || amount === undefined) return true; // optional
  return amount > 0 && this.hasMaxTwoDecimals(amount);
});
isDirty = computed(() => this.budgetAmount() !== this.initialAmount());
validationError = computed(() => {
  if (this.isValid()) return null;
  const amount = this.budgetAmount();
  if (amount !== null && amount <= 0) return 'Budget must be positive';
  return 'Budget can have maximum 2 decimal places';
});

// Helper method
private hasMaxTwoDecimals(value: number): boolean {
  return /^\d+(\.\d{1,2})?$/.test(value.toString());
}
```

---

### 4.6 InvitationLinkCardComponent (Presentational, Organizer Only, Pre-Draw)

**Description**: Displays the invitation link prominently with a copy-to-clipboard button. Provides visual feedback when link is copied.

**Main Elements**:
```html
<div class="card bg-blue-50 border-blue-200 shadow rounded-lg p-6">
  <h2 class="text-xl font-semibold mb-2 text-blue-900">Invite Participants</h2>
  <p class="text-sm text-blue-700 mb-4">
    Share this link to invite people to your Secret Santa group
  </p>

  <div class="flex items-center space-x-2">
    <input
      type="text"
      [value]="invitationLink()"
      readonly
      class="flex-1 p-2 border border-blue-300 rounded bg-white">

    <CopyToClipboardButtonComponent
      [textToCopy]="invitationLink()"
      (copied)="onCopied()" />
  </div>

  @if (showCopiedFeedback()) {
    <div class="mt-2 text-sm text-green-600">
      ✓ Link copied to clipboard!
    </div>
  }
</div>
```

**Handled Events**:
- `copied` (from child): Shows feedback when link is copied

**Validation Conditions**: None

**Types**:
- `string` for invitation link

**Props**:
```typescript
invitationLink = input.required<string>();

// Internal state
showCopiedFeedback = signal<boolean>(false);

onCopied(): void {
  this.showCopiedFeedback.set(true);
  setTimeout(() => this.showCopiedFeedback.set(false), 3000);
}
```

---

### 4.7 ExclusionRulesComponent (Presentational, Organizer Only, Pre-Draw)

**Description**: Manages exclusion rules with a dropdown form to add rules and a list of current rules with delete buttons. Provides real-time validation feedback to prevent invalid rules (same user, duplicate/bidirectional rules).

**Main Elements**:
```html
<div class="card bg-white shadow rounded-lg p-6">
  <h2 class="text-xl font-semibold mb-4">Exclusion Rules</h2>
  <p class="text-sm text-gray-600 mb-4">
    Prevent specific participants from being paired together
  </p>

  <!-- Add Rule Form -->
  <div class="space-y-3">
    <div class="flex space-x-2">
      <select
        [(ngModel)]="selectedUser1"
        class="flex-1 p-2 border rounded">
        <option value="">Select first participant</option>
        @for (participant of participants(); track participant.userId) {
          <option [value]="participant.userId">
            {{ participant.fullName }}
          </option>
        }
      </select>

      <select
        [(ngModel)]="selectedUser2"
        class="flex-1 p-2 border rounded">
        <option value="">Select second participant</option>
        @for (participant of participants(); track participant.userId) {
          <option [value]="participant.userId">
            {{ participant.fullName }}
          </option>
        }
      </select>

      <button
        (click)="onAddRule()"
        [disabled]="!canAddRule()"
        class="btn btn-primary">
        Add Rule
      </button>
    </div>

    @if (validationError()) {
      <div class="text-sm text-red-600">{{ validationError() }}</div>
    }
  </div>

  <!-- Current Rules List -->
  <div class="mt-6">
    <h3 class="text-lg font-medium mb-3">
      Current Rules ({{ currentRules().length }})
    </h3>
    @if (currentRules().length === 0) {
      <p class="text-sm text-gray-500">No exclusion rules yet</p>
    } @else {
      <div class="space-y-2">
        @for (rule of currentRules(); track rule.ruleId) {
          <div class="flex items-center justify-between p-3 border rounded">
            <div>
              <span class="font-medium">{{ rule.user1Name }}</span>
              <span class="mx-2 text-gray-400">↔</span>
              <span class="font-medium">{{ rule.user2Name }}</span>
            </div>
            <button
              (click)="deleteRule.emit(rule.ruleId)"
              class="btn btn-sm btn-danger">
              Delete
            </button>
          </div>
        }
      </div>
    }
  </div>
</div>
```

**Handled Events**:
- `addRule` (output): Emits { user1Id, user2Id } when valid rule is added
- `deleteRule` (output): Emits ruleId when delete button clicked

**Validation Conditions**:
1. **Same user check**: `selectedUser1 !== selectedUser2`
2. **Duplicate check**: Rule doesn't already exist in `currentRules`
3. **Bidirectional check**: Reverse rule (user2, user1) doesn't exist
4. **Real-time validation**: Display error message immediately when invalid

**Types**:
- `ParticipantViewModel[]` for participants
- `ExclusionRuleViewModel[]` for current rules

**Props**:
```typescript
participants = input.required<ParticipantViewModel[]>();
currentRules = input.required<ExclusionRuleViewModel[]>();
addRule = output<{ user1Id: string; user2Id: string }>();
deleteRule = output<string>(); // ruleId

// Internal state
selectedUser1 = signal<string>('');
selectedUser2 = signal<string>('');

canAddRule = computed(() => {
  const user1 = this.selectedUser1();
  const user2 = this.selectedUser2();

  if (!user1 || !user2) return false;
  if (user1 === user2) return false;

  const rules = this.currentRules();
  const exists = rules.some(r =>
    (r.user1Id === user1 && r.user2Id === user2) ||
    (r.user1Id === user2 && r.user2Id === user1)
  );

  return !exists;
});

validationError = computed(() => {
  const user1 = this.selectedUser1();
  const user2 = this.selectedUser2();

  if (!user1 || !user2) return null;

  if (user1 === user2) {
    return 'Cannot exclude the same participant';
  }

  const rules = this.currentRules();
  const exists = rules.some(r =>
    (r.user1Id === user1 && r.user2Id === user2) ||
    (r.user1Id === user2 && r.user2Id === user1)
  );

  if (exists) {
    return 'This exclusion rule already exists';
  }

  return null;
});

onAddRule(): void {
  if (this.canAddRule()) {
    this.addRule.emit({
      user1Id: this.selectedUser1(),
      user2Id: this.selectedUser2()
    });
    // Reset form
    this.selectedUser1.set('');
    this.selectedUser2.set('');
  }
}
```

---

### 4.8 BudgetManagementComponent (Presentational, Organizer Only, Pre-Draw)

**Description**: Displays anonymous budget suggestions from participants in sorted order, shows count of suggestions, and provides an input field for the final budget that will be used during draw execution.

**Main Elements**:
```html
<div class="card bg-white shadow rounded-lg p-6">
  <h2 class="text-xl font-semibold mb-4">Budget Management</h2>

  <!-- Suggestion Summary -->
  <div class="mb-4">
    <h3 class="text-md font-medium mb-2">Participant Suggestions</h3>
    <p class="text-sm text-gray-600">
      {{ suggestionCount() }} of {{ participantCount() }} participants provided suggestions
    </p>
  </div>

  <!-- Anonymous Suggestions List -->
  @if (budgetSuggestions().length > 0) {
    <div class="mb-6">
      <h4 class="text-sm font-medium text-gray-700 mb-2">
        Suggested amounts (sorted):
      </h4>
      <div class="flex flex-wrap gap-2">
        @for (suggestion of budgetSuggestions(); track $index) {
          <div class="px-3 py-1 bg-gray-100 rounded">
            {{ suggestion | number:'1.2-2' }} PLN
          </div>
        }
      </div>
    </div>
  } @else {
    <p class="text-sm text-gray-500 mb-6">
      No budget suggestions yet
    </p>
  }

  <!-- Final Budget Input -->
  <div class="border-t pt-4">
    <h3 class="text-md font-medium mb-2">Final Budget</h3>
    <p class="text-sm text-gray-600 mb-3">
      This budget will be used for the Secret Santa draw
    </p>
    <div class="flex items-center space-x-2">
      <input
        type="number"
        [value]="currentBudget()"
        readonly
        class="w-32 p-2 border rounded bg-gray-50"
        placeholder="Not set">
      <span class="text-gray-700">PLN</span>
    </div>
    <p class="text-xs text-gray-500 mt-2">
      Set the final budget when executing the draw
    </p>
  </div>
</div>
```

**Handled Events**: None (budget is set during draw execution)

**Validation Conditions**: None (validation happens in DrawExecutionComponent)

**Types**:
- `number[]` for budget suggestions
- `number | null` for current budget

**Props**:
```typescript
budgetSuggestions = input.required<number[]>();
participantCount = input.required<number>();
currentBudget = input<number | null>(null);

// Computed
suggestionCount = computed(() => this.budgetSuggestions().length);
```

---

### 4.9 DrawValidationComponent (Presentational, Organizer Only, Pre-Draw)

**Description**: Displays the current validation status for draw execution. Shows a checkmark if valid, or error icon with list of validation errors if invalid.

**Main Elements**:
```html
<div class="card shadow rounded-lg p-6"
     [class.bg-green-50]="validationResult().isValid"
     [class.border-green-200]="validationResult().isValid"
     [class.bg-red-50]="!validationResult().isValid"
     [class.border-red-200]="!validationResult().isValid">

  <div class="flex items-start space-x-3">
    @if (validationResult().isValid) {
      <div class="text-green-600 text-2xl">✓</div>
      <div>
        <h2 class="text-xl font-semibold text-green-900">Ready to Draw</h2>
        <p class="text-sm text-green-700 mt-1">
          All validation checks passed. You can execute the draw.
        </p>
      </div>
    } @else {
      <div class="text-red-600 text-2xl">✕</div>
      <div class="flex-1">
        <h2 class="text-xl font-semibold text-red-900">Validation Errors</h2>
        <p class="text-sm text-red-700 mt-1">
          Fix the following issues before executing the draw:
        </p>
        <ul class="mt-3 space-y-1 list-disc list-inside">
          @for (error of validationResult().errors; track $index) {
            <li class="text-sm text-red-800">{{ error }}</li>
          }
        </ul>
      </div>
    }
  </div>

  <!-- Summary Stats -->
  <div class="mt-4 pt-4 border-t grid grid-cols-2 gap-4 text-sm">
    <div>
      <span class="text-gray-600">Participants:</span>
      <span class="ml-2 font-medium">{{ participantCount() }}</span>
    </div>
    <div>
      <span class="text-gray-600">Exclusion Rules:</span>
      <span class="ml-2 font-medium">{{ exclusionRuleCount() }}</span>
    </div>
  </div>
</div>
```

**Handled Events**: None

**Validation Conditions**: None (displays validation from API)

**Types**:
- `DrawValidationViewModel`

**Props**:
```typescript
validationResult = input.required<DrawValidationViewModel>();
participantCount = input.required<number>();
exclusionRuleCount = input.required<number>();
```

---

### 4.10 DrawExecutionComponent (Presentational, Organizer Only, Pre-Draw)

**Description**: Provides final budget input and "Execute Draw" button. Button is disabled if validation fails. Shows confirmation dialog when clicked, then loading state during execution.

**Main Elements**:
```html
<div class="card bg-white shadow rounded-lg p-6">
  <h2 class="text-xl font-semibold mb-4">Execute Draw</h2>

  <!-- Final Budget Input -->
  <div class="mb-4">
    <label class="block text-sm font-medium text-gray-700 mb-2">
      Final Budget (PLN)
    </label>
    <input
      type="number"
      [(ngModel)]="budgetAmount"
      [disabled]="isExecuting()"
      class="w-full p-2 border rounded"
      placeholder="Enter final budget"
      min="0"
      step="0.01">
    @if (budgetError()) {
      <div class="mt-1 text-sm text-red-600">{{ budgetError() }}</div>
    }
  </div>

  <!-- Execute Button -->
  <button
    (click)="onExecuteClick()"
    [disabled]="!canExecute() || isExecuting()"
    class="w-full btn btn-primary btn-lg"
    [attr.title]="buttonTooltip()">
    @if (isExecuting()) {
      <LoadingSpinnerComponent size="small" />
      <span class="ml-2">Executing draw...</span>
    } @else {
      Execute Draw
    }
  </button>

  @if (!isValidationPassed()) {
    <div class="mt-3 text-sm text-red-600">
      Cannot execute draw: validation errors present
    </div>
  }

  <!-- Warning Message -->
  <div class="mt-4 p-3 bg-yellow-50 border border-yellow-200 rounded">
    <p class="text-sm text-yellow-800">
      ⚠️ This action is permanent and cannot be undone. Make sure all participants have joined and added their wishlists.
    </p>
  </div>
</div>

<!-- Confirmation Dialog -->
@if (showConfirmDialog()) {
  <ConfirmDialogComponent
    title="Execute Secret Santa Draw?"
    message="This action is permanent and cannot be undone. All participants will be assigned their recipients and notified by email. Are you sure you want to continue?"
    confirmText="Yes, Execute Draw"
    cancelText="Cancel"
    (confirmed)="onConfirmed()"
    (cancelled)="onCancelled()" />
}
```

**Handled Events**:
- `executeDraw` (output): Emits budget amount when confirmed
- Button click: Shows confirmation dialog
- Confirmation dialog events: Confirmed or cancelled

**Validation Conditions**:
1. **Validation passed**: `isValidationPassed` must be true
2. **Budget valid**: `budgetAmount > 0` and has max 2 decimals
3. **Can execute**: Both conditions above met and not currently executing

**Types**:
- `number` for budget
- `boolean` for validation status

**Props**:
```typescript
isValidationPassed = input.required<boolean>();
isExecuting = input<boolean>(false);
executeDraw = output<number>(); // budget amount

// Internal state
budgetAmount = signal<number | null>(null);
showConfirmDialog = signal<boolean>(false);

isBudgetValid = computed(() => {
  const amount = this.budgetAmount();
  if (!amount) return false;
  return amount > 0 && this.hasMaxTwoDecimals(amount);
});

canExecute = computed(() =>
  this.isValidationPassed() &&
  this.isBudgetValid() &&
  !this.isExecuting()
);

budgetError = computed(() => {
  const amount = this.budgetAmount();
  if (!amount) return 'Budget is required';
  if (amount <= 0) return 'Budget must be positive';
  if (!this.hasMaxTwoDecimals(amount)) return 'Max 2 decimal places';
  return null;
});

buttonTooltip = computed(() => {
  if (!this.isValidationPassed()) {
    return 'Cannot execute draw: validation errors present';
  }
  if (!this.isBudgetValid()) {
    return 'Please enter a valid budget';
  }
  return 'Execute the Secret Santa draw';
});

onExecuteClick(): void {
  if (this.canExecute()) {
    this.showConfirmDialog.set(true);
  }
}

onConfirmed(): void {
  this.showConfirmDialog.set(false);
  this.executeDraw.emit(this.budgetAmount()!);
}

onCancelled(): void {
  this.showConfirmDialog.set(false);
}

private hasMaxTwoDecimals(value: number): boolean {
  return /^\d+(\.\d{1,2})?$/.test(value.toString());
}
```

---

### 4.11 AssignmentCardComponent (Presentational, Post-Draw)

**Description**: Displays the user's Secret Santa assignment, including recipient name, final budget, and recipient's wishlist (if available).

**Main Elements**:
```html
<div class="card bg-gradient-to-r from-green-50 to-blue-50 border-2 border-green-200 shadow-lg rounded-lg p-6">
  <h2 class="text-2xl font-bold text-green-900 mb-4">
    🎁 Your Secret Santa Assignment
  </h2>

  <div class="bg-white rounded-lg p-6 shadow">
    <p class="text-lg text-gray-700 mb-3">You're buying a gift for:</p>

    <div class="text-3xl font-bold text-green-700 mb-4">
      {{ recipientFullName() }}
    </div>

    <div class="flex items-center space-x-2 mb-6">
      <span class="text-xl text-gray-700">Budget:</span>
      <span class="text-2xl font-bold text-green-600">
        {{ budget() | number:'1.2-2' }} PLN
      </span>
    </div>

    <div class="border-t pt-4">
      <h3 class="text-lg font-semibold text-gray-800 mb-2">
        {{ recipientFirstName() }}'s Wishlist
      </h3>

      @if (hasWishlist()) {
        <div class="bg-gray-50 rounded p-4 whitespace-pre-wrap">
          {{ wishlistContent() }}
        </div>
      } @else {
        <div class="bg-yellow-50 border border-yellow-200 rounded p-4">
          <p class="text-sm text-yellow-800">
            Your giftee hasn't added their wishlist yet - you'll be notified by email when they do.
          </p>
        </div>
      }
    </div>
  </div>
</div>
```

**Handled Events**: None

**Validation Conditions**: None

**Types**:
- `AssignmentViewModel`
- `string` for wishlist content

**Props**:
```typescript
recipientFirstName = input.required<string>();
recipientLastName = input.required<string>();
budget = input.required<number>();
hasWishlist = input.required<boolean>();
wishlistContent = input<string | null>(null);

// Computed
recipientFullName = computed(() =>
  `${this.recipientFirstName()} ${this.recipientLastName()}`
);
```

---

### 4.12 GiftSuggestionsComponent (Presentational, Post-Draw)

**Description**: Provides "Generate Gift Ideas" button to request AI-generated gift suggestions. Shows loading state with cancellation option, then displays 3-5 suggestion cards with category, item name, description, and approximate price.

**Main Elements**:
```html
<div class="card bg-white shadow rounded-lg p-6">
  <h2 class="text-xl font-semibold mb-4">AI Gift Suggestions</h2>

  @if (suggestions().length === 0 && !isGenerating()) {
    <p class="text-sm text-gray-600 mb-4">
      Get personalized gift ideas based on your recipient's wishlist
    </p>
    <button
      (click)="generate.emit()"
      class="btn btn-primary">
      Generate Gift Ideas
    </button>
  }

  @if (isGenerating()) {
    <div class="flex items-center justify-between p-4 bg-blue-50 rounded">
      <div class="flex items-center space-x-3">
        <LoadingSpinnerComponent size="medium" />
        <span class="text-blue-800">Generating gift suggestions...</span>
      </div>
      <button
        (click)="cancel.emit()"
        class="btn btn-sm btn-secondary">
        Cancel
      </button>
    </div>
  }

  @if (suggestions().length > 0) {
    <div class="mb-4">
      <button
        (click)="generate.emit()"
        [disabled]="isGenerating()"
        class="btn btn-sm btn-secondary">
        Regenerate Suggestions
      </button>
    </div>

    <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
      @for (suggestion of suggestions(); track $index) {
        <div class="border rounded-lg p-4 hover:shadow-md transition">
          <div class="text-xs font-semibold text-blue-600 uppercase mb-1">
            {{ suggestion.category }}
          </div>
          <h3 class="text-lg font-bold text-gray-800 mb-2">
            {{ suggestion.itemName }}
          </h3>
          <p class="text-sm text-gray-600 mb-3">
            {{ suggestion.description }}
          </p>
          <div class="text-lg font-semibold text-green-600">
            ~{{ suggestion.approximatePrice | number:'1.2-2' }} PLN
          </div>
        </div>
      }
    </div>
  }

  @if (error()) {
    <div class="p-4 bg-red-50 border border-red-200 rounded">
      <p class="text-sm text-red-800">
        {{ error() }}
      </p>
      <button
        (click)="generate.emit()"
        class="mt-2 btn btn-sm btn-primary">
        Try Again
      </button>
    </div>
  }
</div>
```

**Handled Events**:
- `generate` (output): Emitted when user requests gift suggestions
- `cancel` (output): Emitted when user cancels ongoing generation

**Validation Conditions**: None

**Types**:
- `GiftSuggestion[]`

**Props**:
```typescript
suggestions = input<GiftSuggestion[]>([]);
isGenerating = input<boolean>(false);
error = input<string | null>(null);
generate = output<void>();
cancel = output<void>();
```

---

### 4.13 Shared Components

**LoadingSpinnerComponent**:
```typescript
size = input<'small' | 'medium' | 'large'>('medium');
message = input<string | null>(null);
```

**ErrorAlertComponent**:
```typescript
message = input.required<string>();
type = input<'inline' | 'banner'>('banner');
dismissible = input<boolean>(false);
dismissed = output<void>();
```

**ToastComponent**:
```typescript
message = input.required<string>();
type = input<'success' | 'error' | 'info'>('info');
duration = input<number>(5000);
dismissed = output<void>();
```

**ConfirmDialogComponent**:
```typescript
title = input.required<string>();
message = input.required<string>();
confirmText = input<string>('Confirm');
cancelText = input<string>('Cancel');
confirmed = output<void>();
cancelled = output<void>();
```

**CopyToClipboardButtonComponent**:
```typescript
textToCopy = input.required<string>();
copied = output<void>();
```

## 5. Types

### 5.1 DTOs (from API)

#### GroupDetailsDto (Pre-Draw)
```typescript
interface GroupDetailsDto {
  groupId: string; // UUID
  name: string;
  organizerId: string; // UUID
  organizerName: string;
  isOrganizer: boolean;
  budget: number | null;
  drawCompleted: boolean;
  drawCompletedAt: string | null; // ISO 8601 date string
  createdAt: string; // ISO 8601 date string
  participants: ParticipantDto[];
  participantCount: number;
  exclusionRuleCount: number;
  canDraw: boolean;
  drawValidation: DrawValidationDto;
}
```

#### GroupDetailsDto (Post-Draw)
```typescript
interface GroupDetailsDto {
  groupId: string;
  name: string;
  organizerId: string;
  organizerName: string;
  isOrganizer: boolean;
  budget: number; // Always present post-draw
  drawCompleted: boolean;
  drawCompletedAt: string; // ISO 8601 date string, always present post-draw
  createdAt: string;
  participantCount: number;
  myAssignment: AssignmentDto;
}
```

#### ParticipantDto
```typescript
interface ParticipantDto {
  userId: string; // UUID
  firstName: string;
  lastName: string;
  joinedAt: string; // ISO 8601 date string
  hasWishlist: boolean;
  isOrganizer: boolean;
}
```

#### DrawValidationDto
```typescript
interface DrawValidationDto {
  isValid: boolean;
  errors: string[]; // Array of validation error messages
}
```

#### AssignmentDto
```typescript
interface AssignmentDto {
  recipientId: string; // UUID
  recipientFirstName: string;
  recipientLastName: string;
  hasWishlist: boolean;
}
```

### 5.2 ViewModels (for Component State)

#### GroupDetailsViewModel
```typescript
interface GroupDetailsViewModel {
  // Base information (always present)
  groupId: string;
  name: string;
  organizerName: string;
  isOrganizer: boolean;
  participantCount: number;
  createdAt: Date; // Converted from ISO string

  // State flags
  drawCompleted: boolean;

  // Conditional fields based on draw state
  budget: number | null; // null before draw, number after
  drawCompletedAt: Date | null; // null before draw, Date after

  // Pre-draw specific fields (only when drawCompleted = false)
  participants?: ParticipantViewModel[];
  exclusionRuleCount?: number;
  canDraw?: boolean;
  drawValidation?: DrawValidationViewModel;

  // Post-draw specific fields (only when drawCompleted = true)
  myAssignment?: AssignmentViewModel;
}
```

**Field Descriptions**:
- `groupId`: Unique identifier for the group
- `name`: Group name
- `organizerName`: Full name of the group organizer
- `isOrganizer`: Boolean flag indicating if current user is the organizer
- `participantCount`: Total number of participants in the group
- `createdAt`: Date when group was created (converted from ISO string to Date object)
- `drawCompleted`: Boolean flag indicating if draw has been executed
- `budget`: Final budget amount (null before draw, set during draw execution)
- `drawCompletedAt`: Date when draw was executed (null before draw)
- `participants`: Array of participant view models (only present pre-draw)
- `exclusionRuleCount`: Number of exclusion rules (only present pre-draw)
- `canDraw`: Boolean indicating if draw can be executed (only present pre-draw)
- `drawValidation`: Validation result object (only present pre-draw)
- `myAssignment`: User's Secret Santa assignment (only present post-draw)

#### ParticipantViewModel
```typescript
interface ParticipantViewModel {
  userId: string;
  firstName: string;
  lastName: string;
  fullName: string; // Computed: `${firstName} ${lastName}`
  joinedAt: Date; // Converted from ISO string
  hasWishlist: boolean;
  isOrganizer: boolean;
  canRemove: boolean; // Computed: !isOrganizer (cannot remove organizer)
}
```

**Field Descriptions**:
- `userId`: Unique identifier for the participant
- `firstName`: Participant's first name
- `lastName`: Participant's last name
- `fullName`: Computed full name for display purposes
- `joinedAt`: Date when participant joined the group
- `hasWishlist`: Boolean indicating if participant has added a wishlist
- `isOrganizer`: Boolean indicating if participant is the group organizer
- `canRemove`: Computed boolean - organizer cannot be removed

#### DrawValidationViewModel
```typescript
interface DrawValidationViewModel {
  isValid: boolean;
  errors: string[];
  hasErrors: boolean; // Computed: errors.length > 0
}
```

**Field Descriptions**:
- `isValid`: Boolean indicating if draw can be executed
- `errors`: Array of validation error messages (e.g., "Minimum 3 participants required")
- `hasErrors`: Computed boolean for quick error checking

#### AssignmentViewModel
```typescript
interface AssignmentViewModel {
  recipientId: string;
  recipientFirstName: string;
  recipientLastName: string;
  recipientFullName: string; // Computed: `${recipientFirstName} ${recipientLastName}`
  hasWishlist: boolean;
  budget: number; // From parent group data
}
```

**Field Descriptions**:
- `recipientId`: Unique identifier for the gift recipient
- `recipientFirstName`: Recipient's first name
- `recipientLastName`: Recipient's last name
- `recipientFullName`: Computed full name for display
- `hasWishlist`: Boolean indicating if recipient has a wishlist
- `budget`: Final budget amount (inherited from group)

#### ExclusionRuleViewModel
```typescript
interface ExclusionRuleViewModel {
  ruleId: string; // UUID
  user1Id: string;
  user1Name: string; // Full name
  user2Id: string;
  user2Name: string; // Full name
}
```

**Field Descriptions**:
- `ruleId`: Unique identifier for the exclusion rule
- `user1Id`: First participant's user ID
- `user1Name`: First participant's full name
- `user2Id`: Second participant's user ID
- `user2Name`: Second participant's full name

#### GiftSuggestion
```typescript
interface GiftSuggestion {
  category: string; // e.g., "Books", "Electronics", "Home Decor"
  itemName: string; // e.g., "Wireless Headphones"
  description: string; // Brief description of the gift
  approximatePrice: number; // Price in PLN
}
```

**Field Descriptions**:
- `category`: Gift category for grouping/filtering
- `itemName`: Name of the suggested gift item
- `description`: Brief explanation of why this gift is suitable
- `approximatePrice`: Estimated price in PLN

### 5.3 Request/Response Types

#### SaveWishlistRequest
```typescript
interface SaveWishlistRequest {
  content: string; // Max 1000 characters
}
```

#### SaveBudgetSuggestionRequest
```typescript
interface SaveBudgetSuggestionRequest {
  amount: number; // Positive number with max 2 decimals
}
```

#### CreateExclusionRuleRequest
```typescript
interface CreateExclusionRuleRequest {
  user1Id: string; // UUID
  user2Id: string; // UUID
}
```

#### ExecuteDrawRequest
```typescript
interface ExecuteDrawRequest {
  budget: number; // Positive number with max 2 decimals
}
```

#### BudgetSuggestionsResponse
```typescript
interface BudgetSuggestionsResponse {
  suggestions: number[]; // Anonymous sorted array
}
```

#### ExclusionRulesResponse
```typescript
interface ExclusionRulesResponse {
  rules: ExclusionRuleDto[];
}

interface ExclusionRuleDto {
  ruleId: string;
  user1Id: string;
  user1FirstName: string;
  user1LastName: string;
  user2Id: string;
  user2FirstName: string;
  user2LastName: string;
}
```

#### RecipientWishlistResponse
```typescript
interface RecipientWishlistResponse {
  content: string | null;
}
```

#### GiftSuggestionsResponse
```typescript
interface GiftSuggestionsResponse {
  suggestions: GiftSuggestion[];
}
```

## 6. State Management

### 6.1 Service-Level State (GroupService)

The `GroupService` manages shared group state using Angular signals:

```typescript
@Injectable({ providedIn: 'root' })
export class GroupService {
  private http = inject(HttpClient);

  // Shared state
  activeGroup = signal<GroupDetailsViewModel | null>(null);

  // API methods
  fetchGroupDetails(groupId: string): Observable<GroupDetailsDto> {
    return this.http.get<GroupDetailsDto>(`/api/groups/${groupId}`);
  }

  updateWishlist(groupId: string, content: string): Observable<void> {
    return this.http.put<void>(`/api/groups/${groupId}/wishlist`, { content });
  }

  updateBudgetSuggestion(groupId: string, amount: number | null): Observable<void> {
    return this.http.put<void>(`/api/groups/${groupId}/budget-suggestion`, { amount });
  }

  fetchBudgetSuggestions(groupId: string): Observable<number[]> {
    return this.http.get<BudgetSuggestionsResponse>(`/api/groups/${groupId}/budget-suggestions`)
      .pipe(map(response => response.suggestions));
  }

  fetchExclusionRules(groupId: string): Observable<ExclusionRuleViewModel[]> {
    return this.http.get<ExclusionRulesResponse>(`/api/groups/${groupId}/exclusion-rules`)
      .pipe(map(response => this.mapExclusionRules(response.rules)));
  }

  createExclusionRule(groupId: string, user1Id: string, user2Id: string): Observable<void> {
    return this.http.post<void>(`/api/groups/${groupId}/exclusion-rules`, { user1Id, user2Id });
  }

  deleteExclusionRule(groupId: string, ruleId: string): Observable<void> {
    return this.http.delete<void>(`/api/groups/${groupId}/exclusion-rules/${ruleId}`);
  }

  removeParticipant(groupId: string, userId: string): Observable<void> {
    return this.http.delete<void>(`/api/groups/${groupId}/participants/${userId}`);
  }

  executeDraw(groupId: string, budget: number): Observable<void> {
    return this.http.post<void>(`/api/groups/${groupId}/draw`, { budget });
  }

  fetchRecipientWishlist(groupId: string): Observable<string | null> {
    return this.http.get<RecipientWishlistResponse>(`/api/groups/${groupId}/assignment/wishlist`)
      .pipe(map(response => response.content));
  }

  generateGiftSuggestions(groupId: string): Observable<GiftSuggestion[]> {
    return this.http.post<GiftSuggestionsResponse>(`/api/groups/${groupId}/gift-suggestions`, {})
      .pipe(map(response => response.suggestions));
  }

  // Helper methods
  private mapToViewModel(dto: GroupDetailsDto): GroupDetailsViewModel {
    const base = {
      groupId: dto.groupId,
      name: dto.name,
      organizerName: dto.organizerName,
      isOrganizer: dto.isOrganizer,
      participantCount: dto.participantCount,
      createdAt: new Date(dto.createdAt),
      drawCompleted: dto.drawCompleted,
      budget: dto.budget,
      drawCompletedAt: dto.drawCompletedAt ? new Date(dto.drawCompletedAt) : null
    };

    if (!dto.drawCompleted) {
      // Pre-draw
      return {
        ...base,
        participants: dto.participants?.map(this.mapParticipant),
        exclusionRuleCount: dto.exclusionRuleCount,
        canDraw: dto.canDraw,
        drawValidation: {
          isValid: dto.drawValidation.isValid,
          errors: dto.drawValidation.errors,
          hasErrors: dto.drawValidation.errors.length > 0
        }
      };
    } else {
      // Post-draw
      return {
        ...base,
        myAssignment: {
          recipientId: dto.myAssignment.recipientId,
          recipientFirstName: dto.myAssignment.recipientFirstName,
          recipientLastName: dto.myAssignment.recipientLastName,
          recipientFullName: `${dto.myAssignment.recipientFirstName} ${dto.myAssignment.recipientLastName}`,
          hasWishlist: dto.myAssignment.hasWishlist,
          budget: dto.budget!
        }
      };
    }
  }

  private mapParticipant(dto: ParticipantDto): ParticipantViewModel {
    return {
      userId: dto.userId,
      firstName: dto.firstName,
      lastName: dto.lastName,
      fullName: `${dto.firstName} ${dto.lastName}`,
      joinedAt: new Date(dto.joinedAt),
      hasWishlist: dto.hasWishlist,
      isOrganizer: dto.isOrganizer,
      canRemove: !dto.isOrganizer
    };
  }

  private mapExclusionRules(dtos: ExclusionRuleDto[]): ExclusionRuleViewModel[] {
    return dtos.map(dto => ({
      ruleId: dto.ruleId,
      user1Id: dto.user1Id,
      user1Name: `${dto.user1FirstName} ${dto.user1LastName}`,
      user2Id: dto.user2Id,
      user2Name: `${dto.user2FirstName} ${dto.user2LastName}`
    }));
  }
}
```

### 6.2 Component-Level State (GroupDetailsComponent)

All component state is managed using Angular signals within `GroupDetailsComponent`:

```typescript
export class GroupDetailsComponent implements OnInit, OnDestroy {
  private groupService = inject(GroupService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toastService = inject(ToastService);
  private destroyRef = inject(DestroyRef);

  // Route params
  private groupId = signal<string>('');

  // Loading and error states
  isLoading = signal<boolean>(true);
  error = signal<string | null>(null);
  isRefreshing = signal<boolean>(false);

  // Group data
  groupDetails = signal<GroupDetailsViewModel | null>(null);

  // Pre-draw form states
  myWishlist = signal<string>('');
  myBudgetSuggestion = signal<number | null>(null);
  isSavingWishlist = signal<boolean>(false);
  isSavingBudget = signal<boolean>(false);

  // Organizer states (pre-draw)
  invitationLink = signal<string>('');
  budgetSuggestions = signal<number[]>([]);
  exclusionRules = signal<ExclusionRuleViewModel[]>([]);
  isExecutingDraw = signal<boolean>(false);

  // Post-draw states
  recipientWishlist = signal<string | null>(null);
  giftSuggestions = signal<GiftSuggestion[]>([]);
  isGeneratingSuggestions = signal<boolean>(false);

  // Computed signals
  isPreDraw = computed(() => !this.groupDetails()?.drawCompleted);
  isPostDraw = computed(() => this.groupDetails()?.drawCompleted ?? false);
  isOrganizer = computed(() => this.groupDetails()?.isOrganizer ?? false);
  canExecuteDraw = computed(() =>
    this.groupDetails()?.drawValidation?.isValid ?? false
  );

  ngOnInit(): void {
    // Extract groupId from route and fetch data
    this.route.params
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(params => {
        this.groupId.set(params['groupId']);
        this.loadGroupData();
      });
  }

  private loadGroupData(): void {
    this.isLoading.set(true);
    this.error.set(null);

    // Fetch group details first
    this.groupService.fetchGroupDetails(this.groupId())
      .pipe(
        switchMap(groupDto => {
          const viewModel = this.groupService.mapToViewModel(groupDto);
          this.groupDetails.set(viewModel);

          // Generate invitation link
          if (viewModel.isOrganizer && !viewModel.drawCompleted) {
            this.invitationLink.set(
              `${window.location.origin}/invite/${this.groupId()}`
            );
          }

          // Parallel fetch additional data based on state
          const requests: Observable<any>[] = [];

          // Always fetch user's wishlist
          requests.push(
            this.groupService.fetchMyWishlist(this.groupId()).pipe(
              tap(wishlist => this.myWishlist.set(wishlist ?? ''))
            )
          );

          if (!viewModel.drawCompleted) {
            // Pre-draw: fetch budget suggestion
            requests.push(
              this.groupService.fetchMyBudgetSuggestion(this.groupId()).pipe(
                tap(suggestion => this.myBudgetSuggestion.set(suggestion))
              )
            );

            if (viewModel.isOrganizer) {
              // Organizer: fetch budget suggestions and exclusion rules
              requests.push(
                this.groupService.fetchBudgetSuggestions(this.groupId()).pipe(
                  tap(suggestions => this.budgetSuggestions.set(suggestions))
                )
              );
              requests.push(
                this.groupService.fetchExclusionRules(this.groupId()).pipe(
                  tap(rules => this.exclusionRules.set(rules))
                )
              );
            }
          } else {
            // Post-draw: fetch recipient wishlist
            requests.push(
              this.groupService.fetchRecipientWishlist(this.groupId()).pipe(
                tap(wishlist => this.recipientWishlist.set(wishlist))
              )
            );
          }

          return forkJoin(requests);
        }),
        finalize(() => this.isLoading.set(false))
      )
      .subscribe({
        error: (err) => this.handleError(err)
      });
  }

  // Event handlers for child components
  // ... (see User Interactions section)
}
```

**State Management Strategy**:
1. **Component signals** for local UI state (loading, forms, etc.)
2. **Service signals** for shared data that may be used across components
3. **Computed signals** for derived values (isPreDraw, canExecuteDraw, etc.)
4. **RxJS observables** only for HTTP calls and route params
5. **No optimistic updates** - always wait for API confirmation before updating state

## 7. API Integration

### 7.1 Primary Endpoint

**GET /api/groups/{groupId}**

**Purpose**: Fetch complete group details

**Request**:
```typescript
const groupId: string = 'uuid-here';
this.groupService.fetchGroupDetails(groupId);
```

**Response Type**: `GroupDetailsDto` (varies based on draw state)

**Success Handling**:
```typescript
.subscribe(groupDto => {
  const viewModel = this.mapToViewModel(groupDto);
  this.groupDetails.set(viewModel);
  // Trigger additional data fetches based on state
});
```

**Error Handling**:
- `401 Unauthorized`: ErrorInterceptor redirects to login
- `403 Forbidden`: Display error, redirect to groups list
- `404 Not Found`: Display error, offer navigation to groups list
- Network errors: Display error toast with retry option

### 7.2 Additional Endpoints

#### Wishlist Management

**GET /api/groups/{groupId}/wishlist** (current user)
- Fetches current user's wishlist
- Response: `{ content: string }`

**PUT /api/groups/{groupId}/wishlist**
- Request: `{ content: string }` (max 1000 chars)
- Response: 204 No Content
- Error: 400 if validation fails

**GET /api/groups/{groupId}/assignment/wishlist** (post-draw)
- Fetches recipient's wishlist
- Response: `{ content: string | null }`

#### Budget Management

**GET /api/groups/{groupId}/budget-suggestion** (current user)
- Fetches current user's budget suggestion
- Response: `{ amount: number | null }`

**PUT /api/groups/{groupId}/budget-suggestion**
- Request: `{ amount: number }` (positive, max 2 decimals)
- Response: 204 No Content

**GET /api/groups/{groupId}/budget-suggestions** (organizer only)
- Fetches anonymous budget suggestions
- Response: `{ suggestions: number[] }` (sorted)

#### Exclusion Rules (Organizer Only)

**GET /api/groups/{groupId}/exclusion-rules**
- Response: `{ rules: ExclusionRuleDto[] }`

**POST /api/groups/{groupId}/exclusion-rules**
- Request: `{ user1Id: string, user2Id: string }`
- Response: 201 Created
- Error: 400 if validation fails (same user, duplicate, conflict)

**DELETE /api/groups/{groupId}/exclusion-rules/{ruleId}**
- Response: 204 No Content

#### Participant Management (Organizer Only)

**DELETE /api/groups/{groupId}/participants/{userId}**
- Response: 204 No Content
- Error: 400 if trying to remove organizer

#### Draw Execution (Organizer Only)

**POST /api/groups/{groupId}/draw**
- Request: `{ budget: number }` (positive, max 2 decimals)
- Response: 200 OK (returns updated group details)
- Error: 400 if validation fails

#### Gift Suggestions (Post-Draw)

**POST /api/groups/{groupId}/gift-suggestions**
- Request: Empty body
- Response: `{ suggestions: GiftSuggestion[] }`
- Error: 500 if AI service fails

### 7.3 Error Interceptor Integration

All API errors are handled by the global `ErrorInterceptor`:

```typescript
@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401) {
          // Store return URL and redirect to login
          this.authService.storeReturnUrl(this.router.url);
          this.router.navigate(['/login']);
          this.toastService.showError('Your session has expired. Please log in again.');
        } else if (error.status === 403) {
          this.toastService.showError('You do not have access to this resource.');
          this.router.navigate(['/groups']);
        } else if (error.status === 404) {
          this.toastService.showError('Resource not found.');
        }

        return throwError(() => error);
      })
    );
  }
}
```

## 8. User Interactions

### 8.1 View Group Details (Initial Load)

**User Action**: Navigate to `/groups/:groupId`

**Flow**:
1. Component extracts `groupId` from route params
2. Display loading spinner
3. Fetch group details via `GET /api/groups/{groupId}`
4. Map DTO to ViewModel
5. Determine view state (pre/post draw, organizer/participant)
6. Parallel fetch additional data:
   - User's wishlist
   - Budget suggestion (pre-draw)
   - Budget suggestions list (organizer, pre-draw)
   - Exclusion rules (organizer, pre-draw)
   - Recipient wishlist (post-draw)
7. Hide loading spinner
8. Render appropriate components

**Code**:
```typescript
private loadGroupData(): void {
  this.isLoading.set(true);

  this.groupService.fetchGroupDetails(this.groupId())
    .pipe(
      switchMap(dto => {
        const vm = this.mapToViewModel(dto);
        this.groupDetails.set(vm);
        return this.fetchAdditionalData(vm);
      }),
      finalize(() => this.isLoading.set(false))
    )
    .subscribe({
      error: err => this.handleError(err)
    });
}
```

---

### 8.2 Save Wishlist

**User Action**: User enters wishlist text and clicks "Save Wishlist"

**Flow**:
1. Validate content length (<= 1000 chars)
2. Disable save button, show loading state
3. Call `PUT /api/groups/{groupId}/wishlist`
4. On success:
   - Update `myWishlist` signal
   - Show success toast
   - Mark form as pristine
5. On error:
   - Show error toast
   - Re-enable save button

**Code**:
```typescript
onSaveWishlist(content: string): void {
  if (content.length > 1000) {
    this.toastService.showError('Wishlist cannot exceed 1000 characters');
    return;
  }

  this.isSavingWishlist.set(true);

  this.groupService.updateWishlist(this.groupId(), content)
    .pipe(finalize(() => this.isSavingWishlist.set(false)))
    .subscribe({
      next: () => {
        this.myWishlist.set(content);
        this.toastService.showSuccess('Wishlist saved successfully');
      },
      error: err => {
        this.toastService.showError('Failed to save wishlist. Please try again.');
      }
    });
}
```

---

### 8.3 Save Budget Suggestion

**User Action**: User enters budget amount and clicks "Save Budget Suggestion"

**Flow**:
1. Validate: positive number, max 2 decimals
2. Disable save button, show loading state
3. Call `PUT /api/groups/{groupId}/budget-suggestion`
4. On success:
   - Update `myBudgetSuggestion` signal
   - Show success toast
5. On error:
   - Show error toast

**Code**:
```typescript
onSaveBudget(amount: number | null): void {
  if (amount !== null && (amount <= 0 || !this.hasMaxTwoDecimals(amount))) {
    this.toastService.showError('Please enter a valid budget amount');
    return;
  }

  this.isSavingBudget.set(true);

  this.groupService.updateBudgetSuggestion(this.groupId(), amount)
    .pipe(finalize(() => this.isSavingBudget.set(false)))
    .subscribe({
      next: () => {
        this.myBudgetSuggestion.set(amount);
        this.toastService.showSuccess('Budget suggestion saved');
      },
      error: err => {
        this.toastService.showError('Failed to save budget suggestion');
      }
    });
}
```

---

### 8.4 Copy Invitation Link (Organizer, Pre-Draw)

**User Action**: User clicks "Copy Link" button

**Flow**:
1. Use Clipboard API to copy link
2. Show success feedback (visual change or toast)
3. Feedback auto-dismisses after 3 seconds

**Code**:
```typescript
onCopyInvitationLink(): void {
  navigator.clipboard.writeText(this.invitationLink())
    .then(() => {
      this.toastService.showSuccess('Invitation link copied to clipboard');
    })
    .catch(() => {
      this.toastService.showError('Failed to copy link');
    });
}
```

---

### 8.5 Add Exclusion Rule (Organizer, Pre-Draw)

**User Action**: User selects two participants and clicks "Add Rule"

**Flow**:
1. Validate: different users, no duplicate/bidirectional rule
2. Call `POST /api/groups/{groupId}/exclusion-rules`
3. On success:
   - Fetch updated exclusion rules
   - Fetch updated group details (validation may change)
   - Show success toast
   - Reset form
4. On error:
   - Show error toast with validation message

**Code**:
```typescript
onAddExclusionRule(data: { user1Id: string; user2Id: string }): void {
  this.groupService.createExclusionRule(this.groupId(), data.user1Id, data.user2Id)
    .pipe(
      switchMap(() => forkJoin([
        this.groupService.fetchExclusionRules(this.groupId()),
        this.groupService.fetchGroupDetails(this.groupId())
      ]))
    )
    .subscribe({
      next: ([rules, groupDto]) => {
        this.exclusionRules.set(rules);
        this.groupDetails.set(this.mapToViewModel(groupDto));
        this.toastService.showSuccess('Exclusion rule added');
      },
      error: err => {
        const message = err.error?.message || 'Failed to add exclusion rule';
        this.toastService.showError(message);
      }
    });
}
```

---

### 8.6 Delete Exclusion Rule (Organizer, Pre-Draw)

**User Action**: User clicks delete button, confirms in dialog

**Flow**:
1. Show confirmation dialog
2. On confirm:
   - Call `DELETE /api/groups/{groupId}/exclusion-rules/{ruleId}`
   - On success:
     - Fetch updated rules
     - Fetch updated group details
     - Show success toast
   - On error:
     - Show error toast

**Code**:
```typescript
onDeleteExclusionRule(ruleId: string): void {
  const confirmed = confirm('Delete this exclusion rule?');
  if (!confirmed) return;

  this.groupService.deleteExclusionRule(this.groupId(), ruleId)
    .pipe(
      switchMap(() => forkJoin([
        this.groupService.fetchExclusionRules(this.groupId()),
        this.groupService.fetchGroupDetails(this.groupId())
      ]))
    )
    .subscribe({
      next: ([rules, groupDto]) => {
        this.exclusionRules.set(rules);
        this.groupDetails.set(this.mapToViewModel(groupDto));
        this.toastService.showSuccess('Exclusion rule deleted');
      },
      error: err => {
        this.toastService.showError('Failed to delete exclusion rule');
      }
    });
}
```

---

### 8.7 Remove Participant (Organizer, Pre-Draw)

**User Action**: User clicks "Remove" button, confirms

**Flow**:
1. Show confirmation dialog
2. On confirm:
   - Call `DELETE /api/groups/{groupId}/participants/{userId}`
   - On success:
     - Refresh group details (participant count, validation changes)
     - Show success toast
   - On error:
     - Show error toast

**Code**:
```typescript
onRemoveParticipant(userId: string): void {
  const confirmed = confirm('Remove this participant from the group?');
  if (!confirmed) return;

  this.groupService.removeParticipant(this.groupId(), userId)
    .pipe(
      switchMap(() => this.groupService.fetchGroupDetails(this.groupId()))
    )
    .subscribe({
      next: groupDto => {
        this.groupDetails.set(this.mapToViewModel(groupDto));
        this.toastService.showSuccess('Participant removed');
      },
      error: err => {
        this.toastService.showError('Failed to remove participant');
      }
    });
}
```

---

### 8.8 Execute Draw (Organizer, Pre-Draw)

**User Action**: User enters final budget, clicks "Execute Draw", confirms

**Flow**:
1. Validate: budget > 0, drawValidation.isValid = true
2. Show confirmation dialog
3. On confirm:
   - Set loading state
   - Call `POST /api/groups/{groupId}/draw`
   - On success:
     - Fetch updated group details (now post-draw)
     - Show success toast
     - View automatically switches to post-draw state
   - On error:
     - Show error toast
     - Hide loading state

**Code**:
```typescript
onExecuteDraw(budget: number): void {
  if (!this.canExecuteDraw()) {
    this.toastService.showError('Cannot execute draw: validation errors present');
    return;
  }

  this.isExecutingDraw.set(true);

  this.groupService.executeDraw(this.groupId(), budget)
    .pipe(
      switchMap(() => this.groupService.fetchGroupDetails(this.groupId())),
      finalize(() => this.isExecutingDraw.set(false))
    )
    .subscribe({
      next: groupDto => {
        this.groupDetails.set(this.mapToViewModel(groupDto));
        this.toastService.showSuccess('Draw completed! Check your assignment');
        // Fetch recipient wishlist for post-draw view
        this.loadPostDrawData();
      },
      error: err => {
        const message = err.error?.message || 'Failed to execute draw';
        this.toastService.showError(message);
      }
    });
}
```

---

### 8.9 Generate Gift Suggestions (Post-Draw)

**User Action**: User clicks "Generate Gift Ideas"

**Flow**:
1. Set loading state
2. Call `POST /api/groups/{groupId}/gift-suggestions`
3. On success:
   - Update `giftSuggestions` signal
   - Hide loading state
   - Display suggestions
4. On error:
   - Show error message with retry option
   - Hide loading state

**Code**:
```typescript
onGenerateGiftSuggestions(): void {
  this.isGeneratingSuggestions.set(true);

  this.groupService.generateGiftSuggestions(this.groupId())
    .pipe(finalize(() => this.isGeneratingSuggestions.set(false)))
    .subscribe({
      next: suggestions => {
        this.giftSuggestions.set(suggestions);
        this.toastService.showSuccess('Gift suggestions generated');
      },
      error: err => {
        this.toastService.showError('Failed to generate gift suggestions. Please try again.');
      }
    });
}
```

---

### 8.10 Manual Refresh

**User Action**: User clicks refresh button

**Flow**:
1. Set `isRefreshing` state
2. Re-fetch all data (same as initial load)
3. Update all signals
4. Clear `isRefreshing` state
5. Show success feedback

**Code**:
```typescript
onRefresh(): void {
  this.isRefreshing.set(true);
  this.loadGroupData();
  // loadGroupData will set isRefreshing to false in finalize
}
```

## 9. Conditions and Validation

### 9.1 Component-Level Validation

#### WishlistEditorComponent
- **Condition**: `content.length <= 1000`
- **Effect**:
  - Disable save button if validation fails
  - Show error message below textarea
  - Display character count
- **Implementation**: Computed signal `isValid()`

#### BudgetSuggestionComponent
- **Conditions**:
  1. `amount > 0` (if provided)
  2. Has max 2 decimal places
- **Effect**:
  - Disable save button if validation fails
  - Show specific error message
- **Implementation**: Computed signal `isValid()` with helper method `hasMaxTwoDecimals()`

#### ExclusionRulesComponent
- **Conditions**:
  1. `user1Id !== user2Id` (cannot be same user)
  2. Rule doesn't exist: no (user1, user2) or (user2, user1) in current rules
  3. Both users must be selected
- **Effect**:
  - Disable "Add Rule" button
  - Show real-time validation error message
- **Implementation**: Computed signals `canAddRule()` and `validationError()`

#### DrawExecutionComponent
- **Conditions**:
  1. `drawValidation.isValid === true` (from API)
  2. `budget > 0`
  3. Budget has max 2 decimal places
- **Effect**:
  - Disable "Execute Draw" button
  - Show tooltip explaining why disabled
  - Display warning message
- **Implementation**: Computed signal `canExecute()`

#### ParticipantListComponent (Remove Button)
- **Condition**: `!participant.isOrganizer`
- **Effect**: Hide remove button for organizer
- **Implementation**: `@if` directive with `participant.canRemove`

### 9.2 API-Level Validation

#### Draw Validation (from API)
The API returns `drawValidation` object with validation results:

**Possible Validation Errors**:
- "Minimum 3 participants required for draw"
- "Cannot execute draw: circular exclusion detected"
- "Cannot find valid Secret Santa assignment with current exclusion rules"

**Frontend Handling**:
- Display in `DrawValidationComponent`
- Color-coded UI (green if valid, red if errors)
- List all validation errors
- Disable draw button if `!isValid`

**Validation Re-runs**:
Validation automatically updates when:
- Participant is added or removed
- Exclusion rule is added or removed
- Manual refresh is triggered

### 9.3 Authorization Checks

#### Route-Level Authorization
- **Guard**: `AuthGuard` ensures user is authenticated
- **Component-Level**: Check if user is participant (403 error handling)
- **Effect**: Redirect to login or groups list if unauthorized

#### Feature-Level Authorization
- **Organizer Features**: Hidden using `@if (isOrganizer())`
  - Invitation link
  - Budget management
  - Exclusion rules
  - Participant removal
  - Draw execution
- **Post-Draw Features**: Hidden using `@if (isPostDraw())`
  - Assignment card
  - Gift suggestions
- **Pre-Draw Features**: Hidden using `@if (isPreDraw())`
  - Participant management
  - Draw validation

## 10. Error Handling

### 10.1 HTTP Error Responses

#### 401 Unauthorized
- **Cause**: Token expired or invalid
- **Handling**:
  - ErrorInterceptor catches error
  - Store current URL as return URL
  - Redirect to `/login`
  - Show toast: "Your session has expired. Please log in again."

#### 403 Forbidden
- **Cause**: User is not a participant in the group
- **Handling**:
  - ErrorInterceptor catches error
  - Redirect to `/groups`
  - Show toast: "You do not have access to this group."

#### 404 Not Found
- **Cause**: Group does not exist or was deleted
- **Handling**:
  - Display error message in view
  - Offer navigation buttons:
    - "Go to My Groups"
    - "Create New Group"
  - Show toast: "Group not found."

#### 400 Bad Request
- **Cause**: Validation errors (wishlist too long, invalid budget, etc.)
- **Handling**:
  - Parse error response for field-level errors
  - Display inline error messages in relevant components
  - Show toast with general error message

#### 409 Conflict
- **Cause**:
  - Trying to add duplicate exclusion rule
  - Trying to remove participant that's in a rule
- **Handling**:
  - Show specific error toast with explanation
  - Do not update UI state

#### 500 Internal Server Error
- **Cause**: Server error, AI service failure
- **Handling**:
  - Show error toast: "Something went wrong. Please try again."
  - Offer retry option
  - Log error for debugging

### 10.2 Network Errors

#### Connection Timeout
- **Handling**:
  - Show error toast: "Connection timeout. Please check your internet connection."
  - Offer retry button
  - Don't clear existing data (keep showing stale data)

#### No Internet Connection
- **Handling**:
  - Show error banner: "You appear to be offline. Some features may not work."
  - Disable action buttons
  - Enable retry when connection is restored

### 10.3 Component-Level Error States

#### Loading Failures
- **Scenario**: Initial data fetch fails
- **UI State**:
  - Hide loading spinner
  - Show error message with icon
  - Display retry button
  - Show friendly message: "We couldn't load the group details. Please try again."

#### Partial Failures
- **Scenario**: Group details load but wishlist fetch fails
- **UI State**:
  - Show main content (group details)
  - Display inline error in wishlist section: "Failed to load wishlist. Click to retry."
  - Allow retry for failed section only

#### Action Failures
- **Scenario**: Save wishlist fails
- **UI State**:
  - Keep form data (don't clear)
  - Show error message below form
  - Re-enable save button
  - Allow user to retry or edit

### 10.4 Edge Cases

#### Concurrent Modification
- **Scenario**: Organizer A removes participant while organizer B is viewing
- **Handling**:
  - Next action triggers 404 or stale data error
  - Prompt user to refresh: "This group has been modified. Please refresh to see latest changes."
  - Provide refresh button

#### Draw Execution During Viewing
- **Scenario**: User is viewing pre-draw state, organizer executes draw
- **Handling**:
  - User continues to see pre-draw state (no real-time updates)
  - Manual refresh shows post-draw state
  - No automatic redirect or notification (per UI plan)

#### Invitation Link Expiry
- **Scenario**: Not applicable to this view (handled in Invitation View)

#### Empty States
- **No participants**: Show message "Waiting for participants to join"
- **No wishlist**: Show placeholder "Your giftee hasn't added their wishlist yet"
- **No budget suggestions**: Show "No budget suggestions yet"
- **No exclusion rules**: Show "No exclusion rules yet"
- **No gift suggestions**: Show "Generate Gift Ideas" button

### 10.5 Error Recovery Strategies

1. **Retry with Exponential Backoff**: For network errors
2. **Partial Refresh**: Retry only failed sections
3. **Graceful Degradation**: Show available data, hide unavailable features
4. **User Feedback**: Always inform user of error and next steps
5. **Logging**: Log errors to console for debugging (production: send to logging service)

## 11. Implementation Steps

### Step 1: Setup Component Structure
1. Generate `GroupDetailsComponent` as standalone component
2. Configure route in app routing:
   ```typescript
   {
     path: 'groups/:groupId',
     component: GroupDetailsComponent,
     canActivate: [AuthGuard]
   }
   ```
3. Add basic template structure with loading and error states
4. Inject required services: `GroupService`, `ActivatedRoute`, `Router`, `ToastService`

### Step 2: Implement Data Fetching
1. Extract `groupId` from route params in `ngOnInit`
2. Implement `loadGroupData()` method:
   - Fetch group details
   - Map DTO to ViewModel
   - Store in `groupDetails` signal
3. Add error handling for 401, 403, 404
4. Test basic data loading and display

### Step 3: Create Shared/Common Components
1. Generate `LoadingSpinnerComponent` with size variants
2. Generate `ErrorAlertComponent` with type variants
3. Generate `ToastComponent` with auto-dismiss
4. Generate `ConfirmDialogComponent` with Angular CDK Dialog
5. Generate `CopyToClipboardButtonComponent`
6. Test each component in isolation

### Step 4: Create Always-Visible Components
1. Generate `GroupInfoCardComponent`
   - Implement props for group data
   - Add Tailwind styling for card layout
   - Test with mock data
2. Test integration in `GroupDetailsComponent`

### Step 5: Implement Pre-Draw Components (All Participants)
1. Generate `ParticipantListComponent`
   - Implement participant display
   - Add conditional remove button
   - Emit remove event
2. Generate `WishlistEditorComponent`
   - Implement textarea with character limit
   - Add validation logic
   - Emit save event
3. Generate `BudgetSuggestionComponent`
   - Implement number input
   - Add validation (positive, 2 decimals)
   - Emit save event
4. Test each component with mock data
5. Integrate into `GroupDetailsComponent` with `@if (isPreDraw())`

### Step 6: Implement Pre-Draw Components (Organizer Only)
1. Generate `InvitationLinkCardComponent`
   - Display invitation link
   - Integrate `CopyToClipboardButtonComponent`
   - Test copy functionality
2. Generate `BudgetManagementComponent`
   - Display anonymous suggestions
   - Show suggestion count
   - Display final budget (read-only)
3. Generate `ExclusionRulesComponent`
   - Implement dropdown form
   - Add real-time validation
   - Display rules list with delete buttons
   - Emit add/delete events
4. Generate `DrawValidationComponent`
   - Display validation status
   - Color-coded UI (green/red)
   - List validation errors
5. Generate `DrawExecutionComponent`
   - Implement budget input
   - Add "Execute Draw" button with validation
   - Integrate `ConfirmDialogComponent`
   - Emit execute event
6. Test each component
7. Integrate into `GroupDetailsComponent` with `@if (isOrganizer())`

### Step 7: Implement Post-Draw Components
1. Generate `AssignmentCardComponent`
   - Display recipient name prominently
   - Show budget
   - Display wishlist or empty state
2. Generate `GiftSuggestionsComponent`
   - Implement "Generate" button
   - Show loading state with cancel
   - Display suggestion cards
   - Emit generate/cancel events
3. Test components with mock data
4. Integrate into `GroupDetailsComponent` with `@if (isPostDraw())`

### Step 8: Implement Event Handlers in Container
1. Implement `onSaveWishlist()` with API call
2. Implement `onSaveBudget()` with API call
3. Implement `onAddExclusionRule()` with validation refresh
4. Implement `onDeleteExclusionRule()` with confirmation
5. Implement `onRemoveParticipant()` with confirmation
6. Implement `onExecuteDraw()` with confirmation and state transition
7. Implement `onGenerateGiftSuggestions()` with loading state
8. Implement `onRefresh()` for manual refresh
9. Test each interaction with real API (or mocked API)

### Step 9: Implement Additional Data Fetching
1. Create `fetchAdditionalData()` method in `GroupDetailsComponent`
2. Implement parallel fetching using `forkJoin`:
   - User's wishlist (always)
   - Budget suggestion (pre-draw)
   - Budget suggestions list (organizer, pre-draw)
   - Exclusion rules (organizer, pre-draw)
   - Recipient wishlist (post-draw)
3. Handle partial failures gracefully
4. Test with various user roles and states

### Step 10: Add Validation and Error Handling
1. Implement client-side validation for all forms
2. Add computed signals for validation states
3. Implement error handling for all API calls
4. Add user-friendly error messages
5. Test all error scenarios (401, 403, 404, 400, 500)
6. Test validation edge cases

### Step 11: Implement Mobile Responsiveness
1. Add Tailwind responsive classes to all components
2. Test on mobile viewport:
   - Full-width cards
   - Stacked layout
   - Touch-friendly buttons
3. Test on tablet viewport
4. Test on desktop viewport
5. Ensure all interactions work on touch devices

### Step 12: Implement Unsaved Changes Warning
1. Track dirty state for wishlist and budget forms
2. Implement browser `beforeunload` event handler
3. Implement Angular `canDeactivate` guard
4. Test warning when navigating away with unsaved changes

### Step 13: Add Accessibility Features
1. Add ARIA labels to all interactive elements
2. Ensure keyboard navigation works
3. Add focus management for dialogs
4. Test with screen reader
5. Ensure color contrast meets WCAG standards

### Step 14: Performance Optimization
1. Verify OnPush change detection strategy
2. Add `trackBy` functions to all `@for` loops
3. Optimize computed signals (avoid expensive calculations)
4. Test performance with large participant lists
5. Profile component rendering

### Step 15: Integration Testing
1. Test full user journey: Create group → Add participants → Execute draw → View assignment
2. Test organizer flow: All organizer-only features
3. Test participant flow: Pre-draw and post-draw states
4. Test edge cases: Empty states, errors, validations
5. Test cross-browser compatibility (Chrome, Firefox, Safari, Edge)

### Step 16: Polish and Final Touches
1. Review all UI text for clarity and tone
2. Ensure consistent spacing and styling
3. Add loading states for all async operations
4. Add success feedback for all actions
5. Review and refine error messages
6. Test manual refresh functionality
7. Verify invitation link generation and copy

### Step 17: Documentation and Code Review
1. Add JSDoc comments to all public methods
2. Document component interfaces (props)
3. Add code comments for complex logic
4. Review code for consistency with style guide
5. Ensure all components follow Angular 20 best practices
6. Run linter and fix any issues

### Step 18: Deployment Preparation
1. Build for production: `ng build --configuration production`
2. Test production build locally
3. Verify all environment variables are correctly set
4. Test with production API endpoints
5. Verify error tracking and logging
6. Prepare deployment checklist

---

**Total Estimated Implementation Time**: 40-60 hours (depending on developer experience)

**Priority Order**:
1. Core data loading (Steps 1-2)
2. Shared components (Step 3)
3. Pre-draw participant view (Steps 4-5)
4. Pre-draw organizer view (Step 6)
5. Post-draw view (Step 7)
6. Event handling (Step 8)
7. Polish and testing (Steps 9-18)

