using Cascade.SharedKernel.Security;
using Cascade.SharedKernel.ValueObjects;
using System.Text.Json.Serialization;

namespace Cascade.Commands;

public record CommandEnvelope<TCommand> : CommandEnvelope
    where TCommand : ICommand
{
    public new TCommand Command { get; }

    [JsonConstructor]
    public CommandEnvelope(Guid id, string type, TCommand command, AuthenticatedContext securityContext, ClientChannel channel, DateTimeOffset time) : base(command, securityContext, channel, time)
    {
        Id = id;
        Type = type;
        Command = command;
        Type = typeof(TCommand).Name;
    }
    public CommandEnvelope(TCommand command, AuthenticatedContext securityContext, ClientChannel channel) : base(command, securityContext, channel)
    {
        Command = command;
        Type = typeof(TCommand).Name;
    }
}

public abstract record CommandEnvelope : ICommandEnvelope
{
    public Guid Id { get; protected set; }
    public IAuthenticatedContext SecurityContext { get; }
    public IClientChannel Channel { get; }
    public DateTimeOffset Time { get; }
    public string Type { get; protected set; } = "NotSet";
    public virtual ICommand Command { get; }
    protected CommandEnvelope(ICommand content, AuthenticatedContext context, ClientChannel channel, DateTimeOffset time)
    {
        Id = Guid.NewGuid();
        SecurityContext = context;
        Channel = channel;
        Time = time;
        Command = content;
    }
    protected CommandEnvelope(ICommand content, AuthenticatedContext context, ClientChannel channel)
    {
        Id = Guid.NewGuid();
        SecurityContext = context;
        Channel = channel;
        Time = DateTimeOffset.UtcNow;
        Command = content;
    }
}
