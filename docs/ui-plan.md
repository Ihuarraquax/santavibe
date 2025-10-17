# UI Architecture for SantaVibe

## 1. UI Structure Overview

SantaVibe is a Secret Santa web application built with Angular 20 using standalone components, Tailwind CSS 4.1, and Flowbite 3.1. The UI architecture follows a mobile-first responsive approach with Polish language support and focuses on simplicity for MVP.

**Core Architecture Principles:**
- Mobile-first responsive design with Tailwind breakpoints
- OnPush change detection strategy throughout
- State management using Signals (service-level for shared state, component-level for local state)
- RxJS reserved for async operations only
- No optimistic UI updates - always wait for API confirmation
- Polish-only with i18n-ready structure
- Europe/Warsaw timezone for all date/time displays
- PLN currency display

**Technology Stack:**
- Framework: Angular 20 with standalone components
- Styling: Tailwind CSS 4.1 + Flowbite 3.1
- UI Components: Angular CDK for dialogs/overlays
- Date/Time: date-fns with Polish locale
- Dependency Injection: inject() function pattern
- Control Flow: Native Angular syntax (@if, @for, @switch)

**Navigation Pattern:**
- Mobile: Top bar with hamburger menu
- Desktop: Expanded horizontal navigation
- Menu items: My Groups, Profile, Logout

---

## 2. View List

### 2.1 Public Views (Unauthenticated)

#### 2.1.1 Login View
- **Path:** `/login`
- **Main Purpose:** Authenticate existing users
- **Authentication Required:** No
- **Key Information to Display:**
  - Login form (email, password)
  - Link to registration page
  - Password reset link
  - GDPR/privacy links in footer

- **Key View Components:**
  - LoginFormComponent
  - ErrorAlertComponent (for invalid credentials)

- **UX Considerations:**
  - Auto-focus on email field
  - Show/hide password toggle
  - Clear error messages without revealing whether email exists
  - Remember return URL for redirect after login
  - Mobile-optimized form layout

- **Security:**
  - No email enumeration (generic error: "Invalid email or password")
  - Rate limiting feedback for 429 errors
  - HTTPS only
  - Secure password input (type="password")

---

#### 2.1.2 Registration View
- **Path:** `/register`
- **Main Purpose:** Create new user account
- **Authentication Required:** No
- **Key Information to Display:**
  - Registration form (email, password, firstName, lastName)
  - GDPR consent checkbox with link to terms
  - Link to login page
  - Password strength indicator

- **Key View Components:**
  - RegistrationFormComponent
  - ErrorAlertComponent (for validation errors)

- **UX Considerations:**
  - Real-time password strength feedback
  - On-blur validation for each field
  - All errors displayed on submit
  - Clear GDPR consent requirement
  - Success flow redirects to intended destination or groups list

- **Security:**
  - Password requirements clearly stated: min 8 chars, uppercase, lowercase, digit, special character
  - Email validation (format only)
  - GDPR consent required (checkbox must be checked)
  - Client-side validation + server-side confirmation
  - Prevent registration spam with rate limiting

---

#### 2.1.3 Invitation View (Public)
- **Path:** `/invite/{token}`
- **Main Purpose:** Display group details before authentication and join group after authentication
- **Authentication Required:** No (for viewing), Yes (for joining)
- **Key Information to Display:**
  - **Unauthenticated State:**
    - Group name
    - Organizer name
    - Current participant count
    - CTAs: "Login to Join" or "Register to Join"
  - **Authenticated State (Auto-triggered join flow):**
    - Same group information
    - Budget suggestion field (optional)
    - "Join Group" button

- **Key View Components:**
  - InvitationDetailsComponent
  - BudgetSuggestionInputComponent
  - LoadingSpinnerComponent (during join operation)
  - ErrorAlertComponent

- **UX Considerations:**
  - Token stored during registration flow for seamless auto-join
  - After auth, automatically show join form
  - Budget suggestion is optional (can be empty)
  - Clear messaging if invitation expired (410) or invalid (404)
  - Success redirects to group details page

- **Security:**
  - Token validated server-side
  - Cannot join same group twice (409 error handled)
  - Cannot join after draw completed (410 error)
  - No sensitive information exposed before auth

---

### 2.2 Authenticated Views

#### 2.2.1 Groups List View
- **Path:** `/groups`
- **Main Purpose:** Main landing page after login, displays all groups user participates in
- **Authentication Required:** Yes
- **Key Information to Display:**
  - List of all groups (participant in)
  - For each group:
    - Group name
    - Organizer name
    - Participant count
    - Budget (if set)
    - Draw status badge: "Pending Draw" or "Draw Complete"
    - Joined date
    - Draw completion date (if completed)
  - "Create New Group" button
  - Empty state: "You're not in any groups yet."

- **Key View Components:**
  - GroupCardComponent (repeated for each group)
  - SkeletonLoaderComponent (initial load)
  - LoadingSpinnerComponent (operations)
  - EmptyStateComponent

