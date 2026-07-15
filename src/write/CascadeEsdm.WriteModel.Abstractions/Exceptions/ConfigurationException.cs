using CascadeEsdm.SharedKernel.Exceptions;

namespace CascadeEsdm.WriteModel.Exceptions;

public class ConfigurationException : ExceptionBase
{
    public ConfigurationException(string message) : base(message) { }

    public ConfigurationException(string message, Exception inner) : base(message, inner) { }
}
