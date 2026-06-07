namespace CascadeEsdm.ReadModel.Projecting.Configuration;

public record ExplicitPartitionKey<TView>(Guid Key)
{
    public override string ToString()
    {
        return Key.ToString("n");
    }
}
