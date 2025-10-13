# SantaVibe

A web-based Secret Santa application designed to simplify holiday gift exchanges within groups of friends or family.

[![Angular](https://img.shields.io/badge/Angular-20-red)](https://angular.io/)
[![.NET](https://img.shields.io/badge/.NET-9-512BD4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-Not%20specified-lightgrey)](#license)
[![Status](https://img.shields.io/badge/status-MVP%20Development-yellow)](#project-status)

## Table of Contents

- [Overview](#overview)
- [Key Features](#key-features)
- [Tech Stack](#tech-stack)
- [Getting Started](#getting-started)
- [Available Scripts](#available-scripts)
- [Project Scope](#project-scope)
- [Project Status](#project-status)
- [License](#license)

## Overview

SantaVibe addresses the financial burden of buying gifts for everyone during holiday seasons by organizing a draw where each participant becomes a "Santa" for exactly one other person. The application provides:

- **Anonymous draw system** - Even organizers don't know the pairings
- **Flexible exclusion rules** - Prevent couples or family members from drawing each other
- **AI-powered gift suggestions** - Based on wishlists and budget
- **Budget management** - With anonymous participant suggestions

**Target Market:**
- Geographic region: Poland
- Currency: PLN (Polish ZBoty)
- Platform: Web application (desktop and mobile browsers)

## Key Features

### For Organizers
- Create and manage Secret Santa groups
- Generate unique invitation links
- Set exclusion rules (e.g., married couples can't draw each other)
- View anonymous budget suggestions from participants
- Set final budget for the group
- Execute anonymous draw with validation

### For Participants
- Join groups via invitation link
- Create and edit personal wishlists
- Submit budget suggestions anonymously
- View assigned gift recipient after draw
- Access AI-powered gift suggestions
- Receive email notifications for draw completion and wishlist updates

### Core Functionality
- **Fair Distribution**: Each participant buys exactly one gift within an agreed budget
- **Complete Anonymity**: Draw results are anonymous to all, including the organizer
- **Exclusion Rules**: Prevent specific pairs from drawing each other
- **Smart Validation**: Real-time validation ensures draw feasibility
- **Email Notifications**: Automatic notifications for draw completion and wishlist updates

## Tech Stack

### Frontend
- **Framework**: Angular 20 (standalone components)
- **State Management**: Angular Signals + Services + RxJS
- **UI Components**:
  - Angular CDK (dialogs, overlays)
  - Tailwind CSS 4.1 (utility-first styling)
  - Flowbite 3.1 (ready-made Tailwind components)
- **Development**:
  - TypeScript 5.9
  - ESLint + Prettier for code quality
  - Jasmine + Karma for testing

### Backend
- **Framework**: ASP.NET Core 9 with Minimal APIs
- **Language**: C# 13
- **ORM**: Entity Framework Core
- **Features**:
  - Background tasks with hosted services
  - Rate limiting middleware
  - Serilog for structured logging

### Infrastructure & Services
- **Database**: Neon (Serverless PostgreSQL)
  - 512MB storage in free tier
  - Autoscaling with compute/storage separation

- **Authentication**: ASP.NET Core Identity
  - JWT tokens for Angular integration
  - Email/password authentication
  - Built-in security features (PBKDF2, brute-force protection)

- **Email Service**: Resend
  - 3,000 emails/month free tier
  - Modern API with HTML template support

- **AI Integration**: OpenRouter.ai
  - Access to multiple AI models (OpenAI, Anthropic, Google)
  - Financial limits on API keys

- **CI/CD & Hosting**:
  - GitHub Actions for CI/CD pipelines
  - DigitalOcean for Docker-based hosting

## Getting Started

### Prerequisites

- Node.js (version 18 or higher recommended)
- npm (comes with Node.js)
- .NET 9 SDK
- PostgreSQL database (or Neon account)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/santavibe.git
   cd santavibe
   ```

2. **Frontend Setup**
   ```bash
   cd SantaVibe.Web
   npm install
   ```

3. **Backend Setup**
   ```bash
   cd ../SantaVibe.API
   dotnet restore
   ```

4. **Configure Environment Variables**

   Create configuration files for both frontend and backend with the following settings:
   - Database connection string (Neon PostgreSQL)
   - JWT secret key
   - Email service credentials (Resend)
   - AI service API key (OpenRouter.ai)

5. **Run Database Migrations**
   ```bash
   dotnet ef database update
   ```

### Running the Application

**Development Mode:**

Frontend (Angular):
```bash
cd SantaVibe.Web
npm start
```
The application will be available at `http://localhost:4200`

Backend (.NET):
```bash
cd SantaVibe.API
dotnet run
```
The API will be available at `https://localhost:5001`

## Available Scripts

All scripts are run from the `SantaVibe.Web` directory:

### `npm start`
Runs the Angular development server. Open [http://localhost:4200](http://localhost:4200) to view it in the browser. The page will reload if you make edits.

### `npm run build`
Builds the Angular application for production to the `dist/` folder. It correctly bundles Angular in production mode and optimizes the build for the best performance.

### `npm run watch`
Runs the build in watch mode with development configuration. Useful for continuous development without the dev server overhead.

### `npm test`
Launches the Karma test runner in interactive watch mode. Runs unit tests using Jasmine.

### `npm run lint`
Runs ESLint to check for code quality issues and enforce coding standards. Uses the project's ESLint configuration with Prettier integration.

### `npm run ng`
Provides direct access to the Angular CLI for additional commands (generate components, services, etc.).

## Project Scope

### MVP Features (In Scope)
- User registration and authentication (email/password)
- Group creation and management
- Invitation system via shareable links
- Anonymous budget suggestions and final budget setting
- Exclusion rules definition and validation
- Anonymous draw algorithm
- Wishlist creation and editing
- Email notifications (draw completion, wishlist updates)
- AI-powered gift suggestions
- GDPR compliance with simple terms

### Explicitly Out of Scope for MVP
- Native mobile applications (iOS/Android)
- Multi-language support (Polish only)
- Multi-currency support (PLN only)
- Post-draw tracking (gift purchased status, delivery confirmation)
- In-app messaging or anonymous chat
- Participant withdrawal after draw
- Group templates or recurring events
- Payment integration or e-commerce features
- OAuth/social login
- Two-factor authentication

### Technical Boundaries
- Single region deployment (Poland/EU)
- Designed for typical group sizes (5-30 people)
- Standard web application response times
- Email delivery via standard SMTP

## Project Status

**Current Status**: =� MVP Development

This project is currently in the MVP (Minimum Viable Product) development phase. The goal is to validate the core concept with 1-2 complete group cycles.

### Roadmap
1. **MVP Development** (Current Phase)
   - Core authentication and user management
   - Group creation and invitation system
   - Draw algorithm implementation
   - Email notification system
   - AI integration for gift suggestions

2. **MVP Testing**
   - User testing with 1-2 groups
   - Bug fixes and usability improvements
   - Performance optimization

## License

License information not yet specified. Please contact the project maintainers for licensing details.

---

**Note**: This project is currently in active development. Features and documentation may change as the project evolves. For questions or contributions, please open an issue or contact the development team.
