# SantaVibe Database Schema

## Overview

This document defines the PostgreSQL database schema for the SantaVibe Secret Santa application. The schema is designed for use with Entity Framework Core 9, ASP.NET Core Identity, and Neon (Serverless PostgreSQL).

## Tables

### 1. AspNetUsers (ASP.NET Core Identity - Extended)

Extends the standard `IdentityUser` table with application-specific fields.

| Column Name | Data Type | Constraints | Description |
|-------------|-----------|-------------|-------------|
| Id | VARCHAR(450) | PRIMARY KEY | User identifier (from Identity) |
| UserName | NVARCHAR(256) | NOT NULL, UNIQUE | Email address (from Identity) |
| NormalizedUserName | NVARCHAR(256) | NOT NULL, UNIQUE | Normalized username (from Identity) |
| Email | NVARCHAR(256) | NOT NULL | Email address (from Identity) |
| NormalizedEmail | NVARCHAR(256) | NOT NULL | Normalized email (from Identity) |
| EmailConfirmed | BOOLEAN | NOT NULL, DEFAULT FALSE | Email verification status (from Identity) |
| PasswordHash | NVARCHAR(MAX) | NULL | Hashed password (from Identity) |
| SecurityStamp | NVARCHAR(MAX) | NULL | Security stamp (from Identity) |
| ConcurrencyStamp | NVARCHAR(MAX) | NULL | Concurrency token (from Identity) |
| PhoneNumber | NVARCHAR(MAX) | NULL | Phone number (from Identity) |
| PhoneNumberConfirmed | BOOLEAN | NOT NULL, DEFAULT FALSE | Phone verification status (from Identity) |
| TwoFactorEnabled | BOOLEAN | NOT NULL, DEFAULT FALSE | 2FA status (from Identity) |
| LockoutEnd | TIMESTAMPTZ | NULL | Lockout end time (from Identity) |
| LockoutEnabled | BOOLEAN | NOT NULL, DEFAULT FALSE | Lockout enabled (from Identity) |
| AccessFailedCount | INTEGER | NOT NULL, DEFAULT 0 | Failed login attempts (from Identity) |
| FirstName | VARCHAR(100) | NOT NULL | User's first name (FR-003) |
| LastName | VARCHAR(100) | NOT NULL | User's last name (FR-003) |
| CreatedAt | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | Account creation timestamp |
| LastLoginAt | TIMESTAMPTZ | NULL | Last successful login timestamp |
| IsDeleted | BOOLEAN | NOT NULL, DEFAULT FALSE | Soft delete flag |
| DeletedAt | TIMESTAMPTZ | NULL | Soft delete timestamp |

**Notes:**
- Inherits all standard ASP.NET Core Identity fields
- Soft delete pattern preserves foreign key integrity
- Email field serves as primary identifier for login (FR-001, FR-002)

### 2. Groups

Represents a Secret Santa event.

| Column Name | Data Type | Constraints | Description |
|-------------|-----------|-------------|-------------|
| Id | UUID | PRIMARY KEY, DEFAULT gen_random_uuid() | Group identifier |
| Name | VARCHAR(200) | NOT NULL | Group name (FR-006) |
| OrganizerUserId | VARCHAR(450) | NOT NULL, FK → AspNetUsers(Id) | Organizer user reference |
| Budget | NUMERIC(10,2) | NULL | Final budget in PLN (FR-011) |
| InvitationToken | UUID | NOT NULL, UNIQUE, DEFAULT gen_random_uuid() | Shareable invitation token (FR-007) |
| DrawCompletedAt | TIMESTAMPTZ | NULL | Draw completion timestamp (FR-011) |
| CreatedAt | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | Group creation timestamp |
| UpdatedAt | TIMESTAMPTZ | NULL | Last modification timestamp |

**Business Rules:**
- `DrawCompletedAt IS NULL` indicates draw has not been performed
- `DrawCompletedAt IS NOT NULL` indicates draw is complete and irreversible
- Budget can be set before draw and becomes immutable after draw
- Organizer is automatically added as participant via GroupParticipants

