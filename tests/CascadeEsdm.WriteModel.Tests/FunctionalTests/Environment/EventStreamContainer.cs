using CascadeEsdm.SharedKernel.Infrastructure.Storage;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Environment;

public class EventStreamContainer : IDocumentContainerDefinition
{
    public string Name { get; } = "event-stream";
    public int TimeToLive { get; } = 0;
}