using CascadeEsdm.SharedKernel.Infrastructure.Logging;
using FluentAssertions;

namespace CascadeEsdm.SharedKernel.UnitTests.Infrastructure.Logging;

public class TelemetryParentTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var traceParent = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01";
        var traceState = "congo=t61rcWkgMzE";

        var parent = new TelemetryParent(traceParent, traceState);

        parent.TraceParent.Should().Be(traceParent);
        parent.TraceState.Should().Be(traceState);
    }

    [Fact]
    public void IsValid_WithBothValues_ReturnsTrue()
    {
        var parent = new TelemetryParent("trace-parent", "trace-state");

        parent.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "state")]
    [InlineData("parent", "")]
    [InlineData("", "")]
    [InlineData("  ", "state")]
    [InlineData("parent", "  ")]
    public void IsValid_WithEmptyOrWhitespace_ReturnsFalse(string traceParent, string traceState)
    {
        var parent = new TelemetryParent(traceParent, traceState);

        parent.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithNullValues_ReturnsFalse()
    {
        var parent = new TelemetryParent(null!, null!);

        parent.IsValid.Should().BeFalse();
    }
}
