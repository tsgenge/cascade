using CascadeEsdm.ReadModel.Infrastructure;
using CascadeEsdm.ReadModel.ValueObjects;
using CascadeEsdm.ReadModel.Views;
using CascadeEsdm.SharedKernel.Events;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;

namespace CascadeEsdm.SignalR;

public class SignalRViewNotifier : IViewNotificationService
{
    private readonly ServiceHubContext _context;

    public SignalRViewNotifier(ServiceHubContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task ViewChangedAsync<TView>(IEnumerable<Projection<TView>> projections, EventEnvelope @event)
        where TView : IView
    {
        var viewType = typeof(TView);
        foreach (var effectGroup in projections.GroupBy(p => p.Effect)) {
            var groups = GetGroupNames(effectGroup);

            await _context.Clients.Groups(groups)
                .SendAsync($"view{effectGroup.Key}",
                    new
                    {
                        ids = effectGroup.Select(e => e.View.Id),
                        parents = effectGroup.Where(e => e.View.ParentId.HasValue).Select(e => e.View.ParentId)
                            .Distinct(),
                        view = GetExternalViewName(viewType),
                        eventType = @event.Type,
                        groups
                    });
        }
    }

    private string GetExternalViewName(Type viewType)
    {
        return viewType.Name.Replace("View", "").ToLower();
    }

    private IList<string> GetGroupNames<TView>(IEnumerable<Projection<TView>> projections)
        where TView : IView
    {
        var retVal = new List<string>();
        foreach (var projection in projections) {
            retVal.Add(projection.View.Id.ToString("n"));

            retVal.Add(projection.Partition.AsNotificationGroup().Value);
        }

        return retVal.Distinct().ToList();
    }
}