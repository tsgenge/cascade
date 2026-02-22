using Cascade.SharedKernel.ValueObjects;

namespace Cascade.Commands.Hydration;

internal interface IAggregatePartitionLocator
{
    string GetPartition(Subject subject);
}

internal class AggregatePartitionLocator : IAggregatePartitionLocator
{
    public string GetPartition(Subject subject)
    {
        if (string.IsNullOrWhiteSpace(subject.Type))
            throw new ArgumentNullException(nameof(subject.Type));

        return subject.ForStorage();
    }
}