using Microsoft.Extensions.DependencyInjection;

namespace CascadeEsdm.ReadModel.Composition;

public class ViewBuilder
{
    private IServiceCollection _services;

    public ViewBuilder(IServiceCollection services)
    {
        _services = services;
    }

    internal Type[] Views { get; private set; }

    public void AddView<TView, TContainer>()
    {
        
    }

    public void AddViewsFromAssembly<TExampleType>()
    {
        throw new NotImplementedException();
    }

    public void AddViewsFromNamspace<TExampleType>(bool includeSubspaces = true)
    {
        throw new NotImplementedException();
    }
}