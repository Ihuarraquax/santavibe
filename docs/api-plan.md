# SantaVibe REST API Plan

## 1. Overview

This document defines the RESTful API design for the SantaVibe Secret Santa application. The API is built with ASP.NET Core 9 minimal APIs and follows REST principles with JWT-based authentication.

**Base URL**: `https://api.santavibe.com/api` (development: `https://localhost:5001/api`)

**API Version**: v1 (included in base path)

**Authentication**: JWT Bearer tokens

**Content Type**: `application/json`

---

## 2. Resources

| Resource | Database Table | Description |
|----------|---------------|-------------|
| Auth | AspNetUsers | User registration and authentication |
| Profile | AspNetUsers | Current user profile management |
| Groups | Groups | Secret Santa group/event management |
| Participants | GroupParticipants | Group membership and wishlist management |
| Exclusion Rules | ExclusionRules | Draw constraints (who cannot draw whom) |
| Assignments | Assignments | Draw results (who buys for whom) |
| Invitations | Groups (via InvitationToken) | Group invitation acceptance |
| Budget | Groups + GroupParticipants | Budget suggestions and final budget |
| Gift Suggestions | External AI service | AI-generated gift ideas |

---

## 3. Endpoints

### 3.1 Authentication

#### Register New User

**Endpoint**: `POST /api/auth/register`

**Description**: Create a new user account with email and password.

**Authentication**: None (public endpoint)

**Request Body**:
```json
{
  "email": "user@example.com",
  "password": "SecureP@ssw0rd",
  "firstName": "Jan",
  "lastName": "Kowalski",
  "gdprConsent": true
}
```

**Request Validation**:
- `email`: Required, valid email format, unique, max 256 characters
- `password`: Required, min 8 characters, must contain uppercase, lowercase, digit, and special character
- `firstName`: Required, max 100 characters
- `lastName`: Required, max 100 characters
- `gdprConsent`: Required, must be `true`

**Success Response** (201 Created):
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
- `400 Bad Request`: Validation failed
  ```json
  {
    "error": "ValidationError",
    "message": "Password does not meet security requirements",
    "details": {
      "password": ["Must contain at least one uppercase letter", "Must be at least 8 characters"]
    }
  }
  ```
- `409 Conflict`: Email already registered
  ```json
  {
    "error": "EmailAlreadyExists",
    "message": "An account with this email already exists"
  }
  ```

---

#### Login

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

---

### 3.2 Profile Management

#### Get Current User Profile

**Endpoint**: `GET /api/profile`

**Description**: Retrieve the authenticated user's profile information.

**Authentication**: Required (JWT Bearer token)

**Success Response** (200 OK):
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
- `401 Unauthorized`: Missing or invalid token

---

#### Update Current User Profile

**Endpoint**: `PUT /api/profile`

**Description**: Update the authenticated user's profile information.

**Authentication**: Required (JWT Bearer token)

**Request Body**:
```json
{
  "firstName": "Jan",
  "lastName": "Nowak"
}
```

**Request Validation**:
- `firstName`: Required, max 100 characters
- `lastName`: Required, max 100 characters
- Email cannot be changed (not included in update)

**Success Response** (200 OK):
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
- `401 Unauthorized`: Missing or invalid token

---

### 3.3 Group Management

#### Create Group

**Endpoint**: `POST /api/groups`

**Description**: Create a new Secret Santa group. The authenticated user becomes the organizer and is automatically added as a participant.

**Authentication**: Required (JWT Bearer token)

**Request Body**:
```json
{
  "name": "Family Secret Santa 2025"
}
```

**Request Validation**:
- `name`: Required, max 200 characters, min 3 characters

**Success Response** (201 Created):
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "name": "Family Secret Santa 2025",
  "organizerId": "550e8400-e29b-41d4-a716-446655440000",
  "organizerName": "Jan Kowalski",
  "invitationToken": "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
  "invitationLink": "https://santavibe.com/invite/a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
  "participantCount": 1,
  "budget": null,
  "drawCompleted": false,
  "createdAt": "2025-10-15T10:00:00Z"
}
```

**Error Responses**:
- `400 Bad Request`: Validation failed
- `401 Unauthorized`: Missing or invalid token

---

#### Get User's Groups

**Endpoint**: `GET /api/groups`

**Description**: Retrieve all groups where the authenticated user is a participant.

**Authentication**: Required (JWT Bearer token)

**Query Parameters**:
- `includeCompleted` (optional, boolean, default: true): Include groups where draw has been completed

**Success Response** (200 OK):
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
    },
    {
      "groupId": "8d0f7780-8536-51ef-955c-f18gd2g01bf8",
      "name": "Work Team Secret Santa",
      "organizerId": "660f9511-f30c-52f5-b827-557766551111",
      "organizerName": "Anna Nowak",
      "isOrganizer": false,
      "participantCount": 12,
      "budget": 50.00,
      "drawCompleted": true,
      "joinedAt": "2025-10-10T14:20:00Z",
      "drawCompletedAt": "2025-10-14T16:45:00Z"
    }
  ],
  "totalCount": 2
}
```

**Error Responses**:
- `401 Unauthorized`: Missing or invalid token

---

#### Get Group Details

**Endpoint**: `GET /api/groups/{groupId}`

**Description**: Retrieve detailed information about a specific group. Response varies based on whether draw has been completed and user's role.

**Authentication**: Required (JWT Bearer token)

**Path Parameters**:
- `groupId` (UUID): Group identifier

**Authorization**: User must be a participant in the group

