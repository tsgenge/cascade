# Read Layer — Creating and Configuring Views

## Overview

The read layer materialises domain events into query-optimised **views**. Each view is a denormalised row that represents a projection of one or more events. When an event is received, the framework:

1. **Locates** the target row (or creates a new one)
2. **Determines** what structural change to apply (add, update, or remove)
3. **Maps** event properties onto the view using AutoMapper

All of this is expressed declaratively through a fluent configuration API — no manual mapping code, no switch statements over event types.

---

## Concepts

| Term | Description |
|---|---|
| **View** | A read-model row — the materialised, query-optimised projection of events. Implements `IView` |
| **Partition** | The storage partition key for a view. Declared via `[PartitionFormat]` attribute |
| **ViewProfileConfiguration** | The entry point for mapping events to a view. Package users inherit this and override `Configure` |
| **Row Locator** | How an event finds the target row — a key-value pair identifying which view property to match against |
| **Mutation Strategy** | What the event does to the row — `AddsNewRow`, `ChangesRows`, or `RemovesRows` |
| **Partition Strategy** | How the partition key is resolved — **static** (from the envelope) or **explicit** (from event properties) |

---

## Step 1 — Define the View

A view implements `IView` (or `IAuthoredView` if the row should track who created it):

```csharp
using CascadeEsdm.ReadModel.Views;

[PartitionFormat("orders")]
public class OrderView : IView
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset Modified { get; set; }
    public IList<string> ClientPermissions { get; set; } = new List<string>();

    // Domain-specific properties
    public string Reference { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public decimal Total { get; set; }
}
```

### IView members

| Property | Purpose |
|---|---|
| `Id` | Row identifier — set by the `AddsNewRow` locator |
| `ParentId` | Optional parent aggregate reference — set from the `Subject` when an event creates a row |
| `Created` | Timestamp set automatically when the row is first created |
| `Modified` | Timestamp updated automatically on every event projection |
| `ClientPermissions` | Permission strings for client-side authorisation |

### IAuthoredView

If the view should record the identity of the user who created the row, implement `IAuthoredView` instead:

```csharp
public class OrderView : IAuthoredView
{
    // ... all IView members plus:
    public UserIdentity Author { get; set; } = null!;
}
```

### PartitionFormat

The `[PartitionFormat]` attribute declares how the storage partition key is composed. Supported tokens:

| Token | Source |
|---|---|
| `{partitionId}` | An explicit identifier derived from the event or aggregate |
| `{tenantId}` | The tenant from the authenticated context |
| `{userId}` | The user from the authenticated context |

Examples:

```csharp
[PartitionFormat("orders")]                              // static — all orders in one partition
[PartitionFormat("workitems-{partitionId}")]              // explicit — partition per parent
[PartitionFormat("profiles-{tenantId}")]                  // tenant-scoped
[PartitionFormat("attendees-{tenantId}-{partitionId}")]   // tenant + explicit
```

---

## Step 2 — Create the Configuration

Inherit `ViewProfileConfiguration<TView>` and override `Configure`. This is the only method package users implement — the framework calls `Build` internally:

```csharp
using CascadeEsdm.ReadModel.Projecting.Configuration;

internal class OrderViewConfiguration : ViewProfileConfiguration<OrderView>
{
    protected override void Configure(ViewEventBuilder<OrderView> builder)
    {
        // Configuration goes here
    }
}
```

---

## Step 3 — Choose a Partition Strategy

The first call inside `Configure` selects how the partition key is resolved:

### Static partition

Use when all events for this view share the same partition, derived from the `EventEnvelope` (typically the tenant or a fixed string):

```csharp
var config = builder.UsesStaticPartitionKey();
```

### Explicit partition

Use when the partition key comes from the event itself (e.g. a parent aggregate ID):

```csharp
var config = builder.UsesExplicitPartitionKey();
```

---

## Step 4 — Register Events

Each event type is registered using `.For<TEvent>()`, which begins the fluent chain:

### Static partition flow

```
config.For<TEvent>()
    → .UsingRowLocator(...)       // how to find the row
    → .AddsNewRow(...)            // OR .ChangesRows() OR .RemovesRows()
    → [optional property mapping]
```

### Explicit partition flow

```
config.For<TEvent>()
    → .UsingPartitionIdentifier(...)   // where to find the partition key
    → .AndRowLocator(...)              // how to find the row within that partition
    → .AddsNewRow(...)                 // OR .ChangesRows() OR .RemovesRows()
    → [optional property mapping]
```

---

## Step 5 — Configure Row Location

The row locator tells the framework which view property to match against the event to find existing rows:

```csharp
.UsingRowLocator((evt, envelope) => new KeyValuePair<string, Guid>(
    nameof(OrderView.Id),    // the view property to search
    evt.OrderId))            // the value to match
```

For explicit partitions, the partition identifier comes first:

```csharp
.UsingPartitionIdentifier((evt, envelope) => envelope!.Subject.Id)
.AndRowLocator((evt, envelope) => new KeyValuePair<string, Guid>(
    nameof(OrderView.Id),
    evt.OrderId))
```

---

## Step 6 — Choose a Mutation Strategy

### AddsNewRow

The event creates a new view row. Provide a function that returns the new row's `Id`:

```csharp
.AddsNewRow((evt, envelope) => evt.OrderId)
```

`Created` and `Modified` are set automatically from the envelope timestamp.

### ChangesRows

The event updates an existing row:

```csharp
.ChangesRows()
```

`Modified` is updated automatically from the envelope timestamp.

### RemovesRows

The event deletes the matched row:

```csharp
.RemovesRows()
```

---

