using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace CascadeEsdm.SharedKernel.Composition;

public class WriteModelBuilder
{
    private readonly IServiceCollection _services;
    private readonly Type _eventStreamContainerType;
    
    internal WriteModelBuilder(IServiceCollection services, Type eventStreamContainerType)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _eventStreamContainerType = eventStreamContainerType ?? throw new ArgumentNullException(nameof(eventStreamContainerType));
    }
    
    public Type EventStreamContainerType => _eventStreamContainerType;
    public IServiceCollection Services => _services;

    internal void Validate()
    {
        // Check ICommandExecutors are registered.
        // Check IEventAppliers are registered.
    }
}
