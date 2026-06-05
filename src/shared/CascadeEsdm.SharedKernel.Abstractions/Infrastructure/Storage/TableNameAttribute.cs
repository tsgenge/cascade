namespace CascadeEsdm.SharedKernel.Infrastructure.Storage;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class TableNameAttribute : Attribute
{
    public string? Name { get; set; }
}
