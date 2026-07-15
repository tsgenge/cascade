using CascadeEsdm.WriteModel.Tests.FunctionalTests.Commands;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Environment;
using FluentAssertions;
using Xunit.Abstractions;
using Tools = CascadeEsdm.WriteModel.Tests.FunctionalTests.PolicyPartitioningTestHelpers;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests;

public class MixedPartitioningTests : IntegrationTestBase<MixedPartitioningEnvironment>
{
    public MixedPartitioningTests(ITestOutputHelper output, MixedPartitioningEnvironment environment,
        SharedContainerFixture containers)
        : base(output, environment, containers) { }

    [Fact]
    public async Task Shared_Policies_Execute_Only_On_Unkeyed_Stream()
    {
        var sp = Environment.ServiceProvider;
        await ClearAllChannels(sp);

        await Tools.SendToUnkeyedStreamAsync(sp, "example-stream");

        (await Tools.ReceivedAsync<SharedPolicyOneExecuted>(sp,
            Tools.ReceiveTimeout)).Should().BeTrue();
        (await Tools.ReceivedAsync<SharedPolicyTwoExecuted>(sp,
            Tools.ReceiveTimeout)).Should().BeTrue();
        (await Tools.ReceivedAsync<SharedPolicyThreeExecuted>(sp,
            Tools.ReceiveTimeout)).Should().BeTrue();

        (await Tools.ReceivedAsync<PartitionedPolicyOneExecuted>(sp,
            Tools.NoReceiveTimeout)).Should().BeFalse();
        (await Tools.ReceivedAsync<PartitionedPolicyTwoExecuted>(sp,
            Tools.NoReceiveTimeout)).Should().BeFalse();
        (await Tools.ReceivedAsync<PartitionedPolicyThreeExecuted>(sp,
            Tools.NoReceiveTimeout)).Should().BeFalse();
    }

    [Fact]
    public async Task Partitioned_Policies_Execute_Only_On_Partitioned_Stream()
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

        (await Tools.ReceivedAsync<SharedPolicyOneExecuted>(sp,
            Tools.NoReceiveTimeout)).Should().BeFalse();
        (await Tools.ReceivedAsync<SharedPolicyTwoExecuted>(sp,
            Tools.NoReceiveTimeout)).Should().BeFalse();
        (await Tools.ReceivedAsync<SharedPolicyThreeExecuted>(sp,
            Tools.NoReceiveTimeout)).Should().BeFalse();
    }

    private static async Task ClearAllChannels(IServiceProvider sp)
    {
        await Tools.ClearAsync<SharedPolicyOneExecuted>(sp);
        await Tools.ClearAsync<SharedPolicyTwoExecuted>(sp);
        await Tools.ClearAsync<SharedPolicyThreeExecuted>(sp);
        await Tools.ClearAsync<PartitionedPolicyOneExecuted>(sp);
        await Tools.ClearAsync<PartitionedPolicyTwoExecuted>(sp);
        await Tools.ClearAsync<PartitionedPolicyThreeExecuted>(sp);
    }
}
