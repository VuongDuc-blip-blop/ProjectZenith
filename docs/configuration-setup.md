# Project Zenith: Configuration Management

This document outlines the configuration system for Project Zenith. It details the sources of configuration, the patterns used to consume them, and the methods for verifying that the configuration is loaded correctly during local development.

## 1. Overview: The Options Pattern

Project Zenith uses the **Options Pattern**, a core feature of ASP.NET Core, to manage all application settings. Instead of accessing configuration directly via "magic strings" (e.g., `_config["Kafka:Brokers"]`), we bind configuration sections to strongly-typed C# classes (e.g., `KafkaOptions`).

This approach provides several key benefits:

- **Strong Typing & IntelliSense:** Eliminates typos and provides full code completion in the IDE.
- **Separation of Concerns:** The classes and services that _use_ configuration don't need to know _where_ the configuration comes from (a JSON file, environment variable, etc.).
- **Validation:** We can apply data annotation attributes (like `[Required]`) to our Options classes to ensure the application fails fast on startup if a required setting is missing.
- **Testability:** It makes our services easier to unit test, as we can simply provide a mocked instance of an Options class instead of a complex configuration object.

## 2. Configuration Sources (Local Development)

The .NET Host Builder (`Host.CreateDefaultBuilder`) automatically loads configuration from multiple sources in a specific, layered order. Later sources override earlier ones. For our local development environment, the order is:

1.  **`appsettings.json`:** This file contains the base configuration, default values, and non-sensitive settings for all environments.
2.  **`appsettings.Development.json`:** This file contains overrides that are specific to the local development environment.
3.  **User Secrets:** This is the most important source for local development. The **Secret Manager** tool stores all sensitive data (like database passwords and API keys) in a `secrets.json` file located in the user's profile directory, completely outside the project folder. This file is **never** checked into source control.
4.  **Environment Variables:** System-level environment variables.
5.  **Command-line Arguments:** Arguments passed during application startup.

This layered approach allows us to keep our repository clean of secrets while providing a powerful and flexible way to manage settings.

## 3. Usage: Configuration Registration and Injection

### 3.1. Registration in `Program.cs`

In each runnable project's `Program.cs`, we register our Options classes with the dependency injection (DI) container. This binds a section of our configuration to a C# class.

**Example from `Api.Read/Program.cs`:**

```csharp
// 1. Configure DatabaseOptions to use the "ReadDb" connection string
builder.Services.Configure<DatabaseOptions>(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("ReadDb")
        ?? throw new InvalidOperationException("Connection string 'ReadDb' not found.");
});

// 2. Configure KafkaOptions by binding to the "Kafka" section
builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection("Kafka"));

// 3. Configure RedisOptions by binding to the "Redis" section
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection("Redis"));
```

### 3.2. Injection into Services

Once registered, any service can receive the strongly-typed configuration via **constructor injection**. The DI container automatically provides the configured instance wrapped in `IOptions<T>`.

**Example from `ConfigService.cs`:**

```csharp
public class ConfigService
{
    private readonly DatabaseOptions _dbOptions;
    private readonly KafkaOptions _kafkaOptions;
    private readonly RedisOptions _redisOptions;

    // The DI container injects the IOptions<T> wrappers here
    public ConfigService(
        IOptions<DatabaseOptions> dbOptions,
        IOptions<KafkaOptions> kafkaOptions,
        IOptions<RedisOptions> redisOptions)
    {
        // We access the actual instance via the .Value property
        _dbOptions = dbOptions.Value;
        _kafkaOptions = kafkaOptions.Value;
        _redisOptions = redisOptions.Value;
    }

    public string GetConfigSummary()
    {
        // Now we can use the strongly-typed properties
        return $"Database Connection: {_dbOptions.ConnectionString}, " +
               $"Kafka Brokers: {_kafkaOptions.Brokers}, " +
               $"Redis Connection: {_redisOptions.ConnectionString}";
    }
}
```

## 4. Verification Instructions

To ensure that each service is loading its configuration correctly from all sources (especially User Secrets), you can call the temporary test endpoint we created.

### 4.1. Prerequisites

- The Docker infrastructure must be running (`docker-compose up -d`).
- All runnable projects must be started (e.g., using Visual Studio's "Multiple startup projects" feature).

### 4.2. Verification Steps

1.  **Verify the Write API:**

    - Find the port number for the `ProjectZenith.Api.Write` service from the startup logs (e.g., `https://localhost:7123`).
    - Navigate to its Swagger UI page: `https://localhost:7123/swagger`.
    - Find the `GET /Config` endpoint and execute it.
    - **Expected Result:** The response body should show the full connection string for the **Write DB** (`...Server=localhost,1401...`) and other relevant settings.

2.  **Verify the Read API:**

    - Find the port number for the `ProjectZenith.Api.Read` service (e.g., `https://localhost:7234`).
    - Navigate to its Swagger UI page: `https://localhost:7234/swagger`.
    - Find the `GET /Config` endpoint and execute it.
    - **Expected Result:** The response body should show the full connection string for the **Read DB** (`...Server=localhost,1402...`), the Kafka broker address, and the Redis connection string.

3.  **Verify the Kafka Consumer:**
    - Examine the console window for the `ProjectZenith.Kafka.Consumer` worker service.
    - On startup, there should be **no `InvalidOperationException`**. If the service is running and displaying messages like `Worker running at...`, it has successfully loaded its configuration.

If any service fails to start with an `InvalidOperationException`, it means a required secret was not set for that specific project. Use the `dotnet user-secrets set` command to add the missing value.

```

```
