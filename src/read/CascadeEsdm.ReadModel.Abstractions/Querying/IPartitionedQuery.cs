namespace CascadeEsdm.ReadModel.Querying;

public interface IPartitionedQuery<out TKey>
    where TKey : IEquatable<TKey>
{
    TKey? GetParentId();
}