**Success Response - Before Draw** (200 OK):
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "name": "Family Secret Santa 2025",
  "organizerId": "550e8400-e29b-41d4-a716-446655440000",
  "organizerName": "Jan Kowalski",
  "isOrganizer": true,
  "budget": 100.00,
  "drawCompleted": false,
  "drawCompletedAt": null,
  "createdAt": "2025-10-15T10:00:00Z",
  "participants": [
    {
      "userId": "550e8400-e29b-41d4-a716-446655440000",
      "firstName": "Jan",
      "lastName": "Kowalski",
      "joinedAt": "2025-10-15T10:00:00Z",
      "isOrganizer": true
    },
    {
      "userId": "660f9511-f30c-52f5-b827-557766551111",
      "firstName": "Anna",
      "lastName": "Nowak",
      "joinedAt": "2025-10-15T11:30:00Z",
      "isOrganizer": false
    }
  ],
  "participantCount": 2,
  "exclusionRuleCount": 0,
  "invitationLink": "https://santavibe.com/invite/a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
  "canDraw": false,
  "drawValidation": {
    "isValid": false,
    "errors": ["Minimum 3 participants required for draw"]
  }
}
```

**Success Response - After Draw** (200 OK):
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "name": "Family Secret Santa 2025",
  "organizerId": "550e8400-e29b-41d4-a716-446655440000",
  "organizerName": "Jan Kowalski",
  "isOrganizer": true,
  "budget": 100.00,
  "drawCompleted": true,
  "drawCompletedAt": "2025-10-20T15:30:00Z",
  "createdAt": "2025-10-15T10:00:00Z",
  "participantCount": 5,
  "myAssignment": {
    "recipientId": "770g0622-g41d-63g6-c938-668877662222",
    "recipientFirstName": "Maria",
    "recipientLastName": "Wiśniewska",
    "hasWishlist": true
  }
}
```

**Note on invitationLink field**:
- The `invitationLink` field is only present in the response when:
  - The draw has not been completed (`drawCompleted: false`)
  - The requesting user is the organizer (`isOrganizer: true`)
- Non-organizers and post-draw requests will receive `null` for this field
- The invitation link is constructed using the configured `App:BaseUrl` and the group's `InvitationToken`
- Format: `{baseUrl}/invite/{invitationToken}`

**Error Responses**:
- `401 Unauthorized`: Missing or invalid token
- `403 Forbidden`: User is not a participant in this group
- `404 Not Found`: Group does not exist

---

### 3.4 Invitation Management

#### Get Invitation Details

**Endpoint**: `GET /api/invitations/{token}`

**Description**: Retrieve group information for an invitation link. Used before joining to show group details.

**Authentication**: None (public endpoint, but user must be authenticated to join)

**Path Parameters**:
- `token` (UUID): Invitation token from the shareable link

**Success Response** (200 OK):
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
- `404 Not Found`: Invalid token or group not found
  ```json
  {
    "error": "InvalidInvitation",
    "message": "This invitation link is invalid or has expired"
  }
  ```
- `410 Gone`: Draw already completed
  ```json
  {
    "error": "InvitationExpired",
    "message": "This group has already completed the draw and is no longer accepting participants"
  }
  ```

---

#### Accept Invitation and Join Group

**Endpoint**: `POST /api/invitations/{token}/accept`

**Description**: Join a group using an invitation link. User provides their budget suggestion during this process.

**Authentication**: Required (JWT Bearer token)

**Path Parameters**:
- `token` (UUID): Invitation token from the shareable link

**Request Body**:
```json
{
  "budgetSuggestion": 80.00
}
```

**Request Validation**:
- `budgetSuggestion`: Optional, NUMERIC(10,2), min 0.01, max 99999999.99
- User must not already be a participant
- Draw must not be completed

**Success Response** (201 Created):
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "groupName": "Family Secret Santa 2025",
  "organizerName": "Jan Kowalski",
  "participantCount": 6,
  "budget": null,
  "drawCompleted": false,
  "joinedAt": "2025-10-15T13:00:00Z"
}
```

**Error Responses**:
- `400 Bad Request`: Validation failed
- `401 Unauthorized`: Missing or invalid token
- `404 Not Found`: Invalid invitation token
- `409 Conflict`: User already in group
  ```json
  {
    "error": "AlreadyParticipant",
    "message": "You are already a participant in this group"
  }
  ```
- `410 Gone`: Draw already completed

---

### 3.5 Participant Management

#### Remove Participant from Group

**Endpoint**: `DELETE /api/groups/{groupId}/participants/{userId}`

**Description**: Remove a participant from the group. Only available to organizer before draw. Cannot remove the organizer themselves.

**Authentication**: Required (JWT Bearer token)

**Path Parameters**:
- `groupId` (UUID): Group identifier
- `userId` (string): User identifier to remove

**Authorization**: User must be the group organizer

**Success Response** (204 No Content)

**Error Responses**:
- `400 Bad Request`: Cannot remove organizer or draw already completed
  ```json
  {
    "error": "CannotRemoveOrganizer",
    "message": "The organizer cannot be removed from the group"
  }
  ```
  ```json
  {
    "error": "DrawAlreadyCompleted",
    "message": "Cannot remove participants after draw has been completed"
  }
  ```
- `401 Unauthorized`: Missing or invalid token
- `403 Forbidden`: User is not the organizer
- `404 Not Found`: Group or participant not found

---

### 3.6 Wishlist Management

#### Get My Wishlist for a Group

**Endpoint**: `GET /api/groups/{groupId}/participants/me/wishlist`

**Description**: Retrieve the authenticated user's wishlist for a specific group. Only available after draw completion.

**Authentication**: Required (JWT Bearer token)

**Path Parameters**:
- `groupId` (UUID): Group identifier

**Authorization**: User must be a participant in the group and draw must be completed

**Success Response** (200 OK):
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "wishlistContent": "I would love books about cooking, especially Italian cuisine. Size M for clothes. I also enjoy board games.",
  "lastModified": "2025-10-15T14:30:00Z"
}
```

