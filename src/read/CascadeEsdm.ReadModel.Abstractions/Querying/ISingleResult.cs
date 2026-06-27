namespace CascadeEsdm.ReadModel.Querying;

public interface ISingleResult<out TResult>
{
    TResult Result { get; }
}