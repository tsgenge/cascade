# CascadeEsdm.ReadModel.Abstractions

Read-model (Query side / CQRS) abstractions for the Cascade Event Sourcing framework.

## Overview

This package defines the contracts a consumer needs to build the read side of an event-sourced system:
**view definitions**, declarative **configuration**, and the **projection** and **query** entry points. It contains
only interfaces, the models they reference, and small utilities. The concrete implementation (projectors, query
handlers, storage, mapping) lives in `CascadeEsdm.ReadModel`.

Its only dependency is `CascadeEsdm.SharedKernel.Abstractions`.

## Key types

### View definitions (`CascadeEsdm.ReadModel.Views`)

- **IView** — base contract for a materialised read-model row.
- **IAuthoredView** — a view that records the authoring `UserIdentity`.
- **IOrdered / IText / IEntitled** — optional view capability interfaces.
- **PartitionFormatAttribute** — declares the storage partition format for a view using the
  `{partitionId}`, `{tenantId}` and `{userId}` tokens.

### Value objects (`CascadeEsdm.ReadModel.ValueObjects`)

- **Partition** — a resolved storage partition key (`AsNotificationGroup()` for live updates).
- **NotificationGroup** — identifies the subscribers interested in a partition.
- **Projection&lt;TView&gt; / ProjectionEffect** — a row affected by an event and how it was affected.

### Projection entry points (`CascadeEsdm.ReadModel.Projecting`)

- **IViewProjector&lt;TView&gt;** — applies an `EventEnvelope` to a view, returning a `ProjectionResult<TView>`.
- **ProjectionResult&lt;TView&gt; / ProjectionOutcome** — the outcome of a projection attempt.
- **IProjectionPartitionLocator&lt;TView&gt;** — resolves the partition for an incoming event.

### Query entry points (`CascadeEsdm.ReadModel.Querying`)

- **IQueryHandler / IPageQueryHandler / ISingleQueryHandler** — page and single-row query entry points.
- **IQueryPartitionLocator&lt;TView&gt;** — resolves the partition to query.
- **ScopedPageFilter / ScopedSingleQuery** — base filter and single-row query types.
- **NotifyingPageResult&lt;TItem&gt; / NotifyingSingleResult&lt;TView&gt;** — results carrying a `NotificationGroup`.
- **QueryOperation / QueryActionAttribute** — declarative mapping of filter properties to view properties.

## Usage

### Defining a view

```csharp
using CascadeEsdm.ReadModel.Views;

[PartitionFormat("attendees-{tenantId}-{partitionId}")]
public record AttendeeView : IView
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public float Order { get; set; }
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset Modified { get; set; }
    public IList<string> ClientPermissions { get; set; } = new List<string>();
}
```

### Defining a filter and a single-row query

```csharp
using CascadeEsdm.ReadModel.Querying;
using CascadeEsdm.SharedKernel.Security;

public record AttendeeFilter : ScopedPageFilter
{
    [QueryAction(QueryOperation.Operation.StringContains, nameof(AttendeeView.Order))]
    public override string? Query => base.Query;

    public Guid MeetingId { get; }

    public AttendeeFilter(Guid meetingId, AuthenticatedContext securityContext, string? query, int size)
        : base(securityContext, query, size) => MeetingId = meetingId;

    public override Guid? GetParentId() => MeetingId;
}

public record AttendeeQuery : ScopedSingleQuery
{
    public Guid MeetingId { get; }

    public AttendeeQuery(Guid meetingId, Guid id, AuthenticatedContext securityContext)
        : base(id, securityContext) => MeetingId = meetingId;

    public override Guid? GetParentId() => MeetingId;
}
```

### Consuming the entry points

```csharp
public class AttendeeService
{
    private readonly IViewProjector<AttendeeView> _projector;
    private readonly IQueryHandler<AttendeeView, AttendeeFilter, AttendeeQuery> _queries;

    public AttendeeService(
        IViewProjector<AttendeeView> projector,
        IQueryHandler<AttendeeView, AttendeeFilter, AttendeeQuery> queries)
    {
        _projector = projector;
        _queries = queries;
    }
}
```

## Related packages

- **CascadeEsdm.ReadModel** — concrete query handling and projection implementation.
- **CascadeEsdm.SharedKernel.Abstractions** — core domain abstractions.

## License

BSD 3-Clause License - see LICENSE file for details