- **UX Considerations:**
  - Skeleton screens during initial load
  - Visual distinction between pending and completed draws (badges with colors)
  - Click on group card navigates to group details
  - No sorting/filtering in MVP
  - Manual refresh button with "last updated" timestamp
  - Mobile: Full-width cards
  - Desktop: Grid layout with max-width container

---

#### 2.2.2 Group Create View
- **Path:** `/groups/create`
- **Main Purpose:** Create new Secret Santa group
- **Authentication Required:** Yes
- **Key Information to Display:**
  - Group name input field
  - "Create Group" button
  - Helper text: "You'll be automatically added as a participant and organizer"

- **Key View Components:**
  - GroupCreateFormComponent
  - ErrorAlertComponent
  - LoadingSpinnerComponent

- **UX Considerations:**
  - Simple single-field form
  - Auto-focus on group name field
  - Validation: required, min 3 chars, max 200 chars
  - On success: redirect to group details with success toast
  - Toast message: "Group created! Copy invitation link to invite participants"
  - Browser-native unsaved changes warning

- **Security:**
  - Authenticated user becomes organizer

---

#### 2.2.3 Group Details View
- **Path:** `/groups/:groupId`
- **Main Purpose:** Main hub for all group-related activities, conditionally rendered based on user role and draw state
- **Authentication Required:** Yes
- **Authorization:** User must be participant in group

- **Key Information to Display:**

  **Always Visible:**
  - Group name
  - Organizer name (clearly labeled)
  - Participant count

  **Pre-Draw State - All Participants:**
  - Group information card (name, organizer, participant count)
  - Participant list (first name, last name, join date)
  - My Wishlist section (textarea, 1000 char limit, save button)
  - My Budget Suggestion section (optional numeric input, save button)
  - Status message: "Waiting for organizer to start the draw"

  **Pre-Draw State - Organizer Only (Additional):**
  - Invitation link card (prominent, copy-to-clipboard button)
  - Budget Management section:
    - Anonymous sorted list of budget suggestions
    - Count: "5 of 6 participants provided suggestions"
    - Final budget input field (used during draw execution)
  - Exclusion Rules section:
    - Dropdown form: Select User 1, Select User 2, Add Rule button
    - List of current rules with delete buttons
    - Real-time validation feedback
  - Participant management (remove button for each participant except organizer)
  - Draw Validation Summary:
    - Validation status (checkmark or error icon)
    - List of validation errors (if any)
  - "Execute Draw" button:
    - Disabled if validation fails
    - Confirmation dialog on click
    - Requires final budget input

  **Post-Draw State - All Participants:**
  - Group summary card (read-only):
    - Group name
    - Organizer name
    - Participant count
    - Draw completed date
  - My Assignment card (prominently displayed):
    - "You're buying a gift for:"
    - Recipient first and last name
    - Final budget (prominently displayed)
    - Recipient's wishlist (if provided, else friendly message)
    - AI Gift Suggestions section (expandable):
      - "Generate Gift Ideas" button
      - Loading state with cancellation
      - 3-5 suggestions displayed in cards
  - My Wishlist section (editable):
    - Current wishlist content
    - Edit and save functionality
    - Helper text: "Your Santa will be notified when you update"

- **Key View Components:**
  - GroupInfoCardComponent
  - ParticipantListComponent
  - WishlistEditorComponent
  - BudgetSuggestionComponent
  - InvitationLinkCardComponent (organizer only)
  - BudgetManagementComponent (organizer only)
  - ExclusionRulesComponent (organizer only)
  - DrawValidationComponent (organizer only)
  - DrawExecutionComponent (organizer only)
  - AssignmentCardComponent (post-draw)
  - GiftSuggestionsComponent (post-draw)
  - LoadingSpinnerComponent
  - ErrorAlertComponent
  - ToastComponent
  - ConfirmDialogComponent

- **UX Considerations:**
  - Single scrollable page with card-based sections
  - Conditional rendering based on isOrganizer flag and drawCompleted flag
  - Mobile: Full-width cards stacked vertically
  - Desktop: Max-width container with cards in sections
  - Real-time validation feedback for exclusion rules
  - Draw button disabled with tooltip when validation fails
  - Confirmation dialog for draw execution: "This action is permanent and cannot be undone"
  - Loading state during draw execution
  - After draw: redirect with toast "Draw completed! Check your assignment"
  - Manual refresh button for data updates
  - Empty wishlist message: "Your giftee hasn't added their wishlist yet - you'll be notified by email when they do"
  - Unsaved changes warning for wishlist and budget suggestion forms

- **Security:**
  - Authorization check: user must be participant
  - Organizer-only sections hidden from non-organizers
  - Post-draw content hidden before draw
  - Assignment shows only user's own recipient
  - Budget suggestions fully anonymous (no user attribution)
  - Invitation link only visible to organizer
  - Cannot modify participants/rules/budget after draw

---

