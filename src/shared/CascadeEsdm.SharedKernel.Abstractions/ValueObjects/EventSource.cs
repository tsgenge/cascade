using CascadeEsdm.SharedKernel.Aggregates;
using System.Text.RegularExpressions;

namespace CascadeEsdm.SharedKernel.ValueObjects;

public interface IEventSource : IValueObject<string>
{
    string Aggregate { get; }
    Guid CommandId { get; }
    string Command { get; }
}

public record EventSource : IValueObject<string>, IEventSource
{
    private const string Pattern = @"([\w\.^\/]+)\/([\w^\/]+)\/([\w^\/]+)\/((?im)[{(]?[0-9A-F]{8}[-]?(?:[0-9A-F]{4}[-]?){3}[0-9A-F]{12}[)}]?)";

    public string Aggregate { get; }
    public Guid CommandId { get; }
    public string Command { get; }
    public string Value => Format(Aggregate, Command, CommandId);

    public EventSource(string aggregate, Guid commandId, string command)
    {
        Aggregate = aggregate?.IndexOf("/") > -1 ? aggregate : aggregate + "/" + aggregate;
        CommandId = commandId;
        Command = command;
    }

    public EventSource(string value)
    {
        var match = Regex.Match(value, Pattern);
        if (!match.Success)
            throw new ArgumentException("The value was formatted correctly.");

        Aggregate = match.Groups[1].Value + "/" + match.Groups[2].Value;
        Command = match.Groups[3].Value;
        CommandId = Guid.Parse(match.Groups[4].Value);
    }

    public static EventSource ForAggregate(Type aggregateType, Guid commandId, string command)
    {
        return new(GetUri(aggregateType), commandId, command);
    }

    public static EventSource ForAggregate<TAggregate, TCommand>(Guid commandId)
        where TAggregate : IAggregateRoot
    {
        return new EventSource(GetUri(typeof(TAggregate)), commandId, typeof(TCommand).Name);
    }
    
    public static EventSource ForAggregate<TAggregate>(Guid commandId, string commandType)
        where TAggregate : IAggregateRoot
    {
        return new EventSource(GetUri(typeof(TAggregate)), commandId, commandType);
    }    

    private static string GetUri(Type aggregateType)
    {
        if (string.IsNullOrEmpty(aggregateType.FullName))
            return aggregateType.Name;

        return aggregateType.Assembly.GetName().Name + "/" + aggregateType.Name;
    }

    private static string Format(string aggregate, string commandType, Guid commandId)
    {
        return $"{aggregate}/{commandType}/{commandId}";
    }
}
