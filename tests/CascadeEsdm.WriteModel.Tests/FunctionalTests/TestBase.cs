using CascadeEsdm.WriteModel.Tests.FunctionalTests.Environment;
using Xunit.Abstractions;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests;

[Collection("Integration")]
public abstract class IntegrationTestBase<TEnvironment> : IClassFixture<TEnvironment>
    where TEnvironment : IntegrationEnvironment
{
    protected readonly TEnvironment Environment;
    protected readonly ITestOutputHelper Output;

    protected IntegrationTestBase(ITestOutputHelper output, TEnvironment environment,
        SharedContainerFixture containers)
    {
        Output = output;
        environment.Attach(containers);
        Environment = environment;
    }
}

public abstract class TestBase : IntegrationTestBase<WriteContext>
{
    protected TestBase(ITestOutputHelper output, WriteContext environment, SharedContainerFixture containers)
        : base(output, environment, containers) { }
}
