// <copyright file="PollySqlClientExtensions.cs" company="Justin Bannister">
// Copyright (c) Justin Bannister. All rights reserved.
// </copyright>

namespace PollySqlClient;

/// <summary>
/// Extension methods for wrapping a <see cref="SqlConnection"/> with a Polly v8 resilience pipeline.
/// </summary>
public static class PollySqlClientExtensions
{
    /// <summary>
    /// Wraps <paramref name="connection"/> in a <see cref="ResilientSqlConnection"/> that
    /// executes every query and command inside the supplied <paramref name="pipeline"/>.
    /// </summary>
    public static ResilientSqlConnection WithPolly(
        this SqlConnection connection,
        ResiliencePipeline pipeline)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(pipeline);

        return new ResilientSqlConnection(connection, pipeline);
    }
}
