namespace CascadeEsdm.SharedKernel.Security;

public class RoleBasedSecurityDescriptor : ISecurityDescriptor
{
    public IReadOnlyDictionary<string, Permissions> SecurityDescriptors { get; init; } = new Dictionary<string, Permissions>(StringComparer.OrdinalIgnoreCase);
}

[Flags]
public enum Permissions
{
    None = 0,
    Read = 1,
    Add = 2,
    Change = 4,
    Remove = 8,
    Full = Read | Add | Change | Remove,
}