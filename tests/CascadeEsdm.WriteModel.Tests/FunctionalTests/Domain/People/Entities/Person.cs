using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.ValueObjects;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.Entities;

public class Person
{
    public PersonId Id { get; set; }
    public FirstName FirstName { get; set; }
    public LastName LastName { get; set; }
    public MobileNumber MobileNumber { get; set; }

    public Person(PersonId id, FirstName firstName, LastName lastName, MobileNumber mobileNumber)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        MobileNumber = mobileNumber;
    }
}