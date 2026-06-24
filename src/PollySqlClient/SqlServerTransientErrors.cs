// <copyright file="SqlServerTransientErrors.cs" company="Justin Bannister">
// Copyright (c) Justin Bannister. All rights reserved.
// </copyright>

namespace PollySqlClient;

/// <summary>
/// Pre-built <see cref="PredicateBuilder"/> covering SQL Server and Azure SQL error numbers
/// that represent transient conditions safe to retry.
/// </summary>
/// <remarks>
/// Pass <see cref="IsTransient"/> directly to <see cref="RetryStrategyOptions.ShouldHandle"/>:
/// <code>
/// new RetryStrategyOptions { ShouldHandle = SqlServerTransientErrors.IsTransient }
/// </code>
/// </remarks>
public static class SqlServerTransientErrors
{
    /// <summary>
    /// SQL Server and Azure SQL error numbers that represent transient errors safe to retry.
    /// </summary>
    /// <list type="bullet">
    ///   <item><c>1205</c> — Deadlock victim</item>
    ///   <item><c>1204</c> — Instance ran out of locks</item>
    ///   <item><c>233</c>  — Connection does not exist (named pipes)</item>
    ///   <item><c>64</c>   — Connection lost during login</item>
    ///   <item><c>10053</c> — Transport-level error receiving from the server</item>
    ///   <item><c>10054</c> — Transport-level error sending to the server</item>
    ///   <item><c>10060</c> — Network error or server not found</item>
    ///   <item><c>40197</c> — Azure SQL: error processing request, retry</item>
    ///   <item><c>40501</c> — Azure SQL: service busy, retry after 10 seconds</item>
    ///   <item><c>40613</c> — Azure SQL: database not currently available</item>
    ///   <item><c>49918</c> — Azure SQL: not enough resources to process</item>
    ///   <item><c>49919</c> — Azure SQL: too many create/update operations</item>
    ///   <item><c>49920</c> — Azure SQL: too many operations in progress</item>
    ///   <item><c>10929</c> — Azure SQL: resource limit reached, retry later</item>
    ///   <item><c>10928</c> — Azure SQL: resource limit hit</item>
    ///   <item><c>4221</c>  — Azure SQL: login to read-secondary failed due to long wait</item>
    /// </list>
    public static readonly IReadOnlySet<int> ErrorNumbers = new HashSet<int>
    {
        1205,  // Deadlock victim
        1204,  // Instance ran out of locks
        233,   // Connection does not exist
        64,    // Connection lost during login
        10053, // Transport-level error receiving
        10054, // Transport-level error sending
        10060, // Network error / server not found
        40197, // Azure SQL: error processing request
        40501, // Azure SQL: service busy
        40613, // Azure SQL: database not currently available
        49918, // Azure SQL: not enough resources
        49919, // Azure SQL: too many create/update operations
        49920, // Azure SQL: too many operations in progress
        10929, // Azure SQL: resource limit, retry later
        10928, // Azure SQL: resource limit hit
        4221,  // Azure SQL: login to read-secondary failed
    };

    /// <summary>
    /// A <see cref="PredicateBuilder"/> that matches any <see cref="SqlException"/> containing
    /// at least one error with a known transient SQL Server or Azure SQL error number.
    /// </summary>
    public static PredicateBuilder IsTransient =>
        (PredicateBuilder)new PredicateBuilder().Handle<SqlException>(ex =>
            ex.Errors.Cast<SqlError>().Any(e => ErrorNumbers.Contains(e.Number)));
}
