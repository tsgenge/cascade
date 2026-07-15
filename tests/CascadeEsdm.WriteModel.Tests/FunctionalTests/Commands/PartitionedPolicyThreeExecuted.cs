using CascadeEsdm.SharedKernel.ValueObjects;
using CascadeEsdm.WriteModel;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Commands;

public record PartitionedPolicyThreeExecuted : ICommand
{
    public string PolicyName { get; init; } = nameof(PartitionedPolicyThreeExecuted);

    public Subject GetSubject(ICommandEnvelope envelope) => new(Guid.NewGuid(), "PolicyExecution");
}
