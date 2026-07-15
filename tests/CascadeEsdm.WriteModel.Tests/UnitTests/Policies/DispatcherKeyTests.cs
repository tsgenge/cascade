using CascadeEsdm.WriteModel.Exceptions;
using CascadeEsdm.WriteModel.Policies;
using FluentAssertions;

namespace CascadeEsdm.WriteModel.Tests.UnitTests.Policies;

public class DispatcherKeyTests
{
    [Fact]
    public void Default_IsNotKeyed()
    {
        DispatcherKey.Default.IsKeyed.Should().BeFalse();
        DispatcherKey.Default.Value.Should().BeNull();
    }

    [Fact]
    public void For_WhenNull_ReturnsDefault()
    {
        DispatcherKey.For(null).Should().BeSameAs(DispatcherKey.Default);
    }

    [Fact]
    public void For_WhenNamed_IsKeyed()
    {
        var key = DispatcherKey.For("orders");

        key.IsKeyed.Should().BeTrue();
        key.Value.Should().Be("orders");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void For_WhenEmptyOrWhitespace_ThrowsConfigurationException(string value)
    {
        var act = () => DispatcherKey.For(value);

        act.Should().Throw<ConfigurationException>();
    }

    [Fact]
    public void ImplicitFromString_ProducesKeyedValue()
    {
        DispatcherKey key = "payments";

        key.IsKeyed.Should().BeTrue();
        key.Value.Should().Be("payments");
    }

    [Fact]
    public void ImplicitToString_ReturnsUnderlyingValue()
    {
        string? value = DispatcherKey.For("orders");

        value.Should().Be("orders");
    }
}
