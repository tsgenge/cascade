using CascadeEsdm.WriteModel.Tests.FunctionalTests.Commands;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Environment;
using FluentAssertions;
using Xunit.Abstractions;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests;

[Collection("MixedPartitioning")]
public class MixedPartitioningTests : IntegrationTestBase<MixedPartitioningEnvironment>
{
    public MixedPartitioningTests(ITestOutputHelper output, MixedPartitioningEnvironment environment)
        : base(output, environment) { }

    [Fact]
    public async Task Shared_Policies_Execute_Only_On_Unkeyed_Stream()
    {
        var sp = Environment.ServiceProvider;
        await ClearAllChannels(sp);

        await PolicyPartitioningTestHelpers.SendToUnkeyedStreamAsync(sp, "example-stream");

        (await PolicyPartitioningTestHelpers.ReceivedAsync<SharedPolicyOneExecuted>(sp,
            PolicyPartitioningTestHelpers.ReceiveTimeout)).Should().BeTrue();
        (await PolicyPartitioningTestHelpers.ReceivedAsync<SharedPolicyTwoExecuted>(sp,
            PolicyPartitioningTestHelpers.ReceiveTimeout)).Should().BeTrue();
        (await PolicyPartitioningTestHelpers.ReceivedAsync<SharedPolicyThreeExecuted>(sp,
            PolicyPartitioningTestHelpers.ReceiveTimeout)).Should().BeTrue();

        (await PolicyPartitioningTestHelpers.ReceivedAsync<PartitionedPolicyOneExecuted>(sp,
            PolicyPartitioningTestHelpers.NoReceiveTimeout)).Should().BeFalse();
        (await PolicyPartitioningTestHelpers.ReceivedAsync<PartitionedPolicyTwoExecuted>(sp,
            PolicyPartitioningTestHelpers.NoReceiveTimeout)).Should().BeFalse();
        (await PolicyPartitioningTestHelpers.ReceivedAsync<PartitionedPolicyThreeExecuted>(sp,
            PolicyPartitioningTestHelpers.NoReceiveTimeout)).Should().BeFalse();
    }

    [Fact]
    public async Task Partitioned_Policies_Execute_Only_On_Partitioned_Stream()
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

        (await PolicyPartitioningTestHelpers.ReceivedAsync<SharedPolicyOneExecuted>(sp,
            PolicyPartitioningTestHelpers.NoReceiveTimeout)).Should().BeFalse();
        (await PolicyPartitioningTestHelpers.ReceivedAsync<SharedPolicyTwoExecuted>(sp,
            PolicyPartitioningTestHelpers.NoReceiveTimeout)).Should().BeFalse();
        (await PolicyPartitioningTestHelpers.ReceivedAsync<SharedPolicyThreeExecuted>(sp,
            PolicyPartitioningTestHelpers.NoReceiveTimeout)).Should().BeFalse();
    }

    private static async Task ClearAllChannels(IServiceProvider sp)
    {
        await PolicyPartitioningTestHelpers.ClearAsync<SharedPolicyOneExecuted>(sp);
        await PolicyPartitioningTestHelpers.ClearAsync<SharedPolicyTwoExecuted>(sp);
        await PolicyPartitioningTestHelpers.ClearAsync<SharedPolicyThreeExecuted>(sp);
        await PolicyPartitioningTestHelpers.ClearAsync<PartitionedPolicyOneExecuted>(sp);
        await PolicyPartitioningTestHelpers.ClearAsync<PartitionedPolicyTwoExecuted>(sp);
        await PolicyPartitioningTestHelpers.ClearAsync<PartitionedPolicyThreeExecuted>(sp);
    }
}
