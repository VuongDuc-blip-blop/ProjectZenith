# Project Zenith: Architectural Design Document

## 1. Introduction

This document outlines the core architectural principles for Project Zenith, a production-grade application store. The system is designed using a **Command Query Responsibility Segregation (CQRS)** pattern coupled with an **Event-Driven Architecture**.

This approach was chosen to meet our key non-functional requirements:

- **Scalability & Performance:** By separating the "Write" operations (like submitting an app) from the "Read" operations (like browsing the store), we can scale each side independently. The read side can be heavily optimized with caching and denormalized data for extremely fast load times, supporting thousands of concurrent users.
- **Decoupling & Flexibility:** The event-driven nature, using Apache Kafka as a messaging bridge, decouples our services. This means the team working on the developer dashboard can evolve their system without impacting the public storefront. It also allows for new services (e.g., analytics, notifications) to be added later by simply subscribing to existing event streams.
- **Resilience:** The asynchronous nature of event processing makes the system more resilient. If the read-side consumer is temporarily down, write operations can continue, and events will be processed once the consumer comes back online.

## 2. System Components

Project Zenith is composed of three primary logical stacks, as defined in our technical specification.

### 2.1. The Write Stack (The Source of Truth)

- **Purpose:** To handle all state-changing operations (Commands) with a focus on transactional integrity and data consistency.
- **Components:**
  - **Write API (ASP.NET Core):** The sole entry point for all commands (`POST`, `PUT`, `DELETE`). It validates incoming requests and dispatches them to command services.
  - **Command Services (C#/.NET):** Contain the core business logic for validating and executing commands against the domain models.
  - **Write DB (MSSQL):** A normalized, relational database schema optimized for transactional writes and data integrity. This is the ultimate source of truth for the entire system.
  - **Kafka Producer:** After a command successfully modifies the Write DB, this component publishes a domain event to Kafka to notify the rest of the system of the change.

### 2.2. The Messaging Bridge (The Nervous System)

- **Purpose:** To provide a durable, asynchronous communication channel between the Write Stack and all other services.
- **Component:**
  - **Apache Kafka:** Acts as a distributed, persistent log of all domain events (`AppSubmitted`, `UserRegistered`, `ReviewPosted`, etc.). Services subscribe to "topics" to receive the events they care about.

### 2.3. The Read Stack (The Public Face)

- **Purpose:** To handle all data retrieval operations (Queries) with a focus on high speed and availability.
- **Components:**
  - **Kafka Consumer (.NET Worker Service):** A background service that listens to Kafka topics, processes incoming events, and projects them into the Read DB.
  - **Read DB (MSSQL):** A denormalized database schema optimized for fast queries. Data here is pre-formatted and joined to match the exact needs of the UI, eliminating complex queries at runtime.
  - **Redis Cache:** An in-memory cache sitting in front of the Read DB for storing frequently accessed data, such as trending app lists or popular app details, reducing database load and providing sub-millisecond response times.
  - **Read API (ASP.NET Core):** The public-facing API for all `GET` requests. It serves data from the Redis Cache or the Read DB.

### 2.4. Clients

- **WPF Client (End-User):** The desktop application for consumers to browse, download, and review apps. Primarily interacts with the **Read API** for browsing and the **Write API** for actions like logging in or posting reviews.
- **Developer Dashboard (Web):** The web portal for developers to submit and manage their apps. Primarily interacts with the **Write API**.
- **Admin Dashboard (ASP.NET MVC):** The web portal for administrators to moderate content and manage the platform. Primarily interacts with the **Write API**.

## 3. Event Flow Example: A New App Submission

This example illustrates how the components work together when a developer submits a new application.

1.  **Command Initiation (Client):** A developer fills out the submission form on the **Developer Dashboard** and clicks "Submit." The client sends a `POST` request with the app metadata and files to the **Write API**.

2.  **Command Handling (Write Stack):**

    - The **Write API** receives the request, authenticates the developer, and validates the incoming data.
    - It dispatches an `SubmitAppCommand` to a **Command Service**.
    - The Command Service executes the business logic: it uploads the app files to Blob Storage and, within a single database transaction, inserts new records into the `Apps` and `Versions` tables in the **Write DB**.

3.  **Event Publication (Messaging Bridge):**

    - Upon successful commit of the database transaction, the Command Service uses the **Kafka Producer** to publish a new `AppSubmittedEvent` to the `apps` topic in **Kafka**. This event contains key data like `AppId`, `DeveloperId`, `AppName`, and `Version`.

4.  **Event Consumption & Projection (Read Stack):**

    - The **Kafka Consumer** service, which is subscribed to the `apps` topic, receives the `AppSubmittedEvent`.
    - The consumer processes the event and creates a new, denormalized record for the application in the **Read DB**, preparing it for fast retrieval. The app's status is likely set to "Pending Moderation."

5.  **Querying (Client - Admin):**
    - An administrator viewing the **Admin Dashboard** makes a request to the **Read API** to see pending apps.
    - The Read API queries the **Read DB** and returns a list of apps with "Pending Moderation" status, which now includes the newly submitted app.

## 4. Design Justifications

- **Write DB (MSSQL):** Chosen for its robust transactional support, ACID compliance, and strong referential integrity. These features are critical for the Write side, where data correctness is paramount.
- **Read DB (MSSQL):** While other databases like Elasticsearch could be used, using MSSQL for both allows us to leverage existing team expertise. The schema will be heavily denormalized with pre-calculated aggregates to ensure query performance.
- **Apache Kafka:** Chosen over simpler message queues (like RabbitMQ) because of its durability, persistence, and ability to "replay" events. This makes it a true "log of what happened," which is ideal for rebuilding read models or adding new downstream services later.
- **Redis Cache:** Chosen for its industry-leading speed. It will serve as a high-speed buffer for our most common queries, significantly reducing the load on the Read DB and providing an exceptional user experience for browsing the store.

## 5. Architectural Trade-Offs

- **Increased Complexity:** This architecture is significantly more complex than a standard monolithic CRUD application. It requires managing multiple services, databases, and a messaging infrastructure.

  - **Mitigation:** We mitigate this by establishing a clear, well-documented design from the start (this document), creating a structured syllabus, and using Docker Compose to manage the local development environment, making it simple to run the entire stack.

- **Eventual Consistency:** The Read side of the system is not updated instantaneously. There will be a small delay (typically milliseconds to seconds) between a command succeeding and its effects being visible to users. For example, a newly submitted app won't appear in search results immediately.
  - **Mitigation:** For most of our use cases (browsing, searching), this delay is perfectly acceptable and a worthwhile trade-off for performance and scalability. For user-specific actions where immediate feedback is required (e.g., "Your review has been posted"), the client UI can use "optimistic updates" to show the change immediately while the backend processes the event asynchronously.
