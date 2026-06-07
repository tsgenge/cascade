namespace CascadeEsdm.ReadModel.Projecting;

internal class ProjectionConfigurationException<TView, TEvent> : Exception
{
    public ProjectionConfigurationException(string reason) : base($"View projection for {typeof(TView).Name} using {typeof(TEvent).Name} was not configured correctly ({reason}).")
    {
    }
}

internal class ProjectionConfigurationException<TView> : Exception
{
    public ProjectionConfigurationException(string reason) : base($"View projection for {typeof(TView).Name} was not configured correctly ({reason}).")
    {
    }
}
