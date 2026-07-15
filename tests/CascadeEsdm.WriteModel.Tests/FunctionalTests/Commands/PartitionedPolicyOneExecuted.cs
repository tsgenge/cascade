using CascadeEsdm.SharedKernel.ValueObjects;
using CascadeEsdm.WriteModel;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Commands;

public record PartitionedPolicyOneExecuted : ICommand
{
    public string PolicyName { get; init; } = nameof(PartitionedPolicyOneExecuted);

    public Subject GetSubject(ICommandEnvelope envelope) => new(Guid.NewGuid(), "PolicyExecution");
}
