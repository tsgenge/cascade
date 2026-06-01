using CascadeEsdm.SharedKernel.Aggregates;
using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.TestDomain.People.Entities;

namespace CascadeEsdm.TestDomain.People;

public class PersonAggregate : IAggregateRoot
{
    public Person? Person { get; set; }
    public bool Exists => Id != Guid.Empty;
    public MySecurityDescriptor SecurityDescriptor { get; set; } = GetDefaultSecurityDescriptor();
    public Guid Id { get; set; } = Guid.Empty;
    public int LastSequence { get; set; }

    private static MySecurityDescriptor GetDefaultSecurityDescriptor()
    {
        return new MySecurityDescriptor();
    }
}

public class MySecurityDescriptor : ISecurityDescriptor { }