**Success Response - Empty Wishlist** (200 OK):
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "wishlistContent": null,
  "lastModified": null
}
```

**Error Responses**:
- `401 Unauthorized`: Missing or invalid token
- `403 Forbidden`: User is not a participant or draw not completed
  ```json
  {
    "error": "DrawNotCompleted",
    "message": "Wishlist can only be viewed after the draw has been completed"
  }
  ```
- `404 Not Found`: Group does not exist

---

#### Update My Wishlist for a Group

**Endpoint**: `PUT /api/groups/{groupId}/participants/me/wishlist`

**Description**: Create or update the authenticated user's wishlist for a specific group. Only available after draw completion. Triggers delayed email notification to the user's Santa.

**Authentication**: Required (JWT Bearer token)

**Path Parameters**:
- `groupId` (UUID): Group identifier

**Authorization**: User must be a participant in the group and draw must be completed

**Request Body**:
```json
{
  "wishlistContent": "I would love books about cooking, especially Italian cuisine. Size M for clothes. I also enjoy board games."
}
```

**Request Validation**:
- `wishlistContent`: Optional (can be null or empty to clear wishlist), TEXT type (no hard limit in MVP)

**Success Response** (200 OK):
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "wishlistContent": "I would love books about cooking, especially Italian cuisine. Size M for clothes. I also enjoy board games.",
  "lastModified": "2025-10-15T14:45:00Z"
}
```

**Business Logic**:
- Draw completion is required - endpoint only accessible post-draw
- Create EmailNotification record with 1-hour delay to notify Santa of wishlist change
- Implement deduplication: check for existing pending notification within 1-hour window

**Error Responses**:
- `401 Unauthorized`: Missing or invalid token
- `403 Forbidden`: User is not a participant or draw not completed
  ```json
  {
    "error": "DrawNotCompleted",
    "message": "Wishlist can only be created/modified after the draw has been completed"
  }
  ```
- `404 Not Found`: Group does not exist

---

#### Get Recipient's Wishlist (After Draw)

**Endpoint**: `GET /api/groups/{groupId}/my-assignment/wishlist`

**Description**: Retrieve the wishlist of the user's assigned gift recipient. Only available after draw completion.

**Authentication**: Required (JWT Bearer token)

**Path Parameters**:
- `groupId` (UUID): Group identifier

**Authorization**: User must be a participant and draw must be completed

**Success Response** (200 OK):
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "recipientId": "770g0622-g41d-63g6-c938-668877662222",
  "recipientFirstName": "Maria",
  "recipientLastName": "Wiśniewska",
  "wishlistContent": "I love mystery novels, especially Agatha Christie. I also enjoy gardening tools and flower seeds.",
  "lastModified": "2025-10-20T16:00:00Z"
}
```

**Success Response - Empty Wishlist** (200 OK):
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "recipientId": "770g0622-g41d-63g6-c938-668877662222",
  "recipientFirstName": "Maria",
  "recipientLastName": "Wiśniewska",
  "wishlistContent": null,
  "lastModified": null
}
```

**Error Responses**:
- `401 Unauthorized`: Missing or invalid token
- `403 Forbidden`: User is not a participant or draw not completed
  ```json
  {
    "error": "DrawNotCompleted",
    "message": "Draw has not been completed yet. You cannot view recipient wishlist."
  }
  ```
- `404 Not Found`: Group does not exist or user has no assignment

---

### 3.7 Budget Management

#### Get Anonymous Budget Suggestions

**Endpoint**: `GET /api/groups/{groupId}/budget/suggestions`

**Description**: Retrieve anonymous, sorted list of budget suggestions from participants. Only available to organizer.

**Authentication**: Required (JWT Bearer token)

**Path Parameters**:
- `groupId` (UUID): Group identifier

**Authorization**: User must be the group organizer

**Success Response** (200 OK):
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "suggestions": [50.00, 75.00, 80.00, 100.00, 100.00],
  "count": 5,
  "participantCount": 6,
  "suggestionsReceived": 5,
  "currentBudget": null
}
```

**Business Logic**:
- Suggestions are sorted ascending
- User identities are not revealed (fully anonymous)
- Shows count of participants who provided suggestions vs total participants

**Error Responses**:
- `401 Unauthorized`: Missing or invalid token
- `403 Forbidden`: User is not the organizer
- `404 Not Found`: Group does not exist

---

#### Update My Budget Suggestion

**Endpoint**: `PUT /api/groups/{groupId}/participants/me/budget-suggestion`

**Description**: Update or set the authenticated user's budget suggestion for a group. Available to any participant before draw completion.

**Authentication**: Required (JWT Bearer token)

**Path Parameters**:
- `groupId` (UUID): Group identifier

**Authorization**: User must be a participant in the group

**Request Body**:
```json
{
  "budgetSuggestion": 120.00
}
```

**Request Validation**:
- `budgetSuggestion`: Required, NUMERIC(10,2), min 0.01, max 99999999.99
- Draw must not be completed

**Success Response** (200 OK):
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "budgetSuggestion": 120.00,
  "updatedAt": "2025-10-16T10:30:00Z"
}
```

**Business Logic**:
- Updates the BudgetSuggestion field in GroupParticipants table
- Can be used to set initial suggestion or update existing suggestion
- Setting to null will clear the suggestion
- Only affects organizer's anonymous suggestion list

**Error Responses**:
- `400 Bad Request`: Validation failed or draw already completed
  ```json
  {
    "error": "DrawAlreadyCompleted",
    "message": "Cannot modify budget suggestion after draw has been completed"
  }
  ```
- `401 Unauthorized`: Missing or invalid token
- `403 Forbidden`: User is not a participant
- `404 Not Found`: Group does not exist

---

### 3.8 Exclusion Rules Management

#### Get Exclusion Rules

**Endpoint**: `GET /api/groups/{groupId}/exclusion-rules`

**Description**: Retrieve all exclusion rules for a group. Available to group organizer only.

**Authentication**: Required (JWT Bearer token)

**Path Parameters**:
- `groupId` (UUID): Group identifier

**Authorization**: User must be the group organizer

