namespace Cascade.SharedKernel.Infrastructure.Storage;

public interface IDocumentContainerDefinition
{
    string Name { get; }
    int TimeToLive { get; }
}