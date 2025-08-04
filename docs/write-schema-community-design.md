# Project Zenith: Write Database Schema Design (Community)

## 1. Overview

This document details the design of the **Write Database schema** for managing all community interactions within Project Zenith. This schema is a core component of the "Write" side of our CQRS architecture, focused on capturing user-generated content like reviews, ratings, and abuse reports with **transactional integrity and data consistency**.

The primary purpose of this schema is to act as the definitive source of truth for all community feedback. Every action, such as a user posting a review or reporting content, is a significant domain event. These actions will be recorded in these tables, and corresponding events will be published to Kafka. The Kafka Consumer will then use these events to build and maintain the aggregated, denormalized data (like average app ratings) in the Read Database.

## 2. Table Descriptions

The community interaction schema is composed of two primary tables.

### 2.1. `dbo.Reviews`

This table is designed to efficiently store both the quantitative (star rating) and qualitative (written comment) feedback from users in a single, unified record.

| Column      | Data Type               | Constraints                    | Description                                                 |
| :---------- | :---------------------- | :----------------------------- | :---------------------------------------------------------- |
| `Id`        | `UNIQUEIDENTIDENTIFIER` | **PK**, DEFAULT NEWID()        | The unique identifier for the review.                       |
| `AppId`     | `UNIQUEIDENTIDENTIFIER` | NOT NULL, **FK to Apps.Id**    | Links the review to the specific application it is for.     |
| `UserId`    | `UNIQUEIDENTIDENTIFIER` | NOT NULL, **FK to Users.Id**   | Links the review to the user who submitted it.              |
| `Rating`    | `INT`                   | NOT NULL, **CHECK (1-5)**      | The mandatory star rating value.                            |
| `Comment`   | `NVARCHAR(1000)`        | NULL                           | The optional written text of the review.                    |
| `IsEdited`  | `BIT`                   | NOT NULL, DEFAULT 0            | A flag indicating if the review `Comment` has been updated. |
| `PostedAt`  | `DATETIME2`             | NOT NULL, DEFAULT GETUTCDATE() | The timestamp when the review was initially submitted.      |
| `UpdatedAt` | `DATETIME2`             | NULL                           | The timestamp of the last update.                           |

A `UNIQUE` constraint on `(AppId, UserId)` ensures that a user can only submit one review per application.

### 2.2. `dbo.AbuseReports`

This table serves as a central log for all user-submitted reports against potentially inappropriate content, providing a flexible structure to target different entity types.

| Column       | Data Type               | Constraints                               | Description                                                       |
| :----------- | :---------------------- | :---------------------------------------- | :---------------------------------------------------------------- |
| `Id`         | `UNIQUEIDENTIFIER`      | **PK**, DEFAULT NEWID()                   | The unique identifier for the report.                             |
| `ReporterId` | `UNIQUEIDENTIFIER`      | NOT NULL, **FK to Users.Id**              | The ID of the user who is submitting the report.                  |
| `ReviewId`   | `UNIQUEIDENTIDENTIFIER` | NULL, **FK to Reviews.Id**                | The ID of the review being reported, if applicable.               |
| `AppId`      | `UNIQUEIDENTIFIER`      | NULL, **FK to Apps.Id**                   | The ID of the app being reported, if applicable.                  |
| `UserId`     | `UNIQUEIDENTIFIER`      | NULL, **FK to Users.Id**                  | The ID of the user being reported, if applicable.                 |
| `Reason`     | `NVARCHAR(500)`         | NOT NULL                                  | The user-provided reason for the report.                          |
| `Status`     | `NVARCHAR(50)`          | NOT NULL, DEFAULT 'New', CHECK constraint | The current moderation status (`New`, `UnderReview`, `Resolved`). |
| `ReportedAt` | `DATETIME2`             | NOT NULL, DEFAULT GETUTCDATE()            | The timestamp when the report was submitted.                      |

A `CHECK` constraint ensures that at least one of `ReviewId`, `AppId`, or `UserId` is not null, guaranteeing every report has a target.

## 3. Normalization and Design Choices

The schema is designed to be in at least Third Normal Form (3NF) while making a pragmatic design choice for efficiency.

- **Combined `Reviews` and `Ratings`:** We intentionally combined the concepts of a "review" and a "rating" into a single `Reviews` table. Since a rating is mandatory for every review, there is a one-to-one relationship. Storing them together is a beneficial form of denormalization that simplifies the schema and application logic, reduces JOINs, and still adheres to 3NF because all non-key attributes (`Rating`, `Comment`) are fully dependent on the primary key (`Id`).
- **3NF Compliance:** The schema avoids transitive dependencies. For example, a `Review.Comment` depends only on the `Review.Id`, not on the `App.Id` or `User.Id`. The `AbuseReports` table correctly links to other entities via foreign keys without storing redundant information about them.

## 4. Security: Handling User-Generated Content

User-generated content (UGC), especially the `Reviews.Comment` and `AbuseReports.Reason` fields, is a potential vector for Cross-Site Scripting (XSS) attacks.

- **Input Sanitization:** The application layer (**Write API**) is responsible for sanitizing all incoming string data before it is saved to the database. This involves removing or encoding any potentially malicious HTML, script tags, or dangerous characters.
- **Parameterized Queries:** All database interactions from the application will use parameterized queries (which Entity Framework Core does by default) to prevent SQL Injection attacks.
- **Secure Rendering:** The client applications (WPF, Web Dashboards) are responsible for correctly rendering content. For example, when displaying a review comment, it should be treated as text and not rendered as raw HTML to provide a second layer of defense against X-GSS.

## 5. Example Flow: `ReviewPostedEvent`

This schema directly supports the workflow for a user posting a review.

1.  **Command:** A `SubmitReviewCommand`, containing `UserId`, `AppId`, `Rating`, and an optional `Comment`, arrives at the Write API.
2.  **Validation:** The command handler validates the data (e.g., checks that the `Rating` is between 1 and 5) and verifies that the user is eligible to review the app (e.g., they have downloaded it).
3.  **Transaction Start:** The handler begins a database transaction.
4.  **Insert into `dbo.Reviews`:** A new record is inserted into the `Reviews` table with the provided data. `PostedAt` is set to the current UTC time, and `IsEdited` is `0`.
5.  **Transaction Commit:** The transaction is successfully committed.
6.  **Event Publication:** The command handler then creates a `ReviewPostedEvent`, populating it with data from the newly created record (`Review.Id`, `AppId`, `UserId`, `Rating`). This event is published to Kafka.
7.  **Downstream Processing:** The Kafka Consumer receives this event and updates the denormalized `App` record in the **Read DB**, recalculating its average rating and total review count.

```

```
