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
    [InlineData("CascadeEsdm.WriteModel")]
    [InlineData("Acme.Domain.ValueObjects")]
    [InlineData("Microsoft.Extensions.Logging")]
    public void Filter_RemovesNonAllowedNamespaces(string ns)
    {
        Filter(ns).Should().BeEmpty();
    }

    [Theory]
    [InlineData("System")]
    [InlineData("System.Collections.Generic")]
    [InlineData("CascadeEsdm.SharedKernel")]
    [InlineData("CascadeEsdm.SharedKernel.Events")]
    [InlineData("CascadeEsdm.SharedKernel.ValueObjects")]
    public void Filter_RetainsAllowedNamespaces(string ns)
    {
        Filter(ns).Should().ContainSingle().Which.Should().Be(ns);
    }

    [Fact]
    public void Filter_KeepsOnlyAllowedEntries_FromMixedList()
    {
        var result = Filter(
            "CascadeEsdm.SharedKernel.Events",
            "CascadeEsdm.WriteModel.Hydration",
            "System",
            "CascadeEsdm.WriteModel.CommandHandling",
            "Acme.Domain.ValueObjects");

        result.Should().BeEquivalentTo(["CascadeEsdm.SharedKernel.Events", "System"]);
    }
}
