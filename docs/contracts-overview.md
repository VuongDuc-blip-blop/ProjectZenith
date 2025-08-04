# Project Zenith: The Contracts Project Overview

## 1. Overview

The `ProjectZenith.Contracts` project is the **single source of truth for the shared communication language** across our entire distributed system. In a CQRS and event-driven architecture, services are decoupled and cannot directly reference each other's internal code. The `Contracts` project serves as a neutral, shared library that all services depend on to understand each other.

Its primary roles are:

- **To Standardize Communication:** It defines the precise structure of data that flows between our services, primarily through Kafka events and API responses.
- **To Ensure Type Safety:** By using shared C# classes (`records`, `classes`, `enums`), we prevent errors that arise from mis-typed or malformed JSON payloads.
- **To Enforce Decoupling:** The Write API and the Read API do not know about each other, but they both know about the `Contracts` project. This allows them to evolve independently as long as they adhere to the established contracts.

## 2. Structure

The `Contracts` project is organized into three main folders, each with a distinct purpose:

### 2.1. `Events`

- **Contents:** Contains immutable C# `record` types that represent significant business events that have already occurred in the system.
- **Naming Convention:** Always in the past tense (e.g., `AppSubmittedEvent`, `UserRegisteredEvent`, `ReviewDeletedEvent`).
- **Purpose:** These objects are the "facts" that are published by the Write Stack to a Kafka topic. They contain the data necessary for downstream services (like the Kafka Consumer) to react to the change.

### 2.2. `Dtos` (Data Transfer Objects)

- **Contents:** Contains simple C# classes or records designed to carry data across process boundaries, primarily from an API to a client.
- **Naming Convention:** Typically suffixed with `Dto` (e.g., `UserProfileDto`, `AppDetailsDto`).
- **Purpose:** DTOs are "shaped" to match the specific needs of a UI component. They represent the data a client _wants to see_, which is often a different structure from how it's stored in the database.

### 2.3. `Configuration`

- **Contents:** Contains the `Options` classes (e.g., `DatabaseOptions`, `KafkaOptions`, `RedisOptions`) used for strongly-typed configuration.
- **Purpose:** Since multiple services (APIs, the consumer) need to connect to the same infrastructure, they share the same configuration _shape_. Placing these classes in `Contracts` ensures that our configuration setup is standardized across all projects.

## 3. Usage

The objects defined in the `Contracts` project are used in a clear, directional flow:

| Contract Type | Created By                            | Transported Via  | Consumed By                            |
| :------------ | :------------------------------------ | :--------------- | :------------------------------------- |
| **Events**    | The **Write API**'s command handlers. | **Apache Kafka** | The **Kafka Consumer** worker service. |
| **DTOs**      | The **Read API**'s query handlers.    | **HTTP (JSON)**  | The **Clients** (WPF, Web Dashboards). |

## 4. Example: The User Registration CQRS Flow

This example illustrates how a `UserRegisteredEvent` and a `UserProfileDto` are used in a typical flow.

1.  **The Command:** A new user fills out the registration form in the **WPF Client**. The client sends a request containing the user's details to the **Write API**. The API's controller handles this as a `RegisterUserCommand`.

2.  **The Event:**

    - The command handler in the Write API validates the data, hashes the password, and saves the new user to the **Write DB**.
    - Upon successful database commit, the handler creates a new instance of the **`UserRegisteredEvent`** record (from `ProjectZenith.Contracts`).
    - This event object is serialized and published to the `users` topic in **Kafka**.

3.  **The Projection:**

    - The **Kafka Consumer** service, listening to the `users` topic, receives the `UserRegisteredEvent` message.
    - It deserializes the message back into the `UserRegisteredEvent` object.
    - It then uses the data from the event (`UserId`, `Email`, `Username`, `RegisteredAt`) to create a new, denormalized user record in the **Read DB**, optimized for fast lookups.

4.  **The Query & DTO:**
    - Later, the user (or an admin) wants to view their profile. The client sends a `GET` request to the **Read API**.
    - The Read API's query handler fetches the denormalized user data from the **Read DB**.
    - It then maps this data into a new instance of the **`UserProfileDto`** class (from `ProjectZenith.Contracts`). This DTO might exclude sensitive information like the password hash.
    - The `UserProfileDto` is serialized to JSON and returned in the HTTP response to the client, which then displays the profile information.

```

```
