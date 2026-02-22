namespace Cascade.Commands.Abstractions.Domain.ValueObjects;

public record AvailableAction(string Name, string Uri, HttpMethod Method);