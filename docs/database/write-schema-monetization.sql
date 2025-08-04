-- =============================================================================
-- Project Zenith: Write Database Schema (Monetization)
-- =============================================================================
-- This script defines the normalized tables required for managing user purchases,
-- financial transactions, and developer payouts.
-- =============================================================================

-- Drop tables in reverse order of dependency if they already exist
IF OBJECT_ID('dbo.Transactions', 'U') IS NOT NULL DROP TABLE dbo.Transactions;
IF OBJECT_ID('dbo.Purchases', 'U') IS NOT NULL DROP TABLE dbo.Purchases;
IF OBJECT_ID('dbo.Payouts', 'U') IS NOT NULL DROP TABLE dbo.Payouts;
GO

-- =============================================================================
-- Table: dbo.Purchases
-- Purpose: Records a user's entitlement to an application. This table acts
-- as a master record for each individual purchase event.
-- =============================================================================
CREATE TABLE dbo.Purchases (
    -- The primary key for the purchase record.
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),

    -- Foreign key linking this purchase to the user who made it.
    UserId UNIQUEIDENTIFIER NOT NULL,

    -- Foreign key linking this purchase to the application that was bought.
    AppId UNIQUEIDENTIFIER NOT NULL,

    -- The price of the app at the time of purchase. Stored here to preserve
    -- historical accuracy, even if the app's price changes later.
    Price DECIMAL(18, 2) NOT NULL,

    -- The current status of the purchase.
    Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',

    -- Timestamp for auditing when the purchase was initiated.
    PurchaseDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    -- Constraints
    CONSTRAINT FK_Purchases_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
    -- Use ON DELETE NO ACTION: We do not want purchase history to be automatically
    -- deleted if an app is removed. This maintains the financial record.
    CONSTRAINT FK_Purchases_Apps FOREIGN KEY (AppId) REFERENCES dbo.Apps(Id) ON DELETE NO ACTION,
    -- A user can only purchase a specific app once.
    CONSTRAINT UQ_Purchases_UserId_AppId UNIQUE (UserId, AppId),
    -- Ensures the Status column only contains valid values.
    CONSTRAINT CK_Purchases_Status CHECK (Status IN ('Pending', 'Completed', 'Refunded'))
);
GO

-- =============================================================================
-- Table: dbo.Transactions
-- Purpose: Stores the record of a specific financial transaction from a payment
-- provider (e.g., Stripe). This is a child table to Purchases.
-- =============================================================================
CREATE TABLE dbo.Transactions (
    -- The primary key for the transaction record.
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),

    -- Foreign key linking this transaction to a specific purchase.
    PurchaseId UNIQUEIDENTIFIER NOT NULL,

    -- The amount of money involved in the transaction.
    Amount DECIMAL(18, 2) NOT NULL,

    -- The name of the external payment provider (e.g., "Stripe", "PayPal").
    PaymentProvider NVARCHAR(100) NOT NULL,

    -- The unique reference ID from the external payment provider.
    -- This is CRITICAL for reconciliation and support.
    PaymentId NVARCHAR(256) NOT NULL,

    -- The current status of the transaction.
    Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',

    -- Timestamp for auditing when the transaction occurred.
    TransactionDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    -- Constraints
    CONSTRAINT FK_Transactions_Purchases FOREIGN KEY (PurchaseId) REFERENCES dbo.Purchases(Id),
    -- A unique constraint on the external payment ID ensures we don't record the same transaction twice.
    CONSTRAINT UQ_Transactions_PaymentProvider_PaymentId UNIQUE (PaymentProvider, PaymentId),
    -- Ensures the Status column only contains valid values.
    CONSTRAINT CK_Transactions_Status CHECK (Status IN ('Pending', 'Completed', 'Failed'))
);
GO

-- =============================================================================
-- Table: dbo.Payouts
-- Purpose: Tracks scheduled and processed payouts to developers. This represents
-- the transfer of funds from the platform to the developer.
-- =============================================================================
CREATE TABLE dbo.Payouts (
    -- The primary key for the payout record.
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),

    -- Foreign key linking this payout to a specific developer.
    DeveloperId UNIQUEIDENTIFIER NOT NULL,

    -- The total amount to be paid out to the developer.
    Amount DECIMAL(18, 2) NOT NULL,

    -- The current status of the payout.
    Status NVARCHAR(50) NOT NULL DEFAULT 'Scheduled',

    -- The unique reference ID from the external payment provider for this payout.
    -- Can be null until the payout is actually processed.
    PaymentId NVARCHAR(256) NULL,

    -- Timestamp for auditing when the payout was processed.
    PayoutDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    -- Constraints
    CONSTRAINT FK_Payouts_Developers FOREIGN KEY (DeveloperId) REFERENCES dbo.Developers(UserId),
    -- Ensures the Status column only contains valid values.
    CONSTRAINT CK_Payouts_Status CHECK (Status IN ('Scheduled', 'Processed', 'Cancelled', 'Failed'))
);
GO

PRINT 'Monetization schema created successfully.';
GO