## Step 7 — Map Event Properties to View

After `AddsNewRow` or `ChangesRows`, chain AutoMapper member mappings to express how event properties translate to view properties.

### Direct property mapping

Use `.ForProperty` for type-safe, expression-based mapping between event and view properties of the same type:

```csharp
.AddsNewRow((e, o) => e.OrderId)
.ForProperty(v => v.Reference, e => e.Reference)
.ForProperty(v => v.Status, (e, envelope) => "Placed")
```

### AutoMapper ForMember

Use `.ForMember` for more complex mappings — computing values, accessing existing view state:

```csharp
.ChangesRows()
.ForMember(v => v.Total, x => x.MapFrom(e => e.Amount))
.ForMember(v => v.Status, x => x.MapFrom((e, existing) => existing.Status == "Draft" ? "Submitted" : existing.Status))
```

### ConvertUsing

For mutations that can't be expressed with member mappings (e.g. modifying collections, nested objects):

```csharp
.ChangesRows()
.ConvertUsing((evt, view) =>
{
    view.Items.Add(new LineItem(evt.ProductId, evt.Quantity));
    return view;
})
```

A three-argument overload provides access to the AutoMapper `ResolutionContext` (and thus the `EventEnvelope` via `context.State`):

```csharp
.ConvertUsing((evt, view, context) =>
{
    var envelope = context.State as EventEnvelope;
    view.LastModifiedBy = envelope?.Source.CommandName;
    return view;
})
```

---

## Complete Example — Static Partition

```csharp
[PartitionFormat("orders")]
public class OrderView : IView
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset Modified { get; set; }
    public IList<string> ClientPermissions { get; set; } = new List<string>();
    public string Reference { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public decimal Total { get; set; }
}

internal class OrderViewConfiguration : ViewProfileConfiguration<OrderView>
{
    protected override void Configure(ViewEventBuilder<OrderView> builder)
    {
        var config = builder.UsesStaticPartitionKey();

        config.For<OrderPlaced>()
            .UsingRowLocator((e, o) => new(nameof(OrderView.Id), e.OrderId))
            .AddsNewRow((e, o) => e.OrderId)
            .ForProperty(v => v.Reference, e => e.Reference);

        config.For<OrderTotalUpdated>()
            .UsingRowLocator((e, o) => new(nameof(OrderView.Id), e.OrderId))
            .ChangesRows()
            .ForMember(v => v.Total, x => x.MapFrom(e => e.NewTotal));

        config.For<OrderCancelled>()
            .UsingRowLocator((e, o) => new(nameof(OrderView.Id), e.OrderId))
            .RemovesRows();
    }
}
```

---

## Complete Example — Explicit Partition

Use explicit partitions when the view's storage partition depends on event properties rather than the envelope alone — typically for child entities scoped to a parent aggregate:

```csharp
[PartitionFormat("lineitems-{partitionId}")]
public class LineItemView : IView
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset Modified { get; set; }
    public IList<string> ClientPermissions { get; set; } = new List<string>();
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

internal class LineItemViewConfiguration : ViewProfileConfiguration<LineItemView>
{
    protected override void Configure(ViewEventBuilder<LineItemView> builder)
    {
        var config = builder.UsesExplicitPartitionKey();

        config.For<LineItemAdded>()
            .UsingPartitionIdentifier((e, o) => o!.Subject.Id)
            .AndRowLocator((e, o) => new(nameof(LineItemView.Id), e.LineItemId))
            .AddsNewRow((e, o) => e.LineItemId)
            .ForProperty(v => v.ParentId, (e, o) => o!.Subject.Id);

        config.For<LineItemQuantityChanged>()
            .UsingPartitionIdentifier((e, o) => o!.Subject.Id)
            .AndRowLocator((e, o) => new(nameof(LineItemView.Id), e.LineItemId))
            .ChangesRows();

        config.For<LineItemRemoved>()
            .UsingPartitionIdentifier((e, o) => o!.Subject.Id)
            .AndRowLocator((e, o) => new(nameof(LineItemView.Id), e.LineItemId))
            .RemovesRows();
    }
}
```

---

## Fluent API Reference

```
ViewProfileConfiguration<TView>
  └── Configure(ViewEventBuilder<TView>)
        ├── .UsesStaticPartitionKey()
        │     └── StaticPartitionEventBuilder<TView>
        │           └── .For<TEvent>()
        │                 └── RowLocatorStrategy<TView, TEvent>
        │                       └── .UsingRowLocator(locator)
        │                             └── MutationStrategy<TView, TEvent>
        │                                   ├── .AddsNewRow(idResolver) → IMappingExpression
        │                                   ├── .ChangesRows() → IMappingExpression
        │                                   └── .RemovesRows()
        │
        └── .UsesExplicitPartitionKey()
              └── ExplicitPartitionEventBuilder<TView>
                    └── .For<TEvent>()
                          └── ExplicitPartitionStrategy<TView, TEvent>
                                ├── .UsingPartitionIdentifier(expr)
                                └── .UsingPartitionLocator<TTypeConverter>()
                                      └── ExplicitPartitionRowLocatorStrategy<TView, TEvent>
                                            └── .AndRowLocator(locator)
                                                  └── MutationStrategy<TView, TEvent>
                                                        ├── .AddsNewRow(idResolver) → IMappingExpression
                                                        ├── .ChangesRows() → IMappingExpression
                                                        └── .RemovesRows()
```

---

## Related Conventions

- [Events](Events.md) — event records that views project
- [Aggregates](Aggregates.md) — the write-side aggregates whose events feed projections
- [Exceptions](Exceptions.md) — exception handling conventions
