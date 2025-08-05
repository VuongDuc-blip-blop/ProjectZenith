# Project Zenith: Write Database Schema Design (Administration)

## 1. Overview

This document details the design of the **Write Database schema** for all administrative and auditing functions within Project Zenith. As part of the "Write" side of our CQRS architecture, this schema is designed to create a **durable, secure, and auditable record** of all significant system events and moderation decisions.

The primary purpose of this schema is to serve as the immutable source of truth for platform governance. Every action recorded here, from an automated system log to a deliberate decision by an administrator, is a critical event. These records are not only essential for the operational management of the platform but will also trigger specific domain events (e.g., `AppBannedEvent`, `UserSuspendedEvent`) to be published to Kafka, ensuring that all parts of our distributed system can react appropriately to administrative changes.

## 2. Table Descriptions

The administration schema is logically divided into two key tables: one for deliberate, high-level moderation actions, and another for high-volume, general-purpose system logging.

### 2.1. `dbo.ModerationActions`

This is a high-integrity table that records a specific, deliberate action taken by an administrator against a user or a piece of content. It is the definitive audit trail for all moderation decisions.

| Column       | Data Type          | Constraints                    | Description                                                        |
| :----------- | :----------------- | :----------------------------- | :----------------------------------------------------------------- |
| `Id`         | `UNIQUEIDENTIFIER` | **PK**, DEFAULT NEWID()        | The unique identifier for the action.                              |
| `AdminId`    | `UNIQUEIDENTIFIER` | NOT NULL, **FK to Users.Id**   | The ID of the administrator who performed the action.              |
| `ActionType` | `NVARCHAR(100)`    | NOT NULL                       | The type of action performed (e.g., "BanApp", "SuspendUser").      |
| `Reason`     | `NVARCHAR(500)`    | NULL                           | An optional, administrator-provided reason for the action.         |
| `Status`     | `NVARCHAR(50)`     | NOT NULL, CHECK constraint     | The current status (`Pending`, `Completed`, `Reversed`).           |
| `ActionDate` | `DATETIME2`        | NOT NULL, DEFAULT GETUTCDATE() | The timestamp when the action was taken.                           |
| `TargetType` | `NVARCHAR(50)`     | NOT NULL, CHECK constraint     | The type of entity being targeted (e.g., "App", "User", "Review"). |
| `TargetId`   | `UNIQUEIDENTIFIER` | NOT NULL                       | The ID of the specific entity being targeted.                      |

### 2.2. `dbo.SystemLogs`

This is a high-volume, append-only log for auditing a wide range of system and user activities. It is designed for general tracking rather than specific, reversible moderation decisions.

| Column      | Data Type          | Constraints                    | Description                                                            |
| :---------- | :----------------- | :----------------------------- | :--------------------------------------------------------------------- |
| `Id`        | `BIGINT`           | **PK**, IDENTITY(1,1)          | A sequential, auto-incrementing ID, ideal for high-volume inserts.     |
| `UserId`    | `UNIQUEIDENTIFIER` | NULL, **FK to Users.Id**       | The ID of the user who performed the action (NULL for system actions). |
| `Action`    | `NVARCHAR(100)`    | NOT NULL                       | A short, machine-readable name for the action (e.g., "UserLogin").     |
| `Details`   | `NVARCHAR(1000)`   | NULL                           | A detailed description or a serialized JSON object of the event data.  |
| `IpAddress` | `NVARCHAR(45)`     | NULL                           | The IP address from which the action originated, if applicable.        |
| `Timestamp` | `DATETIME2`        | NOT NULL, DEFAULT GETUTCDATE() | The timestamp when the event occurred.                                 |

## 3. Normalization and Design Choices

The schema is designed to be in at least Third Normal Form (3NF) while incorporating flexible design patterns appropriate for logging and auditing.

- **Polymorphic Association in `ModerationActions`:** The use of `TargetType` and `TargetId` creates a polymorphic relationship. This is a deliberate design choice that allows a single `ModerationActions` table to target multiple different entity types (`App`, `User`, `Review`). This avoids the complexity of having many separate, sparse action tables (e.g., `AppBanActions`, `UserSuspendActions`), and still adheres to 3NF as all attributes are dependent on the `Id` primary key. Referential integrity for this relationship is enforced at the application layer.
- **Separation of Concerns:** Separating `ModerationActions` from `SystemLogs` is a key architectural decision. `ModerationActions` is a low-volume, high-importance table tracking auditable decisions. `SystemLogs` is a high-volume, lower-importance table for general activity. This separation allows each table to be optimized for its specific workload (e.g., using `BIGINT IDENTITY` for `SystemLogs` to handle rapid inserts).

## 4. Security: Secure Logging and Auditing

The design of this schema prioritizes security and the integrity of the audit trail.

- **No Sensitive Data in Logs:** The `SystemLogs.Details` column must **never** be used to store sensitive data such as plaintext passwords, full JWTs, or personal payment information. The application layer is responsible for logging only safe, relevant details about an event.
- **Preservation of Records (`ON DELETE SET NULL`):** The foreign key from `SystemLogs` to `Users` is configured with `ON DELETE SET NULL`. This is a critical security feature. If a user's account is deleted, their activity log is **not** erased. Instead, the `UserId` is nullified, anonymizing the entry while preserving the record of the action itself. This is essential for historical analysis and security incident response.
- **Immutable Record Principle:** While not enforced by the database, the application logic should treat both `ModerationActions` and `SystemLogs` as append-only tables. Actions should be `Reversed` with a new record, not deleted, to maintain a perfect, unaltered history of events.

## 5. Example Flow: `AppBannedEvent`

This schema provides the foundation for the workflow of an administrator banning an application.

1.  **Command:** A `BanAppCommand`, containing the `AdminId`, `AppId`, and a `Reason`, arrives at the Write API.
2.  **Transaction Start:** The command handler begins a database transaction.
3.  **Update `dbo.Apps`:** The handler first updates the `Status` of the corresponding record in the `Apps` table to `Banned`.
4.  **Insert into `dbo.ModerationActions`:** A new record is inserted into the `ModerationActions` table, recording the `AdminId`, an `ActionType` of "BanApp", the `Reason`, a `TargetType` of "App", and the `TargetId` of the banned app.
5.  **Insert into `dbo.SystemLogs`:** A new record is inserted into `SystemLogs` with the `AdminId` as the `UserId`, an `Action` of "AppBanned", and `Details` containing a summary like `{"AppId": "...", "AppName": "..."}`.
6.  **Transaction Commit:** The transaction is committed, ensuring the app's status change and the moderation/audit records are saved atomically.
7.  **Event Publication:** The command handler then creates an `AppBannedEvent`, populating it with the `AppId` and `Reason`. This event is published to Kafka to notify other parts of the system (like the Read side, which will immediately hide the app from public view).
