# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SantaVibe is a full-stack Secret Santa web application for organizing anonymous holiday gift exchanges. The application consists of an Angular 20 frontend and ASP.NET Core 9 backend with PostgreSQL database.

**Target Market**: Poland (PLN currency, Polish language)

**Key Features**:
- Anonymous Secret Santa draw algorithm with exclusion rules
- Wishlist management with email notifications
- AI-powered gift suggestions via OpenRouter.ai
- Budget management with anonymous participant suggestions
- Email notifications via Resend service

## Architecture

### Monorepo Structure

```
/SantaVibe.Web/          - Angular 20 frontend (standalone components)
/SantaVibe.Api/          - ASP.NET Core 9 backend (minimal APIs)
/docs/                    - Project documentation (PRD, tech stack)
```

### Frontend Architecture (SantaVibe.Web)

- **Framework**: Angular 20 with standalone components (no NgModules)
- **State Management**: Angular Signals + Services + RxJS for reactive state
- **Styling**: Tailwind CSS 4.1 with Flowbite 3.1 components
- **UI Components**: Angular CDK for dialogs/overlays (no Material UI)
- **Testing**: Jasmine + Karma
- **Code Quality**: ESLint + Prettier

**State Management Pattern**:
- Use signals for local component state
- Use services with signals for shared state
- Use RxJS only for async operations (HTTP, events)
- Prefer `computed()` for derived state

**Component Architecture**:
- All components are standalone (default in Angular 20)
- Use OnPush change detection strategy
- Use `input()` and `output()` functions instead of decorators
- Use `inject()` function instead of constructor injection
- Use native control flow (@if, @for, @switch)

### Backend Architecture (SantaVibe.Api)

- **Framework**: ASP.NET Core 9 with minimal APIs (not controllers)
- **ORM**: Entity Framework Core 9 with Npgsql for PostgreSQL
- **Database**: Neon (Serverless PostgreSQL)
- **Authentication**: ASP.NET Core Identity with JWT tokens
- **Email**: Resend service for notifications
- **AI**: OpenRouter.ai for gift suggestions
- **Logging**: Serilog for structured logging
- **Background Tasks**: Hosted services for delayed notifications

**API Architecture**:
- Use minimal APIs for all endpoints (not MVC controllers)
- Use endpoint filters for cross-cutting concerns
- Use dependency injection with appropriate lifetimes
- Use repository pattern for data access abstraction

**Key Business Logic**:
- **Draw Algorithm**: Must respect exclusion rules, no self-assignment, no 2-person circles (A→B and B→A)
- **Email Delays**: Wishlist update notifications delayed by 1 hour to batch multiple edits
- **Anonymity**: Draw results anonymous to all users including organizer

## Development Commands

### Frontend (SantaVibe.Web)

```bash
cd SantaVibe.Web

# Development server (http://localhost:4200)
npm start

# Build for production
npm run build

# Run tests
npm test

# Lint code
npm run lint

# Watch mode for continuous build
npm run watch
```

### Backend (SantaVibe.Api)

```bash
cd SantaVibe.Api

# Run development server (https://localhost:5001)
dotnet run

# Build project
dotnet build

# Run tests
dotnet test

# Create EF Core migration
dotnet ef migrations add <MigrationName>

# Apply migrations to database
dotnet ef database update

# Restore packages
dotnet restore
```

## CODING_PRACTICES

### Guidelines for SUPPORT_LEVEL

#### SUPPORT_EXPERT

- Favor elegant, maintainable solutions over verbose code. Assume understanding of language idioms and design patterns.
- Highlight potential performance implications and optimization opportunities in suggested code.
- Frame solutions within broader architectural contexts and suggest design alternatives when appropriate.
- Focus comments on 'why' not 'what' - assume code readability through well-named functions and variables.
- Proactively address edge cases, race conditions, and security considerations without being prompted.
- When debugging, provide targeted diagnostic approaches rather than shotgun solutions.
- Suggest comprehensive testing strategies rather than just example tests, including considerations for mocking, test organization, and coverage.


### Guidelines for DOCUMENTATION

#### SWAGGER

- Define comprehensive schemas for all request and response objects
- Use semantic versioning in API paths to maintain backward compatibility
- Implement detailed descriptions for endpoints, parameters, and {{domain_specific_concepts}}
- Configure security schemes to document authentication and authorization requirements
- Use tags to group related endpoints by resource or functional area
- Implement examples for all endpoints to facilitate easier integration by consumers


### Guidelines for VERSION_CONTROL

#### GIT

- Use conventional commits to create meaningful commit messages
- Use feature branches with descriptive names following {{branch_naming_convention}}
- Write meaningful commit messages that explain why changes were made, not just what
- Keep commits focused on single logical changes to facilitate code review and bisection
- Use interactive rebase to clean up history before merging feature branches
- Leverage git hooks to enforce code quality checks before commits and pushes

#### GITHUB

- Use pull request templates to standardize information provided for code reviews
- Implement branch protection rules for {{protected_branches}} to enforce quality checks
- Configure required status checks to prevent merging code that fails tests or linting
- Use GitHub Actions for CI/CD workflows to automate testing and deployment
- Implement CODEOWNERS files to automatically assign reviewers based on code paths
- Use GitHub Projects for tracking work items and connecting them to code changes