#### 2.2.4 Profile View
- **Path:** `/profile`
- **Main Purpose:** View and manage user profile information
- **Authentication Required:** Yes
- **Key Information to Display:**
  - First name (read-only display)
  - Last name (read-only display)
  - Email (read-only display)
  - Created at date
  - Last login date
  - "Change Password" button

- **Key View Components:**
  - ProfileInfoComponent
  - LoadingSpinnerComponent

- **UX Considerations:**
  - Minimal profile page (MVP scope)
  - Change password button triggers password reset flow via email
  - Future: May add edit functionality
  - Clean, simple card layout

- **Security:**
  - User can only view their own profile
  - Email is read-only (cannot be changed in MVP)
  - Password reset via secure email link

---

#### 2.2.5 Error Views

**404 Not Found**
- **Path:** `/404` or wildcard route
- **Main Purpose:** Handle invalid routes or missing resources
- **Key Information to Display:**
  - Error message: "Page not found"
  - Helpful text with context
  - Link to groups list
  - Link to create group

- **UX Considerations:**
  - Friendly, helpful tone
  - Clear navigation options
  - Consistent with app styling

---

## 3. User Journey Map

### 3.1 Primary User Journeys

#### Journey 1: New User Creates Group and Invites Participants

1. **Entry Point:** User visits landing page
2. **Registration:** `/register`
   - User fills registration form (email, password, firstName, lastName)
   - User checks GDPR consent
   - User submits → Account created → Auto-login
3. **Redirect:** `/groups` (empty state)
   - User sees empty state message
   - User clicks "Create New Group" button
4. **Group Creation:** `/groups/create`
   - User enters group name
   - User clicks "Create Group"
   - Loading state
   - Success → Redirect to `/groups/:groupId`
5. **Group Details (Organizer, Pre-Draw):**
   - User sees success toast: "Group created! Copy invitation link to invite participants"
   - User sees invitation link prominently displayed
   - User clicks "Copy Link" button
   - User shares link via external channels (WhatsApp, email, etc.)
6. **Waiting for Participants:**
   - User refreshes page to see new participants join
   - User adds personal wishlist
   - User sets budget suggestion
7. **Participants Join:**
   - User sees participant list grow
   - User sees anonymous budget suggestions populate
8. **Setting Up Draw:**
   - User reviews budget suggestions
   - User adds exclusion rules (if needed)
   - User sees real-time validation feedback
9. **Executing Draw:**
   - User enters final budget
   - User clicks "Execute Draw"
   - Confirmation dialog appears
   - User confirms
   - Loading state: "Executing draw..."
   - Success → Redirect to `/groups/:groupId` (now post-draw)
   - Toast: "Draw completed! Check your assignment"
10. **Viewing Assignment:**
    - User sees their recipient's name and budget
    - User reads recipient's wishlist
    - User clicks "Generate Gift Ideas"
    - User reviews AI suggestions

---

#### Journey 2: Invited User Joins Group

1. **Entry Point:** User receives invitation link via external channel
2. **Invitation View (Unauthenticated):** `/invite/{token}`
   - User sees group details (name, organizer, participant count)
   - User sees CTAs: "Login to Join" or "Register to Join"
   - User clicks "Register to Join"
3. **Registration:** `/register`
   - Token stored in session
   - User fills registration form
   - User checks GDPR consent
   - User submits → Account created → Auto-login
4. **Redirect:** `/invite/{token}` (authenticated)
   - Join flow auto-triggered
   - User sees group details
   - User enters budget suggestion (optional)
   - User clicks "Join Group"
   - Loading state
   - Success → Redirect to `/groups/:groupId`
5. **Group Details (Participant, Pre-Draw):**
   - User sees group information
   - User sees other participants
   - User adds personal wishlist
   - User waits for organizer to execute draw
6. **Draw Executed (Email Notification Received):**
   - User logs back in
   - User navigates to `/groups`
   - User clicks on group (now shows "Draw Complete" badge)
7. **Viewing Assignment:** `/groups/:groupId` (post-draw)
   - User sees their recipient's name and budget
   - User reads recipient's wishlist
   - User generates AI gift suggestions

---

#### Journey 3: Participant Updates Wishlist After Draw

1. **Entry Point:** User logged in, navigates to `/groups`
2. **Groups List:** User clicks on completed group
3. **Group Details (Post-Draw):** `/groups/:groupId`
   - User scrolls to "My Wishlist" section
   - User clicks "Edit" (or textarea is directly editable)
   - User updates wishlist content
   - User clicks "Save"
   - Loading state
   - Success toast: "Wishlist saved"
   - Backend schedules email to Santa (1hr delay, transparent to user)

---

#### Journey 4: Organizer Manages Participants and Exclusion Rules

1. **Entry Point:** Organizer on `/groups/:groupId` (pre-draw)
2. **View Participants:**
   - Organizer reviews participant list
   - Organizer identifies participant to remove
   - Organizer clicks "Remove" next to participant name
3. **Remove Participant:**
   - Confirmation dialog appears
   - Organizer confirms
   - Loading state
   - Success toast: "Participant removed"
   - Participant count updated
   - Validation re-runs
