# Project Zenith: Write Database Schema Design (Identity)

## 1. Overview

This document details the design of the **Write Database schema** for identity management within Project Zenith. As the "Write" side of our CQRS architecture, this schema's primary purpose is to ensure **transactional integrity, data consistency, and security**. It is the ultimate source of truth for all user, role, and credential information.

The schema is highly **normalized** to prevent data redundancy and anomalies, making it ideal for handling write operations (Commands) like user registration, profile updates, and role changes. All data modifications here will trigger domain events that are published to Kafka, which in turn are used to build the denormalized Read Database.

## 2. Table Descriptions

The identity schema is composed of four distinct tables, each with a specific responsibility.

### 2.1. `dbo.Users`

This table is the central hub for user identity. It stores core, non-sensitive profile information.

| Column      | Data Type          | Constraints                    | Description                                      |
| :---------- | :----------------- | :----------------------------- | :----------------------------------------------- |
| `Id`        | `UNIQUEIDENTIFIER` | **PK**, DEFAULT NEWID()        | The unique identifier (GUID) for the user.       |
| `Email`     | `NVARCHAR(256)`    | NOT NULL, **UNIQUE**           | The user's email address, used for login.        |
| `Username`  | `NVARCHAR(100)`    | NULL, **UNIQUE**               | The user's optional public display name.         |
| `Bio`       | `NVARCHAR(500)`    | NULL                           | A short biography for the user's profile page.   |
| `AvatarUrl` | `NVARCHAR(500)`    | NULL                           | A URL pointing to the user's avatar image.       |
| `CreatedAt` | `DATETIME2`        | NOT NULL, DEFAULT GETUTCDATE() | The timestamp when the user account was created. |
| `UpdatedAt` | `DATETIME2`        | NULL                           | The timestamp of the last profile update.        |

### 2.2. `dbo.Roles`

A simple lookup table that defines the available roles within the system.

| Column | Data Type          | Constraints             | Description                                   |
| :----- | :----------------- | :---------------------- | :-------------------------------------------- |
| `Id`   | `UNIQUEIDENTIFIER` | **PK**, DEFAULT NEWID() | The unique identifier for the role.           |
| `Name` | `NVARCHAR(50)`     | NOT NULL, **UNIQUE**    | The name of the role (e.g., "User", "Admin"). |

### 2.3. `dbo.UserRoles`

A many-to-many join table that maps users to their assigned roles. This is the core of our Role-Based Access Control (RBAC) system.

| Column   | Data Type          | Constraints                          | Description                                       |
| :------- | :----------------- | :----------------------------------- | :------------------------------------------------ |
| `UserId` | `UNIQUEIDENTIFIER` | **PK**, NOT NULL, **FK to Users.Id** | The identifier of the user being assigned a role. |
| `RoleId` | `UNIQUEIDENTIFIER` | **PK**, NOT NULL, **FK to Roles.Id** | The identifier of the role being assigned.        |

### 2.4. `dbo.Credentials`

This table securely stores password hashes and is intentionally separated from the main `Users` table for security (Principle of Least Privilege).

| Column         | Data Type          | Constraints                    | Description                                             |
| :------------- | :----------------- | :----------------------------- | :------------------------------------------------------ |
| `UserId`       | `UNIQUEIDENTIFIER` | **PK**, **FK to Users.Id**     | Establishes a one-to-one relationship with a user.      |
| `PasswordHash` | `NVARCHAR(256)`    | NOT NULL                       | The securely hashed password string (e.g., BCrypt).     |
| `CreatedAt`    | `DATETIME2`        | NOT NULL, DEFAULT GETUTCDATE() | The timestamp when the credential was created/last set. |

## 3. Normalization (Achieving 3NF)

This schema is designed to be in at least Third Normal Form (3NF), which ensures data integrity by minimizing redundancy and dependency issues.

- **First Normal Form (1NF):** Achieved. All tables have a primary key, and all columns contain atomic (indivisible) values.
- **Second Normal Form (2NF):** Achieved. All non-key attributes in every table are fully functionally dependent on the entire primary key. In tables with a single-column primary key (`Users`, `Roles`, `Credentials`), this is satisfied by default. In the `UserRoles` table, there are no non-key attributes, so it is also satisfied.
- **Third Normal Form (3NF):** Achieved. There are no transitive dependencies. For example, in the `Users` table, no non-key attribute (like `Bio`) depends on another non-key attribute (like `Username`). All attributes depend only on the primary key (`Id`). The separation of `Credentials` and `Roles` into their own tables further eliminates potential transitive dependencies.

## 4. Security: Password Hashing

As per our functional requirements, user passwords are **never** stored in plaintext. The `Credentials.PasswordHash` column is designed to store the output of a strong, salted, one-way hashing algorithm.

- **Algorithm:** We will use **BCrypt**.
- **Process:** When a user registers or resets their password, the application will take their plaintext password, generate a random salt, and use BCrypt to compute a hash. Only this resulting hash string is stored in the database.
- **Verification:** During login, the application retrieves the stored hash for the user, and uses BCrypt's built-in comparison function to check if the provided plaintext password matches the stored hash. The plaintext password is never stored or logged.

## 5. Example Flow: `UserRegisteredEvent`

This schema directly supports the user registration command and the subsequent event publication.

1.  **Command:** A `RegisterUserCommand` arrives at the Write API with an email and password.
2.  **Transaction Start:** The command handler begins a database transaction.
3.  **Insert into `dbo.Users`:** A new record is inserted into the `Users` table with the user's email and a newly generated `Id`.
4.  **Insert into `dbo.Credentials`:** The provided password is put through BCrypt. The resulting hash is inserted into the `Credentials` table, using the same `Id` from the `Users` table.
5.  **Insert into `dbo.UserRoles`:** The system looks up the `Id` for the default "User" role from the `Roles` table and inserts a new mapping record into `UserRoles` to link the new user to this role.
6.  **Transaction Commit:** The database transaction is committed. All three tables are now in a consistent state.
7.  **Event Publication:** The command handler then uses the data from the committed transaction (like the new `UserId` and `Email`) to create and publish a `UserRegisteredEvent` to Kafka, notifying the rest of the system that a new user has successfully been created.

```

```
