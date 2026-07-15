using CascadeEsdm.SharedKernel.ValueObjects;
using CascadeEsdm.WriteModel;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Commands;

public record SharedPolicyThreeExecuted : ICommand
{
    public string PolicyName { get; init; } = nameof(SharedPolicyThreeExecuted);

    public Subject GetSubject(ICommandEnvelope envelope) => new(Guid.NewGuid(), "PolicyExecution");
}
