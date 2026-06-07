using AutoMapper;
using CascadeEsdm.ReadModel.Views;

namespace CascadeEsdm.ReadModel.Projecting.Configuration;

internal interface IViewProfileConfiguration<TView>
    where TView : IView
{
    void Build(Profile profile, IViewEventRegister<TView> eventRegisterInstance);
}

internal abstract class ViewProfileConfiguration<TView> : IViewProfileConfiguration<TView>
    where TView : IView
{
    private ViewEventBuilder<TView>? _builder;

    public void Build(Profile profile, IViewEventRegister<TView> eventRegisterInstance)
    {
        _builder = new ViewEventBuilder<TView>(profile, eventRegisterInstance);
        Configure(_builder);
    }

    protected abstract void Configure(ViewEventBuilder<TView> builder);

    public void Validate()
    {
        if (_builder == null)
            throw new InvalidOperationException("Build must be called before Validate");

        _builder.Validate();
    }
}
