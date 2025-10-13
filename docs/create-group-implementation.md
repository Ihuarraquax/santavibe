# Create Group Feature Implementation

This document describes the newly implemented "Create Group" feature for the SantaVibe Secret Santa application.

## What Was Created

### Backend (SantaVibe.Api)

#### Models
- **Models/Group.cs** - Entity representing a Secret Santa group with properties like Name, OrganizerId, InvitationCode, Budget, and IsDrawPerformed
- **Models/User.cs** - Entity representing a user with Email, FirstName, LastName, PasswordHash, and Wishlist
- **Models/Participant.cs** - Entity representing a participant in a group with BudgetSuggestion and AssignedRecipientId
- **Models/ExclusionRule.cs** - Entity representing exclusion rules between participants

#### Data Layer
- **Data/AppDbContext.cs** - Entity Framework Core DbContext with full model configuration including:
  - Unique indexes on User.Email and Group.InvitationCode
  - Proper foreign key relationships
  - Cascade delete behaviors
  - Default values for timestamps

#### DTOs
- **DTOs/CreateGroupRequest.cs** - Request DTO with validation attributes (Required, StringLength)
- **DTOs/GroupResponse.cs** - Response DTO with group details including invitation code

#### API Endpoint
- **POST /group** in Program.cs:40-112 - Minimal API endpoint that:
  - Validates the group name (required, 1-200 characters)
  - Creates a cryptographically secure invitation code (8 characters)
  - Creates the group and adds the organizer as a participant
  - Returns 201 Created with group details
  - NOTE: Currently uses a mock user since authentication is not yet implemented

#### Configuration
- Added CORS policy in Program.cs:13-21 to allow Angular frontend (http://localhost:4200)
- Added PostgreSQL connection string in appsettings.json:9-11
- Registered DbContext with Npgsql provider in Program.cs:24-26

### Frontend (SantaVibe.Web)

#### Models
- **src/app/models/group.model.ts** - TypeScript interfaces for CreateGroupRequest and GroupResponse

#### Services
- **src/app/services/group.service.ts** - Angular service using HttpClient to communicate with the backend API

#### Components
- **src/app/components/create-group/create-group.component.ts** - Standalone Angular 20 component using:
  - Signals for reactive state management
  - `inject()` function for dependency injection
  - Reactive forms with validators
  - OnPush change detection (default in Angular 20)

- **src/app/components/create-group/create-group.component.html** - Template using:
  - Native control flow (@if, @else)
  - Tailwind CSS 4.1 and Flowbite 3.1 styling
  - Reactive form binding
  - Real-time validation error display
  - Success view with invitation link and copy-to-clipboard functionality

#### Configuration
- Added `provideHttpClient()` to src/app/app.config.ts:12
- Created environment files with API URLs:
  - src/environments/environment.ts - Development (https://localhost:5001)
  - src/environments/environment.prod.ts - Production
- Added route configuration in src/app/app.routes.ts:4-7
- Updated main app template in src/app/app.html with navigation bar

## How to Run

### Prerequisites
1. PostgreSQL database running locally (or update connection string in appsettings.json)
2. .NET 9 SDK installed
3. Node.js and npm installed

### Backend Setup

```bash
cd SantaVibe.Api

# Create the database migration
dotnet ef migrations add InitialCreate

# Apply the migration to create database tables
dotnet ef database update

# Run the API (will start on https://localhost:5001)
dotnet run
```

### Frontend Setup

```bash
cd SantaVibe.Web

# Install dependencies (if not already done)
npm install

# Start the development server (will start on http://localhost:4200)
npm start
```

### Testing the Feature

1. Open your browser to http://localhost:4200
2. You should see the "Create Secret Santa Group" form
3. Enter a group name (e.g., "Family Christmas 2025")
4. Click "Create Group"
5. On success, you'll see:
   - A success message
   - The group details
   - A shareable invitation link
   - A unique invitation code
   - A "Copy" button to copy the invitation link to clipboard

## API Documentation

The API endpoint is documented with OpenAPI. When running in development mode, you can access the OpenAPI specification at:
- https://localhost:5001/openapi/v1.json

## Implementation Notes

### Architecture Decisions

1. **Minimal APIs**: Following the CLAUDE.md guidelines, the backend uses minimal APIs instead of controllers
2. **Standalone Components**: The Angular component is standalone (no NgModules) as per Angular 20 best practices
3. **Signals**: Used Angular signals for reactive state management
4. **Tailwind + Flowbite**: Styled with Tailwind CSS 4.1 and Flowbite 3.1 components
5. **Validation**: Both client-side (Angular validators) and server-side (Data Annotations) validation

### Current Limitations

1. **Mock Authentication**: The backend currently creates a mock user since authentication is not yet implemented. This needs to be replaced with actual JWT authentication.
2. **Database Connection**: Uses a local PostgreSQL database. Update the connection string for production Neon database.
3. **Error Handling**: Basic error handling is in place, but could be enhanced with more specific error messages and logging.

### Next Steps

To complete the full group management feature, you should implement:
1. User authentication (registration, login, JWT tokens)
2. GET /group/{id} endpoint to retrieve group details
3. Invitation link functionality (join group via link)
4. Participant management (view, remove participants)
5. Budget setting
6. Exclusion rules management
7. Draw execution

## File Structure

```
SantaVibe.Api/
├── Data/
│   └── AppDbContext.cs
├── DTOs/
│   ├── CreateGroupRequest.cs
│   └── GroupResponse.cs
├── Models/
│   ├── Group.cs
│   ├── User.cs
│   ├── Participant.cs
│   └── ExclusionRule.cs
├── Program.cs
└── appsettings.json

SantaVibe.Web/
├── src/
│   ├── app/
│   │   ├── components/
│   │   │   └── create-group/
│   │   │       ├── create-group.component.ts
│   │   │       ├── create-group.component.html
│   │   │       └── create-group.component.css
│   │   ├── models/
│   │   │   └── group.model.ts
│   │   ├── services/
│   │   │   └── group.service.ts
│   │   ├── app.config.ts
│   │   ├── app.routes.ts
│   │   └── app.html
│   └── environments/
│       ├── environment.ts
│       └── environment.prod.ts
```
