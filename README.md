# Project Zenith

[![Version](https://img.shields.io/badge/version-0.1.0--alpha-orange)](https://github.com/your-username/ProjectZenith)

Project Zenith is a modern, distributed, and scalable digital application storefront built from the ground up on .NET. It provides a complete ecosystem for developers to securely publish their applications and for users to discover, purchase, and manage them.

## 🏛️ Architectural Overview

This project is a practical implementation of advanced software architecture patterns, designed for high performance, scalability, and maintainability.

The core of the system is a **CQRS (Command Query Responsibility Segregation)** and **Event-Driven Architecture**.

- **Write Stack:** Handles all state changes (commands) via a transactional API connected to a normalized MSSQL database. Its sole responsibility is to maintain data integrity and publish events.
- **Event Bridge (Apache Kafka):** Acts as the durable, asynchronous backbone of the system. All state changes from the Write Stack are published as events to Kafka.
- **Read Stack:** Consumes events from Kafka to build and maintain a denormalized, query-optimized read database (MSSQL). A high-speed Redis cache sits on top, providing exceptional performance for all public-facing queries.

This decoupling allows the write and read models to be scaled and optimized independently, creating a highly resilient and performant platform.

---

### ✨ Key Features

- **🚀 High-Performance Backend:** Architected using **CQRS and Event-Driven patterns** with **.NET, C#, and Apache Kafka** to handle high traffic and ensure system resilience.
- **🛡️ Production-Grade Security:** Features a complete, multi-device security system with **JWT-based authentication and rotating refresh tokens** to protect user accounts and prevent session hijacking.
- **🖥️ Rich Native Client:** A user-facing storefront built as a native Windows desktop application with **WPF and the MVVM design pattern**, featuring a multi-threaded download manager and automatic update checking.
- **🔬 Content Security Pipeline:** Ensures platform safety with an **automated malware scanning** pipeline for all developer uploads, using magic byte analysis to block malicious file exploits.
- **💳 E-Commerce & Monetization:** A full e-commerce system with **Stripe integration** for processing user purchases and a backend for tracking developer revenue.
- **🐳 Fully Automated DevOps Lifecycle:** The entire backend is **containerized with Docker** and orchestrated with Docker Compose for local development, with a complete **CI/CD pipeline** for automated testing and deployment.

---

### 💻 Tech Stack

| Layer                        | Technology                                                                     |
| :--------------------------- | :----------------------------------------------------------------------------- |
| **Client (Desktop)**         | **WPF**, **.NET 8**, **MVVM**                                                  |
| **Web Dashboards**           | **ASP.NET Core MVC**                                                           |
| **Backend APIs (CQRS)**      | **ASP.NET Core Web API**, **MediatR**, **FluentValidation**                    |
| **Databases**                | **Microsoft SQL Server** (Separate normalized Write DB & denormalized Read DB) |
| **Messaging Bridge**         | **Apache Kafka**                                                               |
| **Caching**                  | **Redis**                                                                      |
| **DevOps & Infrastructure**  | **Docker**, **Docker Compose**, **GitHub Actions / Azure DevOps**              |
| **Object-Relational Mapper** | **Entity Framework Core 8**                                                    |
| **Security**                 | **ASP.NET Core Identity**, **JWT**, **Data Protection API**                    |

---

## 🚀 Getting Started

Follow these instructions to get the complete Project Zenith stack running on your local machine for development and testing.

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- A SQL Client like [Azure Data Studio](https://azure.microsoft.com/products/data-studio) or SSMS.
- Git

### Local Setup Instructions

1.  **Clone the repository:**

    ```sh
    git clone https://github.com/your-username/ProjectZenith.git
    cd ProjectZenith
    ```

2.  **Create the Docker Network:**
    The project is configured to use a shared external Docker network. Create it by running:

    ```sh
    docker network create mydockernetwork
    ```

3.  **Launch the Backend Infrastructure:**
    This command will start the SQL Server, Kafka, and Redis containers in the background.

    ```sh
    docker-compose up -d
    ```

4.  **Configure User Secrets:**
    The application uses the .NET Secret Manager to handle sensitive data locally. **This is a critical step.** Run these commands from the root directory of the repository.

    - Initialize secrets for all necessary projects:
      ```sh
      dotnet user-secrets init --project src/Api.Write/
      dotnet user-secrets init --project src/Api.Read/
      dotnet user-secrets init --project src/Kafka.Consumer/
      ```
    - Set the required secret values:

      ```sh
      # For the Write API
      dotnet user-secrets set "ConnectionStrings:WriteDb" "Server=localhost,1401;Database=ProjectZenithWriteDb;User Id=sa;Password=yourStrong(!)Password;TrustServerCertificate=True;" --project src/Api.Write/
      dotnet user-secrets set "Jwt:Key" "YourSuperSecretKeyThatIsLongAndSecure123!" --project src/Api.Write/

      # For the Read API
      dotnet user-secrets set "ConnectionStrings:ReadDb" "Server=localhost,1402;Database=ProjectZenithReadDb;User Id=sa;Password=yourStrong(!)Password;TrustServerCertificate=True;" --project src/Api.Read/

      # For the Kafka Consumer
      dotnet user-secrets set "ConnectionStrings:ReadDb" "Server=localhost,1402;Database=ProjectZenithReadDb;User Id=sa;Password=yourStrong(!)Password;TrustServerCertificate=True;" --project src/Kafka.Consumer/
      dotnet user-secrets set "Kafka:Brokers" "[\"localhost:9093\"]" --project src/Kafka.Consumer/
      dotnet user-secrets set "Redis:ConnectionString" "localhost:6379" --project src/Kafka.Consumer/
      ```

5.  **Apply Database Migrations:**
    This command will create the Write Database and apply the entire schema.

    ```sh
    dotnet ef database update --project src/Api.Write/
    ```

6.  **Run the Application:**
    The easiest way to run this multi-project solution is to configure Visual Studio:
    - Right-click the Solution in the Solution Explorer -> **Properties**.
    - Under "Common Properties" -> "Startup Project", select **"Multiple startup projects"**.
    - Set the "Action" to **"Start"** for:
      - `ProjectZenith.Api.Write`
      - `ProjectZenith.Api.Read`
      - `ProjectZenith.Kafka.Consumer`
      - `ProjectZenith.Client.Wpf`
      - `ProjectZenith.Web.Admin`
    - Click **Apply** and then press **F5** to launch the entire platform.

---

## 🤝 Contributing

This project is primarily for educational and portfolio purposes. However, if you have suggestions or find bugs, please feel free to open an issue.