4. **Add Exclusion Rule:**
   - Organizer scrolls to "Exclusion Rules" section
   - Organizer selects User 1 from dropdown
   - Organizer selects User 2 from dropdown
   - Organizer clicks "Add Rule"
   - Real-time validation runs
   - If valid: Rule added to list, success toast
   - If invalid: Error message displayed with suggestion
5. **Review Draw Validation:**
   - Organizer views validation summary
   - If errors: Reviews error messages, takes corrective action
   - If valid: Proceeds to execute draw

---

### 3.2 Edge Case Journeys

#### Journey 5: User Attempts to Join Invalid/Expired Invitation

1. **Entry Point:** User clicks invalid or expired invitation link
2. **Invitation View:** `/invite/{token}`
   - Loading state
   - Error state displayed
   - **404 Error:** "This invitation link is invalid or has expired"
   - **410 Error:** "This group has already completed the draw and is no longer accepting participants"
   - Options provided:
     - "Create Your Own Group"
     - "Go to My Groups"

---

#### Journey 6: Session Expires During Active Use

1. **Context:** User is on any authenticated page
2. **Session Expiry:** Token expires (24 hours)
3. **Next API Call:** Returns 401 Unauthorized
4. **Error Interceptor:** Catches 401
   - Stores current route as return URL
   - Redirects to `/login`
   - Toast message: "Your session has expired. Please log in again."
5. **User Logs In:**
   - After successful login, redirected to stored return URL

---

#### Journey 8: Draw Validation Fails

1. **Context:** Organizer on `/groups/:groupId` (pre-draw)
2. **Scenario:** Only 2 participants in group
3. **Draw Validation Summary:**
   - Error icon displayed
   - Error message: "Minimum 3 participants required for Secret Santa draw"
   - "Execute Draw" button is disabled
4. **Tooltip on Hover:** "Cannot execute draw: validation errors present"
5. **Organizer Action:** Invites more participants
6. **Validation Re-runs:** Automatically when participant count changes
7. **Validation Passes:** Error cleared, button enabled

---

## 4. Layout and Navigation Structure

### 4.1 Global Navigation

**Desktop Layout:**
```
+--------------------------------------------------+
| SantaVibe Logo | My Groups | Profile | Logout   |
+--------------------------------------------------+
|                                                  |
|              Main Content Area                   |
|                                                  |
+--------------------------------------------------+
```

**Mobile Layout:**
```
+--------------------------------------------------+
| ☰ Menu                        SantaVibe Logo    |
+--------------------------------------------------+
|                                                  |
|              Main Content Area                   |
|                                                  |
+--------------------------------------------------+
```

**Mobile Menu (Expanded):**
```
+--------------------------------------------------+
| ✕ Close                                          |
+--------------------------------------------------+
| My Groups                                        |
| Profile                                          |
| Logout                                           |
+--------------------------------------------------+
```

### 4.2 Navigation Items

**For Authenticated Users:**
- **My Groups** → `/groups`
- **Profile** → `/profile`
- **Logout** → Logs out user, redirects to `/login`

**For Unauthenticated Users:**
- **Login** → `/login`
- **Register** → `/register`

### 4.3 Breadcrumb Navigation

Not implemented in MVP for simplicity. Standard browser back button provides navigation.

### 4.4 Footer

- Links to Terms of Service
- Links to Privacy Policy
- Copyright notice

---

## 5. Key Components

### 5.1 Smart Components (Container Components)

These components manage state and business logic, connecting to services.

#### 5.1.1 LoginComponent
- **Purpose:** Handle user authentication
- **Responsibilities:**
  - Form validation
  - API call to login endpoint
  - Store JWT token
  - Redirect to return URL or groups list
  - Display error messages

#### 5.1.2 RegistrationComponent
- **Purpose:** Handle new user registration
- **Responsibilities:**
  - Form validation (client-side)
  - Password strength checking
  - API call to register endpoint
  - Auto-login after registration
  - Redirect to intended destination

#### 5.1.3 GroupsListComponent
- **Purpose:** Display all groups user participates in
- **Responsibilities:**
  - Fetch groups data from API
  - Display loading states
  - Handle empty state
  - Navigate to group details on click
  - Manual refresh functionality

#### 5.1.4 GroupCreateComponent
- **Purpose:** Create new Secret Santa group
- **Responsibilities:**
  - Form validation
  - API call to create group
  - Redirect to group details
  - Display success toast

#### 5.1.5 GroupDetailsComponent
- **Purpose:** Main hub for group activities
- **Responsibilities:**
  - Fetch group data from API
  - Conditional rendering based on user role and draw state
  - Manage all group-related child components
  - Handle refreshes
  - Route guard implementation

#### 5.1.6 InvitationComponent
- **Purpose:** Display invitation details and join flow
- **Responsibilities:**
  - Fetch invitation details
  - Handle unauthenticated state
  - Handle authenticated join flow
  - Store token during registration
  - Redirect after successful join

