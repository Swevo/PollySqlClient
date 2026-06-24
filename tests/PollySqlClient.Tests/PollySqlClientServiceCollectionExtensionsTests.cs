// <copyright file="PollySqlClientServiceCollectionExtensionsTests.cs" company="Justin Bannister">
// Copyright (c) Justin Bannister. All rights reserved.
// </copyright>

namespace PollySqlClient.Tests;

public class PollySqlClientServiceCollectionExtensionsTests
{
    [Fact]
    public void AddPollySqlClient_WithBuilder_RegistersResiliencePipelineSingleton()
    {
        var services = new ServiceCollection();
        services.AddPollySqlClient(pipeline => pipeline.AddTimeout(TimeSpan.FromSeconds(5)));

        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<ResiliencePipeline>());
    }

    [Fact]
    public void AddPollySqlClient_WithPrebuiltPipeline_RegistersSameInstance()
    {
        var services = new ServiceCollection();
        var prebuilt = new ResiliencePipelineBuilder().AddTimeout(TimeSpan.FromSeconds(5)).Build();
        services.AddPollySqlClient(prebuilt);

        var provider = services.BuildServiceProvider();
        Assert.Same(prebuilt, provider.GetService<ResiliencePipeline>());
    }

    [Fact]
    public void AddPollySqlClient_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection? services = null;
        Assert.Throws<ArgumentNullException>(() => services!.AddPollySqlClient(_ => { }));
    }

    [Fact]
    public void AddPollySqlClient_NullConfigure_ThrowsArgumentNullException()
    {
        Action<ResiliencePipelineBuilder>? configure = null;
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddPollySqlClient(configure!));
    }
}
