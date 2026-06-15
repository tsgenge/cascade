using CascadeEsdm.ReadModel.Views;
using CascadeEsdm.SharedKernel.Events;

namespace CascadeEsdm.ReadModel.UnitTests.UnitTests.Projecting.Configuration;

public class ItemAddedEvent : IDomainEvent
{
    public Guid ItemId { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
}

public class ItemRenamedEvent : IDomainEvent
{
    public string NewName { get; init; } = string.Empty;
}

public class ItemRemovedEvent : IDomainEvent;

public class ItemView : IView
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset Modified { get; set; }
    public IList<string> ClientPermissions { get; set; } = new List<string>();
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
