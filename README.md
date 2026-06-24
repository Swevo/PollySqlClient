# PollySqlClient

[![NuGet](https://img.shields.io/nuget/v/PollySqlClient.svg)](https://www.nuget.org/packages/PollySqlClient)
[![NuGet Downloads](https://img.shields.io/nuget/dt/PollySqlClient.svg)](https://www.nuget.org/packages/PollySqlClient)
[![CI](https://github.com/Swevo/PollySqlClient/actions/workflows/build.yml/badge.svg)](https://github.com/Swevo/PollySqlClient/actions)

**Polly v8 resilience for SQL Server and Azure SQL** — retry, timeout, and circuit-breaker for `SqlConnection` queries and commands, plus a built-in `SqlServerTransientErrors` predicate covering all common SQL Server and Azure SQL transient error numbers. Zero changes to your existing SQL.

```csharp
// Before
await cmd.ExecuteNonQueryAsync();

// After — automatic retry + timeout on every operation
var resilient = connection.WithPolly(pipeline =>
    pipeline
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            ShouldHandle = SqlServerTransientErrors.IsTransient, // built-in ✔
        })
        .AddTimeout(TimeSpan.FromSeconds(30)));

await resilient.ExecuteAsync("INSERT INTO Orders (CustomerId) VALUES (@id)",
    parameters: [new SqlParameter("@id", customerId)]);
```

---

## Installation

```bash
dotnet add package PollySqlClient
```

Targets **net6.0**, **net8.0**, and **net9.0**.
Dependencies: `Polly.Core 8.*`, `Microsoft.Data.SqlClient 6.*`, `Microsoft.Extensions.DependencyInjection.Abstractions 8.*`

---

## SqlServerTransientErrors — the key feature

Knowing *which* SQL Server errors are safe to retry is the hard part. `PollySqlClient` ships a pre-built `SqlServerTransientErrors.IsTransient` predicate so you don't have to look up error numbers.

```csharp
new RetryStrategyOptions
{
    MaxRetryAttempts = 3,
    ShouldHandle = SqlServerTransientErrors.IsTransient,
}
```

### SQL Server errors

| Error | Description |
|-------|-------------|
| `1205` | Deadlock victim |
| `1204` | Instance ran out of locks |
| `233`  | Connection does not exist (named pipes) |
| `64`   | Connection lost during login |
| `10053` | Transport-level error receiving from server |
| `10054` | Transport-level error sending to server |
| `10060` | Network error / server not found |

### Azure SQL errors

| Error | Description |
|-------|-------------|
| `40613` | Database not currently available — retry |
| `40501` | Service busy — retry after 10 seconds |
| `40197` | Error processing request — retry |
| `49920` | Too many operations in progress |
| `49919` | Too many create/update operations |
| `49918` | Not enough resources to process request |
| `10929` | Resource limit reached — retry later |
| `10928` | Resource limit hit |
| `4221`  | Login to read-secondary failed |

The raw set is also available for extension:
```csharp
var myErrors = SqlServerTransientErrors.ErrorNumbers.ToHashSet();
myErrors.Add(4060); // Cannot open database (sometimes transient)

new RetryStrategyOptions
{
    ShouldHandle = new PredicateBuilder().Handle<SqlException>(ex =>
        ex.Errors.Cast<SqlError>().Any(e => myErrors.Contains(e.Number)))
}
```

---

## Quick start

### Inline pipeline

```csharp
using PollySqlClient;

await using var connection = new SqlConnection(connectionString);
var resilient = connection.WithPolly(pipeline =>
    pipeline
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(200),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = SqlServerTransientErrors.IsTransient,
        })
        .AddTimeout(TimeSpan.FromSeconds(30)));

await resilient.OpenAsync();

// Non-query
await resilient.ExecuteAsync(
    "INSERT INTO Events (Type, Payload) VALUES (@type, @payload)",
    parameters: [new("@type", "OrderPlaced"), new("@payload", json)]);

// Query with mapper
var orders = await resilient.QueryAsync(
    "SELECT Id, Total FROM Orders WHERE CustomerId = @id",
    reader => new Order(reader.GetInt32(0), reader.GetDecimal(1)),
    parameters: [new SqlParameter("@id", customerId)]);

// Scalar
var count = await resilient.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Orders");
```

### Dependency injection

```csharp
// Program.cs
builder.Services.AddScoped(_ => new SqlConnection(connectionString));

builder.Services.AddPollySqlClient(pipeline =>
    pipeline
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(200),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = SqlServerTransientErrors.IsTransient,
        })
        .AddTimeout(TimeSpan.FromSeconds(30))
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = 10,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(15),
        }));

// Repository
public class OrderRepository(SqlConnection db, ResiliencePipeline pipeline)
{
    public async Task<List<Order>> GetByCustomerAsync(int customerId)
    {
        var resilient = db.WithPolly(pipeline);
        await resilient.OpenAsync();
        return await resilient.QueryAsync(
            "SELECT Id, Total FROM Orders WHERE CustomerId = @id",
            r => new Order(r.GetInt32(0), r.GetDecimal(1)),
            parameters: [new SqlParameter("@id", customerId)]);
    }
}
```

---

## Supported operations

| Method | Description |
|--------|-------------|
| `OpenAsync` | Open the connection with retry |
| `ExecuteAsync` | Execute non-query, returns rows affected |
| `ExecuteScalarAsync<T>` | Execute scalar, returns first column of first row |
| `QueryAsync<T>` | Query with row mapper, returns `List<T>` |
| `QueryFirstOrDefaultAsync<T>` | Query with row mapper, returns first row or `default` |

---

## Pipeline order

```
[Timeout] → [Retry] → [Circuit Breaker] → [SqlClient]
```

```csharp
pipeline
    .AddTimeout(TimeSpan.FromSeconds(30))   // 1. Overall deadline
    .AddRetry(retryOptions)                 // 2. Retry transient failures
    .AddCircuitBreaker(cbOptions)           // 3. Open circuit under load
```

---

## Related packages

| Package | Downloads | Description |
|---|---|---|
| [PollyNpgsql](https://www.nuget.org/packages/PollyNpgsql) | [![Downloads](https://img.shields.io/nuget/dt/PollyNpgsql.svg)](https://www.nuget.org/packages/PollyNpgsql) | Polly v8 resilience for Npgsql (PostgreSQL) with PostgresTransientErrors predicate |
| [PollyDapper](https://www.nuget.org/packages/PollyDapper) | [![Downloads](https://img.shields.io/nuget/dt/PollyDapper.svg)](https://www.nuget.org/packages/PollyDapper) | Polly v8 resilience for Dapper (works with any IDbConnection) |
| [PollyEFCore](https://www.nuget.org/packages/PollyEFCore) | [![Downloads](https://img.shields.io/nuget/dt/PollyEFCore.svg)](https://www.nuget.org/packages/PollyEFCore) | Polly v8 resilience for EF Core queries and SaveChanges |
| [PollyMongo](https://www.nuget.org/packages/PollyMongo) | [![Downloads](https://img.shields.io/nuget/dt/PollyMongo.svg)](https://www.nuget.org/packages/PollyMongo) | Polly v8 resilience for MongoDB.Driver |
| [PollyAzureBlob](https://www.nuget.org/packages/PollyAzureBlob) | [![Downloads](https://img.shields.io/nuget/dt/PollyAzureBlob.svg)](https://www.nuget.org/packages/PollyAzureBlob) | Polly v8 resilience for Azure Blob Storage |
| [PollyAzureEventHub](https://github.com/Swevo/PollyAzureEventHub) | Polly v8 for Azure Event Hubs |
| [PollyAzureServiceBus](https://www.nuget.org/packages/PollyAzureServiceBus) | [![Downloads](https://img.shields.io/nuget/dt/PollyAzureServiceBus.svg)](https://www.nuget.org/packages/PollyAzureServiceBus) | Polly v8 resilience for Azure Service Bus |
| [PollyRedis](https://www.nuget.org/packages/PollyRedis) | [![Downloads](https://img.shields.io/nuget/dt/PollyRedis.svg)](https://www.nuget.org/packages/PollyRedis) | Polly v8 resilience for StackExchange.Redis |
| [PollyMediatR](https://www.nuget.org/packages/PollyMediatR) | [![Downloads](https://img.shields.io/nuget/dt/PollyMediatR.svg)](https://www.nuget.org/packages/PollyMediatR) | Polly v8 resilience for MediatR |
| [PollyOpenAI](https://www.nuget.org/packages/PollyOpenAI) | [![Downloads](https://img.shields.io/nuget/dt/PollyOpenAI.svg)](https://www.nuget.org/packages/PollyOpenAI) | Polly v8 resilience for OpenAI and Azure OpenAI |
| [PollyHealthChecks](https://www.nuget.org/packages/PollyHealthChecks) | [![Downloads](https://img.shields.io/nuget/dt/PollyHealthChecks.svg)](https://www.nuget.org/packages/PollyHealthChecks) | ASP.NET Core health checks for Polly v8 circuit breakers |
| [PollyElasticsearch](https://github.com/Swevo/PollyElasticsearch) | Polly v8 for Elastic.Clients.Elasticsearch |
| [PollyAzureKeyVault](https://github.com/Swevo/PollyAzureKeyVault) | Polly v8 for Azure Key Vault |
| [PollySendGrid](https://github.com/Swevo/PollySendGrid) | Polly v8 for SendGrid |
| [PollyMassTransit](https://github.com/Swevo/PollyMassTransit) | Polly v8 for MassTransit |
| [PollyAzureTableStorage](https://github.com/Swevo/PollyAzureTableStorage) | Polly v8 for Azure Table Storage |
| [PollyMailKit](https://github.com/Swevo/PollyMailKit) | MailKit SMTP email client |
| [PollyAzureQueueStorage](https://github.com/Swevo/PollyAzureQueueStorage) | Azure Queue Storage QueueClient |
| [PollyHangfire](https://github.com/Swevo/PollyHangfire) | Hangfire IBackgroundJobClient |
| [PollyBackoff](https://www.nuget.org/packages/PollyBackoff) | [![Downloads](https://img.shields.io/nuget/dt/PollyBackoff.svg)](https://www.nuget.org/packages/PollyBackoff) | Jitter, linear & custom backoff for Polly v8 retry |

---

## License

MIT