**Success Response** (200 OK):
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "exclusionRules": [
    {
      "ruleId": "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d",
      "user1": {
        "userId": "550e8400-e29b-41d4-a716-446655440000",
        "firstName": "Jan",
        "lastName": "Kowalski"
      },
      "user2": {
        "userId": "770g0622-g41d-63g6-c938-668877662222",
        "firstName": "Maria",
        "lastName": "Kowalska"
      },
      "createdAt": "2025-10-15T15:00:00Z"
    }
  ],
  "totalCount": 1
}
```

**Error Responses**:
- `401 Unauthorized`: Missing or invalid token
- `403 Forbidden`: User is not a group organizer
- `404 Not Found`: Group does not exist

---

#### Create Exclusion Rule

**Endpoint**: `POST /api/groups/{groupId}/exclusion-rules`

**Description**: Create a new exclusion rule preventing two participants from drawing each other. Only available to organizer before draw.

**Authentication**: Required (JWT Bearer token)

**Path Parameters**:
- `groupId` (UUID): Group identifier

**Authorization**: User must be the group organizer

**Request Body**:
```json
{
  "userId1": "550e8400-e29b-41d4-a716-446655440000",
  "userId2": "770g0622-g41d-63g6-c938-668877662222"
}
```

**Request Validation**:
- `userId1`: Required, must be a participant in the group
- `userId2`: Required, must be a participant in the group
- `userId1` ≠ `userId2` (CHECK constraint)
- Unique combination of (groupId, userId1, userId2)
- Draw must not be completed
- Rule must not make draw impossible (validated in real-time)

**Success Response** (201 Created):
```json
{
  "ruleId": "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d",
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "user1": {
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "firstName": "Jan",
    "lastName": "Kowalski"
  },
  "user2": {
    "userId": "770g0622-g41d-63g6-c938-668877662222",
    "firstName": "Maria",
    "lastName": "Kowalska"
  },
  "createdAt": "2025-10-15T15:00:00Z",
  "drawValidation": {
    "isValid": true,
    "errors": []
  }
}
```

**Error Responses**:
- `400 Bad Request`: Validation failed
  ```json
  {
    "error": "InvalidExclusionRule",
    "message": "This exclusion rule would make a valid draw impossible",
    "details": {
      "suggestion": "Remove the exclusion between User A and User B to allow valid assignments"
    }
  }
  ```
  ```json
  {
    "error": "SameUser",
    "message": "Cannot create exclusion rule for the same user"
  }
  ```
  ```json
  {
    "error": "DrawAlreadyCompleted",
    "message": "Cannot add exclusion rules after draw has been completed"
  }
  ```
- `401 Unauthorized`: Missing or invalid token
- `403 Forbidden`: User is not the organizer
- `404 Not Found`: Group or user not found
- `409 Conflict`: Duplicate exclusion rule

---

#### Delete Exclusion Rule

**Endpoint**: `DELETE /api/groups/{groupId}/exclusion-rules/{ruleId}`

**Description**: Remove an exclusion rule. Only available to organizer before draw.

**Authentication**: Required (JWT Bearer token)

**Path Parameters**:
- `groupId` (UUID): Group identifier
- `ruleId` (UUID): Exclusion rule identifier

**Authorization**: User must be the group organizer

**Success Response** (204 No Content)

**Error Responses**:
- `400 Bad Request`: Draw already completed
  ```json
  {
    "error": "DrawAlreadyCompleted",
    "message": "Cannot remove exclusion rules after draw has been completed"
  }
  ```
- `401 Unauthorized`: Missing or invalid token
- `403 Forbidden`: User is not the organizer
- `404 Not Found`: Group or rule not found

---

### 3.9 Draw Management

#### Validate Draw Feasibility

**Endpoint**: `GET /api/groups/{groupId}/draw/validate`

**Description**: Validate whether a draw can be successfully completed with current participants and exclusion rules. Provides real-time feedback for organizers.

**Authentication**: Required (JWT Bearer token)

**Path Parameters**:
- `groupId` (UUID): Group identifier

**Authorization**: User must be the organizer

**Success Response - Valid** (200 OK):
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "isValid": true,
  "canDraw": true,
  "participantCount": 5,
  "exclusionRuleCount": 2,
  "errors": [],
  "warnings": []
}
```

**Success Response - Invalid** (200 OK):
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "isValid": false,
  "canDraw": false,
  "participantCount": 2,
  "exclusionRuleCount": 1,
  "errors": [
    "Minimum 3 participants required for draw",
    "Current exclusion rules prevent valid assignments"
  ],
  "warnings": []
}
```

**Validation Rules**:
- Minimum 3 participants
- Exclusion rules must allow valid assignment graph
- Draw not already completed
- Note: Budget validation not included here as it's provided during draw execution

**Error Responses**:
- `401 Unauthorized`: Missing or invalid token
- `403 Forbidden`: User is not a participant
- `404 Not Found`: Group does not exist

---

#### Execute Draw

**Endpoint**: `POST /api/groups/{groupId}/draw`

**Description**: Execute the Secret Santa draw algorithm. The organizer must provide the final budget when performing the draw. Creates assignments for all participants, sets the final budget, marks draw as completed, and triggers email notifications. This action is irreversible.

**Authentication**: Required (JWT Bearer token)

**Path Parameters**:
- `groupId` (UUID): Group identifier

**Authorization**: User must be the group organizer

**Request Body**:
```json
{
  "budget": 100.00
}
```

**Request Validation**:
- `budget`: Required, NUMERIC(10,2), min 0.01, max 99999999.99

**Pre-execution Validations**:
- User is the organizer
- Draw has not been completed
- Minimum 3 participants
- Exclusion rules allow valid draw
- Budget is provided in request body

**Success Response** (200 OK):
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "budget": 100.00,
  "drawCompleted": true,
  "drawCompletedAt": "2025-10-20T15:30:00Z",
  "participantCount": 5,
  "assignmentsCreated": 5,
  "emailNotificationsScheduled": 5,
  "myAssignment": {
    "recipientId": "770g0622-g41d-63g6-c938-668877662222",
    "recipientFirstName": "Maria",
    "recipientLastName": "Wiśniewska"
  }
}
```

