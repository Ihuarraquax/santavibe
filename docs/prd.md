# Product Requirements Document (PRD) - SantaVibe

## 1. Product Overview

SantaVibe is a web-based Secret Santa application designed to simplify holiday gift exchanges within groups of friends or family. The application addresses the financial burden of buying gifts for everyone by organizing a draw where each participant becomes a "Santa" for exactly one other person.

Target Market:
- Geographic region: Poland
- Currency: PLN (Polish Złoty)
- Platform: Web application (desktop and mobile browsers)
- Target users: Groups of friends, families, workplace teams organizing holiday gift exchanges

Core Value Proposition:
- Eliminates the cost of buying multiple gifts
- Ensures fair and anonymous gift exchange
- Provides AI-powered gift suggestions
- Simplifies organization and coordination

Key Differentiators:
- Anonymous draw (even organizers don't know the pairings)
- Flexible exclusion rules (prevent couples or family members from drawing each other)
- AI integration for personalized gift suggestions based on wishlists
- Budget management with anonymous participant suggestions

## 2. User Problem

During holiday seasons, gift-giving traditions can become financially overwhelming when individuals feel obligated to buy presents for everyone in their social circle or family. This creates several challenges:

Primary Problems:
- Financial burden: Purchasing multiple gifts can be expensive and may strain personal budgets
- Decision paralysis: Choosing appropriate gifts for many people is time-consuming
- Inequality: Different financial capabilities within a group can create uncomfortable situations
- Coordination overhead: Organizing fair gift exchanges manually is complex and prone to errors

User Pain Points:
- Not knowing what others want or need
- Spending more money than planned
- Risk of duplicate gifts or inappropriate selections
- Difficulty maintaining anonymity in manual draws
- Complex coordination when exclusion rules are needed (e.g., married couples shouldn't draw each other)

Expected Outcomes:
- Each participant spends only on one gift within an agreed budget
- Fair and anonymous distribution of gift-giving responsibilities
- Clear guidance on what to buy through wishlists
- Stress-free organization and coordination

## 3. Functional Requirements

### 3.1 Authentication and Account Management

FR-001: Email and Password Registration
- Users must be able to register using email address and password
- Password must meet minimum security requirements
- Email verification is not required for MVP
- GDPR compliance checkbox with link to simple terms must be presented during registration
- System validates email format and password strength

FR-002: Login System
- Users can log in using registered email and password
- Session management maintains user authentication state
- Appropriate error messages for invalid credentials

FR-003: User Profile
- User profile contains: first name, last name, email address
- Profile information is used to identify users within groups
- Users can view and edit their profile information

### 3.2 Wishlist Management

FR-004: Wishlist Creation and Editing
- Users can create and edit a personal wishlist ("list do Mikołaja")
- Wishlist is a single optional text field
- Wishlist can ONLY be created and edited after the draw is completed
- Users must know the final budget before creating their wishlist to avoid unrealistic expectations
- Wishlist is visible only to the person who drew that user as their gift recipient
- No character limit specified for MVP

FR-005: Wishlist Update Notifications
- When a user updates their wishlist, their assigned "Santa" receives an email notification
- Notifications are delayed by approximately 1 hour to prevent spam from multiple edits
- Email notification is brief and encourages logging into the app to see updates
- Email does not contain the wishlist content itself

### 3.3 Group Creation and Management (Organizer Functions)

FR-006: Group Creation
- Any authenticated user can create a group and become its organizer
- Group requires a name
- Organizer is automatically added as a participant in the group
- System generates a unique shareable invitation link upon group creation

FR-007: Invitation System
- Organizer receives a unique, shareable URL for inviting participants
- Link remains valid until the draw is performed
- No limit on number of participants for MVP
- Organizer can share the link through any communication channel

FR-008: Participant Management
- Organizer can view a list of all participants who joined via the invitation link
- Organizer can manually remove participants before the draw is performed
- Removal is not possible after the draw
- System displays participant names and whether they've submitted budget suggestions

FR-009: Budget Suggestions and Setting
- When joining a group, each participant provides an anonymous budget suggestion
- Organizer can view an anonymous, sorted list of all budget suggestions
- Organizer sets a final, binding budget for the entire group
- Final budget applies to all participants equally

FR-010: Exclusion Rules Definition
- Organizer can define pairs of participants who cannot draw each other
- Common use cases: married couples, parent-child relationships, roommates
- System validates exclusion rules in real-time
- System alerts organizer if rules make a valid draw impossible
- Exclusion rules are applied bidirectionally (if A cannot draw B, then B cannot draw A)

FR-011: Draw Initiation
- Organizer can trigger the draw using a clearly labeled action button
- Draw action is irreversible and permanent
- UI clearly communicates the permanent nature of this action
- Confirmation dialog required before executing the draw
- Draw cannot be performed if validation rules fail

### 3.4 Participant Functions

FR-012: Joining a Group
- Users can join a group by accessing the unique invitation link
- If not authenticated, users are prompted to register or log in
- Upon joining, users provide their anonymous budget suggestion
- Users can see basic group information (name, organizer, participant count)

FR-013: Pre-Draw Status View
- Before the draw, participants see a waiting status
- Interface displays: group name, participants, organizer name
- Clear messaging indicates waiting for the organizer to initiate the draw

FR-014: Post-Draw Result View
- After the draw, participants see the name of the person they are buying a gift for
- Participants can view the recipient's wishlist (if provided)
- Final budget amount is displayed
- This view becomes the primary interface after the draw

FR-015: Own Wishlist Access
- Participants can access and edit their own wishlist at any time
- Changes trigger delayed notification to their assigned Santa (after the draw)
- Clear indication if wishlist is empty or has content

### 3.5 Draw Algorithm

FR-016: Draw Execution
- Algorithm randomly assigns each participant exactly one gift recipient
- No participant can be assigned to themselves
- No 2 person circles - Person A cannot be the Santa for B if B is the Santa for A
- Each participant is assigned as a recipient to exactly one other participant
- All exclusion rules defined by the organizer must be respected
- Organizer participates as a regular participant (their assignment is also secret)
- Draw results are stored securely and permanently

FR-017: Anonymity Guarantee
- Draw results are anonymous to all participants, including the organizer
- Each participant can only see who they are buying for
- No participant can see who is buying for them
- No interface or feature allows revealing the complete draw results

### 3.6 Email Notifications

FR-018: Draw Completion Notification
- All participants receive an email when the draw is completed
- Email includes: confirmation that draw occurred, final budget amount
- Email encourages users to log in to see their assignment
- Email does not contain the assignment information itself

FR-019: Wishlist Update Notification
- When a recipient updates their wishlist after the draw, their Santa receives an email
- Notification is delayed by approximately 1 hour to batch multiple edits
- Email is brief and does not contain wishlist content
- Email encourages logging in to view updates

### 3.7 AI Integration

FR-020: AI Gift Suggestions
- Authenticated users can request AI-generated gift suggestions for their assigned recipient
- Feature is available only after the draw
- AI analyzes: recipient's wishlist text, recipient's gender (inferred from Polish first name), budget amount
- AI generates 3-5 gift suggestions with brief descriptions
- Suggestions are generated on-demand, not pre-computed
- Feature available only to the Santa, not to the recipient for their own wishlist

### 3.8 Data Protection and Compliance

FR-021: GDPR Compliance
- Simple terms of service and privacy policy available
- Checkbox consent required during registration
- Users agree to processing of: email, name, wishlist content
- Minimal data collection approach

## 4. Product Boundaries

### 4.1 Out of Scope for MVP

Mobile Applications:
- No native iOS or Android applications
- Web application must be responsive but native mobile apps are not included

Geographic and Currency Expansion:
- Application supports only Poland region
- Only PLN currency is supported
- No multi-currency or multi-language support

Post-Draw Features:
- No "gift purchased" status tracking
- No confirmation that gifts were delivered
- No post-event feedback or rating system
- No budget tracking or expense management

Advanced Communication Features:
- No anonymous chat between participants (moved to backlog)
- No direct messaging system
- No in-app notifications (email only)

Participant Withdrawal:
- No mechanism for participants to leave a group after the draw
- Participants cannot request a re-draw
- Organizer cannot cancel or redo a draw

Advanced Group Features:
- No group templates or recurring events
- No group archiving or history
- No multi-organizer support
- No sub-groups or hierarchical organization

Payment Integration:
- No payment processing
- No gift purchasing through the platform
- No affiliate links or e-commerce integration

Advanced AI Features:
- No natural language conversation with AI
- No image generation for gift ideas
- No price comparison or availability checking
- No automated purchasing suggestions

### 4.2 Technical Boundaries

Infrastructure:
- Single region deployment (Poland/EU)
- No CDN or global distribution for MVP
- Standard relational database (no complex data warehousing)

Scalability:
- Designed for groups of typical size (5-30 people)
- No optimization for very large groups (100+ participants)

Security:
- Basic authentication (email/password only)
- No two-factor authentication in MVP
- No OAuth or social login integration
- No advanced fraud detection

Performance:
- Standard web application response times
- No real-time synchronization or WebSocket features
- Email delivery through standard SMTP (no SLA guarantees)

## 5. User Stories

### 5.1 Authentication and Account Management

US-001: User Registration
Title: Register a new account

Description:
As a new user, I want to register for an account using my email and password so that I can participate in Secret Santa groups.

Acceptance Criteria:
- Given I am on the registration page
- When I enter a valid email address, a password meeting security requirements, and check the GDPR consent checkbox
- Then my account is created and I am logged in automatically
- And I receive a confirmation message
- And I can access the main application

Edge cases:
- System displays error if email is already registered
- System displays error if password doesn't meet requirements
- System prevents registration without GDPR consent checkbox
- System validates email format before submission

US-002: User Login
Title: Log into existing account

Description:
As a registered user, I want to log in with my email and password so that I can access my groups and wishlist.

Acceptance Criteria:
- Given I have a registered account
- When I enter my correct email and password on the login page
- Then I am authenticated and redirected to my dashboard
- And my session is maintained across page navigation
- And I remain logged in until I explicitly log out or session expires

Edge cases:
- System displays "Invalid credentials" for wrong email or password
- System does not reveal whether email exists or password is wrong
- System handles case-insensitive email matching

US-003: View and Edit Profile
Title: Manage personal profile information

Description:
As a logged-in user, I want to view my profile information (first name, last name, email) so that I can keep my information current.

Acceptance Criteria:
- Given I am logged in
- When I access my profile page
- Then I can see my first name, last name, and email address

US-004: Create and Edit Wishlist
Title: Manage personal wishlist

Description:
As a logged-in user, I want to create and edit my wishlist so that my Secret Santa knows what gifts I would like to receive.

Acceptance Criteria:
- Given I am logged in
- When I access my wishlist section
- Then I can enter or edit text in a single text field
- And I can save changes at any time
- And changes are confirmed with a success message
- And if I'm already assigned a Santa (after draw), they will be notified of changes after 1-hour delay

Edge cases:
- Wishlist can be left empty (it's optional)
- No character limit is enforced in MVP
- Multiple rapid edits within 1 hour result in only one notification
- Changes before the draw do not trigger notifications

US-005: Accept Terms and Conditions
Title: Consent to data processing

Description:
As a new user during registration, I must review and accept the terms and conditions to comply with GDPR requirements.

Acceptance Criteria:
- Given I am on the registration page
- When I review the registration form
- Then I see a checkbox with a link to terms of service and privacy policy
- And I can click the link to view the full terms in a new page or modal
- And I cannot complete registration without checking the consent checkbox
- And my consent is recorded with timestamp

Edge cases:
- Submit button is disabled until checkbox is checked
- Link to terms opens in a way that doesn't lose registration form data

### 5.2 Organizer Workflows

US-006: Create a Group
Title: Create a new Secret Santa group

Description:
As a logged-in user, I want to create a new Secret Santa group so that I can organize a gift exchange with my friends or family.

Acceptance Criteria:
- Given I am logged in
- When I click "Create Group" and enter a group name
- Then a new group is created with me as the organizer and participant
- And I receive a unique shareable invitation link
- And I am taken to the group management dashboard
- And I can see the invitation link prominently displayed

Edge cases:
- Group name is required and cannot be empty
- Group name can be edited later by organizer (nice to have)
- Each group has a unique identifier
- Invitation link is cryptographically secure

US-007: Share Invitation Link
Title: Invite participants to join the group

Description:
As a group organizer, I want to share a unique invitation link so that others can easily join my Secret Santa group.

Acceptance Criteria:
- Given I am the organizer of a group
- When I access the group management page
- Then I see the unique invitation link clearly displayed
- And I can copy the link to clipboard with one click
- And I receive confirmation that link was copied
- And the link remains valid until the draw is performed

Edge cases:
- Link works in incognito/private browser sessions
- Link works for users who are not yet registered
- Copy-to-clipboard works across different browsers and devices

US-008: View Participant List
Title: Monitor who has joined the group

Description:
As a group organizer, I want to see a list of all participants who have joined so that I can monitor participation and ensure everyone is included.

Acceptance Criteria:
- Given I am the organizer of a group
- When I access the group management page
- Then I see a list of all participants including their names
- And I can see when each participant joined
- And I can see a total participant count
- And I can see which participants have submitted budget suggestions

Edge cases:
- List updates when new participants join (requires page refresh in MVP)
- Organizer appears in the list as a participant
- List is sorted by join date or alphabetically

US-009: Remove Participants Before Draw
Title: Remove participants who can't participate

Description:
As a group organizer, I want to remove participants from the group before the draw so that I can handle last-minute cancellations or errors.

Acceptance Criteria:
- Given I am the organizer and the draw has not been performed
- When I click "Remove" next to a participant's name
- Then a confirmation dialog appears
- And upon confirmation, the participant is removed from the group
- And the participant count is updated
- And the removed user no longer has access to the group

Edge cases:
- Organizer cannot remove themselves
- Removal is not possible after draw is performed
- Removed participant's budget suggestion is also removed
- System alerts if removal would make draw impossible (too few participants)

US-010: View Anonymous Budget Suggestions
Title: Review participant budget preferences

Description:
As a group organizer, I want to view anonymous budget suggestions from participants so that I can set a fair budget that accommodates everyone.

Acceptance Criteria:
- Given I am the organizer and participants have joined
- When I access the budget management section
- Then I see a sorted list of all budget suggestions (lowest to highest)
- And I cannot see which participant suggested which amount (fully anonymous)
- And I can see the count of participants who submitted suggestions
- And suggestions are displayed in PLN currency

Edge cases:
- List is empty if no participants have joined yet
- List updates as new participants join (requires page refresh in MVP)
- Duplicate budget amounts are shown multiple times if suggested by multiple participants

US-011: Set Final Budget
Title: Establish binding budget for all participants

Description:
As a group organizer, I want to set a final budget amount so that all participants know how much to spend on their gifts.

Acceptance Criteria:
- Given I am the organizer
- When I enter a budget amount in PLN and save it
- Then the budget is set for the entire group
- And all participants will see this budget amount after the draw
- And I receive confirmation that the budget was set
- And I can modify the budget before the draw

Edge cases:
- Budget must be a positive number
- Budget is recommended to consider the lowest suggestion but organizer has final say
- Budget cannot be changed after the draw
- System warns if budget is significantly lower than most suggestions

US-012: Define Exclusion Rules
Title: Prevent specific pairs from drawing each other

Description:
As a group organizer, I want to define exclusion rules so that certain participants (like married couples) cannot draw each other as Secret Santa.

Acceptance Criteria:
- Given I am the organizer
- When I access the exclusion rules management section
- Then I can select two participants to create an exclusion pair
- And I can add multiple exclusion rules
- And I can remove previously created rules
- And the system validates rules in real-time
- And I receive a warning if rules make a valid draw impossible

Edge cases:
- System prevents creating duplicate exclusion rules
- Exclusion is bidirectional (A→B exclusion automatically means B→A)
- System calculates whether remaining valid pairings exist
- Clear error message explains why certain rule combinations are invalid
- Organizer can create rules for themselves as a participant

US-013: Validate Draw Feasibility
Title: Ensure draw is possible before execution

Description:
As a group organizer, I want the system to validate whether a draw is possible so that I don't initiate a draw that will fail.

Acceptance Criteria:
- Given I am the organizer
- When I attempt to trigger the draw or modify exclusion rules
- Then the system validates whether a valid draw configuration exists
- And if validation fails, I receive a clear error message explaining the issue
- And if validation passes, I receive confirmation that draw is ready
- And the "Start Draw" button is disabled if validation fails

Edge cases:
- Minimum 3 participants required for a draw
- System checks all exclusion rules create a solvable pairing problem
- Validation runs automatically when rules or participants change

US-014: Initiate Draw
Title: Execute the Secret Santa draw

Description:
As a group organizer, I want to initiate the Secret Santa draw so that all participants can learn their gift recipient assignments.

Acceptance Criteria:
- Given I am the organizer and all validations pass
- When I click the "Start Draw" button
- Then a confirmation dialog appears warning that this action is permanent
- And upon confirmation, the draw algorithm executes
- And all participants are assigned exactly one gift recipient
- And all exclusion rules are respected
- And no 2 person circles - Person A cannot be the Santa for B if B is the Santa for A
- And all participants including myself receive email notifications
- And I cannot modify participants, rules, or budget anymore

Edge cases:
- Draw button is disabled if validation fails
- Draw cannot be reversed or repeated
- System prevents concurrent draw attempts
- If draw algorithm fails (should be impossible after validation), system shows error
- Organizer also participates and cannot see all results

US-015: View Own Assignment After Draw
Title: Discover my Secret Santa assignment as organizer

Description:
As a group organizer after the draw, I want to view my own Secret Santa assignment so that I can participate like any other participant.

Acceptance Criteria:
- Given I am the organizer and the draw has been completed
- When I access the group page or my dashboard
- Then I see the name of the person I am buying a gift for
- And I can view their wishlist (if they provided one)
- And I can see the final budget amount
- And I cannot see anyone else's assignments

Edge cases:
- Organizer's view is identical to regular participants after draw
- Organizer cannot access administrative functions that would reveal assignments
- Organizer's assignment respects exclusion rules like any other participant

### 5.3 Participant Workflows

US-016: Join Group via Invitation Link
Title: Accept invitation to join Secret Santa group

Description:
As a user (registered or new), I want to join a Secret Santa group via an invitation link so that I can participate in the gift exchange.

Acceptance Criteria:
- Given I have received an invitation link
- When I click the link
- Then if I'm not logged in, I'm prompted to register or log in
- And after authentication, I see the group details (name, organizer, participants)
- And I'm prompted to enter my budget suggestion
- And upon submitting, I'm added to the group as a participant
- And I receive confirmation that I successfully joined

Edge cases:
- Link is invalid or expired: show error message
- User is already in the group: redirect to group page with message
- Link is used after draw is completed: show error that group is closed
- Budget suggestion is required to complete joining process (can be left empty)

US-017: Submit Budget Suggestion
Title: Suggest preferred budget amount

Description:
As a participant joining a group, I want to submit my budget suggestion so that the organizer can set a budget that works for everyone.

Acceptance Criteria:
- Given I am in the process of joining a group
- When I enter a budget amount in PLN
- Then my suggestion is recorded anonymously
- And I receive confirmation that I joined the group
- And my suggestion appears in the organizer's anonymous list
- And I cannot see other participants' suggestions

Edge cases:
- Budget suggestion is required (can be empty)
- Budget must be a positive number
- System does not accept decimal values (e.g., 50.50 PLN)
- Suggestion can be modified before draw (future enhancement)

US-018: View Pre-Draw Status
Title: Monitor group status before draw

Description:
As a participant before the draw, I want to see the group status so that I know what's happening and when to expect the draw.

Acceptance Criteria:
- Given I am a participant and the draw has not occurred
- When I access the group page
- Then I see the group name
- And I see the organizer's name
- And I see participants
- And I see a message indicating "Waiting for organizer to start the draw"
- And I see a message that I can create my wishlist after the draw when the final budget is known

Edge cases:
- Status updates requires page refresh
- Clear indication that no action is required from participant
- Wishlist creation is not available until after the draw

US-019: Create Personal Wishlist After Draw
Title: Prepare wishlist after knowing the final budget

Description:
As a participant after the draw, I want to create my wishlist once I know the final budget so that whoever draws me will know what I'd like to receive within the budget constraints.

Acceptance Criteria:
- Given I am a participant and the draw has been completed
- When I access my wishlist section and see the final budget
- Then I can enter free-form text describing my gift preferences appropriate for the budget
- And I can save my wishlist at any time after the draw
- And I receive confirmation that wishlist was saved
- And my wishlist is optional (I can leave it empty)
- And if I attempt to create wishlist before draw, I see a message that wishlists can only be created after the draw

Edge cases:
- Wishlist can include links, specific product names, general ideas, or size information
- No character limit enforced in MVP
- Wishlist CANNOT be created before the draw - only after draw completion when final budget is known
- Multiple saves are allowed without restriction after draw

US-020: View Assignment After Draw
Title: Discover who I'm buying a gift for

Description:
As a participant after the draw, I want to see my Secret Santa assignment so that I know who to buy a gift for.

Acceptance Criteria:
- Given I am a participant and the draw has been completed
- When I log in and access the group page
- Then I see the first and last name of my assigned gift recipient
- And I can see their wishlist (if they provided one)
- And I see the final budget amount set by the organizer
- And I cannot see who is buying for me or any other assignments

Edge cases:
- Assignment is permanent and cannot be changed
- If recipient's wishlist is empty, I see a message indicating no wishlist was provided
- Budget is displayed prominently
- Clear call-to-action to view recipient's wishlist

US-021: View Recipient's Wishlist
Title: Understand what my recipient wants

Description:
As a participant after the draw, I want to view my recipient's wishlist so that I can choose an appropriate gift.

Acceptance Criteria:
- Given I am a participant and the draw is complete
- When I access my assignment page
- Then I see my recipient's wishlist content (if provided)
- And I can see when the wishlist was last updated
- And if the wishlist is empty, I see a helpful message
- And I receive email notifications if they update their wishlist

Edge cases:
- Wishlist updates after I first viewed it are highlighted or indicated
- Empty wishlist shows suggestion to use AI gift ideas feature
- Wishlist content is displayed with proper formatting (line breaks, etc.)

US-022: Create and Edit Personal Wishlist After Draw
Title: Create and update my wishlist after knowing the budget

Description:
As a participant after the draw, I want to create and edit my wishlist once I know the final budget so that I can provide appropriate gift guidance within budget constraints and update my preferences as needed.

Acceptance Criteria:
- Given I am a participant and the draw is complete
- When I create or edit and save my wishlist
- Then my changes are saved successfully
- And the final budget is displayed prominently before the wishlist creation area
- And my Secret Santa receives an email notification after ~1 hour delay (if wishlist was already created)
- And I receive confirmation that my wishlist was created/updated
- And I can make multiple changes (notifications are batched)

Edge cases:
- Multiple edits within 1 hour result in single notification
- Notification email is sent only to my Secret Santa, not all participants
- I can completely clear my wishlist if I change my mind
- Changes are visible immediately to my Santa when they refresh
- Wishlists cannot be created or viewed before the draw

US-023: Request AI Gift Suggestions
Title: Get AI-powered gift ideas for my recipient

Description:
As a participant after the draw, I want to request AI-generated gift suggestions so that I have ideas if my recipient's wishlist is general or empty.

Acceptance Criteria:
- Given I am a participant and the draw is complete
- When I click "Get Gift Suggestions" on my assignment page
- Then the system analyzes my recipient's wishlist, name (for gender inference), and budget
- And I receive 3-5 gift suggestions with brief descriptions
- And suggestions are within the budget amount
- And suggestions are relevant to Polish market and culture
- And I can request suggestions multiple times

Edge cases:
- If wishlist is empty, AI generates general suggestions based on gender and budget
- If name doesn't clearly indicate gender, AI uses neutral suggestions
- Suggestions include variety of categories (books, experiences, gadgets, etc.)
- Each suggestion includes approximate price in PLN
- Loading state is shown while AI generates suggestions
- Error handling if AI service is unavailable

US-024: Receive Draw Completion Notification
Title: Get notified when draw is performed

Description:
As a participant, I want to receive an email when the draw is completed so that I know I can log in to see my assignment.

Acceptance Criteria:
- Given I am a participant and the organizer initiated the draw
- When the draw is successfully completed
- Then I receive an email notification
- And the email includes: group name, confirmation that draw occurred
- And the email encourages me to log in to see my assignment
- And the email does not contain my assignment information directly

Edge cases:
- Email is sent even if I'm currently logged in
- Email delivery failure doesn't block the draw completion
- Email includes a direct link to log in and view assignment
- Email is sent from a recognizable sender address

US-025: Receive Wishlist Update Notification
Title: Get notified when my recipient updates their wishlist

Description:
As a participant after the draw, I want to receive an email when my recipient updates their wishlist so that I can adjust my gift plans accordingly.

Acceptance Criteria:
- Given I am a participant, the draw is complete, and I have an assigned recipient
- When my recipient updates their wishlist
- Then I receive an email notification after approximately 1 hour delay
- And the email informs me that my recipient updated their wishlist
- And the email encourages me to log in to view the changes
- And the email does not contain the actual wishlist content
- And multiple updates within 1 hour result in only one email

Edge cases:
- Wishlists cannot be created before the draw, so all notifications are for post-draw updates
- Notification is sent only to the assigned Santa, not to all participants
- Email includes direct link to view the updated wishlist
- If recipient makes multiple rapid changes, only the final version triggers notification

### 5.4 Email Notifications and System Features

US-026: Send Draw Completion Email
Title: Notify all participants of completed draw

Description:
As the system, I want to send email notifications to all participants when a draw completes so that they know to check their assignments.

Acceptance Criteria:
- Given a draw has been successfully completed
- When the draw algorithm finishes
- Then an email is sent to each participant (including organizer)
- And the email contains: group name and login link
- And the email has clear subject line (e.g., "Your Secret Santa Draw is Complete!")
- And emails are sent asynchronously to avoid blocking the draw process

Edge cases:
- Email failures are logged but don't block draw completion
- Batch email sending to handle large groups
- Email includes branding consistent with application
- Email is mobile-friendly

US-027: Send Wishlist Update Email with Delay
Title: Notify Santa of recipient wishlist changes

Description:
As the system, I want to send wishlist update notifications with a 1-hour delay so that multiple rapid edits don't spam the recipient's Santa.

Acceptance Criteria:
- Given a participant has updated their wishlist after the draw
- When 1 hour has passed since the most recent update
- Then an email is sent to their assigned Santa
- And the email indicates that the wishlist was updated
- And the email includes a link to view the updated wishlist
- And if multiple updates occurred within the hour, only one email is sent

Edge cases:
- Wishlists can only be created/updated after draw completion, so all notifications are post-draw
- Delay timer does not reset with each new edit within the 1-hour window - first one counts for delay
- System tracks pending notifications to avoid duplicates
- Email includes timestamp of when wishlist was last updated

US-028: Generate AI Gift Suggestions
Title: Provide personalized gift ideas using AI

Description:
As the system, I want to analyze wishlist content and generate gift suggestions using AI so that participants have helpful ideas for choosing gifts.

Acceptance Criteria:
- Given a participant requests gift suggestions
- When the AI service receives: recipient wishlist text, recipient first name (for gender), budget amount
- Then AI generates 3-5 relevant gift suggestions
- And each suggestion includes: item/category name, brief description, approximate price in PLN
- And suggestions are appropriate for Polish market
- And suggestions respect the budget constraint (at or below budget)
- And suggestions are returned within 10 seconds

Edge cases:
- If wishlist is empty, generate general suggestions based on gender and budget
- If AI service fails, show user-friendly error message
- Suggestions include mix of specific items and general categories

US-029: Validate Exclusion Rules
Title: Ensure draw feasibility with exclusion rules

Description:
As the system, I want to validate exclusion rules in real-time so that organizers know if their rules make a draw impossible before attempting to execute it.

Acceptance Criteria:
- Given an organizer is defining or modifying exclusion rules
- When a rule is added, removed, or participants change
- Then the system validates whether a valid draw configuration exists
- And if validation fails, system displays specific error message
- And if validation passes, system confirms draw is ready
- And validation uses graph theory to check for valid pairing

Edge cases:
- Minimum 3 participants required for any draw
- System checks for scenarios where participant would have no valid recipients
- Error messages suggest specific actions (e.g., "Remove rule between A and B")
- Validation is fast enough for real-time feedback (<3 second)
- Complex exclusion patterns are detected (e.g., circular exclusions) - Person A cannot be the Santa for B if B is the Santa for A

US-030: Execute Draw Algorithm
Title: Perform anonymous Secret Santa assignment

Description:
As the system, I want to execute a draw algorithm that respects all rules so that each participant gets exactly one assignment fairly and anonymously.

Acceptance Criteria:
- Given organizer initiates the draw and validation passes
- When the draw algorithm executes
- Then each participant is assigned exactly one gift recipient
- And no participant is assigned to themselves
- And each participant is someone else's recipient exactly once
- And all exclusion rules defined by organizer are respected
- And organizer's assignment is also anonymous (even they don't know all pairings)
- And results are stored permanently and securely
- And algorithm completes within 5 seconds for groups up to 30 people

Edge cases:
- Algorithm uses randomization for fairness
- If algorithm fails (should be prevented by validation), system shows error and doesn't save partial results
- Results are cryptographically secure (assignments cannot be predicted or manipulated)
- Transaction ensures all-or-nothing persistence (either all assignments succeed or none)

### 5.5 Edge Cases and Error Handling

US-031: Handle Invalid Invitation Link
Title: Manage invalid group invitation links

Description:
As the system, I want to handle invalid invitation links gracefully so that users understand why they cannot join a group.

Acceptance Criteria:
- Given a user accesses an invitation link
- When the link is invalid or the group doesn't exist
- Then the user sees a clear error message explaining the issue
- And the user is provided options: contact organizer, create own group, or go to dashboard
- And the system logs the invalid link attempt for security monitoring

Edge cases:
- Link format is invalid (not a valid URL structure)
- Group ID in link doesn't exist in database
- Group draw has already been performed (link is expired)
- Different error messages for different scenarios (invalid vs expired)

US-032: Handle Minimum Participant Requirements
Title: Enforce minimum group size for draw

Description:
As the system, I want to enforce minimum participant requirements so that draws only occur when mathematically valid.

Acceptance Criteria:
- Given an organizer attempts to initiate a draw
- When the group has fewer than 3 participants
- Then the draw is prevented
- And organizer sees error message: "At least 3 participants required for Secret Santa draw"
- And the draw button is disabled
- And system suggests inviting more participants

Edge cases:
- Count includes organizer as a participant
- If participants are removed and count drops below 3, draw button becomes disabled

US-033: Handle Concurrent Group Operations
Title: Prevent race conditions in group management

Description:
As the system, I want to handle concurrent operations on groups so that data consistency is maintained when multiple users interact simultaneously.

Acceptance Criteria:
- Given multiple users are interacting with the same group
- When concurrent operations occur (joining, draw initiation, participant removal)
- Then the system ensures data consistency through proper locking or transactions
- And users see accurate, up-to-date information
- And no participant assignments are corrupted or duplicated
- And only one draw can be initiated successfully

Edge cases:
- Organizer removes participant while they're updating their wishlist
- Draw initiated while participants are joining

US-034: Handle Email Delivery Failures
Title: Manage email notification failures gracefully

Description:
As the system, I want to handle email delivery failures so that core functionality continues working even when email service has issues.

Acceptance Criteria:
- Given an email notification needs to be sent
- When the email service is unavailable or delivery fails
- Then the failure is logged for monitoring
- And the core operation (draw completion, wishlist save) still succeeds
- And system attempts retry with exponential backoff
- And after max retries, failure is logged for manual follow-up
- And user is not shown error (email is auxiliary to main functionality)

Edge cases:
- Invalid email addresses are caught and logged
- Temporary email service outages trigger retry logic
- Permanent failures are flagged for admin review
- Users can still access information in-app even if email failed

US-035: Handle AI Service Unavailability
Title: Manage AI integration failures

Description:
As the system, I want to handle AI service failures gracefully so that users receive helpful messages when gift suggestions cannot be generated.

Acceptance Criteria:
- Given a user requests AI gift suggestions
- When the AI service is unavailable, times out, or returns an error
- Then the user sees a user-friendly error message
- And the message suggests trying again later
- And the failure is logged for monitoring
- And the user can still proceed with gift planning without AI suggestions
- And error message doesn't expose technical details

Edge cases:
- Timeout after 10 seconds of waiting for AI response
- AI service rate limiting (implement exponential backoff)
- Partial response from AI (fewer than 3 suggestions) is still shown
- User can retry immediately or wait