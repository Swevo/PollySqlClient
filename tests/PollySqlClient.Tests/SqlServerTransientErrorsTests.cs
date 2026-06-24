// <copyright file="SqlServerTransientErrorsTests.cs" company="Justin Bannister">
// Copyright (c) Justin Bannister. All rights reserved.
// </copyright>

namespace PollySqlClient.Tests;

public class SqlServerTransientErrorsTests
{
    [Theory]
    [InlineData(1205)]  // Deadlock victim
    [InlineData(1204)]  // Instance ran out of locks
    [InlineData(233)]   // Connection does not exist
    [InlineData(64)]    // Connection lost during login
    [InlineData(10053)] // Transport-level error receiving
    [InlineData(10054)] // Transport-level error sending
    [InlineData(10060)] // Network error
    [InlineData(40197)] // Azure SQL: error processing request
    [InlineData(40501)] // Azure SQL: service busy
    [InlineData(40613)] // Azure SQL: database not currently available
    [InlineData(49918)] // Azure SQL: not enough resources
    [InlineData(49919)] // Azure SQL: too many create/update operations
    [InlineData(49920)] // Azure SQL: too many operations in progress
    [InlineData(10929)] // Azure SQL: resource limit
    [InlineData(10928)] // Azure SQL: resource limit hit
    [InlineData(4221)]  // Azure SQL: login to read-secondary failed
    public void ErrorNumbers_ContainsTransientError(int errorNumber)
    {
        Assert.Contains(errorNumber, SqlServerTransientErrors.ErrorNumbers);
    }

    [Theory]
    [InlineData(547)]   // Constraint conflict — not transient
    [InlineData(2627)]  // Unique constraint violation — not transient
    [InlineData(208)]   // Invalid object name — not transient
    [InlineData(515)]   // Cannot insert null — not transient
    public void ErrorNumbers_DoesNotContainNonTransientError(int errorNumber)
    {
        Assert.DoesNotContain(errorNumber, SqlServerTransientErrors.ErrorNumbers);
    }

    [Fact]
    public void IsTransient_ReturnsPredicateBuilder()
    {
        var predicate = SqlServerTransientErrors.IsTransient;
        Assert.NotNull(predicate);
    }
}
