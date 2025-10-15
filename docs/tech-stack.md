Frontend - Angular:
- Angular 20
- State Management: Angular Signals + Services + RxJS - native reactivity, zero dependencies, fast MVP delivery
- Angular CDK - logic for dialogs, overlays (without Material UI)
- Tailwind CSS - full design flexibility
- Flowbite - ready-made Tailwind components (buttons, cards, modals) with friendly design

Frontend state

Backend - .NET Core:
- ASP.NET Core + minimal api
- .NET 9
- C# 13
- EF Core
- Serilog
- Background tasks with hosted services
- Rate limiting middleware
- xUnit with Verify.Xunit for snapshot testing

Database: Neon (Serverless Postgres)
- Fast MVP: Serverless, no infrastructure management needed, 512MB storage in free tier
- Scalability: Autoscaling, separates compute from storage
- Cost: Free tier sufficient for MVP (3 projects, 0.5GB each), then pay-as-you-go
- Simplicity: It's just Postgres - compatible with EF Core without changes
- Security: Encryption, automatic backups, connection pooling

Authorization and Authentication - ASP.NET Core Identity
- Fast MVP: Built into .NET, zero external dependencies
- Scalability: Proven in production applications
- Cost: Completely free, no user limits
- Simplicity: Sufficient for email/password auth (FR-001, FR-002), full control
- Security: Password hashing (PBKDF2), token management, brute-force protection
- Identity + JWT tokens for Angular
- Entity Framework Core for user storage
- Simple implementation without external services

Email Service: Resend
- Fast MVP: Modern API, easy integration with .NET
- Scalability: 3,000 emails/month free, then $20/month for 50k
- Cost: Free tier sufficient for start (FR-018: draw notifications, FR-019: wishlist updates)
- Simplicity: Simpler than SendGrid, great DX, HTML templates
- Security: DKIM, SPF, delivery tracking

AI - Communication with models through Openrouter.ai service:
- Access to a wide range of models (OpenAI, Anthropic, Google and many others), allowing us to find a solution ensuring high efficiency and low costs
- Allows setting financial limits on API keys

CI/CD and Hosting:
- Github Actions for creating CI/CD pipelines
- DigitalOcean for application hosting via Docker image