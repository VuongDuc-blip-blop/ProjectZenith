# Project Zenith: Local Infrastructure Setup

This document provides a comprehensive guide to the local development infrastructure for Project Zenith, managed via Docker Compose. This setup is designed to fully replicate our production CQRS and event-driven architecture on a local machine.

## 1. Overview of Services

Our `docker-compose.yml` file defines the core backend services required to run the platform. Each service plays a distinct role in our CQRS architecture.

- **`write-db` (Microsoft SQL Server):**

  - **Role:** The **Write Database**. This is the transactional source of truth for the entire system. It uses a normalized schema optimized for data integrity and consistent writes. All commands from the Write API interact exclusively with this database.

- **`read-db` (Microsoft SQL Server):**

  - **Role:** The **Read Database**. This is a projection of the Write DB, optimized for fast queries. It uses a denormalized schema where data is pre-joined and formatted to match the needs of the UI. It is populated by the Kafka Consumer and serves all queries from the Read API.

- **`kafka` (Apache Kafka):**

  - **Role:** The **Messaging Bridge**. Kafka acts as the durable, asynchronous "nervous system" of our architecture. When the Write DB is updated, an event is published to a Kafka topic. This decouples the write and read sides of the system.

- **`redis` (Redis):**
  - **Role:** The **Caching Layer**. Redis is an in-memory data store that sits in front of the Read DB. It is used to cache frequently accessed data (e.g., trending apps, user profiles) to provide sub-millisecond response times and reduce the load on the Read DB.

## 2. Service Configuration Highlights

The `docker-compose.yml` file contains specific configurations to ensure our local environment is robust and mimics production behavior.

- **Why Two MSSQL Instances?** We run two separate SQL Server containers (`write-db` and `read-db`) to strictly enforce the CQRS pattern. This physical separation prevents any temptation to create a "shortcut" query from the Write API to the Read DB (or vice-versa), forcing all data synchronization to happen correctly through Kafka.

- **Kafka in KRaft Mode:** Our Kafka service runs in modern **KRaft mode**, which does not require a separate Zookeeper instance. This simplifies the setup, reduces resource consumption, and aligns with current best practices for Kafka. The `KAFKA_CFG_PROCESS_ROLES=controller,broker` environment variable enables this mode.

- **Kafka Listeners (`ADVERTISED_LISTENERS`):** We configure two listeners for Kafka:

  1.  `PLAINTEXT://kafka:9092`: For internal communication between services within the Docker network.
  2.  `EXTERNAL://localhost:9093`: For our .NET applications running on the host machine to connect to Kafka.

- **Redis Persistence:** The Redis service is configured with a volume (`redis_data:/data`). This ensures that any data cached in Redis will persist even if the container is restarted, which is useful for local development and testing.

## 3. Setup and Verification Instructions

Follow these steps to launch and verify the entire infrastructure stack.

### 3.1. Prerequisites

- Docker Desktop installed and running.
- A terminal (PowerShell, Command Prompt, or Git Bash).
- A database client like Azure Data Studio or SQL Server Management Studio (SSMS).

### 3.2. Starting the Infrastructure

1.  Navigate to the root directory of the `ProjectZenith` repository in your terminal.
2.  Ensure the external Docker network exists by running:
    ```sh
    docker network create mydockernetwork
    ```
    _(If it already exists, this command will show a harmless error, which can be ignored.)_
3.  Launch all services in detached mode:
    ```sh
    docker-compose up -d
    ```

### 3.3. Verifying Service Health

1.  **Check Running Containers:** Verify that all containers are running and healthy.

    ```sh
    docker-compose ps
    ```

    You should see `write-db`, `read-db`, `kafka`, and `redis` with a `State` of `Up`.

2.  **Connect to Databases (MSSQL):**

    - Open Azure Data Studio or SSMS.
    - **Connect to Write DB:**
      - Server: `localhost,1401`
      - Authentication: `SQL Login`
      - User: `sa`
      - Password: `yourStrong(!)Password` (or whatever is set in `docker-compose.yml`)
    - **Connect to Read DB:**
      - Server: `localhost,1402`
      - Authentication: `SQL Login`
      - User: `sa`
      - Password: `yourStrong(!)Password`
    - Success is being able to connect to both instances.

3.  **Verify Kafka:**

    - Exec into the Kafka container to use its command-line tools.

    ```sh
    docker-compose exec kafka /bin/bash
    ```

    - Inside the container, list all topics. Since `AUTO_CREATE_TOPICS_ENABLE` is true, you should see internal topics after a short while.

    ```sh
    kafka-topics.sh --bootstrap-server localhost:9092 --list
    ```

    - Exit the container by typing `exit`.

4.  **Ping Redis:**
    - Exec into the Redis container.
    ```sh
    docker-compose exec redis /bin/bash
    ```
    - Start the Redis CLI and run the `ping` command.
    ```sh
    redis-cli
    ping
    ```
    - The server should reply with `PONG`. Exit the CLI with `quit` and the container with `exit`.

## 4. Network Design

All services defined in our `docker-compose.yml` are attached to a pre-existing, external Docker bridge network named `mydockernetwork`.

- **Purpose:** Using a shared external network allows for decoupling. In the future, we could have a separate `docker-compose.yml` for monitoring tools (like Prometheus/Grafana) and have them join the same network to communicate with our services.
- **Service Discovery:** Within this network, Docker provides automatic DNS resolution. This means the Kafka container can be reached by other containers simply by using its service name `kafka` as the hostname (e.g., `kafka:9092`). This is how our .NET services running inside Docker will connect.
- **Host Communication:** Ports are exposed to the `localhost` of the host machine (e.g., `1401:1433` for the DB, `9093:9093` for Kafka) to allow our .NET applications, when run directly from Visual Studio or the CLI, to connect to the containerized services.
