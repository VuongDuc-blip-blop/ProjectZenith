# Project Zenith: Write Database Schema Design (Monetization)

## 1. Overview

This document details the design of the **Write Database schema** for handling all monetization features within Project Zenith. This schema is a core component of the "Write" side of our CQRS architecture, meticulously designed for **transactional integrity, data consistency, and auditable financial records**.

This schema serves as the definitive source of truth for every financial event, including user purchases, payment provider transactions, and developer payouts. Every state change in these tables represents a significant financial event. These changes will be captured and published as domain events (e.g., `AppPurchasedEvent`, `PayoutProcessedEvent`) to Kafka, enabling the construction of real-time financial dashboards and analytics in our Read models.

## 2. Table Descriptions

The monetization schema is composed of three primary tables that track the flow of money from the user to the developer.

### 2.1. `dbo.Purchases`

This table represents a user's **entitlement** to an application. It is the master record for a successful or pending purchase, linking a user to an app.

| Column         | Data Type          | Constraints                    | Description                                                                 |
| :------------- | :----------------- | :----------------------------- | :-------------------------------------------------------------------------- |
| `Id`           | `UNIQUEIDENTIFIER` | **PK**, DEFAULT NEWID()        | The unique identifier for the purchase record.                              |
| `UserId`       | `UNIQUEIDENTIFIER` | NOT NULL, **FK to Users.Id**   | Links the purchase to the user who made it.                                 |
| `AppId`        | `UNIQUEIDENTIFIER` | NOT NULL, **FK to Apps.Id**    | Links the purchase to the app that was bought.                              |
| `Price`        | `DECIMAL(18, 2)`   | NOT NULL                       | The price of the app at the time of purchase, for historical accuracy.      |
| `Status`       | `NVARCHAR(50)`     | NOT NULL, CHECK constraint     | The current status of the entitlement (`Pending`, `Completed`, `Refunded`). |
| `PurchaseDate` | `DATETIME2`        | NOT NULL, DEFAULT GETUTCDATE() | The timestamp when the purchase was initiated.                              |

A `UNIQUE` constraint on `(UserId, AppId)` ensures a user can only purchase a specific app once.

### 2.2. `dbo.Transactions`

This table stores a detailed log of every individual **financial attempt** made through a payment provider. It serves as a child record to the `Purchases` table, providing a complete audit trail.

| Column            | Data Type          | Constraints                      | Description                                                    |
| :---------------- | :----------------- | :------------------------------- | :------------------------------------------------------------- |
| `Id`              | `UNIQUEIDENTIFIER` | **PK**, DEFAULT NEWID()          | The unique identifier for the transaction.                     |
| `PurchaseId`      | `UNIQUEIDENTIFIER` | NOT NULL, **FK to Purchases.Id** | Links this transaction to its parent purchase attempt.         |
| `Amount`          | `DECIMAL(18, 2)`   | NOT NULL                         | The amount of money involved in the transaction.               |
| `PaymentProvider` | `NVARCHAR(100)`    | NOT NULL                         | The name of the external payment provider (e.g., "Stripe").    |
| `PaymentId`       | `NVARCHAR(256)`    | NOT NULL, **UNIQUE**             | The unique reference ID from the external payment provider.    |
| `Status`          | `NVARCHAR(50)`     | NOT NULL, CHECK constraint       | The outcome of the attempt (`Pending`, `Completed`, `Failed`). |
| `TransactionDate` | `DATETIME2`        | NOT NULL, DEFAULT GETUTCDATE()   | The timestamp when the transaction was processed.              |

### 2.3. `dbo.Payouts`

This table tracks the lifecycle of payouts made from the platform to developers.

