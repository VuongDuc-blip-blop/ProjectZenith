# Project Zenith: Database Migrations for the Write DB

## 1. Overview

This document outlines the process for managing the **Write Database** schema using **Entity Framework Core Migrations**. Migrations are the definitive, code-first approach for evolving the database schema over time.

### The Role of Migrations

- **Source of Truth:** The collection of migration files in our project serves as the **chronological history and the ultimate source of truth** for the database schema. The schema is defined by our C# domain models and `DbContext` configuration, not by raw SQL scripts.
- **Repeatable & Version Controlled:** Each migration is a C# file that can be checked into source control (Git). This allows us to track every change to the database, share it with the team, and apply it consistently across different environments (local development, staging, production).
- **Automated Schema Management:** EF Core's tooling can compare our C# models to the current database state (or an existing migration snapshot) and automatically generate the necessary SQL `CREATE`, `ALTER`, or `DROP` statements.

## 2. Migration Commands (EF Core CLI)

All migration operations are performed using the `dotnet ef` command-line tools. These commands must be run from a terminal in the **root of the solution directory** (`/ProjectZenith/`).

### 2.1. Generating a New Migration

This command is used whenever you make a change to your domain models or `DbContext` configuration in the `ProjectZenith.Api.Write` project.

```bash
# General Syntax:
# dotnet ef migrations add <MigrationName> --project <PathToDbContextProject> --startup-project <PathToStartupProject>

# Our Specific Command:
dotnet ef migrations add InitialCreate --project src/Api.Write/ --startup-project src/Api.Write/
```

- **`InitialCreate`**: This is the descriptive name for our first migration. Subsequent migrations will have names like `AddAppMonetization` or `FixUserBioLength`.
- **`--project src/Api.Write/`**: This tells EF Core where to find our `WriteDbContext` and where to create the new `Migrations` folder.
- **`--startup-project src/Api.Write/`**: This tells EF Core which project to build and run to get the necessary configuration (like the database provider and connection string). For our Write DB, both are the same project.

### 2.2. Applying Migrations to the Database

This command reads the migration files and executes the corresponding SQL to update the database schema.

```bash
# General Syntax:
# dotnet ef database update --project <PathToDbContextProject> --startup-project <PathToStartupProject>

# Our Specific Command:
dotnet ef database update --project src/Api.Write/ --startup-project src/Api.Write/
```

## 3. Applying Migrations at Application Startup

For local development and some deployment scenarios, it's convenient to have the application automatically apply any pending migrations when it starts up. This ensures the database is always in sync with the code.

This logic is added to the `Program.cs` file of the `Api.Write` project.

**Example `Program.cs` snippet:**

```csharp
// ... after 'var app = builder.Build();'

// --- Apply Migrations Automatically ---
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WriteDbContext>();
    // This will create the database if it doesn't exist and apply pending migrations.
    dbContext.Database.Migrate();
}
// ------------------------------------

app.Run();
```

**Note:** While convenient for development, automatically applying migrations in a multi-instance production environment should be done with caution. A dedicated migration step in a CI/CD pipeline is often the preferred production strategy.

## 4. Verification

After applying the `InitialCreate` migration, you must verify that the database schema was created correctly.

1.  **Start the Infrastructure:** Run `docker-compose up -d` to ensure the `write-db` container is running.
2.  **Apply the Migration:** Run the `dotnet ef database update ...` command as described above.
3.  **Connect to the Database:** Use a tool like Azure Data Studio or SSMS to connect to the Write DB at `localhost,1401`.
4.  **Verification Checklist:**
    - **✅ Tables:** Confirm that all tables (`Users`, `Roles`, `Apps`, `Purchases`, etc.) have been created.
    - **✅ Constraints:** Inspect a few key tables to ensure constraints are present.
      - Check the `Reviews` table for the `CK_Reviews_Rating` check constraint.
      - Check the `Users` table for the `UQ_Users_Email` unique index.
    - **✅ Seeded Data:** Run a query to confirm the initial roles were seeded correctly:
      ```sql
      SELECT * FROM dbo.Roles;
      ```
      You should see the "User", "Developer", and "Admin" roles.
    - **✅ \_\_EFMigrationsHistory Table:** Confirm that a table named `__EFMigrationsHistory` exists. This is how EF Core tracks which migrations have already been applied to this database. It should contain one entry for your `InitialCreate` migration.

## 5. Application-Layer Validation

As documented in `docs/database/write-schema-administration-design.md`, our `ModerationActions` table uses a polymorphic association for its target. This means there is no database-level `FOREIGN KEY` on the `TargetId` column.

It is a **critical responsibility** of the application layer (specifically, the command handlers in the `Api.Write` project) to validate that the `TargetId` exists in the correct table (based on `TargetType`) **before** creating a `ModerationAction` record. This prevents orphaned records and maintains the integrity of our audit trail.

```

```
