using CascadeEsdm.ReadModel.Projecting;
using CascadeEsdm.ReadModel.Views;
using CascadeEsdm.SharedKernel.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CascadeEsdm.ReadModel.Composition;

public class ViewBuilder
{
    private readonly IServiceCollection _services;

    public ViewBuilder(IServiceCollection services)
    {
        _services = services;
    }

    internal List<Type> Views { get; private set; }

    public void AddView<TView, TContainer>()
        where TView : IView
        where TContainer : IDocumentContainerDefinition
    {
        AddView(typeof(TView), typeof(TContainer));
    }

    public void AddView(Type viewType, Type containerType)
    {
        if (!typeof(IView).IsAssignableFrom(viewType)) {
            throw new InvalidOperationException($"The provided view does not implement IView ({viewType.FullName}).");
        }

        if (!typeof(IDocumentContainerDefinition).IsAssignableFrom(containerType)) {
            throw new InvalidOperationException(
                $"The provided container does not implement IDocumentContainerDefinition ({containerType.FullName}).");
        }

        if (Views.All(v => v.FullName != viewType.FullName)) {
            var storeInterface = typeof(IViewProjectionStore<>).MakeGenericType(viewType);
            var storeType = typeof(ViewProjectionStore<,>).MakeGenericType(viewType, containerType);
            _services.AddScoped(storeInterface, storeType);
            Views.Add(viewType);
        }
    }

    public void AddViewsFromAssembly<TExampleType>(Func<Type, Type?> getContainerDefinitionForView)
    {
        var assembly = typeof(TExampleType).Assembly;
        var viewTypes = GetViewTypes(assembly);
        var containerLessTypes = new List<Type>();
        foreach (var viewType in viewTypes) {
            var containerType = getContainerDefinitionForView(viewType);
            if (containerType != null) {
                AddView(viewType, containerType);
            }
            else {
                containerLessTypes.Add(viewType);
            }
        }

        if (containerLessTypes.Any()) {
            throw new InvalidOperationException(
                string.Join("\n",
                    containerLessTypes.Select(t =>
                        $"Could not determine Document Container Type (IDocumentContainerDefinition) for view {t.Name}")));
        }
    }

    private static Type[] GetViewTypes(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(IView).IsAssignableFrom(t))
            .ToArray();
    }
}