#### 5.1.7 ProfileComponent
- **Purpose:** Display user profile information
- **Responsibilities:**
  - Fetch user profile data
  - Display read-only information
  - Trigger password reset flow

---

### 5.2 Presentational Components

These components receive data via inputs and emit events via outputs.

#### 5.2.1 GroupCardComponent
- **Purpose:** Display single group summary
- **Inputs:** Group data (name, organizer, participant count, draw status, etc.)
- **Outputs:** Click event
- **Visual Elements:**
  - Group name (heading)
  - Organizer name
  - Participant count
  - Budget (if set)
  - Status badge (Pending/Complete)
  - Dates

#### 5.2.2 ParticipantListComponent
- **Purpose:** Display list of group participants
- **Inputs:** Array of participants, isOrganizer flag, canRemove flag
- **Outputs:** Remove participant event
- **Visual Elements:**
  - Table or card list of participants
  - First name, last name
  - Join date
  - Wishlist indicator
  - Remove button (organizer only, pre-draw)

#### 5.2.3 WishlistEditorComponent
- **Purpose:** Create and edit wishlist
- **Inputs:** Current wishlist content, isEditable flag
- **Outputs:** Save event
- **Visual Elements:**
  - Textarea (1000 char limit)
  - Save button
  - Character count (hidden in MVP, but validated)
  - Helper text

#### 5.2.4 BudgetSuggestionComponent
- **Purpose:** Input and display budget suggestion
- **Inputs:** Current suggestion, isEditable flag
- **Outputs:** Save event
- **Visual Elements:**
  - Numeric input with PLN label
  - Save button
  - Helper text

#### 5.2.5 InvitationLinkCardComponent
- **Purpose:** Display and copy invitation link
- **Inputs:** Invitation link
- **Outputs:** Copy event
- **Visual Elements:**
  - Prominent card styling
  - Read-only input with link
  - Copy to clipboard button
  - Success feedback

#### 5.2.6 BudgetManagementComponent
- **Purpose:** Manage budget suggestions and final budget (organizer)
- **Inputs:** Anonymous budget suggestions array, participant count
- **Outputs:** Set budget event
- **Visual Elements:**
  - Sorted list of suggestions
  - Count display: "X of Y participants provided suggestions"
  - Final budget input field
  - Helper text

#### 5.2.7 ExclusionRulesComponent
- **Purpose:** Manage exclusion rules (organizer)
- **Inputs:** Participants array, current rules array
- **Outputs:** Add rule event, delete rule event
- **Visual Elements:**
  - User 1 dropdown
  - User 2 dropdown
  - Add rule button
  - List of current rules with delete buttons
  - Real-time validation feedback
  - Error messages

#### 5.2.8 DrawValidationComponent
- **Purpose:** Display draw validation summary
- **Inputs:** Validation result (isValid, errors array)
- **Visual Elements:**
  - Participant count
  - Exclusion rules count
  - Validation status icon (checkmark or error)
  - List of errors (if any)
  - Color-coded sections (green for valid, red for errors)

#### 5.2.9 DrawExecutionComponent
- **Purpose:** Execute draw action (organizer)
- **Inputs:** Validation status, budget value
- **Outputs:** Execute draw event
- **Visual Elements:**
  - Final budget input
  - "Execute Draw" button (disabled if validation fails)
  - Confirmation dialog
  - Loading state

#### 5.2.10 AssignmentCardComponent
- **Purpose:** Display user's Secret Santa assignment (post-draw)
- **Inputs:** Recipient data, budget, wishlist
- **Visual Elements:**
  - Heading: "You're buying a gift for:"
  - Recipient name (prominent)
  - Budget (prominently displayed with PLN)
  - Wishlist content or empty state message
  - Card styling with visual hierarchy

#### 5.2.11 GiftSuggestionsComponent
- **Purpose:** Display AI-generated gift suggestions
- **Inputs:** Suggestions array, loading state
- **Outputs:** Generate suggestions event, cancel event
- **Visual Elements:**
  - "Generate Gift Ideas" button (if not generated)
  - Loading state with cancellation option
  - List of suggestions (3-5 cards):
    - Category
    - Item name
    - Description
    - Approximate price in PLN
  - Error state (if AI service fails)

---

### 5.3 Shared/Common Components

#### 5.3.1 LoadingSpinnerComponent
- **Purpose:** Display loading indicator
- **Inputs:** Message (optional)
- **Visual Elements:**
  - Spinner icon
  - Optional loading message

#### 5.3.2 SkeletonLoaderComponent
- **Purpose:** Display skeleton screens during initial loads
- **Inputs:** Type (card, list, etc.)
- **Visual Elements:**
  - Animated placeholder shapes

#### 5.3.3 ToastComponent
- **Purpose:** Display temporary notification messages
- **Inputs:** Message, type (success, error, info)
- **Responsibilities:**
  - Auto-dismiss after timeout
  - Manual dismiss option
  - Stacking multiple toasts
