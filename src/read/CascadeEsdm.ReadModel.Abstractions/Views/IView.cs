namespace CascadeEsdm.ReadModel.Views;

/// <summary>
///     The base contract for a read-model row (a "view"). A view is the materialised,
///     query-optimised projection of one or more domain events.
/// </summary>
public interface IView
{
    Guid Id { get; set; }
    DateTimeOffset Created { get; set; }
    DateTimeOffset Modified { get; set; }
    IList<string> ClientPermissions { get; set; }
    Guid? ParentId { get; set; }
}