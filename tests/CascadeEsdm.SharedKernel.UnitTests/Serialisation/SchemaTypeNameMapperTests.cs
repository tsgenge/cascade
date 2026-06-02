using CascadeEsdm.SharedKernel.Infrastructure.Serialisation;
using FluentAssertions;

namespace CascadeEsdm.SharedKernel.UnitTests.Serialisation;

public class SchemaTypeNameMapperTests
{
    [Theory]
    [InlineData("Acme.Orders.WriteModel", "Acme.Orders.Schema")]
    [InlineData("Acme.Orders.Domain",     "Acme.Orders.Schema")]
    [InlineData("Acme.Orders.Write",      "Acme.Orders.Schema")]
    [InlineData("Acme.Orders.Application","Acme.Orders.Schema")]
    [InlineData("Acme.Orders",            "Acme.Orders.Schema")]
    public void ComputeSchemaAssemblyName_StripsSuffix_AndAppendsSchema(
        string assemblyName, string expected)
    {
        SchemaTypeNameMapper.ComputeSchemaAssemblyName(assemblyName)
            .Should().Be(expected);
    }

    [Theory]
    [InlineData(
        "Acme.Orders.WriteModel.Orders.Events.OrderPlaced, Acme.Orders.WriteModel",
        "Acme.Orders.Schema.Orders.Events.OrderPlaced, Acme.Orders.Schema")]
    [InlineData(
        "Acme.Orders.Domain.People.Events.PersonAdded, Acme.Orders.Domain",
        "Acme.Orders.Schema.People.Events.PersonAdded, Acme.Orders.Schema")]
    [InlineData(
        "Acme.Orders.Write.Basket.Events.ItemAdded, Acme.Orders.Write",
        "Acme.Orders.Schema.Basket.Events.ItemAdded, Acme.Orders.Schema")]
    [InlineData(
        "Acme.Orders.Application.Checkout.Events.OrderSubmitted, Acme.Orders.Application",
        "Acme.Orders.Schema.Checkout.Events.OrderSubmitted, Acme.Orders.Schema")]
    [InlineData(
        "Acme.Orders.Orders.Events.OrderPlaced, Acme.Orders",
        "Acme.Orders.Schema.Orders.Events.OrderPlaced, Acme.Orders.Schema")]
    public void RewriteToSchemaTypeName_RewritesNamespaceAndAssembly(
        string input, string expected)
    {
        SchemaTypeNameMapper.RewriteToSchemaTypeName(input)
            .Should().Be(expected);
    }

    [Fact]
    public void RewriteToSchemaTypeName_WhenNoAssemblyComponent_ReturnsOriginal()
    {
        const string input = "Acme.Orders.WriteModel.Orders.Events.OrderPlaced";

        SchemaTypeNameMapper.RewriteToSchemaTypeName(input)
            .Should().Be(input);
    }

    [Fact]
    public void RewriteToSchemaTypeName_WhenAssemblyAlreadySchema_ReturnsOriginal()
    {
        const string input = "Acme.Orders.Schema.Orders.Events.OrderPlaced, Acme.Orders.Schema";

        SchemaTypeNameMapper.RewriteToSchemaTypeName(input)
            .Should().Be(input);
    }

    [Fact]
    public void RewriteToSchemaTypeName_PreservesTypeNameWithoutNamespaceChange_WhenPrefixIsRootOnly()
    {
        const string input = "Acme.Orders.WriteModel, Acme.Orders.WriteModel";

        SchemaTypeNameMapper.RewriteToSchemaTypeName(input)
            .Should().Be("Acme.Orders.Schema, Acme.Orders.Schema");
    }
}
