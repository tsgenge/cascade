using CascadeEsdm.SharedKernel.ValueObjects;
using CascadeEsdm.WriteModel;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Commands;

public record SharedPolicyOneExecuted : ICommand
{
    public string PolicyName { get; init; } = nameof(SharedPolicyOneExecuted);

    public Subject GetSubject(ICommandEnvelope envelope) => new(Guid.NewGuid(), "PolicyExecution");
}
