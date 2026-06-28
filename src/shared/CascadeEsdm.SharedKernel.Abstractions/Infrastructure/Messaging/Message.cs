namespace CascadeEsdm.SharedKernel.Infrastructure.Messaging;

public record Message
{
    public string Body { get; }
    public IReadOnlyDictionary<string, string> ApplicationProperties { get; }

    public Message(string body, IReadOnlyDictionary<string, string> applicationProperties)
    {
        Body = body;
        ApplicationProperties = applicationProperties;
    }
}
