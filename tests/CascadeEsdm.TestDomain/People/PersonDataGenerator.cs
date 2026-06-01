using Bogus;
using CascadeEsdm.TestDomain.People.ValueObjects;

namespace CascadeEsdm.TestDomain.People;

public static class PersonDataGenerator
{
    private static readonly Faker Faker = new();

    public static FirstName FirstName() => new(Faker.Name.FirstName());

    public static LastName LastName() => new(Faker.Name.LastName());

    public static MobileNumber MobileNumber() => new(Faker.Phone.PhoneNumber("077########"));
}
