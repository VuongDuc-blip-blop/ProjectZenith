-- =============================================================================
-- Project Zenith: Write Database Schema (Community Interactions)
-- =============================================================================
-- This script defines the normalized tables required for managing reviews,
-- ratings, and abuse reports.
-- =============================================================================

-- Drop tables in reverse order of dependency if they already exist
IF OBJECT_ID('dbo.AbuseReports', 'U') IS NOT NULL DROP TABLE dbo.AbuseReports;
IF OBJECT_ID('dbo.Reviews', 'U') IS NOT NULL DROP TABLE dbo.Reviews;
GO

-- =============================================================================
-- Table: dbo.Reviews
-- Purpose: Stores user-submitted reviews and ratings for applications.
-- This table combines the concept of a rating (star value) and an optional
-- written comment into a single record for efficiency and simplicity.
-- =============================================================================
CREATE TABLE dbo.Reviews (
    -- The primary key for the review.
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),

    -- Foreign key linking this review to a specific application.
    AppId UNIQUEIDENTIFIER NOT NULL,

    -- Foreign key linking this review to the user who wrote it.
    UserId UNIQUEIDENTIFIER NOT NULL,

    -- The star rating value, from 1 to 5. This field is required.
    Rating INT NOT NULL,

    -- The optional written comment for the review.
    Comment NVARCHAR(1000) NULL,

    -- Flag to indicate if the comment has been edited after its initial posting.
    IsEdited BIT NOT NULL DEFAULT 0,

    -- Timestamps for auditing.
    PostedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,

    -- Constraints
    CONSTRAINT FK_Reviews_Apps FOREIGN KEY (AppId) REFERENCES dbo.Apps(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Reviews_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,
    -- A user can only submit one review per application.
    CONSTRAINT UQ_Reviews_AppId_UserId UNIQUE (AppId, UserId),
    -- Ensures the rating value is always between 1 and 5.
    CONSTRAINT CK_Reviews_Rating CHECK (Rating >= 1 AND Rating <= 5)
);
GO

-- =============================================================================
-- Table: dbo.AbuseReports
-- Purpose: Stores reports submitted by users against potentially abusive or
-- inappropriate content (e.g., a review, an app, or another user).
-- =============================================================================
CREATE TABLE dbo.AbuseReports (
    -- The primary key for the abuse report.
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),

    -- The ID of the user who is submitting the report.
    ReporterId UNIQUEIDENTIFIER NOT NULL,

    -- --- The target of the report (at least one of these should be non-null) ---
    -- The ID of the review being reported, if applicable.
    ReviewId UNIQUEIDENTIFIER NULL,
    -- The ID of the app being reported, if applicable.
    AppId UNIQUEIDENTIFIER NULL,
    -- The ID of the user being reported, if applicable.
    UserId UNIQUEIDENTIFIER NULL,
    -- --------------------------------------------------------------------------

    -- The user-provided reason for the report.
    Reason NVARCHAR(500) NOT NULL,

    -- The current moderation status of the report.
    Status NVARCHAR(50) NOT NULL DEFAULT 'New',

    -- Timestamp for auditing when the report was submitted.
    ReportedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    -- Constraints
    CONSTRAINT FK_AbuseReports_Reporter FOREIGN KEY (ReporterId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_AbuseReports_Review FOREIGN KEY (ReviewId) REFERENCES dbo.Reviews(Id),
    -- Use ON DELETE NO ACTION here to prevent accidental data loss if an app or user is deleted.
    -- The report should be resolved manually by an admin first.
    CONSTRAINT FK_AbuseReports_App FOREIGN KEY (AppId) REFERENCES dbo.Apps(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_AbuseReports_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE NO ACTION,

    -- Ensures the Status column only contains valid values.
    CONSTRAINT CK_AbuseReports_Status CHECK (Status IN ('New', 'UnderReview', 'Resolved')),

    -- Ensures that a report targets at least one entity.
    CONSTRAINT CK_AbuseReports_HasTarget CHECK (ReviewId IS NOT NULL OR AppId IS NOT NULL OR UserId IS NOT NULL)
);
GO

PRINT 'Community Interactions schema created successfully.';
GO