**Business Logic**:
1. Validate all preconditions including budget in request body
2. Set the final budget: Update Groups.Budget field (transaction)
3. Execute draw algorithm:
   - Each participant assigned exactly one recipient
   - No self-assignments (SantaUserId ≠ RecipientUserId)
   - No 2-person circles (if A→B then B≠A)
   - Respect all exclusion rules
   - Randomized for fairness
4. Create Assignment records in database (transaction)
5. Update Groups.DrawCompletedAt timestamp
6. Schedule DrawCompleted email notifications for all participants
7. Return organizer's own assignment with the final budget (they cannot see others)

**Error Responses**:
- `400 Bad Request`: Validation failed
  ```json
  {
    "error": "ValidationError",
    "message": "Budget is required",
    "details": {
      "budget": ["Budget must be between 0.01 and 99999999.99"]
    }
  }
  ```
  ```json
  {
    "error": "DrawValidationFailed",
    "message": "Cannot execute draw: validation errors found",
    "details": {
      "errors": ["Minimum 3 participants required"]
    }
  }
  ```
  ```json
  {
    "error": "DrawAlreadyCompleted",
    "message": "Draw has already been completed for this group"
  }
  ```
- `401 Unauthorized`: Missing or invalid token
- `403 Forbidden`: User is not the organizer
- `404 Not Found`: Group does not exist
- `500 Internal Server Error`: Draw algorithm failed (should be prevented by validation)
  ```json
  {
    "error": "DrawExecutionFailed",
    "message": "An unexpected error occurred during draw execution. Please contact support."
  }
  ```

---

### 3.10 Assignment Viewing

#### Get My Assignment

**Endpoint**: `GET /api/groups/{groupId}/my-assignment`

**Description**: Retrieve the authenticated user's Secret Santa assignment (who they are buying a gift for). Only available after draw completion.

**Authentication**: Required (JWT Bearer token)

**Path Parameters**:
- `groupId` (UUID): Group identifier

**Authorization**: User must be a participant and draw must be completed

**Success Response** (200 OK):
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "groupName": "Family Secret Santa 2025",
  "budget": 100.00,
  "drawCompletedAt": "2025-10-20T15:30:00Z",
  "recipient": {
    "userId": "770g0622-g41d-63g6-c938-668877662222",
    "firstName": "Maria",
    "lastName": "Wiśniewska",
    "hasWishlist": true,
    "wishlistLastModified": "2025-10-20T16:00:00Z"
  }
}
```

**Business Logic**:
- Only the assignment where SantaUserId = authenticated user is returned
- User cannot see who is buying for them
- Organizer cannot see all assignments (same restriction)

**Error Responses**:
- `401 Unauthorized`: Missing or invalid token
- `403 Forbidden`: User is not a participant or draw not completed
  ```json
  {
    "error": "DrawNotCompleted",
    "message": "Draw has not been completed yet"
  }
  ```
- `404 Not Found`: Group does not exist or no assignment found

---

### 3.11 AI Gift Suggestions

#### Generate Gift Suggestions

**Endpoint**: `POST /api/groups/{groupId}/my-assignment/gift-suggestions`

**Description**: Generate AI-powered gift suggestions for the authenticated user's assigned recipient. Analyzes recipient's wishlist, inferred gender from name, and budget.

**Authentication**: Required (JWT Bearer token)

**Path Parameters**:
- `groupId` (UUID): Group identifier

**Authorization**: User must be a participant and draw must be completed

**Request Body**: None (context gathered from assignment and wishlist)

**Success Response** (200 OK):
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "recipientFirstName": "Maria",
  "budget": 100.00,
  "suggestions": [
    {
      "category": "Books",
      "itemName": "Agatha Christie Mystery Collection",
      "description": "A curated box set of 5 classic Agatha Christie mystery novels, perfect for mystery enthusiasts",
      "approximatePrice": 85.00,
      "currency": "PLN"
    },
    {
      "category": "Gardening",
      "itemName": "Premium Gardening Tool Set",
      "description": "Professional-grade gardening tools including pruner, trowel, and cultivator with ergonomic handles",
      "approximatePrice": 95.00,
      "currency": "PLN"
    },
    {
      "category": "Flowers",
      "itemName": "Heirloom Flower Seed Collection",
      "description": "Assorted heirloom flower seeds including roses, tulips, and daisies with planting guide",
      "approximatePrice": 45.00,
      "currency": "PLN"
    },
    {
      "category": "Books",
      "itemName": "Polish Mystery Novel by Marek Krajewski",
      "description": "Contemporary Polish mystery novel set in Wrocław, for fans of detective fiction",
      "approximatePrice": 40.00,
      "currency": "PLN"
    },
    {
      "category": "Experience",
      "itemName": "Botanical Garden Annual Pass",
      "description": "Year-long access to local botanical gardens with seasonal exhibitions",
      "approximatePrice": 80.00,
      "currency": "PLN"
    }
  ],
  "generatedAt": "2025-10-20T17:00:00Z",
  "aiModel": "gpt-4o"
}
```

**Business Logic**:
1. Verify draw is completed and user has assignment
2. Fetch recipient's wishlist and name
3. Infer gender from Polish first name (for context)
4. Call OpenRouter.ai API with:
   - Recipient wishlist content
   - Inferred gender/context
   - Budget amount
   - Polish market context
5. Generate 3-5 suggestions within budget
6. Return structured suggestions

**Error Responses**:
- `401 Unauthorized`: Missing or invalid token
- `403 Forbidden`: User is not a participant or draw not completed
- `404 Not Found`: Group or assignment not found
- `500 Internal Server Error`: AI service unavailable
  ```json
  {
    "error": "AIServiceUnavailable",
    "message": "Gift suggestion service is temporarily unavailable. Please try again later."
  }
  ```
