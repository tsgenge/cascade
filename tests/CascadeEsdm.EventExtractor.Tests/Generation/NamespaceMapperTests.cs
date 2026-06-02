using CascadeEsdm.EventExtractor.Generation;
using FluentAssertions;

namespace CascadeEsdm.EventExtractor.Tests.Generation;

public class NamespaceMapperTests
{
    [Fact]
    public void MapNamespace_ReplacesSourceRootWithTargetRoot()
    {
        var mapper = new NamespaceMapper("Acme.Orders.WriteModel", "Acme.Orders.Events");

        mapper.MapNamespace("Acme.Orders.WriteModel.Orders.Events")
              .Should().Be("Acme.Orders.Events.Orders.Events");
    }

    [Fact]
    public void MapNamespace_WhenNamespaceEqualsSourceRoot_ReturnsTargetRoot()
    {
        var mapper = new NamespaceMapper("Acme.Orders.WriteModel", "Acme.Orders.Events");

        mapper.MapNamespace("Acme.Orders.WriteModel")
              .Should().Be("Acme.Orders.Events");
    }

    [Fact]
    public void MapNamespace_WhenNoMatch_PrefixesWithTargetRoot()
    {
        var mapper = new NamespaceMapper("Acme.Orders.WriteModel", "Acme.Orders.Events");

        mapper.MapNamespace("SomeOther.Namespace")
              .Should().Be("Acme.Orders.Events.SomeOther.Namespace");
    }

    [Fact]
    public void GetRelativeOutputFolder_ReturnsSubfolderBelowSourceRoot()
    {
        var mapper = new NamespaceMapper("Acme.Orders.WriteModel", "Acme.Orders.Events");

        var folder = mapper.GetRelativeOutputFolder("Acme.Orders.WriteModel.Orders.Events");

        folder.Should().Be($"Orders{Path.DirectorySeparatorChar}Events");
    }

    [Fact]
    public void GetRelativeOutputFolder_WhenNamespaceEqualsSourceRoot_ReturnsEmpty()
    {
        var mapper = new NamespaceMapper("Acme.Orders.WriteModel", "Acme.Orders.Events");

        mapper.GetRelativeOutputFolder("Acme.Orders.WriteModel")
              .Should().BeEmpty();
    }

    [Fact]
    public void GetRelativeOutputFolder_WhenNoMatch_ReturnsFullNamespaceAsPath()
    {
        var mapper = new NamespaceMapper("Acme.Orders.WriteModel", "Acme.Orders.Events");

        mapper.GetRelativeOutputFolder("SomeOther.Namespace")
              .Should().Be($"SomeOther{Path.DirectorySeparatorChar}Namespace");
    }
}
