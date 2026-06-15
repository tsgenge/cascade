using Microsoft.Extensions.DependencyInjection;

namespace CascadeEsdm.SharedKernel.Composition;

public class ModelBuilder
{
    private readonly InfrastructureBuilder _infraBuilder;
    private readonly IServiceCollection _services;

    internal ModelBuilder(IServiceCollection services, InfrastructureBuilder infraBuilder)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _infraBuilder = infraBuilder ?? throw new ArgumentNullException(nameof(infraBuilder));
    }

    public IServiceCollection WithWriteModel(Action<WriteModelBuilder> configure)
    {
        var builder = new WriteModelBuilder(_services, _infraBuilder.EventStreamContainerType!);
        configure(builder);

        builder.Validate();

        return _services;
    }

    public IServiceCollection WithReadModel(Action<ReadModelBuilder> configure)
    {
        var builder = new ReadModelBuilder(_services);
        configure(builder);

        builder.Validate();

        return _services;
    }
}