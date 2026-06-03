using CascadeEsdm.SharedKernel.ValueObjects;

namespace CascadeEsdm.ReadModel.Querying;

/// <summary>
///     Describes how a filter value should be matched against a view property when a query is built.
/// </summary>
public record QueryOperation : IValueObject<QueryOperation.Operation>
{
    public QueryOperation(Operation value)
    {
        Value = value;
    }

    public Operation Value { get; }

    public string ToQueryString(string fieldAddress, string propName)
    {
        return Value switch
        {
            Operation.StringContains => $"contains({fieldAddress}, {propName}, true)",
            Operation.ArrayContains => $"array_contains({fieldAddress}, {propName})",
            _ => $"{fieldAddress} = {propName}"
        };
    }

    public enum Operation
    {
        StringContains,
        ArrayContains,
        Equals
    }

    public static QueryOperation StringContains => new(Operation.StringContains);
    public static QueryOperation ArrayContains => new(Operation.ArrayContains);
    public static QueryOperation EqualsValue => new(Operation.Equals);
}

public static class OperationExtensions
{
    public static QueryOperation ToOperation(this QueryOperation.Operation operation)
    {
        return new QueryOperation(operation);
    }

    public static string ToQueryString(this QueryOperation.Operation operation, string fieldAddress, string propName)
    {
        return new QueryOperation(operation).ToQueryString(fieldAddress, propName);
    }
}
