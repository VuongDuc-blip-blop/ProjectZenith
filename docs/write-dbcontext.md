# Project Zenith: WriteDbContext Setup

## 1. Overview

The `WriteDbContext` is the Entity Framework Core database context for the **Write Stack** of our CQRS architecture. Its primary role is to serve as the gateway for all state-changing operations in the system. It is responsible for translating C# domain model objects into database records, managing transactions, and ensuring data integrity for our normalized SQL Server database.

Every command handled by the `Api.Write` project (e.g., `RegisterUserCommand`, `SubmitAppCommand`) will interact with the `WriteDbContext` to persist changes. It is the final checkpoint for data before it becomes the "source of truth."

## 2. Configuration

The `WriteDbContext` is configured in the `Program.cs` file of the `Api.Write` project.

### 2.1. Database Provider and Connection String

- **Provider:** It is configured to use **Microsoft SQL Server** via the `UseSqlServer()` extension method.
- **Connection String:** The connection string is retrieved from the configuration system using `builder.Configuration.GetConnectionString("WriteDb")`. For local development, this value is provided securely via the **.NET User Secrets** tool, ensuring that sensitive credentials are never checked into source control.

**Example Registration in `Program.cs`:**

```csharp
builder.Services.AddDbContext<WriteDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("WriteDb");
    options.UseSqlServer(connectionString,
        b => b.MigrationsAssembly("ProjectZenith.Api.Write"));
});
```

### 2.2. Migrations Assembly

The migrations are configured to reside within the `ProjectZenith.Api.Write` project itself using `MigrationsAssembly()`. This keeps the schema evolution history co-located with the API that owns the database.

## 3. Entity Mapping and Relationships

The `WriteDbContext` maps all domain entities to their corresponding database tables. All relationships are explicitly defined in the `OnModelCreating` method using the Fluent API to ensure clarity and correctness.

### 3.1. Mapped Entities

- **Identity:** `User`, `Role`, `UserRole`, `Credential`, `Developer`
- **App Lifecycle:** `App`, `AppVersion`, `AppFile`
- **Community:** `Review`, `AbuseReport`
- **Monetization:** `Purchase`, `Transaction`, `Payout`
- **Administration:** `ModerationAction`, `SystemLog`

### 3.2. Key Relationship Configurations

- **One-to-One:** Relationships like `User <-> Credential` and `User <-> Developer` are configured with a shared primary key, ensuring a true one-to-one mapping.
- **One-to-Many:** Standard relationships like `App -> Versions` and `Purchase -> Transactions` are configured with foreign keys and collection navigation properties.
- **Many-to-Many:** The `User <-> Role` relationship is implemented using an explicit join entity (`UserRole`) with two one-to-many relationships pointing from it.
- **Complex Relationships:** The dual relationship between `User` and `AbuseReport` (`Reporter` and `ReportedUser`) is explicitly configured to resolve ambiguity.
- **Delete Behaviors:** Critical `ON DELETE` behaviors are explicitly set (e.g., `Cascade` for user data, `Restrict` for financial records, `SetNull` for audit logs) to protect data integrity.

## 4. Initial Data Seeding

To ensure the platform has a foundational set of roles, the `OnModelCreating` method uses `modelBuilder.Entity<Role>().HasData(...)` to seed the database.

- **Seeded Roles:** "User", "Developer", "Admin".
- **Implementation:** These roles are seeded with hardcoded, predictable GUIDs. This allows the application code to reliably reference these essential roles without needing to query the database first.

## 5. Application-Layer Validation for Polymorphic Relationships

A key design choice in our schema is the use of a **polymorphic association** in the `ModerationActions` table, where the `TargetId` column can refer to an ID from different tables (`Apps`, `Users`, etc.).

- **The Trade-Off:** This design provides immense flexibility but means we cannot use a database-level `FOREIGN KEY` constraint on `TargetId`.
- **The Responsibility:** Consequently, the responsibility for ensuring referential integrity for this relationship shifts to the **application layer**.
- **Implementation:** Before any `ModerationAction` is saved, the command handler or a dedicated validator **must** perform a check. It will use the `TargetType` property to determine which table to query (e.g., `Apps`, `Users`) and verify that a record with the given `TargetId` actually exists. This **application-layer validation** is a critical step that prevents orphaned moderation records and ensures the integrity of our audit trail.

```

```
