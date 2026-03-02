namespace CascadeEsdm.SharedKernel.Aggregates;

public interface IAggregateRoot
{
    Guid Id { get; set; }
    int LastSequence { get; set; }
}