- **Visual Elements:**
  - Colored background based on type
  - Message text
  - Close button
  - Icon based on type

#### 5.3.4 ConfirmDialogComponent
- **Purpose:** Display confirmation dialogs
- **Inputs:** Title, message, confirm button text, cancel button text
- **Outputs:** Confirm event, cancel event
- **Visual Elements:**
  - Modal overlay
  - Dialog box
  - Title
  - Message
  - Confirm button (primary)
  - Cancel button (secondary)
  - Close icon

#### 5.3.5 ErrorAlertComponent
- **Purpose:** Display error messages (inline or banner)
- **Inputs:** Error message, type (inline, banner)
- **Visual Elements:**
  - Error icon
  - Error message text
  - Colored background/border
  - Optional dismiss button

#### 5.3.6 CopyToClipboardButtonComponent
- **Purpose:** Copy text to clipboard
- **Inputs:** Text to copy
- **Outputs:** Copy event
- **Visual Elements:**
  - Button with copy icon
  - Success feedback (icon change or tooltip)

---

### 5.4 Services (State Management)

#### 5.4.1 AuthService
- **Purpose:** Manage authentication state
- **State (Signals):**
  - currentUser (user data or null)
  - isAuthenticated (boolean)
  - token (JWT token)
- **Methods:**
  - login(email, password)
  - register(userData)
  - logout()
  - refreshToken() (future)
  - storeReturnUrl(url)
  - getReturnUrl()

