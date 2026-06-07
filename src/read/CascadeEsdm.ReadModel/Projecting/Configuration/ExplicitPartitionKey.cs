namespace CascadeEsdm.ReadModel.Projecting.Configuration;

internal record ExplicitPartitionKey<TView>(Guid Key)
{
    public override string ToString()
    {
        return Key.ToString("n");
    }
}
