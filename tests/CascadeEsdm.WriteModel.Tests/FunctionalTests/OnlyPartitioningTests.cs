using CascadeEsdm.WriteModel.Tests.FunctionalTests.Commands;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Environment;
using FluentAssertions;
using Xunit.Abstractions;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests;

[Collection("OnlyPartitioning")]
public class OnlyPartitioningTests : IntegrationTestBase<OnlyPartitioningEnvironment>
{
    public OnlyPartitioningTests(ITestOutputHelper output, OnlyPartitioningEnvironment environment)
        : base(output, environment) { }

    [Fact]
    public async Task Partitioned_Policies_Execute_On_Partitioned_Stream()
    {
        var sp = Environment.ServiceProvider;
        await ClearAllChannels(sp);

        await PolicyPartitioningTestHelpers.SendEventAsync(sp, "partitioned", "partitioned-stream");

        (await PolicyPartitioningTestHelpers.ReceivedAsync<PartitionedPolicyOneExecuted>(sp,
            PolicyPartitioningTestHelpers.ReceiveTimeout)).Should().BeTrue();
        (await PolicyPartitioningTestHelpers.ReceivedAsync<PartitionedPolicyTwoExecuted>(sp,
            PolicyPartitioningTestHelpers.ReceiveTimeout)).Should().BeTrue();
        (await PolicyPartitioningTestHelpers.ReceivedAsync<PartitionedPolicyThreeExecuted>(sp,
            PolicyPartitioningTestHelpers.ReceiveTimeout)).Should().BeTrue();
    }

    [Fact]
    public async Task No_Execution_On_Unkeyed_Stream()
    {
        var sp = Environment.ServiceProvider;
        await ClearAllChannels(sp);

        // No unkeyed listener is registered; reuse the partitioned client just to publish.
        await PolicyPartitioningTestHelpers.SendEventAsync(sp, "partitioned", "example-stream");

        (await PolicyPartitioningTestHelpers.ReceivedAsync<PartitionedPolicyOneExecuted>(sp,
            PolicyPartitioningTestHelpers.NoReceiveTimeout)).Should().BeFalse();
        (await PolicyPartitioningTestHelpers.ReceivedAsync<PartitionedPolicyTwoExecuted>(sp,
            PolicyPartitioningTestHelpers.NoReceiveTimeout)).Should().BeFalse();
        (await PolicyPartitioningTestHelpers.ReceivedAsync<PartitionedPolicyThreeExecuted>(sp,
            PolicyPartitioningTestHelpers.NoReceiveTimeout)).Should().BeFalse();
    }

    private static async Task ClearAllChannels(IServiceProvider sp)
    {
        await PolicyPartitioningTestHelpers.ClearAsync<PartitionedPolicyOneExecuted>(sp);
        await PolicyPartitioningTestHelpers.ClearAsync<PartitionedPolicyTwoExecuted>(sp);
        await PolicyPartitioningTestHelpers.ClearAsync<PartitionedPolicyThreeExecuted>(sp);
    }
}
