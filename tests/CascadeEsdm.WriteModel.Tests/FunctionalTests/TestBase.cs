using CascadeEsdm.WriteModel.Tests.FunctionalTests.Environment;
using Xunit.Abstractions;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests;

public abstract class IntegrationTestBase<TEnvironment>
    where TEnvironment : AsbIntegrationEnvironmentBase
{
    protected readonly TEnvironment Environment;
    protected readonly ITestOutputHelper Output;

    protected IntegrationTestBase(ITestOutputHelper output, TEnvironment environment)
    {
        Output = output;
        Environment = environment;
    }
}

[Collection("FunctionalTests")]
public abstract class TestBase : IntegrationTestBase<WriteContext>
{
    protected TestBase(ITestOutputHelper output, WriteContext environment) : base(output, environment) { }
}
