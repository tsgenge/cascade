using CascadeEsdm.WriteModel.Tests.FunctionalTests.Commands;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Environment;
using FluentAssertions;
using Xunit.Abstractions;
using Tools = CascadeEsdm.WriteModel.Tests.FunctionalTests.PolicyPartitioningTestHelpers;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests;

public class AllSharedPoliciesTests : IntegrationTestBase<AllSharedPoliciesEnvironment>
{
    public AllSharedPoliciesTests(ITestOutputHelper output, AllSharedPoliciesEnvironment environment,
        SharedContainerFixture containers)
        : base(output, environment, containers) { }

    [Fact]
    public async Task All_Policies_Execute_On_Example_Stream()
    {
        var sp = Environment.ServiceProvider;
        await ClearAllChannels(sp);

        await Tools.SendToUnkeyedStreamAsync(sp);

        (await Tools.ReceivedAsync<SharedPolicyOneExecuted>(sp,
            Tools.ReceiveTimeout)).Should().BeTrue();
        (await Tools.ReceivedAsync<SharedPolicyTwoExecuted>(sp,
            Tools.ReceiveTimeout)).Should().BeTrue();
        (await Tools.ReceivedAsync<SharedPolicyThreeExecuted>(sp,
            Tools.ReceiveTimeout)).Should().BeTrue();
    }

    [Fact]
    public async Task All_Policies_Execute_On_Second_Stream()
    {
        var sp = Environment.ServiceProvider;
        await ClearAllChannels(sp);

        await Tools.SendEventAsync(sp, "second-stream", "second-stream");

        (await Tools.ReceivedAsync<SharedPolicyOneExecuted>(sp,
            Tools.ReceiveTimeout)).Should().BeTrue();
        (await Tools.ReceivedAsync<SharedPolicyTwoExecuted>(sp,
            Tools.ReceiveTimeout)).Should().BeTrue();
        (await Tools.ReceivedAsync<SharedPolicyThreeExecuted>(sp,
            Tools.ReceiveTimeout)).Should().BeTrue();
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