| Column            | Data Type          | Constraints                           | Description                                                                      |
| :---------------- | :----------------- | :------------------------------------ | :------------------------------------------------------------------------------- |
| `Id`              | `UNIQUEIDENTIFIER` | **PK**, DEFAULT NEWID()               | The unique identifier for the payout record.                                     |
| `DeveloperId`     | `UNIQUEIDENTIFIER` | NOT NULL, **FK to Developers.UserId** | Links the payout to the developer receiving the funds.                           |
| `Amount`          | `DECIMAL(18, 2)`   | NOT NULL                              | The total amount to be paid out.                                                 |
| `Status`          | `NVARCHAR(50)`     | NOT NULL, CHECK constraint            | The current status of the payout (`Scheduled`, `Processing`, `Processed`, etc.). |
| `ScheduledAt`     | `DATETIME2`        | NOT NULL, DEFAULT GETUTCDATE()        | The timestamp when this payout was created/scheduled.                            |
| `CompletedAt`     | `DATETIME2`        | NULL                                  | The timestamp when the payout was confirmed as successful.                       |
| `PaymentProvider` | `NVARCHAR(100)`    | NULL                                  | The provider used for the payout (e.g., "Stripe Connect").                       |
| `PaymentId`       | `NVARCHAR(256)`    | NULL                                  | The external reference ID for the processed payout.                              |

## 3. Normalization and Design Choices

The schema is designed to be in at least Third Normal Form (3NF) to ensure financial data integrity.

- **Separation of `Purchases` and `Transactions`:** This one-to-many relationship is a key design choice. A single `Purchase` can have multiple associated `Transactions` (e.g., a failed attempt followed by a successful one). This provides a complete and auditable history of all payment attempts, which is critical for support and analytics.
- **Historical Price Storage:** The `Purchases.Price` column captures the price at the moment of sale. This correctly avoids a transitive dependency on the `Apps.Price` (which can change over time) and ensures financial records are historically accurate.
- **Lifecycle Timestamps in `Payouts`:** The `Payouts` table uses multiple, specific timestamps (`ScheduledAt`, `CompletedAt`) instead of a single ambiguous date column. This accurately models the real-world lifecycle of a payout and provides clear, unambiguous data for auditing.

## 4. Security: Handling Financial Data

The schema is designed to be secure and compliant with standards like PCI DSS by **never storing sensitive payment details**.

- **No Sensitive Data:** The database **does not** contain any credit card numbers, bank account details, or other sensitive payment instrument information.
- **External Payment IDs:** All financial operations are linked to an external, trusted payment provider (like Stripe). We only store the non-sensitive **`PaymentId`** (reference token) that the provider gives us. This allows us to reconcile our records with the provider's records without handling any sensitive data ourselves.
- **Data Integrity:** The use of `FOREIGN KEY` constraints, `CHECK` constraints, and `UNIQUE` indexes ensures that the financial data stored is internally consistent and correct.

## 5. Example Flow: `AppPurchasedEvent`

This schema provides the foundation for the application purchase workflow.

1.  **Command:** A `CreateCheckoutSessionCommand` arrives at the Write API from a logged-in user for a specific app.
2.  **Transaction Start:** The command handler begins a database transaction.
3.  **Insert into `dbo.Purchases`:** A new record is inserted into the `Purchases` table with the user's ID, the app's ID, the current price, and a `Status` of `Pending`.
4.  **Transaction Commit:** The transaction is committed.
5.  **Payment Gateway:** The user is redirected to the payment provider. When the payment is successful, the provider sends a webhook to our server.
6.  **Webhook Handler:** The webhook handler begins a new transaction.
7.  **Insert into `dbo.Transactions`:** A new record is inserted into the `Transactions` table with the details from the webhook (amount, provider, `PaymentId`) and a `Status` of `Completed`. This record is linked to the `PurchaseId` from step 3.
8.  **Update `dbo.Purchases`:** The status of the original `Purchases` record is updated from `Pending` to `Completed`.
9.  **Transaction Commit:** The second transaction is committed.
10. **Event Publication:** The webhook handler now publishes an `AppPurchasedEvent`, containing key information like `PurchaseId`, `UserId`, `AppId`, and `Price`, to Kafka for downstream processing.

```

```
