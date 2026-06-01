using CascadeEsdm.EventExtractor.Configuration;
using FluentAssertions;

namespace CascadeEsdm.EventExtractor.Tests.Configuration;

public class ExtractorOptionsTests
{
    [Theory]
    [InlineData("Acme.Orders.WriteModel", "Acme.Orders.Events")]
    [InlineData("Acme.Orders.Domain",     "Acme.Orders.Events")]
    [InlineData("Acme.Orders.Write",      "Acme.Orders.Events")]
    [InlineData("Acme.Orders.Application","Acme.Orders.Events")]
    [InlineData("Acme.Orders",            "Acme.Orders.Events")]
    public void ResolvedAssemblyName_StripsSuffix_AndAppendsEvents(string rootNamespace, string expected)
    {
        var options = new ExtractorOptions
        {
            SourceRoot   = ".",
            OutputDir    = ".",
            RootNamespace = rootNamespace,
        };

        options.ResolvedAssemblyName.Should().Be(expected);
    }

    [Fact]
    public void ResolvedAssemblyName_UsesExplicitAssemblyName_WhenProvided()
    {
        var options = new ExtractorOptions
        {
            SourceRoot    = ".",
            OutputDir     = ".",
            RootNamespace = "Acme.Orders.WriteModel",
            AssemblyName  = "Custom.Events",
        };

        options.ResolvedAssemblyName.Should().Be("Custom.Events");
    }

    [Fact]
    public void ResolvedEventsNamespace_DefaultsToResolvedAssemblyName()
    {
        var options = new ExtractorOptions
        {
            SourceRoot    = ".",
            OutputDir     = ".",
            RootNamespace = "Acme.Orders.WriteModel",
        };

        options.ResolvedEventsNamespace.Should().Be(options.ResolvedAssemblyName);
    }

    [Fact]
    public void ResolvedEventsNamespace_UsesExplicitNamespace_WhenProvided()
    {
        var options = new ExtractorOptions
        {
            SourceRoot      = ".",
            OutputDir       = ".",
            RootNamespace   = "Acme.Orders.WriteModel",
            EventsNamespace = "Custom.Ns",
        };

        options.ResolvedEventsNamespace.Should().Be("Custom.Ns");
    }
}
