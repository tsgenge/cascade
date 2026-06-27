namespace CascadeEsdm.ReadModel.Querying;

public interface ISingleQuery<out TKey>
    where TKey : IEquatable<TKey>
{
    TKey Id { get; }
}