### Guidelines for ARCHITECTURE

#### ADR

- Create ADRs in /docs/adr/{name}.md for:
- 1) Major dependency changes
- 2) Architectural pattern changes
- 3) New integration patterns
- 4) Database schema changes


### Guidelines for STATIC_ANALYSIS

#### ESLINT

- Configure project-specific rules in eslint.config.js to enforce consistent coding standards
- Use shareable configs like eslint-config-airbnb or eslint-config-standard as a foundation
- Implement custom rules for {{project_specific_patterns}} to maintain codebase consistency
- Configure integration with Prettier to avoid rule conflicts for code formatting
- Use the --fix flag in CI/CD pipelines to automatically correct fixable issues
- Implement staged linting with husky and lint-staged to prevent committing non-compliant code

#### PRETTIER

- Define a consistent .prettierrc configuration across all {{project_repositories}}
- Configure editor integration to format on save for immediate feedback
- Use .prettierignore to exclude generated files, build artifacts, and {{specific_excluded_patterns}}
- Set printWidth based on team preferences (80-120 characters) to improve code readability
- Configure consistent quote style and semicolon usage to match team conventions
- Implement CI checks to ensure all committed code adheres to the defined style

## FRONTEND

### Guidelines for ANGULAR

#### ANGULAR_CODING_STANDARDS

- Use standalone components, directives, and pipes instead of NgModules
- Implement signals for state management instead of traditional RxJS-based approaches
- Use the new inject function instead of constructor injection
- Implement control flow with @if, @for, and @switch instead of *ngIf, *ngFor, etc.
- Leverage functional guards and resolvers instead of class-based ones
- Use the new deferrable views for improved loading states
- Implement OnPush change detection strategy for improved performance
- Use TypeScript decorators with explicit visibility modifiers (public, private)
- Leverage Angular CLI for schematics and code generation
- Implement proper lazy loading with loadComponent and loadChildren


### Guidelines for STYLING

#### TAILWIND

- Use the @layer directive to organize styles into components, utilities, and base layers
- Implement Just-in-Time (JIT) mode for development efficiency and smaller CSS bundles
- Use arbitrary values with square brackets (e.g., w-[123px]) for precise one-off designs
- Leverage the @apply directive in component classes to reuse utility combinations
- Implement the Tailwind configuration file for customizing theme, plugins, and variants
- Use component extraction for repeated UI patterns instead of copying utility classes
- Leverage the theme() function in CSS for accessing Tailwind theme values
- Implement dark mode with the dark: variant
- Use responsive variants (sm:, md:, lg:, etc.) for adaptive designs
- Leverage state variants (hover:, focus:, active:, etc.) for interactive elements

## BACKEND

### Guidelines for DOTNET

#### ENTITY_FRAMEWORK

- Use the repository and unit of work patterns to abstract data access logic and simplify testing
- Implement eager loading with Include() to avoid N+1 query problems for {{entity_relationships}}
- Use migrations for database schema changes and version control with proper naming conventions
- Apply appropriate tracking behavior (AsNoTracking() for read-only queries) to optimize performance
- Implement query optimization techniques like compiled queries for frequently executed database operations
- Use value conversions for complex property transformations and proper handling of {{custom_data_types}}

#### ASP_NET

- Use minimal APIs for simple endpoints in .NET 6+ applications to reduce boilerplate code
- Implement the mediator pattern with MediatR for decoupling request handling and simplifying cross-cutting concerns
- Use API controllers with model binding and validation attributes for {{complex_data_models}}
- Apply proper response caching with cache profiles and ETags for improved performance on {{high_traffic_endpoints}}
- Implement proper exception handling with ExceptionFilter or middleware to provide consistent error responses
- Use dependency injection with scoped lifetime for request-specific services and singleton for stateless services

## DATABASE

### Guidelines for SQL

#### POSTGRES

- Use connection pooling to manage database connections efficiently
- Implement JSONB columns for semi-structured data instead of creating many tables for {{flexible_data}}
- Use materialized views for complex, frequently accessed read-only data

## DEVOPS

### Guidelines for CI_CD

#### GITHUB_ACTIONS

- Check if `package.json` exists in project root and summarize key scripts
- Check if `.nvmrc` exists in project root
- Check if `.env.example` exists in project root to identify key `env:` variables
- Always use terminal command: `git branch -a | cat` to verify whether we use `main` or `master` branch
- Always use `env:` variables and secrets attached to jobs instead of global workflows
- Always use `npm ci` for Node-based dependency setup
- Extract common steps into composite actions in separate files
- Once you're done, as a final step conduct the following: for each public action always use <tool>"Run Terminal"</tool> to see what is the most up-to-date version (use only major version) - extract tag_name from the response:
- ```bash curl -s https://api.github.com/repos/{owner}/{repo}/releases/latest ```


### Guidelines for CONTAINERIZATION

#### DOCKER

- Use multi-stage builds to create smaller production images
- Implement layer caching strategies to speed up builds for {{dependency_types}}
- Use non-root users in containers for better security