### 3. GroupParticipants

Junction table for many-to-many relationship between Users and Groups.

| Column Name | Data Type | Constraints | Description |
|-------------|-----------|-------------|-------------|
| GroupId | UUID | PRIMARY KEY, FK → Groups(Id) ON DELETE CASCADE | Group reference |
| UserId | VARCHAR(450) | PRIMARY KEY, FK → AspNetUsers(Id) | User reference |
| BudgetSuggestion | NUMERIC(10,2) | NULL | Anonymous budget suggestion (FR-009) |
| JoinedAt | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | Join timestamp |
| WishlistContent | TEXT | NULL | User's wishlist (FR-004) |
| WishlistLastModified | TIMESTAMPTZ | NULL | Last wishlist update timestamp |

**Composite Primary Key:** `(GroupId, UserId)`

**Business Rules:**
- Each user can join a group only once
- Budget suggestion is optional but recommended
- Cascade delete when group is deleted
- User removal before draw deletes this record (FR-008)

### 4. ExclusionRules

Defines bidirectional exclusion pairs for the draw algorithm.

| Column Name | Data Type | Constraints | Description |
|-------------|-----------|-------------|-------------|
| Id | UUID | PRIMARY KEY, DEFAULT gen_random_uuid() | Rule identifier |
| GroupId | UUID | NOT NULL, FK → Groups(Id) ON DELETE CASCADE | Group reference |
| UserId1 | VARCHAR(450) | NOT NULL, FK → AspNetUsers(Id) | First user in exclusion pair |
| UserId2 | VARCHAR(450) | NOT NULL, FK → AspNetUsers(Id) | Second user in exclusion pair |
| CreatedByUserId | VARCHAR(450) | NOT NULL, FK → AspNetUsers(Id) | Organizer who created rule |
| CreatedAt | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | Rule creation timestamp |

**Constraints:**
- `CHECK (UserId1 <> UserId2)` - prevents same user in both positions
- `UNIQUE (GroupId, UserId1, UserId2)` - prevents duplicate rules
- Bidirectional: if A→B exclusion exists, algorithm treats as B→A automatically

**Business Rules:**
- Only organizer can create exclusion rules (FR-010)
- Rules validated before draw to ensure solvable assignment (FR-013)
- Cascade delete when group is deleted

### 5. Assignments

Stores Secret Santa draw results.

| Column Name | Data Type | Constraints | Description |
|-------------|-----------|-------------|-------------|
| Id | UUID | PRIMARY KEY, DEFAULT gen_random_uuid() | Assignment identifier |
| GroupId | UUID | NOT NULL, FK → Groups(Id) ON DELETE CASCADE | Group reference |
| SantaUserId | VARCHAR(450) | NOT NULL, FK → AspNetUsers(Id) | User buying the gift |
| RecipientUserId | VARCHAR(450) | NOT NULL, FK → AspNetUsers(Id) | User receiving the gift |
| AssignedAt | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | Assignment creation timestamp |

**Constraints:**
- `UNIQUE (GroupId, SantaUserId)` - each Santa assigned exactly once per group
- `UNIQUE (GroupId, RecipientUserId)` - each Recipient has exactly one Santa per group
- `CHECK (SantaUserId <> RecipientUserId)` - no self-assignment

**Business Rules:**
- Created only after successful draw execution (FR-016)
- Enforces one-to-one mapping within a group
- Unique constraints prevent 2-person circles when combined with algorithm logic
- Anonymous to all users including organizer (FR-017)
- Cascade delete when group is deleted

### 6. EmailNotifications

Queue system for delayed email delivery.

