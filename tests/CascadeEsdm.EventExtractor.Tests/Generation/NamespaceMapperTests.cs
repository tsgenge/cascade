using CascadeEsdm.EventExtractor.Generation;
using FluentAssertions;

namespace CascadeEsdm.EventExtractor.Tests.Generation;

public class NamespaceMapperTests
{
    [Fact]
    public void MapNamespace_ReplacesSourceRootWithTargetRoot()
    {
        var mapper = new NamespaceMapper("Acme.Orders.WriteModel", "Acme.Orders.Schema");

        mapper.MapNamespace("Acme.Orders.WriteModel.Orders.Events")
              .Should().Be("Acme.Orders.Schema.Orders.Events");
    }

    [Fact]
    public void MapNamespace_WhenNamespaceEqualsSourceRoot_ReturnsTargetRoot()
    {
        var mapper = new NamespaceMapper("Acme.Orders.WriteModel", "Acme.Orders.Schema");

        mapper.MapNamespace("Acme.Orders.WriteModel")
              .Should().Be("Acme.Orders.Schema");
    }

    [Fact]
    public void MapNamespace_WhenNoMatch_PrefixesWithTargetRoot()
    {
        var mapper = new NamespaceMapper("Acme.Orders.WriteModel", "Acme.Orders.Schema");

        mapper.MapNamespace("SomeOther.Namespace")
              .Should().Be("Acme.Orders.Schema.SomeOther.Namespace");
    }

    [Fact]
    public void GetRelativeOutputFolder_ReturnsSubfolderBelowSourceRoot()
    {
        var mapper = new NamespaceMapper("Acme.Orders.WriteModel", "Acme.Orders.Schema");

        var folder = mapper.GetRelativeOutputFolder("Acme.Orders.WriteModel.Orders.Events");

        folder.Should().Be($"Orders{Path.DirectorySeparatorChar}Events");
    }

    [Fact]
    public void GetRelativeOutputFolder_WhenNamespaceEqualsSourceRoot_ReturnsEmpty()
    {
        var mapper = new NamespaceMapper("Acme.Orders.WriteModel", "Acme.Orders.Schema");

        mapper.GetRelativeOutputFolder("Acme.Orders.WriteModel")
              .Should().BeEmpty();
    }

    [Fact]
    public void GetRelativeOutputFolder_WhenNoMatch_ReturnsFullNamespaceAsPath()
    {
        var mapper = new NamespaceMapper("Acme.Orders.WriteModel", "Acme.Orders.Schema");

        mapper.GetRelativeOutputFolder("SomeOther.Namespace")
              .Should().Be($"SomeOther{Path.DirectorySeparatorChar}Namespace");
    }

    [Fact]
    public void FolderToNamespace_ReturnsFolderSegmentsAppendedToTargetRoot()
    {
        var mapper = new NamespaceMapper("Acme.Orders.WriteModel", "Acme.Orders.Schema");

        mapper.FolderToNamespace($"Person{Path.DirectorySeparatorChar}Events")
              .Should().Be("Acme.Orders.Schema.Person.Events");
    }

    [Fact]
    public void FolderToNamespace_WhenFolderIsEmpty_ReturnsTargetRoot()
    {
        var mapper = new NamespaceMapper("Acme.Orders.WriteModel", "Acme.Orders.Schema");

        mapper.FolderToNamespace(string.Empty)
              .Should().Be("Acme.Orders.Schema");
    }
}
