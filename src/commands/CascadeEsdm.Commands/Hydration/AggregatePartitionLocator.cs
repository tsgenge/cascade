using CascadeEsdm.SharedKernel.ValueObjects;

namespace CascadeEsdm.Commands.Hydration;

internal interface IAggregatePartitionLocator
{
    string GetPartition(ISubject subject);
}

internal class AggregatePartitionLocator : IAggregatePartitionLocator
{
    public string GetPartition(ISubject subject)
    {
        if (string.IsNullOrWhiteSpace(subject.Type))
            throw new ArgumentNullException(nameof(subject.Type));

        return subject.ForStorage();
    }
}