using CascadeEsdm.SharedKernel.Composition;
using CascadeEsdm.ReadModel.Projecting;
using Microsoft.Extensions.DependencyInjection;

namespace CascadeEsdm.ReadModel.Composition;

public static class ReadModelBuilderExtensions
{
    public static ReadModelBuilder WithProjectors(this ReadModelBuilder builder)
    {
        builder.Services.AddGeneric(typeof(IViewSequenceStore<>), typeof(SequenceStore<>));
        builder.Services.AddGeneric(typeof(IViewProjector<>), typeof(ViewProjector<>));

        return builder;
    }
}
