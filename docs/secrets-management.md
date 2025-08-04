# Project Zenith: Local Development Secrets Management

This document outlines the procedure for managing sensitive configuration data (secrets) for local development using the .NET Secret Manager tool (User Secrets).

## 1. Overview: The Role of User Secrets

The .NET User Secrets feature is the **official, Microsoft-recommended method** for handling sensitive configuration data during local development. Its primary purpose is to **prevent sensitive data like database passwords, API keys, and other credentials from being checked into source control (Git)**.

### How It Works

- When you initialize User Secrets for a project, the .NET SDK adds a unique `<UserSecretsId>` tag to that project's `.csproj` file.
- It then creates a `secrets.json` file in a secure location within your local user profile directory, completely outside of the project folder structure.
- The `UserSecretsId` acts as a link between your project and its corresponding `secrets.json` file.
- When an application is run in the `Development` environment, the .NET Host Builder automatically discovers and loads the values from this `secrets.json` file, overriding any values from `appsettings.json`.

This ensures that our code remains clean of sensitive data while providing a seamless way to inject these secrets at runtime for local debugging and testing.

## 2. Setup Instructions

To configure your local environment, you must initialize and set the required secrets for each runnable project.

### 2.1. Prerequisites

- .NET SDK installed.
- A terminal (PowerShell, Command Prompt, Git Bash).

### 2.2. Initialization

First, you must initialize User Secrets for each project that requires them. Run these commands from the **root of the solution directory** (`/ProjectZenith/`):

```bash
# Initialize secrets for the projects that need configuration
dotnet user-secrets init --project src/Api.Write/
dotnet user-secrets init --project src/Api.Read/
dotnet user-secrets init --project src/Kafka.Consumer/
dotnet user-secrets init --project src/Web.Admin/
```

### 2.3. Setting the Secrets

Next, set the specific secret values for each project. Note that each project has its own isolated set of secrets.

**Run these commands from the solution root:**

```bash
# --- Set secrets for Api.Write ---
dotnet user-secrets set "ConnectionStrings:WriteDb" "Server=localhost,1401;Database=ProjectZenithWriteDb;User Id=sa;Password=yourStrong(!)Password;TrustServerCertificate=True;" --project src/Api.Write/
dotnet user-secrets set "Kafka:Brokers" "[\"localhost:9093\"]" --project src/Api.Write/

# --- Set secrets for Api.Read ---
dotnet user-secrets set "ConnectionStrings:ReadDb" "Server=localhost,1402;Database=ProjectZenithReadDb;User Id=sa;Password=yourStrong(!)Password;TrustServerCertificate=True;" --project src/Api.Read/
dotnet user-secrets set "Redis:ConnectionString" "localhost:6379" --project src/Api.Read/
dotnet user-secrets set "Kafka:Brokers" "[\"localhost:9093\"]" --project src/Api.Read/

# --- Set secrets for Kafka.Consumer ---
dotnet user-secrets set "ConnectionStrings:ReadDb" "Server=localhost,1402;Database=ProjectZenithReadDb;User Id=sa;Password=yourStrong(!)Password;TrustServerCertificate=True;" --project src/Kafka.Consumer/
dotnet user-secrets set "Kafka:Brokers" "[\"localhost:9093\"]" --project src/Kafka.Consumer/
dotnet user-secrets set "Redis:ConnectionString" "localhost:6379" --project src/Kafka.Consumer/
```

_(Note: When setting a JSON array from the command line, it's often necessary to escape the quotes as shown for `Kafka:Brokers`.)_

## 3. Verification

There are two primary ways to verify that secrets have been loaded correctly.

### 3.1. Listing Secrets via CLI

You can view all secrets set for a specific project using the `list` command. This is useful for confirming what you have set.

**Example for `Api.Read`:**

```bash
dotnet user-secrets list --project src/Api.Read/
```

**Expected Output:**

```
ConnectionStrings:ReadDb = Server=localhost,1402;...
Redis:ConnectionString = localhost:6379
Kafka:Brokers = ["localhost:9093"]
```

### 3.2. Checking the Health Endpoint

Our API projects include a test `/api/health` endpoint that displays the configuration summary.

1.  Start all services (`docker-compose up -d` and the .NET projects).
2.  Navigate to the health endpoint for an API (e.g., `https://localhost:7234/api/health` for the Read API).
3.  **Expected Result:** The JSON response should show the fully resolved connection strings and broker addresses. If you see empty strings or default values, it means the User Secrets for that project are either not set or not being loaded.

## 4. Security Considerations

- **Never Commit `secrets.json`:** The entire purpose of this system is that the `secrets.json` file lives outside the git repository. The `.gitignore` file should be respected, and no attempt should be made to add these files to source control.
- **Local Development Only:** User Secrets is designed **exclusively for the local development environment**. It is not a production secret vault.
- **Production Environment:** In a production environment (like Azure), secrets must be stored in a secure service like **Azure Key Vault** or provided as secure environment variables in the App Service configuration. The application code does not need to change; the configuration provider for production will securely fetch these values.

```

```
