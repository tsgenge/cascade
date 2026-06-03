namespace CascadeEsdm.ReadModel.Views.Interfaces;

/// <summary>
///     A view capability indicating the row carries an explicit sort order.
/// </summary>
public interface IOrdered
{
    float Order { get; set; }
}
