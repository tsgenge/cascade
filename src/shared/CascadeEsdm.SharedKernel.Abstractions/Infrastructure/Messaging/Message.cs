namespace CascadeEsdm.SharedKernel.Infrastructure.Messaging;

public record Message
{
    public string Body { get; }
    public IReadOnlyDictionary<string, object> ApplicationProperties { get; }

    public Message(string body, IReadOnlyDictionary<string, object> applicationProperties)
    {
        Body = body;
        ApplicationProperties = applicationProperties;
    }
}
