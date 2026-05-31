using Bogus;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.ValueObjects;

namespace CascadeEsdm.WriteModel.Tests.Helpers;

public static class PersonDataGenerator
{
    private static readonly Faker Faker = new();

    public static FirstName FirstName() => new(Faker.Name.FirstName());

    public static LastName LastName() => new(Faker.Name.LastName());

    public static MobileNumber MobileNumber() => new(Faker.Phone.PhoneNumber("077########"));
}
