-- =============================================================================
-- Project Zenith: Write Database Schema (Application Lifecycle)
-- =============================================================================
-- This script defines the normalized tables required for managing developers,
-- applications, versions, and their associated files. It builds upon the
-- User Identity schema.
-- =============================================================================

-- Drop tables in reverse order of dependency if they already exist
IF OBJECT_ID('dbo.Versions', 'U') IS NOT NULL DROP TABLE dbo.Versions;
IF OBJECT_ID('dbo.Files', 'U') IS NOT NULL DROP TABLE dbo.Files;
IF OBJECT_ID('dbo.Apps', 'U') IS NOT NULL DROP TABLE dbo.Apps;
IF OBJECT_ID('dbo.Developers', 'U') IS NOT NULL DROP TABLE dbo.Developers;
GO

-- =============================================================================
-- Table: dbo.Developers
-- Purpose: Stores profile information for users who have been promoted to the
-- "Developer" role. This establishes a one-to-one relationship with the Users table.
-- =============================================================================
CREATE TABLE dbo.Developers (
    -- The UserId is both the primary key and the foreign key, linking directly
    -- to the core user record.
    UserId UNIQUEIDENTIFIER PRIMARY KEY,

    -- A public-facing description of the developer or their company.
    Description NVARCHAR(1000) NULL,

    -- A public contact email, which may be different from their login email.
    ContactEmail NVARCHAR(256) NULL,

    -- Timestamp for auditing when the user became a developer.
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    -- Foreign key constraint to maintain referential integrity.
    -- If a user is deleted, their developer profile is also deleted.
    CONSTRAINT FK_Developers_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE
);
GO

-- =============================================================================
-- Table: dbo.Apps
-- Purpose: Stores the core metadata for each application submitted to the store.
-- =============================================================================
CREATE TABLE dbo.Apps (
    -- The primary key for the application.
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),

    -- Foreign key linking the app to its owner.
    DeveloperId UNIQUEIDENTIFIER NOT NULL,

    -- The public name of the application.
    Name NVARCHAR(255) NOT NULL,

    -- A detailed description of the application (supports Markdown).
    Description NVARCHAR(MAX) NULL,

    -- The primary category of the application (e.g., "Productivity", "Game").
    Category NVARCHAR(100) NOT NULL,

    -- The target platform ("Windows", "Android").
    Platform NVARCHAR(50) NOT NULL,

    -- The one-time purchase price of the application. 0.00 for free apps.
    Price DECIMAL(18, 2) NOT NULL DEFAULT 0.00,

    -- The current moderation and visibility status of the app.
    Status NVARCHAR(50) NOT NULL DEFAULT 'Draft',

    -- Timestamps for auditing.
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,

    -- Constraints
    CONSTRAINT FK_Apps_Developers FOREIGN KEY (DeveloperId) REFERENCES dbo.Developers(UserId),
    -- Ensures the Status column only contains valid values.
    CONSTRAINT CK_Apps_Status CHECK (Status IN ('Draft', 'Pending', 'Published', 'Rejected', 'Banned'))
);
GO

-- =============================================================================
-- Table: dbo.Files
-- Purpose: Stores metadata about a specific file that has been uploaded.
-- The actual file binary is stored in a cloud service like Azure Blob Storage.
-- =============================================================================
CREATE TABLE dbo.Files (
    -- The primary key for the file record.
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),

    -- The full path or URI to the file in the external blob storage.
    Path NVARCHAR(1024) NOT NULL,

    -- The size of the file in bytes.
    Size BIGINT NOT NULL,

    -- The cryptographic checksum (e.g., SHA256) of the file, used to verify integrity.
    Checksum NVARCHAR(256) NOT NULL,

    -- Timestamp for auditing when the file was uploaded.
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    -- A unique constraint on the checksum can prevent duplicate file uploads.
    CONSTRAINT UQ_Files_Checksum UNIQUE (Checksum)
);
GO

-- =============================================================================
-- Table: dbo.Versions
-- Purpose: Represents a specific version of an application, linking an app
-- to a downloadable file and its changelog.
-- =============================================================================
CREATE TABLE dbo.Versions (
    -- The primary key for the version record.
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),

    -- Foreign key linking this version to its parent application.
    AppId UNIQUEIDENTIFIER NOT NULL,

    -- The semantic version number string (e.g., "1.0.0", "2.1.0-beta").
    VersionNumber NVARCHAR(50) NOT NULL,

    -- Release notes or a changelog for this specific version (supports Markdown).
    Changelog NVARCHAR(MAX) NULL,

    -- Foreign key linking this version to a specific uploaded file record.
    FileId UNIQUEIDENTIFIER NOT NULL,

    -- Timestamp for auditing when this version was created.
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    -- Constraints
    CONSTRAINT FK_Versions_Apps FOREIGN KEY (AppId) REFERENCES dbo.Apps(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Versions_Files FOREIGN KEY (FileId) REFERENCES dbo.Files(Id),
    -- Ensures that a specific app cannot have duplicate version numbers.
    CONSTRAINT UQ_Versions_AppId_VersionNumber UNIQUE (AppId, VersionNumber)
);
GO

PRINT 'Application Lifecycle schema created successfully.';
GO