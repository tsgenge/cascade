using CascadeEsdm.EventExtractor.Extraction;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;

namespace CascadeEsdm.EventExtractor.Tests.Extraction;

public class UsingsFilterTests
{
    private static IReadOnlyList<string> Filter(params string[] namespaces)
    {
        var usings = namespaces
            .Select(ns => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(ns)))
            .ToList();

        return UsingsFilter
            .Filter(usings, "Target.Events")
            .Select(u => u.Name!.ToString())
            .ToList();
    }

    [Theory]
    [InlineData("CascadeEsdm.WriteModel.Hydration")]
    [InlineData("CascadeEsdm.WriteModel.CommandHandling")]
    [InlineData("CascadeEsdm.WriteModel.Security")]
    [InlineData("CascadeEsdm.WriteModel.Composition")]
    [InlineData("CascadeEsdm.WriteModel.EventStream")]
    [InlineData("CascadeEsdm.WriteModel.Hydration.Internal")]
    public void Filter_RemovesWriteModelOnlyNamespaces(string ns)
    {
        Filter(ns).Should().BeEmpty();
    }

    [Theory]
    [InlineData("CascadeEsdm.SharedKernel.Events")]
    [InlineData("CascadeEsdm.WriteModel")]
    [InlineData("System")]
    [InlineData("Acme.Domain.ValueObjects")]
    public void Filter_RetainsNonWriteModelNamespaces(string ns)
    {
        Filter(ns).Should().ContainSingle().Which.Should().Be(ns);
    }

    [Fact]
    public void Filter_RemovesOnlyWriteModelEntries_FromMixedList()
    {
        var result = Filter(
            "CascadeEsdm.SharedKernel.Events",
            "CascadeEsdm.WriteModel.Hydration",
            "System",
            "CascadeEsdm.WriteModel.CommandHandling");

        result.Should().BeEquivalentTo(["CascadeEsdm.SharedKernel.Events", "System"]);
    }
}
