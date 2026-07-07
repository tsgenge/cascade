namespace CascadeEsdm.SharedKernel.Infrastructure.Logging;

public sealed class NoopDisposable : IDisposable
{
    public static readonly NoopDisposable Instance = new();
    public void Dispose() { }
}