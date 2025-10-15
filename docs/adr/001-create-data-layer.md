# ADR 001: Create Data Layer with EF Core 9 and PostgreSQL

**Status:** Accepted

**Date:** 2025-10-15

**Deciders:** Development Team

**Technical Story:** Implementation of the complete database schema and data access layer for SantaVibe Secret Santa application based on db-plan.md specification.

## Context

The SantaVibe application requires a robust data persistence layer to support:
- User authentication and profile management
- Secret Santa group organization and participant management
- Anonymous draw algorithm with exclusion rules
- Wishlist management with change notifications
- Email notification queue with retry logic

The schema must support:
- **Anonymity guarantees** - even organizers cannot see complete draw results
- **Draw algorithm constraints** - no self-assignment, no 2-person circles, respect exclusion rules
- **Soft delete pattern** - preserve data integrity for historical records
- **GDPR compliance** - minimal data collection with consent tracking
- **High availability** - serverless PostgreSQL with Neon
- **Scalability** - efficient indexing for critical queries

## Decision

### Technology Stack

**ORM:** Entity Framework Core 9.0.9
- Native support for PostgreSQL features (UUID generation, TIMESTAMPTZ)
- Fluent API for complex relationship configuration
- Migration-based schema versioning
- LINQ query translation for type-safe data access

**Database:** PostgreSQL via Neon (Serverless)
- UUID primary keys (cryptographically secure, unpredictable)
- NUMERIC(10,2) for precise currency calculations
- TIMESTAMPTZ for timezone-aware timestamps
- TEXT columns for unlimited wishlist content
- Native gen_random_uuid() for ID generation

**Identity:** ASP.NET Core Identity 9.0.9
- Built-in user management and authentication
- Password hashing (PBKDF2) and security
- Extended with custom fields (FirstName, LastName, soft delete)

### Schema Architecture

#### 1. Entities (6 Core Tables)

**ApplicationUser** (extends IdentityUser)
```csharp
- FirstName, LastName (VARCHAR 100)
- CreatedAt, LastLoginAt (TIMESTAMPTZ)
- IsDeleted, DeletedAt (soft delete pattern)
- Navigation: OrganizedGroups, GroupParticipants, Assignments, EmailNotifications
```

**Group** (Secret Santa events)
```csharp
- Id (UUID, PK)
- Name (VARCHAR 200)
- OrganizerUserId (FK → ApplicationUser)
- Budget (NUMERIC 10,2)
- InvitationToken (UUID, UNIQUE)
- DrawCompletedAt (TIMESTAMPTZ, nullable)
- Navigation: Participants, ExclusionRules, Assignments, EmailNotifications
```

**GroupParticipant** (junction table)
```csharp
- Composite PK: (GroupId, UserId)
- BudgetSuggestion (NUMERIC 10,2, nullable)
- WishlistContent (TEXT, nullable)
- WishlistLastModified (TIMESTAMPTZ)
- Cascade delete with Group
```

**ExclusionRule** (draw algorithm constraints)
```csharp
- Id (UUID, PK)
- GroupId, UserId1, UserId2 (bidirectional pairs)
- CreatedByUserId (FK → ApplicationUser)
- CHECK: UserId1 <> UserId2
- UNIQUE: (GroupId, UserId1, UserId2)
- Cascade delete with Group
```

**Assignment** (draw results)
```csharp
- Id (UUID, PK)
- GroupId, SantaUserId, RecipientUserId
- CHECK: SantaUserId <> RecipientUserId
- UNIQUE: (GroupId, SantaUserId)
- UNIQUE: (GroupId, RecipientUserId)
- Cascade delete with Group
```

**EmailNotification** (queue system)
```csharp
- Id (UUID, PK)
- Type (enum: DrawCompleted, WishlistUpdated)
- RecipientUserId, GroupId
- ScheduledAt, SentAt, FirstAttemptAt, LastAttemptAt
- AttemptCount (INT, default 0)
- LastError (TEXT, nullable)
- Cascade delete with Group
```

#### 2. Configuration Strategy

**Fluent API Pattern:**
- Separate configuration classes per entity (IEntityTypeConfiguration<T>)
- Assembly scanning: `ApplyConfigurationsFromAssembly()`
- Centralized in `Data/Configurations/` folder

