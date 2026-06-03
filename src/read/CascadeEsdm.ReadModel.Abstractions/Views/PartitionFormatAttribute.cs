using System.Collections.Concurrent;
using System.Reflection;

namespace CascadeEsdm.ReadModel.Views;

/// <summary>
///     Declares how the storage partition key for a view is composed. Apply to a view type and use the
///     supported tokens, which are substituted at projection/query time from the event envelope and the
///     authenticated context:
///     <list type="bullet">
///         <item><description><c>{partitionId}</c> — an explicit identifier derived from the event/aggregate.</description></item>
///         <item><description><c>{tenantId}</c> — the tenant of the authenticated context.</description></item>
///         <item><description><c>{userId}</c> — the user of the authenticated context.</description></item>
///     </list>
///     e.g. <c>[PartitionFormat("attendees-{tenantId}-{partitionId}")]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class PartitionFormatAttribute : Attribute
{
    public const string PartitionIdPattern = @"\{partitionId\}";
    public const string TenantIdPattern = @"\{tenantId\}";
    public const string UserIdPattern = @"\{userId\}";

    public static readonly string[] SupportedTokens = { "userId", "tenantId", "partitionId" };

    private static readonly ConcurrentDictionary<Type, PartitionFormatAttribute> AttributeCache = new();

    public PartitionFormatAttribute(string format)
    {
        Format = format ?? throw new ArgumentNullException(nameof(format));
    }

    public string Format { get; set; } = string.Empty;

    /// <summary>
    ///     Resolves (and caches) the <see cref="PartitionFormatAttribute" /> applied to <typeparamref name="TView" />.
    /// </summary>
    /// <exception cref="InvalidOperationException">The view type has no <see cref="PartitionFormatAttribute" />.</exception>
    public static PartitionFormatAttribute GetFromView<TView>()
    {
        if (!AttributeCache.TryGetValue(typeof(TView), out var attribute))
        {
            attribute = typeof(TView).GetCustomAttribute<PartitionFormatAttribute>();

            if (attribute == null)
                throw new InvalidOperationException(
                    $"The view type {typeof(TView).Name} did not have a {nameof(PartitionFormatAttribute)} defined.");

            AttributeCache.TryAdd(typeof(TView), attribute);
        }

        return attribute;
    }
}
