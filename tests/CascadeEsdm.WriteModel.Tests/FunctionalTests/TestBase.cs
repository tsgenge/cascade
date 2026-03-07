using CascadeEsdm.WriteModel.Tests.FunctionalTests.Environment;
using Xunit.Abstractions;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests;

[Collection("FunctionalTests")]
public abstract class TestBase
{
    protected readonly WriteContext Environment;
    protected readonly ITestOutputHelper Output;

    protected TestBase(ITestOutputHelper output, WriteContext environment)
    {
        Output = output;
        Environment = environment;
    }
}