// <copyright file="PollySqlClientExtensionsTests.cs" company="Justin Bannister">
// Copyright (c) Justin Bannister. All rights reserved.
// </copyright>

namespace PollySqlClient.Tests;

public class PollySqlClientExtensionsTests
{
    private readonly ResiliencePipeline _pipeline = ResiliencePipeline.Empty;

    [Fact]
    public void WithPolly_NullConnection_ThrowsArgumentNullException()
    {
        SqlConnection? connection = null;
        Assert.Throws<ArgumentNullException>(() => connection!.WithPolly(_pipeline));
    }

    [Fact]
    public void WithPolly_NullPipeline_ThrowsArgumentNullException()
    {
        using var connection = new SqlConnection("Server=localhost;Database=test;Integrated Security=true;");
        ResiliencePipeline? pipeline = null;
        Assert.Throws<ArgumentNullException>(() => connection.WithPolly(pipeline!));
    }

    [Fact]
    public void WithPolly_ValidArguments_ReturnsResilientSqlConnection()
    {
        using var connection = new SqlConnection("Server=localhost;Database=test;Integrated Security=true;");
        var result = connection.WithPolly(_pipeline);
        Assert.NotNull(result);
        Assert.IsType<ResilientSqlConnection>(result);
    }

    [Fact]
    public void WithPolly_ExposesInnerConnection()
    {
        using var connection = new SqlConnection("Server=localhost;Database=test;Integrated Security=true;");
        var result = connection.WithPolly(_pipeline);
        Assert.Same(connection, result.InnerConnection);
    }
}
