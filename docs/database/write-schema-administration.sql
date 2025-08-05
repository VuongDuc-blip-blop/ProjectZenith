-- =============================================================================
-- Project Zenith: Write Database Schema (Administration)
-- =============================================================================
-- This script defines the normalized tables required for tracking administrative
-- moderation actions and creating a comprehensive system audit log.
-- =============================================================================

-- Drop tables in reverse order of dependency if they already exist
IF OBJECT_ID('dbo.SystemLogs', 'U') IS NOT NULL DROP TABLE dbo.SystemLogs;
IF OBJECT_ID('dbo.ModerationActions', 'U') IS NOT NULL DROP TABLE dbo.ModerationActions;
GO

-- =============================================================================
-- Table: dbo.ModerationActions
-- Purpose: Records a specific, deliberate action taken by an administrator
-- against a user or a piece of content. This table serves as the definitive
-- audit trail for all moderation decisions.
-- =============================================================================
CREATE TABLE dbo.ModerationActions (
    -- The primary key for the moderation action.
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),

    -- The ID of the administrator who performed the action.
    AdminId UNIQUEIDENTIFIER NOT NULL,

    -- The type of action performed (e.g., "BanApp", "SuspendUser").
    ActionType NVARCHAR(100) NOT NULL,

    -- A short, administrator-provided reason for the action.
    Reason NVARCHAR(500) NULL,
    
    -- The current status of the action. "Reversed" allows for undoing actions.
    Status NVARCHAR(50) NOT NULL DEFAULT 'Completed',

    -- Timestamp for auditing when the action was taken.
    ActionDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    -- --- Polymorphic Association: The Target of the Action ---
    -- The type of entity being targeted (e.g., "App", "User", "Review").
    TargetType NVARCHAR(50) NOT NULL,
    -- The ID of the specific entity being targeted.
    TargetId UNIQUEIDENTIFIER NOT NULL,
    -- -----------------------------------------------------------

    -- Constraints
    CONSTRAINT FK_ModerationActions_Admins FOREIGN KEY (AdminId) REFERENCES dbo.Users(Id),
    -- Ensures the Status column only contains valid values.
    CONSTRAINT CK_ModerationActions_Status CHECK (Status IN ('Pending', 'Completed', 'Reversed')),
    -- Ensures the TargetType is one of the expected values.
    CONSTRAINT CK_ModerationActions_TargetType CHECK (TargetType IN ('App', 'User', 'Review', 'AbuseReport'))
);
GO

-- Create a non-clustered index on the target columns for efficient lookups
-- of moderation history for a specific app or user.
CREATE INDEX IX_ModerationActions_Target ON dbo.ModerationActions (TargetType, TargetId);
GO

-- =============================================================================
-- Table: dbo.SystemLogs
-- Purpose: A generic, append-only log for auditing a wide range of system
-- and user activities. This is more for general tracking than specific
-- moderation decisions.
-- =============================================================================
CREATE TABLE dbo.SystemLogs (
    -- The primary key for the log entry.
    Id BIGINT PRIMARY KEY IDENTITY(1,1), -- Using a BIGINT IDENTITY for high-volume logging.

    -- The ID of the user who performed the action. Can be NULL for system-level actions.
    UserId UNIQUEIDENTIFIER NULL,

    -- A short, machine-readable name for the action (e.g., "UserLogin", "AppVersionUploaded").
    Action NVARCHAR(100) NOT NULL,

    -- A more detailed, human-readable description or a serialized JSON object
    -- containing relevant data about the event.
    Details NVARCHAR(1000) NULL,

    -- The IP address from which the action originated, if applicable.
    IpAddress NVARCHAR(45) NULL,

    -- Timestamp for when the event occurred.
    Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    -- Constraints
    -- Use ON DELETE SET NULL: If a user is deleted, we want to keep their logs
    -- for historical/security analysis, but just nullify the direct link.
    CONSTRAINT FK_SystemLogs_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE SET NULL
);
GO

-- Create a non-clustered index for efficient querying of a user's activity or a specific action type.
CREATE INDEX IX_SystemLogs_Action_Timestamp ON dbo.SystemLogs (Action, Timestamp DESC);
CREATE INDEX IX_SystemLogs_UserId ON dbo.SystemLogs (UserId) WHERE UserId IS NOT NULL;
GO

PRINT 'Administration schema created successfully.';
GO