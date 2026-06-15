using Microsoft.Extensions.DependencyInjection;

namespace CascadeEsdm.SharedKernel.Composition;

public class WriteModelBuilder
{
    internal WriteModelBuilder(IServiceCollection services, Type eventStreamContainerType)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        EventStreamContainerType = eventStreamContainerType ??
                                   throw new ArgumentNullException(nameof(eventStreamContainerType));
    }

    public Type EventStreamContainerType { get; }

    public IServiceCollection Services { get; }
}