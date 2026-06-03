namespace CascadeEsdm.WriteModel.Abstractions.Domain.ValueObjects;

public record AvailableAction
{
    public string Name { get; }
    public string Uri { get; }
    public HttpMethod Method { get; }

    public AvailableAction(string name, string uri, HttpMethod method)
    {
        Name = name;
        Uri = uri;
        Method = method;
    }
}