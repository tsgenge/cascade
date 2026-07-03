namespace CascadeEsdm.WriteModel.CommandHandling;

public interface ICommandHandler<in TCommand> : ICommandHandler<TCommand, ICommandResponse>
    where TCommand : ICommand { }

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand
    where TResponse : ICommandResponse
{
    Task<TResponse> HandleAsync(ICommandEnvelope<TCommand> envelope);
}