**Key Configurations:**
```csharp
// Check constraints (EF Core 9 syntax)
builder.ToTable(t => t.HasCheckConstraint(
    "CK_Assignments_NoSelfAssignment",
    "\"SantaUserId\" <> \"RecipientUserId\""));

// Composite primary key
builder.HasKey(gp => new { gp.GroupId, gp.UserId });

// Cascade vs Restrict
- Cascade: Group → GroupParticipants, ExclusionRules, Assignments, EmailNotifications
- Restrict: ApplicationUser → All references (soft delete)

// PostgreSQL conventions
- All DateTime → timestamp with time zone
- Enums → VARCHAR(50) stored as string
- UUID generation → gen_random_uuid()
```

#### 3. Indexing Strategy

**Performance-Critical Indexes:**
```sql
-- Groups
IX_Groups_InvitationToken (UNIQUE)
IX_Groups_OrganizerUserId
IX_Groups_DrawCompletedAt (PARTIAL, WHERE DrawCompletedAt IS NULL)

-- GroupParticipants
PK_GroupParticipants (GroupId, UserId) - auto-created
IX_GroupParticipants_UserId (reverse lookup)

-- ExclusionRules
IX_ExclusionRules_GroupId
IX_ExclusionRules_Unique (GroupId, UserId1, UserId2) UNIQUE

-- Assignments
IX_Assignments_GroupId_SantaUserId (UNIQUE)
IX_Assignments_GroupId_RecipientUserId (UNIQUE)
IX_Assignments_SantaUserId

-- EmailNotifications
IX_EmailNotifications_Queue (PARTIAL, WHERE SentAt IS NULL)
IX_EmailNotifications_RecipientUserId
IX_EmailNotifications_GroupId
```

#### 4. Data Integrity Constraints

**Database-Level Constraints:**
- CHECK constraints prevent invalid data (self-assignment, same user in exclusion)
- UNIQUE constraints enforce business rules (one Santa per group)
- NOT NULL constraints on required fields
- CASCADE DELETE maintains referential integrity on group deletion

**Application-Level Logic:**
- Soft delete filtering: `WHERE IsDeleted = FALSE`
- Draw validation before execution
- Email deduplication (1-hour window for wishlist updates)
- Bidirectional exclusion rule enforcement

### Project Structure

```
SantaVibe.Api/
├── Data/
│   ├── Enums/
│   │   └── EmailNotificationType.cs
│   ├── Entities/
│   │   ├── ApplicationUser.cs
│   │   ├── Assignment.cs
│   │   ├── EmailNotification.cs
│   │   ├── ExclusionRule.cs
│   │   ├── Group.cs
│   │   └── GroupParticipant.cs
│   ├── Configurations/
│   │   ├── ApplicationUserConfiguration.cs
│   │   ├── AssignmentConfiguration.cs
│   │   ├── EmailNotificationConfiguration.cs
│   │   ├── ExclusionRuleConfiguration.cs
│   │   ├── GroupConfiguration.cs
│   │   └── GroupParticipantConfiguration.cs
│   └── ApplicationDbContext.cs
└── SantaVibe.Api.csproj
```

## Consequences

### Positive

1. **Type Safety:** Strong typing with C# entities prevents runtime errors
2. **Migration Support:** Schema changes tracked in version control
3. **Query Optimization:** LINQ queries translated to efficient SQL
4. **Relationship Management:** EF Core handles complex FK relationships automatically
5. **PostgreSQL Features:** Native UUID, TIMESTAMPTZ, CHECK constraints fully supported
6. **Separation of Concerns:** Configuration classes separate from entity models
7. **Testability:** DbContext can be mocked for unit testing
8. **Documentation:** Entity summaries and comments serve as living documentation
9. **GDPR Compliance:** Soft delete pattern preserves referential integrity
10. **Security:** UUID tokens prevent enumeration attacks

### Negative

1. **Learning Curve:** Team must understand EF Core conventions and Fluent API
2. **Performance Overhead:** ORM abstraction adds minimal latency vs raw SQL
3. **Partial Index Limitation:** Requires custom migration for partial indexes (not supported in Fluent API)
4. **Migration Complexity:** Schema changes require careful migration management
5. **N+1 Query Risk:** Developers must use `.Include()` to avoid lazy loading issues

### Mitigations

1. **Documentation:** Inline comments reference PRD requirements (FR-001, FR-002, etc.)
2. **Conventions:** Consistent naming (PK: Id, FK: {Entity}Id, Junction: Composite PK)
3. **Configuration Separation:** Each entity has dedicated configuration class
4. **Index Comments:** Configuration files document where partial indexes needed
5. **Build Validation:** Zero warnings/errors in compilation

## Implementation Details

### NuGet Packages Added
```xml
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="9.0.9" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.9" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.9" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.4" />
```

