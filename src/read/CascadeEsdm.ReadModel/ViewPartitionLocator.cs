using CascadeEsdm.ReadModel.ValueObjects;
using CascadeEsdm.ReadModel.Views;
using CascadeEsdm.SharedKernel.Security;
using System.Text.RegularExpressions;

namespace CascadeEsdm.ReadModel;

internal abstract class ViewPartitionLocator<TView>
{
    protected Partition GetPartition(AuthenticatedContext securityContext, Func<string> ExplicitKeyMethod)
    {
        var attribute = PartitionFormatAttribute.GetFromView<TView>();
        var partition = attribute.Format;

        if (Regex.IsMatch(attribute.Format, PartitionFormatAttribute.PartitionIdPattern)) {
            var partitionKey = ExplicitKeyMethod();

            partition = Regex.Replace(partition, PartitionFormatAttribute.PartitionIdPattern, partitionKey);
        }

        if (Regex.IsMatch(attribute.Format, PartitionFormatAttribute.TenantIdPattern))
            partition = Regex.Replace(partition, PartitionFormatAttribute.TenantIdPattern,
                securityContext.Tenant.ToString());

        if (Regex.IsMatch(attribute.Format, PartitionFormatAttribute.UserIdPattern))
            partition = Regex.Replace(partition, PartitionFormatAttribute.UserIdPattern,
                securityContext.User.ToString());

        return new Partition(partition);
    }
}