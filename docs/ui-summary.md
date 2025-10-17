Conversation Summary
Decisions
Landing Page After Login: Groups list view (/groups) as main landing page showing both active and completed groups
Group Creation Flow: Dedicated route (/groups/create) instead of modal dialog
Invitation Link Sharing: Copy-to-clipboard only (no social sharing in MVP)
Group View Architecture: Single route (/groups/:groupId) with conditional rendering based on isOrganizer flag
Budget Suggestion in Join Flow: Optional field in join invitation flow, can be updated later
Exclusion Rules UI: Simple dropdown-based form with list display, no visual graph
Draw Validation Display: Real-time validation feedback with disabled button when validation fails
Wishlist Format: Plain text only (<textarea>), no rich text
AI Suggestions Presentation: Expandable section within assignment view
Email Notification Preferences: Automatically enabled, no UI controls in MVP
Draw Execution UX: Loading state → redirect → success toast
Route Guards: Implement guards using drawCompleted flag to protect post-draw routes
Session Expiration: Forced logout on token expiration with redirect
Participant List Display: No pagination, simple scrolling for MVP
Error Communication Strategy: Inline errors for forms, toasts for operations, alert banners for resource-level errors, full pages for 404/500
UI Update Pattern: Wait for API confirmation for all operations in MVP (no optimistic updates)
State Management: Service-level signals for shared state, component-level for local UI state
Mobile Navigation: Responsive approach with hamburger menu on mobile, expanded navigation on desktop
Concurrent Modification Handling: Display error messages and refresh view data
Invitation Flow: Show public group details before requiring authentication
Real-time Updates: Static data on page load with manual refresh options (no WebSockets/polling)
Wishlist Character Limit: 1000 characters without visible counter
Onboarding: Rely on intuitive UI with tooltips/helper text, no full onboarding tour
Anonymity Messaging: No special emphasis banner in MVP
Participant Visibility: Show full participant list (names only) to all participants
Group List View: Single list without sorting/filtering, click navigates to group details page with all functionalities
Caching: Simplest HTTP caching approach respecting API headers
Form Validation Timing: Simple approach (on-blur for fields, all errors on submit)
Draw Preview: Summary section showing participant count, exclusion rules, budget range, validation status
Unsaved Changes Warning: Browser-native confirmation dialog for critical forms
Auto-refresh: No automatic refresh, manual refresh button with "last updated" indicator
Loading States: Spinners for short operations, skeleton screens for list data
Offline Support: Require active internet connection, display retry option on network errors
AI Suggestions Loading: Dedicated loading state with cancellation option
Password Reset: Email-only as per API plan
Budget Display to Participants: Only own suggestion (before draw) and final budget (after draw), no aggregated information
Language Support: Polish-only for MVP with i18n-ready structure
Date/Time Display: Polish format (DD.MM.YYYY) with relative time, using date-fns library
Analytics: No analytics in MVP
Browser Navigation: Standard Angular routing with session-persisted form data
Group Details Organization: Single scrollable page with card-based sections
Insufficient Participants: Validation error with disabled draw button
Leave Group: Not supported in MVP (organizer must remove)
Invitation Route: Dedicated public route /invite/{token}
Email Visibility: Only names in participant list, emails removed from API response
Wishlist Email Delay: No need to communicate 1-hour delay to users
Edit Group Details: Allow editing before draw only (check API for endpoints)
Auto-join After Registration: Seamless flow with invitation token stored during registration
Draw Confirmation: Required confirmation dialog warning of irreversible action
API Validation Error Mapping: Standard service parsing field-level errors with inline display
Organizer Display: Show organizer name clearly in group details
Empty Wishlist Handling: Friendly message suggesting email notification
Input Sanitization: Rely on Angular's built-in XSS protection
Group Creation Success: Redirect to group details with success toast and prominent invitation link
Email Delivery Status: No UI indicators, trust email service
AI Suggestions Persistence: Generate fresh on-demand, no caching in MVP
Profile Page: Minimal page with name, email (read-only), change password button
Timezone Handling: Europe/Warsaw timezone for all displays
Image Upload: Not supported in MVP
Final Budget Display: Prominently featured in assignment view with recipient name
Matched Recommendations
Groups list as landing page - Simple view showing active and completed groups without dashboard complexity
Dedicated group creation route - Better mobile experience and future extensibility
Copy-to-clipboard for invitations - Reduces complexity and third-party dependencies
Single route with conditional rendering - Reduces code duplication while maintaining organizer/participant distinction
Budget suggestion optional in join flow - Can be updated later via dedicated section
Simple dropdown-based exclusion rules - Avoids complex graph UI, suitable for MVP
Real-time draw validation - Provides immediate feedback with clear error messages
Plain text wishlist - Reduces security concerns and implementation time
AI suggestions as expandable section - Keeps context visible, avoids navigation disruption
Auto-enabled email notifications - No configuration UI needed, aligns with PRD
Loading state for draw execution - Appropriate feedback for critical operation
Route guards for post-draw features - Prevents accessing features before draw completion
Forced logout on session expiration - Clear user experience with re-authentication flow
No pagination for participants - Appropriate for expected group sizes (5-30 people)
Combined error strategy - Context-appropriate error communication
Wait for API confirmation - Ensures data consistency for MVP scope
Service-level signals for shared state - Aligns with Angular 20 architecture
Responsive navigation approach - Familiar UX patterns across devices
Simple concurrent modification handling - Error messages sufficient for MVP
Public invitation details view - Reduces friction before authentication
Static data with manual refresh - Avoids real-time complexity while providing control
1000 character limit for wishlist - Prevents database issues without visible counter
Intuitive UI with helper text - Defers complex onboarding based on user feedback needs
Show participant list to all - Transparency about group composition
Single list view for groups - Unified view with visual state indicators
Simple HTTP caching - Standard approach without complex client-side strategies
On-blur validation - Balances UX with immediate feedback
Summary section before draw - Increases organizer confidence and reduces errors
Browser-native unsaved changes dialog - Prevents accidental data loss
Manual refresh with staleness indicator - User control without unnecessary API calls
Context-appropriate loading states - Proper feedback based on operation type
Require internet connection - Avoids offline support complexity
Dedicated AI loading with cancellation - Handles potentially long AI operations
Email-only password reset - Aligns with API plan and MVP scope
Individual budget suggestion visibility - Maintains privacy while providing guidance
Polish-only with i18n structure - MVP focus with future expansion readiness
Polish date format with date-fns - Consistent localized formatting
No analytics in MVP - Focus on core functionality
Standard Angular routing - Respects browser history with session persistence
Single scrollable page with cards - Mobile-friendly, simpler than tabs
Validation error for insufficient participants - Clear feedback with disabled action
No leave group endpoint - Organizer-managed participant removal
Dedicated public invitation route - Seamless join experience
Names-only in participant list - Privacy-focused display
No delay communication - Backend optimization detail not relevant to users
Edit before draw only - Prevents post-draw confusion
Seamless auto-join flow - Reduces friction for new users
Confirmation dialog for draw - Prevents accidental execution
Standard error mapping service - Consistent field-level error display
Display organizer name - Transparency and contact clarity
Empty wishlist message - Manages expectations
Angular built-in XSS protection - Sufficient for plain text inputs
Group creation success flow - Clear next steps with prominent invitation link
Trust email service - Reduces UI complexity for auxiliary feature
On-demand AI generation - Simplifies MVP implementation
Minimal profile page - Essential account management only
Single timezone approach - Appropriate for Poland-focused MVP
No image uploads - Significant complexity reduction
Prominent budget display - Ensures clear budget constraint visibility
UI Architecture Planning Summary
Main UI Architecture Requirements
Framework & Technology Stack:
Angular 20 with standalone components (no NgModules)
State management: Signals (component & service level) + RxJS for async operations
Styling: Tailwind CSS 4.1 + Flowbite 3.1 components
UI Components: Angular CDK for dialogs/overlays
Change Detection: OnPush strategy throughout
Dependency Injection: inject() function pattern
Control Flow: Native Angular syntax (@if, @for, @switch)
Core Architecture Principles:
Mobile-first responsive design
Polish language only (i18n-ready structure)
Europe/Warsaw timezone for all dates
PLN currency (Polish market focus)
Simplicity over feature richness for MVP
Key Views, Screens, and User Flows
Public Routes (Unauthenticated):
/login - Login page
/register - Registration with GDPR consent
/invite/{token} - Public invitation details view
Displays group name, organizer, participant count
CTAs: "Login to Join" or "Register to Join"
After auth: auto-triggers join flow with token
Authenticated Routes:
/groups - Main landing page (post-login default)
List of all groups (participant in)
Visual badges: "Pending Draw" vs "Draw Complete"
No sorting/filtering in MVP
Click navigates to group details
/groups/create - Group creation
Simple form: group name field
On success: redirect to group details with toast
Invitation link prominently displayed
/groups/:groupId - Group details (main hub)
Single scrollable page with card-based sections
Conditional rendering based on:
User role (isOrganizer flag)
Draw state (drawCompleted flag)
Pre-Draw Sections:
Group info (name, organizer, participant count)
Invitation link (organizer only, copy-to-clipboard)
Participant list (names only, visible to all)
Budget management (organizer sees suggestions, sets final budget during draw)
Exclusion rules (organizer only, dropdown form + list)
Draw validation summary (organizer only)
"Execute Draw" button (organizer only, disabled if validation fails)
My wishlist (all participants)
My budget suggestion (all participants, optional)
Post-Draw Sections:
Group summary (read-only)
My assignment:
Recipient name
Final budget (prominently displayed)
Recipient's wishlist (if provided)
AI gift suggestions (expandable section with loading state)
My wishlist (editable, triggers email to Santa after 1hr)
/profile - User profile (minimal)
Name, email (read-only)
"Change Password" button → password reset flow
Route Guards:
Protect /groups/:groupId/assignment and AI suggestions before draw
Redirect to group details with informative message if accessed pre-draw
Guard implementation uses drawCompleted flag from API
Navigation Structure:
Mobile: Top bar with hamburger menu
Desktop: Expanded horizontal navigation
Menu items: My Groups, Profile, Logout
API Integration and State Management Strategy
HTTP Client Configuration:
JWT Bearer token authentication
Interceptor for auth headers
Interceptor for HTTP cache control
Error interceptor for 401 → forced logout flow
State Management Patterns:
Service-Level Signals (Shared State):
AuthService: Current user data, token, login state
GroupService: Active group data
Pattern: Services expose signals, components consume with computed()
Component-Level Signals (Local State):
Form inputs
Toggle states
Local UI state
RxJS for Async Operations Only:
HTTP requests
Events that require composition
Avoid over-using RxJS where signals suffice
API Communication Strategy:
No Optimistic Updates in MVP - Always wait for API confirmation
Error Handling:
Inline validation errors for forms (field-level from API 400 responses)
Toast notifications for operation success/failure
Alert banners for resource-level errors (e.g., draw validation)
Full error pages for 404/500
Loading States:
Spinners: Short operations (<2s, form submissions, draw execution)
Skeleton screens: Initial page loads with list data
Caching:
Simple HTTP cache headers from API
Invalidate on mutations
No complex client-side caching
Session Management:
Token expiry (24 hours) → forced logout with redirect
Store intended route, redirect after re-auth
Browser-native confirmation for unsaved changes in critical forms
Form Handling:
Validation: On-blur for fields, all errors on submit
Standard Angular form validation with reactive forms
Service to map API validation errors to form fields
Max 1000 characters for wishlist (no visible counter)
Responsiveness, Accessibility, and Security Considerations
Responsiveness:
Mobile-first Tailwind approach
Breakpoints: Tailwind defaults (sm, md, lg, xl)
Navigation: Hamburger menu (mobile) → expanded nav (desktop)
Forms: Stack vertically on mobile, grid on desktop
Cards/sections: Full width mobile, max-width containers desktop
Touch-friendly targets (min 44x44px)
Accessibility (Basic MVP Level):
Semantic HTML5 elements
ARIA labels where needed (especially for icon-only buttons)
Keyboard navigation support
Focus management for modals/dialogs
Color contrast compliance (Flowbite provides accessible defaults)
Screen reader friendly error messages
Form labels properly associated
Security:
Input Sanitization: Angular built-in XSS protection (automatic template sanitization)
Authentication: JWT token stored securely
Authorization: Route guards prevent unauthorized access
HTTPS Only: Production deployment
Email Display: Never show emails in participant lists (API removes them)
Anonymity Enforcement: UI only shows user's own assignment post-draw
GDPR: Consent checkbox during registration (required, with linked terms)
Privacy Principles:
Budget suggestions: Anonymous display (no user attribution)
Assignments: Only user's own assignment visible
Wishlist: Only visible to assigned Santa post-draw
No reverse lookup capability in UI
Data Flow Patterns
Group Creation Flow:
User navigates to /groups/create
Submits form → POST /api/groups
On success: redirect to /groups/:groupId
Display toast: "Group created! Copy invitation link to invite participants"
Invitation link in prominent card
Join Group Flow:
User clicks invitation link → /invite/{token}
If unauthenticated: prompt login/register
After auth: fetch invitation details GET /api/invitations/{token}
Display group info + budget suggestion field (optional)
Submit → POST /api/invitations/{token}/accept
On success: redirect to /groups/:groupId
Draw Execution Flow:
Organizer on group details page
Views draw validation summary
Sets final budget in form
Clicks "Execute Draw" → confirmation dialog
Confirms → POST /api/groups/{groupId}/draw with budget
Loading state (spinner + message)
On success: redirect to group details (now post-draw view)
Display toast: "Draw completed! Check your assignment"
User sees their assignment immediately
AI Gift Suggestions Flow:
User on assignment view (post-draw)
Clicks "Generate Gift Ideas" button
Loading state: "Generating personalized gift ideas..." (with cancel option)
POST /api/groups/{groupId}/my-assignment/gift-suggestions
Display 3-5 suggestions in card format
Error handling: User-friendly message if AI service fails
Wishlist Update Flow:
User edits wishlist in group details
Saves → PUT /api/groups/{groupId}/participants/me/wishlist
Success toast: "Wishlist saved"
If post-draw: Backend schedules email to Santa (1hr delay, transparent to user)
Component Architecture (Proposed)
Smart Components (Container):
GroupsListComponent - /groups
GroupDetailsComponent - /groups/:groupId
GroupCreateComponent - /groups/create
InvitationComponent - /invite/:token
ProfileComponent - /profile
Presentational Components:
GroupCardComponent - Group list item
ParticipantListComponent - Display participants
BudgetSuggestionComponent - Budget suggestion input/display
ExclusionRulesComponent - Exclusion rules management
DrawValidationComponent - Validation summary
AssignmentCardComponent - Assignment display
WishlistEditorComponent - Wishlist textarea
GiftSuggestionsComponent - AI suggestions display
LoadingSpinnerComponent - Reusable spinner
SkeletonLoaderComponent - Skeleton screens
Shared/Common Components:
ToastComponent - Toast notifications
ConfirmDialogComponent - Confirmation dialogs
ErrorAlertComponent - Alert banners
CopyToClipboardButtonComponent - Copy link button
Date/Time Handling
Library: date-fns with Polish locale
Format: DD.MM.YYYY for dates
Relative time for recent activities: "Created 2 days ago", "Draw completed today"
Timezone: Europe/Warsaw (all dates displayed in Polish timezone)
Performance Considerations
MVP Optimizations:
OnPush change detection strategy
Lazy loading: Not needed for MVP (small app size)
Image optimization: No images in MVP
Bundle size: Minimal with tree-shaking
User Experience Optimizations:
Manual refresh with "last updated" indicator (no auto-polling)
Retry button for failed network requests
Loading states prevent user confusion
Debounced form validation (300ms for real-time fields)
Error Handling Strategy
Network Errors:
Offline/connectivity: "Unable to connect. Please check your internet connection. [Retry]"
Timeout: "Request timed out. Please try again."
API Errors:
400 Validation: Inline field errors + form-level summary
401 Unauthorized: Redirect to login with return URL
403 Forbidden: "You don't have permission to perform this action"
404 Not Found: "Group not found" or dedicated 404 page
409 Conflict: "You're already a participant in this group"
410 Gone: "This invitation has expired"
429 Rate Limit: "Too many requests. Please try again in X seconds"
500 Server Error: "Something went wrong. Please try again later"
503 Service Unavailable: "Service temporarily unavailable. Please try again later"
Business Logic Errors:
Draw validation failures: Alert banner with specific errors list
Insufficient participants: "At least 3 participants required for Secret Santa draw"
Invalid exclusion rules: "This exclusion rule would make a valid draw impossible"
Unresolved Issues
Group Editing Endpoint: Need to verify if PUT /api/groups/{groupId} exists in API for editing group name/description before draw. If not, this feature should be added to API plan or explicitly marked as not supported in MVP.
Email Field in API Response: Decision to "remove email from API response" for participant lists needs to be confirmed with backend team and reflected in API plan documentation update.
AI Suggestions Cancellation: Technical implementation of cancellation for AI requests needs clarification - whether it's client-side cancellation only or includes backend request cancellation.
Browser History Management in Multi-Step Flows: Need to define exactly which form data persists in session storage during group creation flow when users navigate back. Scope and implementation details need specification.
Refresh Token Strategy: While confirmed "not in MVP", the forced logout experience after 24 hours may need user testing to validate UX acceptability for MVP users.
Empty Wishlist Guidance: The exact message shown when recipient has no wishlist needs copywriting: "Your giftee hasn't added their wishlist yet - you'll be notified by email when they do" vs alternative messaging.
Concurrent Modification Edge Cases: Specific error messages for each concurrent modification scenario (e.g., "This participant was removed by the organizer" vs "This group draw was just completed") need definition.
Rate Limiting UI Feedback: How to display rate limit errors (429) for AI suggestions specifically - should it include countdown timer or just retry message?
Browser Compatibility: No explicit browser compatibility requirements defined - need minimum supported browser versions for Angular 20 + Tailwind CSS 4.1.
Polish Language Content: Actual Polish translations and copywriting for all UI text not defined - need content strategy and possibly native speaker review.