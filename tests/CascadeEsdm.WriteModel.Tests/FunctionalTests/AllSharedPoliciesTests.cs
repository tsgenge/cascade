using CascadeEsdm.WriteModel.Tests.FunctionalTests.Commands;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Environment;
using FluentAssertions;
using Xunit.Abstractions;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests;

[Collection("AllSharedPolicies")]
public class AllSharedPoliciesTests : IntegrationTestBase<AllSharedPoliciesEnvironment>
{
    public AllSharedPoliciesTests(ITestOutputHelper output, AllSharedPoliciesEnvironment environment)
        : base(output, environment) { }

    [Fact]
    public async Task All_Policies_Execute_On_Example_Stream()
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
    }

    [Fact]
    public async Task All_Policies_Execute_On_Second_Stream()
    {
        var sp = Environment.ServiceProvider;
        await ClearAllChannels(sp);

        await PolicyPartitioningTestHelpers.SendEventAsync(sp, "second-stream", "second-stream");

        (await PolicyPartitioningTestHelpers.ReceivedAsync<SharedPolicyOneExecuted>(sp,
            PolicyPartitioningTestHelpers.ReceiveTimeout)).Should().BeTrue();
        (await PolicyPartitioningTestHelpers.ReceivedAsync<SharedPolicyTwoExecuted>(sp,
            PolicyPartitioningTestHelpers.ReceiveTimeout)).Should().BeTrue();
        (await PolicyPartitioningTestHelpers.ReceivedAsync<SharedPolicyThreeExecuted>(sp,
            PolicyPartitioningTestHelpers.ReceiveTimeout)).Should().BeTrue();
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
