# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SantaVibe is a full-stack Secret Santa web application for organizing anonymous holiday gift exchanges. The application consists of an Angular 20 frontend and ASP.NET Core 9 backend with PostgreSQL database.

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
- **Testing:** xUnit with Verify.Xunit for snapshot testing
- **Background Tasks**: Hosted services for delayed notifications

**API Architecture**:
- Use minimal APIs for all endpoints (not MVC controllers)
- Use endpoint filters for cross-cutting concerns
- Use dependency injection with appropriate lifetimes


- follow DDD principles with business logic in the domain model and a cleaner handler that focuses on orchestration