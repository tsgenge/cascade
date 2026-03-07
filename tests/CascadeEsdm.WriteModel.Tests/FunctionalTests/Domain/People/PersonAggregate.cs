using CascadeEsdm.SharedKernel.Aggregates;
using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.Entities;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People;

public class PersonAggregate : IAggregateRoot
{
    public Guid Id { get; set; } = Guid.Empty;
    public Person? Person { get; set; }
    public int LastSequence { get; set; }
    public bool Exists => Id != Guid.Empty;
    public RoleBasedSecurityDescriptor SecurityDescriptor { get; set; } = GetDefaultSecurityDescriptor();

    private static RoleBasedSecurityDescriptor GetDefaultSecurityDescriptor()
    {
        return new RoleBasedSecurityDescriptor();
    }
}