### DbContext Configuration
```csharp
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupParticipant> GroupParticipants => Set<GroupParticipant>();
    public DbSet<ExclusionRule> ExclusionRules => Set<ExclusionRule>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<EmailNotification> EmailNotifications => Set<EmailNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Identity tables
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // PostgreSQL: All DateTime → timestamp with time zone
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) ||
                    property.ClrType == typeof(DateTime?))
                {
                    property.SetColumnType("timestamp with time zone");
                }
            }
        }
    }
}
```

### Critical Business Rules Enforced

1. **Draw Completion Immutability:**
   - `DrawCompletedAt IS NULL` → draw not executed
   - `DrawCompletedAt IS NOT NULL` → draw completed (irreversible)
   - Budget becomes immutable after draw

2. **Anonymity Guarantee:**
   - Application layer filters: User can only query Assignments WHERE SantaUserId = CurrentUserId
   - No endpoint exposes complete draw results
   - Optional: PostgreSQL RLS policies for defense-in-depth

3. **Exclusion Rule Bidirectionality:**
   - Single row represents both A→B and B→A exclusion
   - Draw algorithm reads rules and enforces both directions
   - Unique constraint prevents duplicate pairs

4. **Email Notification Deduplication:**
   - Before inserting WishlistUpdated notification:
     - Check for pending notification with same (Type, RecipientUserId, GroupId)
     - If exists within 1-hour window, skip insert
   - Prevents spam from rapid wishlist edits

5. **Soft Delete Pattern:**
   - Users: `IsDeleted = TRUE`, `DeletedAt = NOW()`
   - Preserves FK integrity for Assignments, ExclusionRules
   - Application filters: `WHERE IsDeleted = FALSE`
   - Supports future GDPR "right to be forgotten" (anonymize vs delete)

## Next Steps

1. **Connection String Configuration:**
   - Add Neon PostgreSQL connection string to `appsettings.json`
   - Configure in `Program.cs` with DbContext registration

2. **Initial Migration:**
   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

3. **Partial Indexes (Custom Migration):**
   ```csharp
   public partial class AddPartialIndexes : Migration
   {
       protected override void Up(MigrationBuilder migrationBuilder)
       {
           migrationBuilder.Sql(@"
               CREATE INDEX IX_Groups_DrawCompletedAt
               ON ""Groups""(""DrawCompletedAt"")
               WHERE ""DrawCompletedAt"" IS NULL;

               CREATE INDEX IX_EmailNotifications_Queue
               ON ""EmailNotifications""(""ScheduledAt"", ""SentAt"")
               WHERE ""SentAt"" IS NULL;
           ");
       }
   }
   ```

4. **Optional - Row Level Security (RLS):**
   - Implement PostgreSQL RLS policies for Assignments table
   - Requires session variable management in EF Core connection setup
   - Recommended for production defense-in-depth

5. **Repository Pattern Implementation:**
   - Create repository interfaces for data access abstraction
   - Implement unit of work pattern for transactional consistency
   - Enable easier unit testing with mocked repositories

## References

- **PRD:** `docs/prd.md` - Functional Requirements
- **Database Plan:** `docs/db-plan.md` - Detailed Schema Specification
- **Tech Stack:** `docs/tech-stack.md` - Technology Decisions
- **EF Core 9 Docs:** https://learn.microsoft.com/ef/core/
- **Npgsql Provider:** https://www.npgsql.org/efcore/

## Related ADRs

- ADR-002: Repository Pattern Implementation (planned)
- ADR-003: Draw Algorithm Design (planned)
- ADR-004: Email Notification Service (planned)

---

**Compliance Matrix:**

| PRD Requirement | Database Support |
|----------------|------------------|
| FR-001, FR-002 | ASP.NET Core Identity (email/password auth) |
| FR-003 | ApplicationUser (FirstName, LastName, Email) |
| FR-004 | GroupParticipant.WishlistContent |
| FR-005, FR-019 | EmailNotification (delayed, deduplicated) |
| FR-006 | Group entity with Name |
| FR-007 | Group.InvitationToken (UUID, unique) |
| FR-008 | GroupParticipant deletion before draw |
| FR-009 | GroupParticipant.BudgetSuggestion (anonymous) |
| FR-010 | ExclusionRule entity |
| FR-011 | Group.DrawCompletedAt, Group.Budget |
| FR-012 | GroupParticipant composite PK (one join per user) |
| FR-013 | ExclusionRule validation (application layer) |
| FR-016 | Assignment entity with constraints |
| FR-017 | Assignment anonymity (application filtering) |
| FR-018 | EmailNotification (DrawCompleted type) |

**Build Status:** ✅ Success (0 warnings, 0 errors)
