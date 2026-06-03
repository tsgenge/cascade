namespace CascadeEsdm.ReadModel.Querying;

/// <summary>
///     Declares, on a filter property, how that property maps onto one or more view properties when a page query
///     is built. Multiple attributes may be applied to express OR-ed predicates across several view properties.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class QueryActionAttribute : Attribute
{
    public QueryActionAttribute(QueryOperation.Operation operation, params string[] viewProperties)
    {
        Operation = operation;
        ViewProperties = viewProperties;
    }

    public string[] ViewProperties { get; }
    public QueryOperation.Operation Operation { get; }
}
