# Project Zenith: Write Database Schema Design (App Lifecycle)

## 1. Overview

This document details the design of the **Write Database schema** for managing the complete application lifecycle, from developer profiles to individual file uploads. As part of the "Write" side of our CQRS architecture, this schema is engineered for **transactional integrity, data consistency, and clear, normalized relationships**.

This schema serves as the source of truth for all developer and application data. Every state change within these tables—such as a new app submission, a version update, or a status change—is a business-critical event. These changes will be captured and published as domain events to Kafka, which will then be used to construct the denormalized Read Database for public browsing.

## 2. Table Descriptions

The app lifecycle schema is composed of four interconnected tables.

### 2.1. `dbo.Developers`

This table extends the core `Users` table, storing profile information specific to users who have been granted the "Developer" role. It maintains a one-to-one relationship with `dbo.Users`.

| Column         | Data Type          | Constraints                    | Description                                            |
| :------------- | :----------------- | :----------------------------- | :----------------------------------------------------- |
| `UserId`       | `UNIQUEIDENTIFIER` | **PK**, **FK to Users.Id**     | Links this developer profile to a core user record.    |
| `Description`  | `NVARCHAR(1000)`   | NULL                           | A public-facing description of the developer.          |
| `ContactEmail` | `NVARCHAR(256)`    | NULL                           | A public contact email for support purposes.           |
| `CreatedAt`    | `DATETIME2`        | NOT NULL, DEFAULT GETUTCDATE() | The timestamp when the user was promoted to developer. |

### 2.2. `dbo.Apps`

This is the central table for application metadata, representing a single product in the store.

| Column        | Data Type          | Constraints                                 | Description                                                            |
| :------------ | :----------------- | :------------------------------------------ | :--------------------------------------------------------------------- |
| `Id`          | `UNIQUEIDENTIFIER` | **PK**, DEFAULT NEWID()                     | The unique identifier for the application.                             |
| `DeveloperId` | `UNIQUEIDENTIFIER` | NOT NULL, **FK to Developers.UserId**       | Links the app to its owner.                                            |
| `Name`        | `NVARCHAR(255)`    | NOT NULL                                    | The public display name of the app.                                    |
| `Description` | `NVARCHAR(MAX)`    | NULL                                        | The detailed description, supporting Markdown.                         |
| `Category`    | `NVARCHAR(100)`    | NOT NULL                                    | The primary category of the app (e.g., "Game", "Productivity").        |
| `Platform`    | `NVARCHAR(50)`     | NOT NULL                                    | The target platform (e.g., "Windows", "Android").                      |
| `Price`       | `DECIMAL(18, 2)`   | NOT NULL, DEFAULT 0.00                      | The one-time purchase price. `0.00` indicates a free app.              |
| `Status`      | `NVARCHAR(50)`     | NOT NULL, DEFAULT 'Draft', CHECK constraint | The current moderation status (`Draft`, `Pending`, `Published`, etc.). |
| `CreatedAt`   | `DATETIME2`        | NOT NULL, DEFAULT GETUTCDATE()              | The timestamp when the app record was first created.                   |
| `UpdatedAt`   | `DATETIME2`        | NULL                                        | The timestamp of the last metadata update.                             |

### 2.3. `dbo.Files`

This table stores metadata about uploaded binary files. The actual files are stored externally.

| Column      | Data Type          | Constraints                    | Description                                                   |
| :---------- | :----------------- | :----------------------------- | :------------------------------------------------------------ |
| `Id`        | `UNIQUEIDENTIFIER` | **PK**, DEFAULT NEWID()        | The unique identifier for the file record.                    |
| `Path`      | `NVARCHAR(1024)`   | NOT NULL                       | The full URI to the file in external Blob Storage.            |
| `Size`      | `BIGINT`           | NOT NULL                       | The size of the file in bytes.                                |
| `Checksum`  | `NVARCHAR(256)`    | NOT NULL, **UNIQUE**           | A cryptographic hash (e.g., SHA256) to verify file integrity. |
| `CreatedAt` | `DATETIME2`        | NOT NULL, DEFAULT GETUTCDATE() | The timestamp when the file was uploaded.                     |

