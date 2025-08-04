-- =============================================================================
-- Project Zenith: Write Database Schema (User Identity Management)
-- =============================================================================
-- This script defines the normalized tables required for user identity,
-- roles, and credentials. It is designed for transactional integrity (3NF).
-- =============================================================================

-- Drop tables in reverse order of dependency if they already exist
-- This is useful for development to allow the script to be re-runnable.
IF OBJECT_ID('dbo.Credentials', 'U') IS NOT NULL DROP TABLE dbo.Credentials;
IF OBJECT_ID('dbo.UserRoles', 'U') IS NOT NULL DROP TABLE dbo.UserRoles;
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL DROP TABLE dbo.Roles;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
GO

-- =============================================================================
-- Table: dbo.Users
-- Purpose: Stores the core, non-sensitive profile information for each user.
-- =============================================================================
CREATE TABLE dbo.Users (
    -- The primary key for the user, a unique identifier.
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),

    -- The user's email address, used for login and notifications.
    -- Must be unique across the entire system.
    Email NVARCHAR(256) NOT NULL,

    -- The user's public display name. Can be null initially.
    -- Must be unique if provided.
    Username NVARCHAR(100) NULL,

    -- A short biography or description for the user's profile.
    Bio NVARCHAR(500) NULL,

    -- A URL pointing to the user's avatar image.
    AvatarUrl NVARCHAR(500) NULL,

    -- Timestamps for auditing.
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,

    -- Constraints to enforce data integrity.
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT UQ_Users_Username UNIQUE (Username)
);
GO

-- =============================================================================
-- Table: dbo.Roles
-- Purpose: Stores the role definitions for the entire system (e.g., User, Developer, Admin).
-- This acts as a lookup table.
-- =============================================================================
CREATE TABLE dbo.Roles (
    -- The primary key for the role.
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),

    -- The name of the role (e.g., "User", "Developer", "Admin").
    -- Must be unique.
    Name NVARCHAR(50) NOT NULL,

    -- Constraint to enforce unique role names.
    CONSTRAINT UQ_Roles_Name UNIQUE (Name)
);
GO

-- =============================================================================
-- Table: dbo.UserRoles
-- Purpose: A many-to-many join table that maps users to their assigned roles.
-- =============================================================================
CREATE TABLE dbo.UserRoles (
    -- Foreign key referencing the user.
    UserId UNIQUEIDENTIFIER NOT NULL,

    -- Foreign key referencing the role.
    RoleId UNIQUEIDENTIFIER NOT NULL,

    -- A composite primary key ensures that a user can only have a specific role once.
    CONSTRAINT PK_UserRoles PRIMARY KEY (UserId, RoleId),

    -- Foreign key constraints to maintain referential integrity.
    -- ON DELETE CASCADE means if a user or role is deleted, the corresponding mapping is also deleted.
    CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id) ON DELETE CASCADE
);
GO

-- =============================================================================
-- Table: dbo.Credentials
-- Purpose: Securely stores the user's password hash. This table is separated
-- from the main Users table for security (Principle of Least Privilege).
-- =============================================================================
CREATE TABLE dbo.Credentials (
    -- This table has a one-to-one relationship with the Users table.
    -- The UserId is both the primary key and the foreign key.
    UserId UNIQUEIDENTIFIER PRIMARY KEY,

    -- The securely hashed password string. This field should be large enough
    -- to accommodate the output of modern hashing algorithms like BCrypt.
    PasswordHash NVARCHAR(256) NOT NULL,

    -- Timestamp for auditing credential creation.
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    -- Foreign key constraint to maintain referential integrity.
    -- If a user is deleted, their credentials are also deleted.
    CONSTRAINT FK_Credentials_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE
);
GO

-- =============================================================================
-- Seed initial roles required by the system.
-- This ensures that the "User", "Developer", and "Admin" roles always exist
-- with known, stable IDs if needed.
-- =============================================================================
INSERT INTO dbo.Roles (Id, Name) VALUES 
(NEWID(), 'User'),
(NEWID(), 'Developer'),
(NEWID(), 'Admin');
GO

PRINT 'User Identity schema created and initial roles seeded successfully.';
GO