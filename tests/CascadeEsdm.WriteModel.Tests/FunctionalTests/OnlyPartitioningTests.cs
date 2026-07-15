using CascadeEsdm.WriteModel.Tests.FunctionalTests.Commands;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Environment;
using FluentAssertions;
using Xunit.Abstractions;
using Tools = CascadeEsdm.WriteModel.Tests.FunctionalTests.PolicyPartitioningTestHelpers;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests;

public class OnlyPartitioningTests : IntegrationTestBase<OnlyPartitioningEnvironment>
{
    public OnlyPartitioningTests(ITestOutputHelper output, OnlyPartitioningEnvironment environment,
        SharedContainerFixture containers)
        : base(output, environment, containers) { }

    [Fact]
    public async Task Partitioned_Policies_Execute_On_Partitioned_Stream()
    {
        var sp = Environment.ServiceProvider;
        await ClearAllChannels(sp);

        await Tools.SendEventAsync(sp, "partitioned", "partitioned-stream");

        (await Tools.ReceivedAsync<PartitionedPolicyOneExecuted>(sp,
            Tools.ReceiveTimeout)).Should().BeTrue();
        (await Tools.ReceivedAsync<PartitionedPolicyTwoExecuted>(sp,
            Tools.ReceiveTimeout)).Should().BeTrue();
        (await Tools.ReceivedAsync<PartitionedPolicyThreeExecuted>(sp,
            Tools.ReceiveTimeout)).Should().BeTrue();
    }

    [Fact]
    public async Task No_Execution_On_Unkeyed_Stream()
    {
        var sp = Environment.ServiceProvider;
        await ClearAllChannels(sp);

        // No unkeyed listener is registered; reuse the partitioned client just to publish.
        await Tools.SendEventAsync(sp, "partitioned", "example-stream");

        (await Tools.ReceivedAsync<PartitionedPolicyOneExecuted>(sp,
            Tools.NoReceiveTimeout)).Should().BeFalse();
        (await Tools.ReceivedAsync<PartitionedPolicyTwoExecuted>(sp,
            Tools.NoReceiveTimeout)).Should().BeFalse();
        (await Tools.ReceivedAsync<PartitionedPolicyThreeExecuted>(sp,
            Tools.NoReceiveTimeout)).Should().BeFalse();
    }

    private static async Task ClearAllChannels(IServiceProvider sp)
    {
        await Tools.ClearAsync<PartitionedPolicyOneExecuted>(sp);
        await Tools.ClearAsync<PartitionedPolicyTwoExecuted>(sp);
        await Tools.ClearAsync<PartitionedPolicyThreeExecuted>(sp);
    }
}
