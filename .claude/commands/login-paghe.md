As a senior frontend developer, your task is to create a detailed implementation plan for a new view in a web application. This plan should be comprehensive and clear enough for another frontend developer to implement the view correctly and efficiently.

First, review the following information:

1. Product Requirements Document (PRD):
<prd>
@docs/prd.md
</prd>

2. View Description:
<view_description>
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

</view_description>

3. User Stories:
<user_stories>
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
</user_stories>

4. Endpoint Description:
<endpoint_description>
**Endpoint**: `POST /api/auth/login`

**Description**: Authenticate user and receive JWT token.

**Authentication**: None (public endpoint)

**Request Body**:
```json
{
  "email": "user@example.com",
  "password": "SecureP@ssw0rd"
}
```

**Success Response** (200 OK):
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "firstName": "Jan",
  "lastName": "Kowalski",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2025-10-16T14:30:00Z"
}
```

**Error Responses**:
- `401 Unauthorized`: Invalid credentials
  ```json
  {
    "error": "InvalidCredentials",
    "message": "Invalid email or password"
  }
  ```

</endpoint_description>

5. Endpoint Implementation:
<endpoint_implementation>
@SantaVibe.Backend\SantaVibe.Api\Features\Authentication\Login
</endpoint_implementation>

6. Type Definitions:
<type_definitions>
@SantaVibe.Web\src\api\model\login-request.ts
@SantaVibe.Web\src\api\model\login-response.ts
</type_definitions>

7. Tech Stack:
<tech_stack>
@docs/tech-stack.md
</tech_stack>

Before creating the final implementation plan, conduct analysis and planning inside <implementation_breakdown> tags in your thinking block. This section can be quite long, as it's important to be thorough.

In your implementation breakdown, execute the following steps:
1. For each input section (PRD, User Stories, Endpoint Description, Endpoint Implementation, Type Definitions, Tech Stack):
  - Summarize key points
 - List any requirements or constraints
 - Note any potential challenges or important issues
2. Extract and list key requirements from the PRD
3. List all needed main components, along with a brief description of their purpose, needed types, handled events, and validation conditions
4. Create a high-level component tree diagram
5. Identify required DTOs and custom ViewModel types for each view component. Explain these new types in detail, breaking down their fields and associated types.
6. Identify potential state variables and custom hooks, explaining their purpose and how they'll be used
7. List required API calls and corresponding frontend actions
8. Map each user story to specific implementation details, components, or functions
9. List user interactions and their expected outcomes
10. List conditions required by the API and how to verify them at the component level
11. Identify potential error scenarios and suggest how to handle them
12. List potential challenges related to implementing this view and suggest possible solutions

After conducting the analysis, provide an implementation plan in Markdown format with the following sections:

1. Overview: Brief summary of the view and its purpose.
2. View Routing: Specify the path where the view should be accessible.
3. Component Structure: Outline of main components and their hierarchy.
4. Component Details: For each component, describe:
 - Component description, its purpose and what it consists of
 - Main HTML elements and child components that build the component
 - Handled events
 - Validation conditions (detailed conditions, according to API)
 - Types (DTO and ViewModel) required by the component
 - Props that the component accepts from parent (component interface)
5. Types: Detailed description of types required for view implementation, including exact breakdown of any new types or view models by fields and types.
6. State Management: Detailed description of how state is managed in the view, specifying whether a custom hook is required.
7. API Integration: Explanation of how to integrate with the provided endpoint. Precisely indicate request and response types.
8. User Interactions: Detailed description of user interactions and how to handle them.
9. Conditions and Validation: Describe what conditions are verified by the interface, which components they concern, and how they affect the interface state
10. Error Handling: Description of how to handle potential errors or edge cases.
11. Implementation Steps: Step-by-step guide for implementing the view.

Ensure your plan is consistent with the PRD, user stories, and includes the provided tech stack.

The final output should be in English and saved in a file named docs/{view-name}-view-implementation-plan.md. Do not include any analysis and planning in the final output.

Here's an example of what the output file should look like (content is to be replaced):

```markdown
# View Implementation Plan [View Name]

## 1. Overview
[Brief description of the view and its purpose]

## 2. View Routing
[Path where the view should be accessible]

## 3. Component Structure
[Outline of main components and their hierarchy]

## 4. Component Details
### [Component Name 1]
- Component description [description]
- Main elements: [description]
- Handled interactions: [list]
- Handled validation: [list, detailed]
- Types: [list]
- Props: [list]

### [Component Name 2]
[...]

## 5. Types
[Detailed description of required types]

## 6. State Management
[Description of state management in the view]

## 7. API Integration
[Explanation of integration with provided endpoint, indication of request and response types]

## 8. User Interactions
[Detailed description of user interactions]

## 9. Conditions and Validation
[Detailed description of conditions and their validation]

## 10. Error Handling
[Description of handling potential errors]

## 11. Implementation Steps
1. [Step 1]
2. [Step 2]
3. [...]
```

Begin analysis and planning now. Your final output should consist solely of the implementation plan in English in markdown format, which you will save in the docs/{view-name}-view-implementation-plan.md file and should not duplicate or repeat any work done in the implementation breakdown.