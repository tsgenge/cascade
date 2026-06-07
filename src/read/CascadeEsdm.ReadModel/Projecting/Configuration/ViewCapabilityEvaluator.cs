using CascadeEsdm.ReadModel.Views;

namespace CascadeEsdm.ReadModel.Projecting.Configuration;

internal interface IViewCapabilityEvaluator<TView>
    where TView : IView
{
    bool IsMultiAggregateView();
}

internal class ViewCapabilityEvaluator<TView> : IViewCapabilityEvaluator<TView> where
    TView : IView
{
    private readonly IViewEventRegister<TView> _eventRegister;
    private bool? _isMultiAggregateView;

    public ViewCapabilityEvaluator(IViewEventRegister<TView> eventRegister)
    {
        _eventRegister = eventRegister ?? throw new ArgumentNullException(nameof(eventRegister));
    }

    public bool IsMultiAggregateView()
    {
        return _isMultiAggregateView ??= _eventRegister.GetEvents().Select(t => t.Namespace).Distinct().Count() > 1;
    }
}