| Column Name | Data Type | Constraints | Description |
|-------------|-----------|-------------|-------------|
| Id | UUID | PRIMARY KEY, DEFAULT gen_random_uuid() | Notification identifier |
| Type | VARCHAR(50) | NOT NULL | Notification type enum |
| RecipientUserId | VARCHAR(450) | NOT NULL, FK → AspNetUsers(Id) | Email recipient reference |
| GroupId | UUID | NOT NULL, FK → Groups(Id) ON DELETE CASCADE | Related group reference |
| ScheduledAt | TIMESTAMPTZ | NOT NULL | Scheduled send time |
| SentAt | TIMESTAMPTZ | NULL | Actual send timestamp |
| FirstAttemptAt | TIMESTAMPTZ | NULL | First delivery attempt timestamp |
| LastAttemptAt | TIMESTAMPTZ | NULL | Most recent attempt timestamp |
| AttemptCount | INTEGER | NOT NULL, DEFAULT 0 | Number of delivery attempts |
| LastError | TEXT | NULL | Last error message if failed |

**Email Types (C# Enum):**
- `DrawCompleted` - Sent to all participants when draw is executed (FR-018)
- `WishlistUpdated` - Sent to Santa when recipient updates wishlist (FR-019)

**Status Determination:**
- **Pending:** `SentAt IS NULL AND ScheduledAt <= NOW() AND AttemptCount < MAX_ATTEMPTS`
- **Failed:** `SentAt IS NULL AND AttemptCount >= MAX_ATTEMPTS`
- **Sent:** `SentAt IS NOT NULL`

**Business Rules:**
- Wishlist update notifications delayed by 1 hour (FR-005, FR-019)
- Deduplication: before inserting WishlistUpdated, check for existing pending notification with same `(Type, RecipientUserId, GroupId)` within 1-hour window
- Retry logic: exponential backoff with configurable max attempts
- Core functionality succeeds even if email fails (FR-034)
- Cascade delete when group is deleted

## Relationships

### User ↔ Group (Many-to-Many via GroupParticipants)
- One user can participate in multiple groups
- One group has multiple participants
- Junction table: `GroupParticipants`

### User → Group (One-to-Many as Organizer)
- One user can organize multiple groups
- One group has exactly one organizer
- Foreign key: `Groups.OrganizerUserId → AspNetUsers.Id`

### Group → ExclusionRules (One-to-Many)
- One group can have multiple exclusion rules
- One exclusion rule belongs to one group
- Foreign key: `ExclusionRules.GroupId → Groups.Id`
- Cascade delete

### Group → Assignments (One-to-Many)
- One group has multiple assignments (one per participant)
- One assignment belongs to one group
- Foreign key: `Assignments.GroupId → Groups.Id`
- Cascade delete

### User → Assignment (One-to-Many as Santa)
- One user can be Santa in multiple assignments (different groups)
- One assignment has one Santa
- Foreign key: `Assignments.SantaUserId → AspNetUsers.Id`

### User → Assignment (One-to-Many as Recipient)
- One user can be Recipient in multiple assignments (different groups)
- One assignment has one Recipient
- Foreign key: `Assignments.RecipientUserId → AspNetUsers.Id`

### Group → EmailNotifications (One-to-Many)
- One group can have multiple email notifications
- One notification belongs to one group
- Foreign key: `EmailNotifications.GroupId → Groups.Id`
- Cascade delete

### User → EmailNotifications (One-to-Many)
- One user can receive multiple email notifications
- One notification has one recipient
- Foreign key: `EmailNotifications.RecipientUserId → AspNetUsers.Id`

## Indexes

### Performance-Critical Indexes

```sql
-- Groups
CREATE UNIQUE INDEX IX_Groups_InvitationToken ON Groups(InvitationToken);
CREATE INDEX IX_Groups_OrganizerUserId ON Groups(OrganizerUserId);
CREATE INDEX IX_Groups_DrawCompletedAt ON Groups(DrawCompletedAt) WHERE DrawCompletedAt IS NULL; -- Partial index for active groups

-- GroupParticipants
-- Composite PK automatically creates index on (GroupId, UserId)
CREATE INDEX IX_GroupParticipants_UserId ON GroupParticipants(UserId); -- Reverse lookup: user's groups

-- ExclusionRules
CREATE INDEX IX_ExclusionRules_GroupId ON ExclusionRules(GroupId); -- Draw validation queries
CREATE UNIQUE INDEX IX_ExclusionRules_Unique ON ExclusionRules(GroupId, UserId1, UserId2); -- Prevent duplicate rules

-- Assignments
CREATE UNIQUE INDEX IX_Assignments_GroupId_SantaUserId ON Assignments(GroupId, SantaUserId);
CREATE UNIQUE INDEX IX_Assignments_GroupId_RecipientUserId ON Assignments(GroupId, RecipientUserId);
CREATE INDEX IX_Assignments_SantaUserId ON Assignments(SantaUserId); -- "Who am I buying for?" queries

-- EmailNotifications
CREATE INDEX IX_EmailNotifications_Queue ON EmailNotifications(ScheduledAt, SentAt) WHERE SentAt IS NULL; -- Queue processing
CREATE INDEX IX_EmailNotifications_RecipientUserId ON EmailNotifications(RecipientUserId);
CREATE INDEX IX_EmailNotifications_GroupId ON EmailNotifications(GroupId);
```

### Identity Indexes (Auto-created by ASP.NET Core Identity)
- `IX_AspNetUsers_NormalizedUserName` (UNIQUE)
- `IX_AspNetUsers_NormalizedEmail`

## PostgreSQL Policies (Row-Level Security)

### RLS for Assignments (Anonymity Guarantee)

Users can only view their own Santa assignment, not others.

```sql
-- Enable RLS on Assignments table
ALTER TABLE "Assignments" ENABLE ROW LEVEL SECURITY;

-- Policy: Users can only see assignments where they are the Santa
CREATE POLICY assignments_select_policy ON "Assignments"
    FOR SELECT
    USING (
        "SantaUserId" = current_setting('app.current_user_id', true)::VARCHAR
    );
```

**Implementation Note:**
- ASP.NET Core sets `app.current_user_id` session variable on each request
- Alternative: Handle in application layer with filtered queries

### RLS for GroupParticipants (Privacy)

Users can only see participants in groups they belong to.

```sql
-- Enable RLS on GroupParticipants table
ALTER TABLE "GroupParticipants" ENABLE ROW LEVEL SECURITY;

-- Policy: Users can see participants in groups they are part of
CREATE POLICY group_participants_select_policy ON "GroupParticipants"
    FOR SELECT
    USING (
        "GroupId" IN (
            SELECT "GroupId" FROM "GroupParticipants"
            WHERE "UserId" = current_setting('app.current_user_id', true)::VARCHAR
        )
    );
```

### RLS for EmailNotifications (Privacy)

Users can only see their own email notifications.

```sql
-- Enable RLS on EmailNotifications table
ALTER TABLE "EmailNotifications" ENABLE ROW LEVEL SECURITY;

-- Policy: Users can only see their own email notifications
CREATE POLICY email_notifications_select_policy ON "EmailNotifications"
    FOR SELECT
    USING (
        "RecipientUserId" = current_setting('app.current_user_id', true)::VARCHAR
    );
```

**MVP Recommendation:**
- RLS policies are **optional for MVP** if application layer properly filters queries
- Implement RLS for defense-in-depth security in production
- Requires session variable management in EF Core connection setup

## Data Type Standards

| Concept | PostgreSQL Type | C# Type | Notes |
|---------|-----------------|---------|-------|
| Money/Budget | NUMERIC(10,2) | decimal | Precise calculations, max 99,999,999.99 PLN |
| Timestamps | TIMESTAMPTZ | DateTime | Time zone aware, UTC recommended |
| User IDs | VARCHAR(450) | string | ASP.NET Core Identity default |
| Group/Other IDs | UUID | Guid | Cryptographically secure, indexed efficiently |
| Invitation Token | UUID | Guid | Secure, unpredictable |
| Email Type | VARCHAR(50) | enum | Store as string for readability |
| Wishlist | TEXT | string | Unlimited length for MVP |
| Names | VARCHAR(100-200) | string | Fixed max length for indexing |

## Cascade Delete Strategy

| Parent Table | Child Tables (Cascade Delete) | Rationale |
|--------------|-------------------------------|-----------|
| Groups | GroupParticipants, ExclusionRules, Assignments, EmailNotifications | Group deletion removes all related data |
| AspNetUsers | ❌ **Soft Delete Only** | Preserve foreign key integrity, GDPR compliance |

**Application Logic:**
- No group deletion UI in MVP
- User deletion: set `IsDeleted = TRUE`, `DeletedAt = NOW()`
- Participant removal: `DELETE FROM GroupParticipants WHERE GroupId = ? AND UserId = ?` (before draw only)

## Constraints Summary

### Check Constraints

```sql
-- ExclusionRules: Prevent same user in both positions
ALTER TABLE "ExclusionRules"
    ADD CONSTRAINT CK_ExclusionRules_DifferentUsers
    CHECK ("UserId1" <> "UserId2");

-- Assignments: No self-assignment
ALTER TABLE "Assignments"
    ADD CONSTRAINT CK_Assignments_NoSelfAssignment
    CHECK ("SantaUserId" <> "RecipientUserId");
```

### Unique Constraints

```sql
-- Groups: Unique invitation token
ALTER TABLE "Groups"
    ADD CONSTRAINT UQ_Groups_InvitationToken
    UNIQUE ("InvitationToken");

-- GroupParticipants: Composite PK ensures uniqueness
ALTER TABLE "GroupParticipants"
    ADD CONSTRAINT PK_GroupParticipants
    PRIMARY KEY ("GroupId", "UserId");

-- ExclusionRules: Prevent duplicate pairs
ALTER TABLE "ExclusionRules"
    ADD CONSTRAINT UQ_ExclusionRules_Pair
    UNIQUE ("GroupId", "UserId1", "UserId2");

-- Assignments: One Santa per group, one Recipient per group
ALTER TABLE "Assignments"
    ADD CONSTRAINT UQ_Assignments_GroupSanta
    UNIQUE ("GroupId", "SantaUserId");

ALTER TABLE "Assignments"
    ADD CONSTRAINT UQ_Assignments_GroupRecipient
    UNIQUE ("GroupId", "RecipientUserId");
```

## Design Decisions and Rationale

### 1. DrawCompletedAt vs Status Enum
**Decision:** Use nullable `DrawCompletedAt` timestamp instead of status enum.

**Rationale:**
- Simple boolean logic: `IS NULL` = draw not executed, `IS NOT NULL` = completed
- Provides audit trail (exact draw execution time)
- Avoids enum synchronization between database and C# code
- Aligns with event sourcing pattern (timestamp captures state change)

### 2. Email Status via Date Fields
**Decision:** Use `SentAt`, `FirstAttemptAt`, `LastAttemptAt` fields instead of status enum.

**Rationale:**
- Status derived from data: `SentAt IS NULL` = pending/failed, `IS NOT NULL` = sent
- Provides complete audit trail for debugging email issues
- Supports retry logic without additional state management
- `AttemptCount` field enables configurable max retry limit

### 3. Budget Suggestions in GroupParticipants
**Decision:** Store `BudgetSuggestion` in junction table rather than separate table.

**Rationale:**
- One suggestion per participant per group (natural fit for junction table)
- Avoids additional join for organizer's budget review query (FR-010)
- Nullable field allows optional suggestions (FR-012)
- Deleted automatically when participant is removed

### 4. Exclusion Rules Bidirectionality
**Decision:** Store as `(UserId1, UserId2)` pair; algorithm treats as bidirectional.

**Rationale:**
- Single record represents both A→B and B→A exclusions (storage efficiency)
- Application logic enforces bidirectionality in draw algorithm
- Unique constraint prevents duplicate pairs regardless of order
- Simpler than storing both directions or using check constraints for ordering

### 5. Soft Delete for Users
**Decision:** Use `IsDeleted` and `DeletedAt` fields instead of hard delete.

**Rationale:**
- Preserves foreign key integrity for historical data (assignments, exclusion rules)
- Supports future GDPR "right to be forgotten" implementation (anonymize instead of delete)
- Prevents orphaned records in groups/assignments
- Application layer filters `WHERE IsDeleted = FALSE` in queries

### 6. UUID for Primary Keys (Except Users)
**Decision:** Use `UUID` for Groups, Assignments, ExclusionRules, EmailNotifications; inherit `VARCHAR(450)` from Identity for Users.

**Rationale:**
- Security: UUIDs are unpredictable (prevents enumeration attacks on invitation tokens)
- Distribution: Better for sharding/partitioning in future scaling
- ASP.NET Core Identity uses `VARCHAR(450)` by default; consistent with framework
- PostgreSQL `gen_random_uuid()` provides efficient native generation

### 7. Partial Index on Active Groups
**Decision:** Create partial index `WHERE DrawCompletedAt IS NULL` on Groups table.

**Rationale:**
- Optimizes queries for active groups (most common: "groups awaiting draw")
- Smaller index footprint (excludes completed draws)
- PostgreSQL-specific feature; EF Core supports via raw SQL or custom migration

### 8. No Caching for AI Suggestions
**Decision:** Generate AI gift suggestions on-demand without database storage.

**Rationale:**
- MVP simplicity: avoid cache invalidation complexity
- Low usage frequency (1-2 requests per participant per group)
- Wishlist updates would require cache invalidation
- OpenRouter.ai API is fast enough for real-time generation (<5 seconds)

### 9. Deduplication Logic for Email Notifications
**Decision:** Application logic checks for existing pending notifications before insert (not database constraint).

**Rationale:**
- Complex time-based logic (1-hour window) difficult to express as constraint
- Allows flexibility for retry/batching strategies
- Prevents spam from rapid wishlist edits (FR-005)
- Background service handles deduplication in email queue processor

## Unresolved Configuration Items

These require application configuration values (not schema changes):

1. **Email Retry Configuration**
   - Max retry attempts: Recommended 3-5
   - Exponential backoff intervals: Recommended [5min, 15min, 1hr, 6hr]
   - Validation: `WHERE AttemptCount >= MAX_ATTEMPTS` determines "failed" status

2. **Invitation Token Format**
   - Currently: UUID (cryptographically secure)
   - Alternative: Custom alphanumeric tokens (requires generator logic)
   - Decision: UUID sufficient for MVP

3. **Wishlist Character Limit**
   - Currently: Unlimited (TEXT type)
   - Recommendation: Soft limit 5000 characters with UI warning (not database constraint)
   - Future: Consider migrating to VARCHAR(5000) if performance issues arise

4. **Group Name Editability**
   - Schema supports editing (UpdatedAt field tracks changes)
   - Application logic decision: Allow editing before draw? (Recommended: Yes)

5. **Participant Withdrawal After Draw**
   - No schema support for post-draw withdrawal (FR-008)
   - Future consideration: Add "withdrawn" flag to GroupParticipants with business logic for re-draw

## EF Core Migration Considerations

### Minimal API + EF Core 9 Compatibility

```csharp
// Example: DbContext configuration for constraints
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ExclusionRules check constraint
        modelBuilder.Entity<ExclusionRule>()
            .HasCheckConstraint("CK_ExclusionRules_DifferentUsers",
                "\"UserId1\" <> \"UserId2\"");

        // Assignments check constraint
        modelBuilder.Entity<Assignment>()
            .HasCheckConstraint("CK_Assignments_NoSelfAssignment",
                "\"SantaUserId\" <> \"RecipientUserId\"");

        // Composite PK for GroupParticipants
        modelBuilder.Entity<GroupParticipant>()
            .HasKey(gp => new { gp.GroupId, gp.UserId });

        // Partial index (raw SQL in migration)
        // See custom migration file for partial index creation
    }
}
```

### Custom Migration for Partial Index

```csharp
public partial class AddPartialIndexOnActiveGroups : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            @"CREATE INDEX IX_Groups_DrawCompletedAt
              ON ""Groups""(""DrawCompletedAt"")
              WHERE ""DrawCompletedAt"" IS NULL;"
        );
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"DROP INDEX IX_Groups_DrawCompletedAt;");
    }
}
```

## Query Optimization Examples

### Critical Query Patterns

```sql
-- Q1: Get user's assignment for a group (most frequent query)
SELECT a."RecipientUserId", u."FirstName", u."LastName", u."WishlistContent"
FROM "Assignments" a
JOIN "AspNetUsers" u ON a."RecipientUserId" = u."Id"
WHERE a."GroupId" = @GroupId
  AND a."SantaUserId" = @CurrentUserId
  AND u."IsDeleted" = FALSE;
-- Uses: IX_Assignments_GroupId_SantaUserId (unique index)

-- Q2: Get pending email notifications (background service)
SELECT *
FROM "EmailNotifications"
WHERE "SentAt" IS NULL
  AND "ScheduledAt" <= NOW()
  AND "AttemptCount" < @MaxAttempts
ORDER BY "ScheduledAt"
LIMIT 100;
-- Uses: IX_EmailNotifications_Queue (partial index)

-- Q3: Validate exclusion rules before draw
SELECT "UserId1", "UserId2"
FROM "ExclusionRules"
WHERE "GroupId" = @GroupId;
-- Uses: IX_ExclusionRules_GroupId

-- Q4: Get all participants for a group (draw algorithm)
SELECT "UserId"
FROM "GroupParticipants"
WHERE "GroupId" = @GroupId;
-- Uses: PK_GroupParticipants (composite PK index)

-- Q5: Get anonymous budget suggestions (organizer view)
SELECT "BudgetSuggestion"
FROM "GroupParticipants"
WHERE "GroupId" = @GroupId
  AND "BudgetSuggestion" IS NOT NULL
ORDER BY "BudgetSuggestion" ASC;
-- Uses: PK_GroupParticipants (composite PK index)
```

## Security Considerations

### Authentication & Authorization
- ASP.NET Core Identity handles password hashing (PBKDF2)
- JWT tokens for Angular frontend authentication
- Invitation tokens are UUIDs (unpredictable, secure)


## Monitoring and Observability

## Future Schema Enhancements (Post-MVP)

### Potential Additions

1. **GDPR Consent Tracking**
   - Add `ConsentVersion`, `ConsentTimestamp` to AspNetUsers
   - Separate `UserConsents` table for audit trail

2. **Group Templates**
   - `GroupTemplates` table for recurring events
   - Copy exclusion rules from previous year's group

3. **Anonymous Chat Messages**
   - `Messages` table with `SantaUserId`, `RecipientUserId`, `GroupId`
   - Support anonymous Q&A between Santa and Recipient

4. **Gift Purchase Status**
   - Add `GiftPurchased` (boolean) to Assignments
   - Optional feature for organizer visibility

5. **Multi-Currency Support**
   - Add `Currency` field to Groups table
   - Modify budget fields to store currency code

6. **Audit Logging**
   - Separate `AuditLogs` table for all mutations
   - Track organizer actions (rule creation, participant removal)

### Scalability Enhancements

1. **Partitioning**
   - Partition `EmailNotifications` by `ScheduledAt` (monthly partitions)
   - Partition `Assignments` by `GroupId` if query patterns support

2. **Read Replicas**
   - Neon supports read replicas for scaling read-heavy queries
   - Route assignment queries to replicas

3. **Caching Layer**
   - Redis cache for active groups and assignments
   - Invalidate on draw completion or wishlist updates

## Conclusion

This schema provides a solid foundation for the SantaVibe MVP, supporting all functional requirements from the PRD while maintaining flexibility for future enhancements. The design prioritizes:

- **Simplicity:** Minimal tables, denormalized where appropriate for MVP speed
- **Security:** Soft deletes, RLS policies, unpredictable tokens
- **Performance:** Strategic indexes for critical queries
- **Scalability:** UUID primary keys, partitioning-ready design
- **Maintainability:** Clear constraints, audit fields, comprehensive documentation

The schema is ready for EF Core 9 migration generation and can be deployed to Neon (Serverless PostgreSQL) without modifications.
