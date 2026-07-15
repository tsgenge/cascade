using CascadeEsdm.SharedKernel.ValueObjects;
using CascadeEsdm.WriteModel;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Commands;

public record PartitionedPolicyTwoExecuted : ICommand
{
    public string PolicyName { get; init; } = nameof(PartitionedPolicyTwoExecuted);

    public Subject GetSubject(ICommandEnvelope envelope) => new(Guid.NewGuid(), "PolicyExecution");
}
