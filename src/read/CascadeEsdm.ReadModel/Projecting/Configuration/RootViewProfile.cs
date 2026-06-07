using AutoMapper;
using CascadeEsdm.ReadModel.Views;

namespace CascadeEsdm.ReadModel.Projecting.Configuration;

internal class RootViewProfile<TView> : Profile
    where TView : IView
{
    public RootViewProfile(IViewEventRegister<TView> eventRegister, IViewProfileConfiguration<TView>[] builders)
    {
        foreach (var builder in builders) {
            builder.Build(this, eventRegister);
        }
    }
}