### 2.4. `dbo.Versions`

This table represents a specific, downloadable version of an application. It links an `App` to a `File`.

| Column          | Data Type          | Constraints                    | Description                                         |
| :-------------- | :----------------- | :----------------------------- | :-------------------------------------------------- |
| `Id`            | `UNIQUEIDENTIFIER` | **PK**, DEFAULT NEWID()        | The unique identifier for this version.             |
| `AppId`         | `UNIQUEIDENTIFIER` | NOT NULL, **FK to Apps.Id**    | Links this version to its parent application.       |
| `VersionNumber` | `NVARCHAR(50)`     | NOT NULL                       | The semantic version string (e.g., "1.0.0").        |
| `Changelog`     | `NVARCHAR(MAX)`    | NULL                           | Release notes for this specific version.            |
| `FileId`        | `UNIQUEIDENTIFIER` | NOT NULL, **FK to Files.Id**   | Links this version to its downloadable file record. |
| `CreatedAt`     | `DATETIME2`        | NOT NULL, DEFAULT GETUTCDATE() | The timestamp when this version was created.        |

## 3. Normalization (Achieving 3NF)

This schema adheres to Third Normal Form (3NF) to ensure data integrity and eliminate redundancy.

- **1NF & 2NF:** All tables have a primary key, all columns are atomic, and all non-key attributes are fully dependent on the primary key.
- **3NF:** The schema avoids transitive dependencies. For example:
  - An app's `Name` depends only on the `App.Id`.
  - A version's `Changelog` depends only on the `Version.Id`.
  - A file's `Checksum` depends only on the `File.Id`.
  - Separating `Apps`, `Versions`, and `Files` into distinct tables is a classic example of normalization. We don't repeat the app's `Name` in every version record; instead, we link back to the single, authoritative `Apps` record.

## 4. Integration Strategy

### 4.1. Integration with `dbo.Users`

The `Developers` table acts as a "profile extension" for the `Users` table. It is linked via a **one-to-one relationship** where `Developers.UserId` is both the primary key and a foreign key referencing `Users.Id`. This is a clean and efficient way to add specialized data for a subset of users without cluttering the main `Users` table.

### 4.2. Integration with Blob Storage

The schema is designed to **not store large binary files** directly in the database, as this is inefficient and costly.

- The `Files.Path` column stores a URI pointing to the actual file (e.g., APK, EXE) in a dedicated cloud storage service like **Azure Blob Storage**.
- This approach leverages the strengths of each system: SQL Server for transactional metadata management, and Blob Storage for cost-effective, scalable, and high-throughput file hosting.

## 5. Example Flow: `AppSubmittedEvent`

This schema provides the foundation for the app submission workflow.

1.  **Command:** A `SubmitAppCommand` arrives at the Write API from a verified developer. The command includes the app's binary file and its metadata (name, version number, etc.).
2.  **File Upload:** The application first uploads the binary file to **Azure Blob Storage**.
3.  **Transaction Start:** The command handler begins a database transaction.
4.  **Insert into `dbo.Files`:** A new record is inserted into the `Files` table, storing the `Path` returned by Blob Storage, the file `Size`, and a calculated `Checksum`.
5.  **Insert into `dbo.Apps`:** A new record is inserted into the `Apps` table with the core metadata (Name, Description, etc.), linking it to the developer's ID. The initial `Status` is set to `Pending`.
6.  **Insert into `dbo.Versions`:** A new record is inserted into the `Versions` table, linking the new `App.Id` to the new `File.Id` and storing the `VersionNumber`.
7.  **Transaction Commit:** The transaction is committed, ensuring that the App, its first Version, and its File are all created atomically.
8.  **Event Publication:** The command handler then uses the data from the transaction (e.g., `App.Id`, `DeveloperId`, `App.Name`, `Version.VersionNumber`) to create and publish an `AppSubmittedEvent` to Kafka.

```

```