#### 5.4.2 GroupService
- **Purpose:** Manage group data and operations
- **State (Signals):**
  - activeGroup (current group data or null)
  - groups (array of user's groups)
- **Methods:**
  - fetchGroups()
  - fetchGroupDetails(groupId)
  - createGroup(name)
  - updateGroup(groupId, data)
  - executeDraws(groupId, budget)
  - removeParticipant(groupId, userId)
  - refreshGroupData(groupId)

#### 5.4.3 InvitationService
- **Purpose:** Manage invitation flow
- **Methods:**
  - fetchInvitationDetails(token)
  - acceptInvitation(token, budgetSuggestion)
  - storeInvitationToken(token)
  - getInvitationToken()

#### 5.4.4 WishlistService
- **Purpose:** Manage wishlist operations
- **Methods:**
  - fetchMyWishlist(groupId)
  - updateMyWishlist(groupId, content)
  - fetchRecipientWishlist(groupId)

#### 5.4.5 BudgetService
- **Purpose:** Manage budget operations
- **Methods:**
  - fetchBudgetSuggestions(groupId)
  - updateMyBudgetSuggestion(groupId, amount)

#### 5.4.6 ExclusionRuleService
- **Purpose:** Manage exclusion rules
- **Methods:**
  - fetchExclusionRules(groupId)
  - createExclusionRule(groupId, userId1, userId2)
  - deleteExclusionRule(groupId, ruleId)

#### 5.4.7 DrawValidationService
- **Purpose:** Validate draw feasibility
- **Methods:**
  - validateDraw(groupId)

#### 5.4.8 AssignmentService
- **Purpose:** Manage assignment viewing
- **Methods:**
  - fetchMyAssignment(groupId)

#### 5.4.9 GiftSuggestionService
- **Purpose:** Manage AI gift suggestions
- **Methods:**
  - generateGiftSuggestions(groupId)
  - cancelSuggestionRequest()

#### 5.4.10 ErrorHandlingService
- **Purpose:** Centralized error handling and mapping
- **Methods:**
  - mapApiErrors(error)
  - handleGlobalError(error)
  - displayToast(message, type)

---

### 5.5 Interceptors

#### 5.5.1 AuthInterceptor
- **Purpose:** Attach JWT token to all API requests
- **Responsibilities:**
  - Add Authorization header with Bearer token
  - Skip for public endpoints (login, register, invitation details)

#### 5.5.2 ErrorInterceptor
- **Purpose:** Handle API errors globally
- **Responsibilities:**
  - Catch 401 errors → trigger logout and redirect
  - Map other errors to user-friendly messages
  - Pass errors to ErrorHandlingService

#### 5.5.3 CacheInterceptor
- **Purpose:** Manage HTTP caching
- **Responsibilities:**
  - Respect cache headers from API
  - Invalidate cache on mutations

---

### 5.6 Guards

#### 5.6.1 AuthGuard
- **Purpose:** Protect authenticated routes
- **Responsibilities:**
  - Check if user is authenticated
  - Redirect to login if not
  - Store return URL

#### 5.6.2 PostDrawGuard
- **Purpose:** Protect post-draw routes
- **Responsibilities:**
  - Check if draw has been completed for group
  - Redirect to group details with message if not
  - Use drawCompleted flag from API

---

## 6. State Management Strategy

### 6.1 Signal-Based State

**Service-Level Signals (Shared State):**
- Used for: Authentication state, active group data, user profile
- Pattern: Services expose signals, components consume with computed()
- Example: `authService.isAuthenticated()` consumed in components

**Component-Level Signals (Local State):**
- Used for: Form inputs, toggle states, local UI state
- Pattern: Defined within component, not shared
- Example: `showPassword = signal(false)`

### 6.2 RxJS Usage

**Limited to:**
- HTTP requests (return Observables)
- Event streams that require composition
- Avoid over-using RxJS where signals suffice

**Pattern:**
- Convert Observables to signals when needed
- Use `toSignal()` for automatic signal conversion

---

## 7. Error Handling and User Feedback

### 7.1 Error Display Patterns

**Inline Errors (Forms):**
- Field-level validation errors
- Displayed below/next to field
- Red text with error icon

**Toast Notifications (Operations):**
- Success: "Group created successfully"
- Error: "Failed to create group. Please try again."
- Auto-dismiss after 5 seconds
- Manual dismiss option
- Positioned top-right (desktop) or top (mobile)

**Alert Banners (Resource-Level):**
- Draw validation errors
- Group-level warnings
- Positioned at top of content area
- Color-coded (red for error, yellow for warning)
- Dismissible

**Full Error Pages:**
- 404 Not Found
- Friendly messaging with navigation options

### 7.2 Loading States

**Spinners (Short Operations <2s):**
- Form submissions
- Draw execution
- Button loading states
- Small, inline spinners

**Skeleton Screens (Initial Loads):**
- Groups list initial load
- Group details initial load
- Animated placeholder shapes matching final content

**Progress Indicators (Long Operations):**
- AI gift suggestion generation
- Large spinner with message

---

## 9. Security Considerations

### 9.1 Input Sanitization

- Angular built-in XSS protection (automatic template sanitization)
- No innerHTML usage
- Server-side validation for all inputs

### 9.2 Authentication & Authorization

- JWT token stored securely (httpOnly cookie or secure storage)
- Token included in all authenticated requests
- Route guards prevent unauthorized access
- Session expiry handled gracefully

### 9.3 Privacy & Anonymity

- Email addresses never displayed in participant lists
- Budget suggestions fully anonymous
- Only user's own assignment visible post-draw
- GDPR consent required during registration

### 9.4 Rate Limiting

- Client-side feedback for 429 errors

---

## 10. Responsive Design Strategy

### 10.1 Breakpoints (Tailwind Defaults)

- **sm:** 640px
- **md:** 768px
- **lg:** 1024px
- **xl:** 1280px

### 10.2 Mobile-First Approach

**Mobile (<640px):**
- Full-width cards
- Stacked vertical layout
- Hamburger menu
- Touch-friendly targets (min 44x44px)
- Single column forms

**Tablet (640px - 1024px):**
- Max-width containers
- 2-column layouts where appropriate
- Expanded navigation or hamburger based on preference

**Desktop (>1024px):**
- Max-width containers (1200px)
- Multi-column layouts
- Expanded horizontal navigation
- Grid layouts for cards

### 10.3 Touch Interactions

- Minimum touch target size: 44x44px
- Adequate spacing between interactive elements
- No hover-dependent functionality
- Swipe gestures not implemented in MVP

---

## 11. Performance Considerations

### 11.1 Change Detection

- OnPush strategy throughout app
- Signals trigger efficient updates
- Avoid unnecessary re-renders

### 11.2 Lazy Loading

- Not needed for MVP (small app size)
- Future: Lazy load feature modules

### 11.3 Bundle Size

- Tree-shaking enabled
- Minimal dependencies
- Tailwind CSS purged in production

### 11.4 User Experience Optimizations

- Manual refresh with staleness indicator
- Debounced form validation (300ms)
- Skeleton screens for perceived performance
- Loading states prevent user confusion

---

## 12. Internationalization (i18n)

### 12.1 MVP Approach

- Polish-only for MVP
- Structure code to be i18n-ready
- Use date-fns with Polish locale
- All UI text in Polish

---

## 13. Browser Compatibility

**Minimum Supported Versions:**
- Chrome: Last 2 versions
- Firefox: Last 2 versions
- Safari: Last 2 versions
- Edge: Last 2 versions

**Testing Strategy:**
- Primary testing on Chrome (desktop and mobile)
- Secondary testing on Firefox and Safari
- Mobile testing on iOS Safari and Chrome Android

---

## 14. Implementation Priorities

### 14.1 Phase 1 (Core MVP)

1. Authentication (Login, Register)
2. Groups List View
3. Group Create Flow
4. Invitation Flow
5. Group Details (Pre-Draw)
6. Participant Management
7. Wishlist Management
8. Budget Management
9. Exclusion Rules
10. Draw Validation
11. Draw Execution
12. Assignment View (Post-Draw)

### 14.2 Phase 2 (Enhanced Features)

1. AI Gift Suggestions
2. Profile Page
3. Error Pages (404, 500)
4. Enhanced Loading States
5. Toast Notifications
6. Polish Language Content

### 14.3 Phase 3 (Polish & Refinement)

1. Copy refinement with native Polish speaker

---

## 15. Open Questions and Future Considerations

### 15.1 Future Enhancements (Post-MVP)

1. Edit group name/description
2. Dashboard with statistics
3. Group templates
4. Multiple group organizers
5. Anonymous chat between Santa and recipient
6. Gift purchased status tracking
7. Post-event feedback
8. Multi-language support
9. Dark mode
10. Push notifications
11. Calendar integration for important dates
12. Gift idea wishlist links
13. Budget tracking per participant
14. Group archiving
15. Export group data

---

## 16. Component Dependency Map

```
App
├── Navigation (authenticated state)
├── Router Outlet
    ├── Public Routes
    │   ├── LoginComponent
    │   ├── RegistrationComponent
    │   └── InvitationComponent (public)
    │       └── BudgetSuggestionComponent
    ├── Authenticated Routes
    │   ├── GroupsListComponent
    │   │   ├── SkeletonLoaderComponent
    │   │   └── GroupCardComponent (repeated)
    │   ├── GroupCreateComponent
    │   │   └── LoadingSpinnerComponent
    │   ├── GroupDetailsComponent
    │   │   ├── GroupInfoCardComponent
    │   │   ├── InvitationLinkCardComponent (organizer, pre-draw)
    │   │   │   └── CopyToClipboardButtonComponent
    │   │   ├── ParticipantListComponent
    │   │   ├── BudgetManagementComponent (organizer, pre-draw)
    │   │   ├── ExclusionRulesComponent (organizer, pre-draw)
    │   │   ├── DrawValidationComponent (organizer, pre-draw)
    │   │   ├── DrawExecutionComponent (organizer, pre-draw)
    │   │   │   ├── ConfirmDialogComponent
    │   │   │   └── LoadingSpinnerComponent
    │   │   ├── WishlistEditorComponent
    │   │   ├── BudgetSuggestionComponent
    │   │   ├── AssignmentCardComponent (post-draw)
    │   │   └── GiftSuggestionsComponent (post-draw)
    │   │       └── LoadingSpinnerComponent
    │   ├── ProfileComponent
    │   └── Error Pages
    │       ├── NotFoundComponent
    │       └── ServerErrorComponent
    └── Global Components
        ├── ToastComponent
        └── LoadingSpinnerComponent
```

---

## 17. Data Flow Diagram

```
User Action → Component → Service → HTTP Interceptor → API
                ↓                            ↓
            Update UI ← Parse Response ← Response
                ↓
        Signal Update → Computed Values → Template Re-render
```

**Example: Execute Draw Flow**

```
1. User clicks "Execute Draw"
2. DrawExecutionComponent validates budget input
3. DrawExecutionComponent shows confirmation dialog
4. User confirms
5. DrawExecutionComponent emits event to GroupDetailsComponent
6. GroupDetailsComponent calls GroupService.executeDraw(groupId, budget)
7. GroupService makes HTTP POST to /api/groups/{groupId}/draw
8. AuthInterceptor adds JWT token
9. API processes draw
10. Response returns with draw results
11. GroupService updates activeGroup signal
12. GroupDetailsComponent computed values update
13. Template re-renders with post-draw view
14. Toast notification shown: "Draw completed!"
15. User sees assignment immediately
```

---

## 18. Form Validation Strategy

### 18.1 Client-Side Validation

**Timing:**
- On-blur for individual fields
- On-submit for all fields

**Validation Rules:**
- Email: Required, valid format
- Password: Required, min 8 chars, complexity requirements
- First Name: Required, max 100 chars
- Last Name: Required, max 100 chars
- Group Name: Required, min 3 chars, max 200 chars
- Budget: Positive number, max 2 decimal places
- Wishlist: Max 1000 chars (no visible counter)

**Display:**
- Inline error messages below fields
- Red border on invalid fields
- Error icon
- Form-level summary for multiple errors

### 18.2 Server-Side Validation

**API Error Mapping:**
- 400 Bad Request with field-level details
- ErrorHandlingService maps to form fields
- Display inline errors from API response

**Pattern:**
```
API Error Response:
{
  "error": "ValidationError",
  "details": {
    "email": ["Email already exists"],
    "password": ["Must contain uppercase letter"]
  }
}

Mapped to Form:
- email field: "Email already exists"
- password field: "Must contain uppercase letter"
```

---

## 19. Conclusion

This UI architecture provides a comprehensive blueprint for building the SantaVibe Secret Santa application. The design prioritizes:

- **Simplicity:** MVP-focused, avoiding unnecessary complexity
- **User Experience:** Clear flows, helpful feedback, intuitive navigation
- **Security:** Authentication, authorization
- **Responsiveness:** Mobile-first design, touch-friendly interactions
- **Performance:** OnPush change detection, efficient state management
- **Maintainability:** Signals-based state, component architecture, service abstraction

The architecture is designed to be compatible with the API plan, aligned with the PRD requirements, and informed by the planning session decisions. It provides a solid foundation for frontend development while remaining flexible for future enhancements.
