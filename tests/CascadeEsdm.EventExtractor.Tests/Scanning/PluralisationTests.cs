using CascadeEsdm.EventExtractor.Scanning;
using FluentAssertions;

namespace CascadeEsdm.EventExtractor.Tests.Scanning;

public class PluralisationTests
{
    [Theory]
    [InlineData("Door", "Doors")]
    [InlineData("Order", "Orders")]
    [InlineData("Person", "People")]
    [InlineData("Child", "Children")]
    [InlineData("Company", "Companies")]
    [InlineData("Bus", "Buses")]
    [InlineData("Box", "Boxes")]
    [InlineData("Status", "Statuses")]
    [InlineData("Match", "Matches")]
    [InlineData("Wish", "Wishes")]
    [InlineData("Quiz", "Quizzes")]
    [InlineData("Baby", "Babies")]
    [InlineData("City", "Cities")]
    [InlineData("Day", "Days")]
    [InlineData("Key", "Keys")]
    [InlineData("Boy", "Boys")]
    [InlineData("Man", "Men")]
    [InlineData("Woman", "Women")]
    [InlineData("Knife", "Knives")]
    [InlineData("Account", "Accounts")]
    [InlineData("Payment", "Payments")]
    [InlineData("Shipment", "Shipments")]
    [InlineData("Sale", "Sales")]
    [InlineData("Invoice", "Invoices")]
    public void Pluralise_HandlesCommonEnglishWords(string singular, string expected)
    {
        Pluraliser.Pluralise(singular).Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Pluralise_ReturnsInput_WhenNullOrEmpty(string? input)
    {
        Pluraliser.Pluralise(input!).Should().Be(input);
    }
}
