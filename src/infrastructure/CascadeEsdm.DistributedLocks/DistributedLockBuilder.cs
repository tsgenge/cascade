public class DistributedLockBuilder
{
    internal string ConnectionString { get; private set; } = string.Empty;
    
    public DistributedLockBuilder WithConnectionString(string connectionString)
    {
        ConnectionString = connectionString;
        return this;
    }

    internal void Validate()
    {
        if (string.IsNullOrEmpty(ConnectionString))
        {
            throw new InvalidOperationException("Connection string is required for distributed locks.");
        }
    }
}