- `503 Service Unavailable`: AI service timeout
  ```json
  {
    "error": "AIServiceTimeout",
    "message": "Gift suggestion generation timed out. Please try again."
  }
  ```

**Rate Limit**: 5 requests per user per group per hour (prevent abuse of external AI API)

---

## 4. Authentication and Authorization

### 4.1 Authentication Mechanism

**Type**: JWT (JSON Web Token) Bearer authentication

**Implementation**:
- ASP.NET Core Identity for user management
- JWT tokens issued upon successful login or registration
- Tokens include claims: `userId`, `email`, `firstName`, `lastName`
- Token expiration: 24 hours (configurable)
- Refresh tokens: Not implemented in MVP (future enhancement)

**Token Format**:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### 4.2 Authorization Rules

#### Public Endpoints (No Authentication Required)
- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/invitations/{token}`

#### Authenticated Endpoints (Valid JWT Required)
All other endpoints require authentication

#### Organizer-Only Endpoints
These endpoints require the authenticated user to be the group organizer:
- `GET /api/groups/{groupId}/budget/suggestions`
- `POST /api/groups/{groupId}/exclusion-rules`
- `DELETE /api/groups/{groupId}/exclusion-rules/{ruleId}`
- `DELETE /api/groups/{groupId}/participants/{userId}`
- `POST /api/groups/{groupId}/draw` (includes setting final budget)

#### Participant-Only Endpoints
These endpoints require the authenticated user to be a participant in the group:
- `GET /api/groups/{groupId}`
- `GET /api/groups/{groupId}/exclusion-rules`
- `PUT /api/groups/{groupId}/participants/me/budget-suggestion`
- `GET /api/groups/{groupId}/draw/validate`

#### Post-Draw Endpoints
These endpoints require draw to be completed:
- `GET /api/groups/{groupId}/participants/me/wishlist`
- `PUT /api/groups/{groupId}/participants/me/wishlist`
- `GET /api/groups/{groupId}/my-assignment`
- `GET /api/groups/{groupId}/my-assignment/wishlist`
- `POST /api/groups/{groupId}/my-assignment/gift-suggestions`

### 4.3 GDPR Compliance

**Registration Consent**:
- `gdprConsent` field required in registration
- Must be `true` to create account
- Timestamp recorded in database

**Data Processing**:
- Users consent to processing: email, name, wishlist content
- Soft delete pattern: `IsDeleted` flag instead of hard delete
- Future: Right to be forgotten implementation (anonymization)

---

## 5. Validation and Business Logic

### 5.1 Entity Validation Rules

#### User (AspNetUsers)
- **Email**: Required, valid email format, unique, max 256 characters
- **Password**: Required, min 8 characters, must contain uppercase, lowercase, digit, special character
- **FirstName**: Required, max 100 characters, NOT NULL
- **LastName**: Required, max 100 characters, NOT NULL
- **IsDeleted**: Default FALSE, NOT NULL

#### Group (Groups)
- **Name**: Required, max 200 characters, min 3 characters, NOT NULL
- **OrganizerUserId**: Required, must reference existing user, NOT NULL
- **Budget**: Optional, NUMERIC(10,2), min 0.01, max 99,999,999.99
- **InvitationToken**: Auto-generated UUID, UNIQUE, NOT NULL
- **DrawCompletedAt**: Nullable, set once on draw execution (immutable after set)

#### GroupParticipant (GroupParticipants)
- **GroupId + UserId**: Composite primary key (must be unique together)
- **BudgetSuggestion**: Optional, NUMERIC(10,2), min 0.01, max 99,999,999.99
- **WishlistContent**: Optional, TEXT (no character limit in MVP)

#### ExclusionRule (ExclusionRules)
- **UserId1**: Required, must be participant in group, NOT NULL
- **UserId2**: Required, must be participant in group, NOT NULL
- **CHECK**: UserId1 ≠ UserId2
- **UNIQUE**: (GroupId, UserId1, UserId2)
- **Business Rule**: Both users must be active participants

#### Assignment (Assignments)
- **GroupId**: Required, NOT NULL
- **SantaUserId**: Required, must be participant, NOT NULL
- **RecipientUserId**: Required, must be participant, NOT NULL
- **CHECK**: SantaUserId ≠ RecipientUserId
- **UNIQUE**: (GroupId, SantaUserId) - each Santa assigned exactly once
- **UNIQUE**: (GroupId, RecipientUserId) - each Recipient has exactly one Santa

### 5.2 Business Logic Rules

#### Draw Execution Logic
1. **Budget Required**: Organizer must provide budget in request body (required field)
2. **Minimum Participants**: At least 3 participants required
3. **No Self-Assignment**: SantaUserId ≠ RecipientUserId (CHECK constraint)
4. **One-to-One Mapping**: Each participant is exactly one Santa and exactly one Recipient
5. **No 2-Person Circles**: If A→B (A is Santa for B), then B≠A (B cannot be Santa for A)
6. **Exclusion Rules**: All bidirectional exclusion rules must be respected
7. **Feasibility Validation**: Graph theory validation ensures valid assignment exists
8. **Randomization**: Algorithm uses randomization for fairness
9. **Transaction**: Budget update and all assignments created in single transaction (all-or-nothing)
10. **Immutability**: Once DrawCompletedAt is set, neither budget nor assignments can be changed
11. **Email Notifications**: Trigger EmailNotification records for all participants

#### Wishlist Update Notification Logic
1. **Trigger Condition**: Wishlist created or updated (only possible after draw completion)
2. **Delay**: 1-hour delay before email sent
3. **Deduplication**: Check for existing pending notification within 1-hour window
4. **Recipient**: Only the user's assigned Santa receives notification
5. **Content**: Email does not contain wishlist content, only notification of update
6. **Background Job**: Handled by hosted service, not synchronous API call
7. **Note**: Wishlists can only be created/updated after draw, so all notifications are post-draw

#### Exclusion Rules Validation Logic
1. **Real-Time Validation**: Validate upon creation/deletion of rule
2. **Graph Analysis**: Check if valid assignment graph exists with constraints
3. **User Feedback**: Provide specific error messages if validation fails
4. **Bidirectionality**: A→B exclusion automatically means B→A
5. **Participant Verification**: Both users must be current participants

#### Budget Management Logic
1. **Anonymous Suggestions**: Budget suggestions displayed without user identity
2. **Sorted Display**: Suggestions sorted ascending for organizer review
3. **Final Budget**: Organizer must provide final binding budget when executing draw (not auto-calculated)
4. **Budget Set During Draw**: Budget is set as part of the draw execution (POST /api/groups/{groupId}/draw)
5. **Immutability After Draw**: Budget cannot be changed after draw completion (set during draw, immutable after)
6. **Budget Suggestion Updates**: Participants can update their budget suggestions at any time before draw
7. **Pre-Draw Only**: Budget suggestions can only be modified before DrawCompletedAt is set

#### Participant Management Logic
1. **Organizer Auto-Add**: Organizer automatically added as participant on group creation
2. **Cannot Remove Organizer**: Organizer cannot be removed from their own group
3. **Pre-Draw Only**: Participants can only be removed before draw
4. **Cascade Delete**: Removing participant deletes budget suggestion, but exclusion rules may need cleanup

#### Invitation Logic
1. **Token Uniqueness**: Each group has unique invitation token (UUID)
2. **Validity**: Link valid until DrawCompletedAt is set
3. **One-Time Join**: User cannot join same group twice
4. **Authentication Required**: User must register/login to join

### 5.3 Anonymity and Privacy Rules

1. **Assignment Privacy**: Users can only see their own assignment (where they are Santa)
2. **Organizer Restriction**: Organizer cannot see all assignments (participates like others)
3. **Budget Anonymity**: Budget suggestions displayed without user attribution
4. **Wishlist Privacy**: Wishlists can only be created/viewed after draw completion. Wishlist only visible to assigned Santa after draw
5. **No Reverse Lookup**: No endpoint reveals who is buying for a specific user

### 5.4 Error Handling Strategy

**Validation Errors (400 Bad Request)**:
- Include field-level details in response
- User-friendly error messages
- Actionable suggestions when possible

**Authentication Errors (401 Unauthorized)**:
- Generic "Invalid credentials" message (don't reveal if email exists)
- Token expiration handled gracefully
- Redirect to login with returnUrl

**Authorization Errors (403 Forbidden)**:
- Clear message about missing permissions
- Don't reveal information about resources user shouldn't access

**Not Found Errors (404)**:
- Generic message for security (don't reveal what doesn't exist)
- Consistent response format

**Conflict Errors (409)**:
- Used for duplicate resources (email already exists, already in group)
- Clear explanation of conflict

**Server Errors (500)**:
- Generic user-facing message
- Detailed logging on server side
- Never expose stack traces to client

**Service Unavailable (503)**:
- Used for external service failures (AI, email)
- Suggest retry with appropriate backoff

---

## 6. Rate Limiting

**Implementation**: ASP.NET Core rate limiting middleware

### 6.1 Rate Limit Rules

| Endpoint Pattern | Limit | Window | Reason |
|-----------------|-------|--------|--------|
| `POST /api/auth/register` | 5 requests | Per IP per minute | Prevent abuse/bot registration |
| `POST /api/auth/login` | 10 requests | Per IP per minute | Prevent brute force attacks |
| `POST /api/groups/*/my-assignment/gift-suggestions` | 5 requests | Per user per group per hour | Limit external AI API costs |
| All other authenticated endpoints | 100 requests | Per user per minute | General abuse prevention |

### 6.2 Rate Limit Response

**Status Code**: `429 Too Many Requests`

**Response Body**:
```json
{
  "error": "RateLimitExceeded",
  "message": "Too many requests. Please try again later.",
  "retryAfter": 60
}
```

**Headers**:
- `X-RateLimit-Limit`: Maximum requests allowed
- `X-RateLimit-Remaining`: Requests remaining in window
- `X-RateLimit-Reset`: Unix timestamp when limit resets
- `Retry-After`: Seconds until retry allowed

---

## 7. Pagination and Filtering

### 7.1 Pagination (Future Enhancement)

For MVP, pagination is not implemented as group sizes are expected to be small (5-30 participants). Future endpoints that may need pagination:

- `GET /api/groups` (if users participate in many groups)

**Recommended Pagination Format**:
```
GET /api/groups?page=1&pageSize=20
```

**Response Format**:
```json
{
  "items": [...],
  "pagination": {
    "currentPage": 1,
    "pageSize": 20,
    "totalPages": 3,
    "totalItems": 57
  }
}
```

### 7.2 Filtering

**Implemented Filters**:
- `GET /api/groups?includeCompleted=false` - Filter completed draws

**Future Filters** (Post-MVP):
- `GET /api/groups?search=name` - Search groups by name
- `GET /api/groups?role=organizer` - Filter by user role

---

## 8. Response Format Standards

### 8.1 Success Responses

**Consistent JSON Structure**:
- Use camelCase for property names
- Include relevant timestamps in ISO 8601 format with timezone (Z for UTC)
- Use null for absent optional values (not undefined or missing keys)
- Booleans for flags (drawCompleted, isOrganizer, etc.)
- Numeric types: integers for counts, decimals (2 places) for currency

### 8.2 Error Responses

**Standard Error Format**:
```json
{
  "error": "ErrorCode",
  "message": "User-friendly error message",
  "details": {
    "field": ["Validation message 1", "Validation message 2"]
  }
}
```

**Error Codes**:
- Use PascalCase for error codes
- Make codes specific and actionable
- Include optional details object for field-level errors

### 8.3 Currency Format

**All monetary values**:
- Type: NUMERIC(10,2)
- Format: Decimal with exactly 2 decimal places
- Example: `100.00`, `50.50`, `0.99`
- Currency: PLN (implied, not included in field name)

### 8.4 Date/Time Format

**All timestamps**:
- Format: ISO 8601 with timezone
- Example: `2025-10-15T14:30:00Z`
- Timezone: UTC (Z suffix)
- Precision: Seconds (no milliseconds in API responses)

---

## 9. Security Considerations

### 9.1 Input Validation
- Validate all inputs server-side (never trust client)
- Sanitize text inputs to prevent XSS
- Use parameterized queries (EF Core handles this)
- Enforce maximum string lengths per schema

### 9.2 Authentication Security
- Passwords hashed with PBKDF2 (ASP.NET Core Identity default)
- JWT tokens signed with secure secret key
- Token expiration enforced
- HTTPS required for all endpoints in production

### 9.3 Authorization Security
- Always verify user identity from JWT claims (not request body)
- Check organizer status before allowing organizer actions
- Verify participant membership before exposing group data
- Prevent privilege escalation (user cannot make themselves organizer)

### 9.4 Data Privacy
- Users can only access data they are authorized to see
- Assignments filtered to current user only
- Budget suggestions are fully anonymous
- No endpoint reveals who is buying for whom (reverse lookup)

### 9.5 Rate Limiting
- Prevent brute force attacks on authentication
- Limit AI API calls to control costs
- General abuse prevention with per-user limits

### 9.6 CORS (Cross-Origin Resource Sharing)
- Configure allowed origins for Angular frontend
- Development: `http://localhost:4200`
- Production: `https://santavibe.com`

---

## 10. API Versioning Strategy

**MVP Approach**: No versioning in MVP (v1 implicit in base path)

**Future Versioning**: URL path versioning
- Example: `/api/v2/groups`
- Maintain v1 for backward compatibility
- Deprecation notices in response headers

---

## 11. Background Jobs and Async Operations

### 11.1 Email Notifications

**Not Exposed via REST API** - Handled by background hosted service

**Email Types** (from EmailNotifications table):
1. **DrawCompleted**: Sent immediately after draw execution
2. **WishlistUpdated**: Sent 1 hour after wishlist update

**Queue Processing**:
- Background service polls EmailNotifications table
- Query: `WHERE SentAt IS NULL AND ScheduledAt <= NOW() AND AttemptCount < MAX_ATTEMPTS`
- Retry logic with exponential backoff
- Update SentAt timestamp on success
- Increment AttemptCount and log LastError on failure

### 11.2 Draw Algorithm

**Synchronous Operation** - Executes during `POST /api/groups/{groupId}/draw`

**Algorithm Requirements**:
- Use graph theory to find valid assignment
- Respect all exclusion rules (bidirectional)
- Ensure no 2-person circles (if A→B then B≠A)
- Randomize for fairness
- Complete within 5 seconds for groups up to 30 people
- Transaction ensures all-or-nothing persistence

---

## 12. Testing Considerations

### 12.1 Endpoint Testing
- Unit tests for validation logic
- Integration tests for full request/response cycle
- Test authentication and authorization enforcement
- Test error handling and edge cases

### 12.2 Business Logic Testing
- Draw algorithm correctness (no violations of rules)
- Exclusion rule validation accuracy
- Email notification scheduling and deduplication
- Anonymous budget suggestion handling

### 12.3 Security Testing
- Rate limit enforcement

---

## 13. API Documentation

**Tool**: Swagger/OpenAPI 3.0

**Implementation**:
- Auto-generate from ASP.NET Core minimal API definitions
- Include all endpoints with request/response schemas
- Document authentication requirements
- Provide example requests and responses
- Include error response formats

**Swagger UI**: Available at `/swagger` in development

---

## 14. Future Enhancements (Post-MVP)

### 14.1 Potential New Endpoints
- `PATCH /api/groups/{groupId}` - Update group name/description
- `GET /api/users/me/notifications` - View notification history
- `GET /api/groups/{groupId}/statistics` - Group statistics for organizer

### 14.2 Feature Additions
- Refresh token endpoint for JWT renewal
- File upload for wishlist images
- Anonymous chat between Santa and Recipient

### 14.3 Performance Optimizations
- Database query optimization with compiled queries

---

## 15. Appendix

### 15.1 HTTP Status Code Usage

| Code | Usage |
|------|-------|
| 200 OK | Successful GET, PUT, POST (non-creation) |
| 201 Created | Successful POST with resource creation |
| 204 No Content | Successful DELETE |
| 400 Bad Request | Validation error, malformed request |
| 401 Unauthorized | Missing or invalid authentication |
| 403 Forbidden | Insufficient permissions |
| 404 Not Found | Resource does not exist |
| 409 Conflict | Duplicate resource, constraint violation |
| 410 Gone | Resource permanently unavailable (expired invitation) |
| 429 Too Many Requests | Rate limit exceeded |
| 500 Internal Server Error | Unexpected server error |
| 503 Service Unavailable | External service failure (AI, email) |

### 15.2 Common Error Codes

| Error Code | HTTP Status | Description |
|-----------|-------------|-------------|
| ValidationError | 400 | Input validation failed |
| InvalidCredentials | 401 | Login failed |
| EmailAlreadyExists | 409 | Email already registered |
| DrawAlreadyCompleted | 400 | Cannot modify after draw |
| DrawNotCompleted | 403 | Cannot access pre-draw |
| InvalidInvitation | 404 | Invitation token invalid |
| InvitationExpired | 410 | Group closed to new participants |
| AlreadyParticipant | 409 | User already in group |
| CannotRemoveOrganizer | 400 | Organizer cannot be removed |
| InvalidExclusionRule | 400 | Rule makes draw impossible |
| DrawValidationFailed | 400 | Cannot execute draw |
| AIServiceUnavailable | 500 | AI service error |
| AIServiceTimeout | 503 | AI service timeout |
| RateLimitExceeded | 429 | Too many requests |

---

## Document Version

**Version**: 1.0
**Date**: 2025-10-15
**Author**: API Architecture Team
**Status**: Final - Ready for Implementation
