using System.Reflection;
using FluentAssertions;

namespace CascadeEsdm.SharedKernel.UnitTests.Extensions;

public class ReflectionExtensionsTests
{
    private interface IGeneric<T> { }
    private interface IGenericTwo<T1, T2> { }
    private interface INonGeneric { }

    private class DirectImplementor : IGeneric<string> { }
    private class GenericBase<T> : IGeneric<T> { }
    private class DerivedFromGenericBase : GenericBase<int> { }
    private class DeepDerived : DerivedFromGenericBase { }
    private class NonGenericImpl : INonGeneric { }
    private class MultiInterface : IGeneric<string>, IGenericTwo<int, string> { }

    [Fact]
    public void IsAssignableToGenericType_DirectInterface_ReturnsTrue()
    {
        typeof(DirectImplementor).IsAssignableToGenericType(typeof(IGeneric<>))
            .Should().BeTrue();
    }

    [Fact]
    public void IsAssignableToGenericType_InheritedFromGenericBase_ReturnsTrue()
    {
        typeof(DerivedFromGenericBase).IsAssignableToGenericType(typeof(IGeneric<>))
            .Should().BeTrue();
    }

    [Fact]
    public void IsAssignableToGenericType_DeeplyInherited_ReturnsTrue()
    {
        typeof(DeepDerived).IsAssignableToGenericType(typeof(IGeneric<>))
            .Should().BeTrue();
    }

    [Fact]
    public void IsAssignableToGenericType_GenericBaseItself_ReturnsTrue()
    {
        typeof(GenericBase<int>).IsAssignableToGenericType(typeof(GenericBase<>))
            .Should().BeTrue();
    }

    [Fact]
    public void IsAssignableToGenericType_NoMatch_ReturnsFalse()
    {
        typeof(NonGenericImpl).IsAssignableToGenericType(typeof(IGeneric<>))
            .Should().BeFalse();
    }

    [Fact]
    public void IsAssignableToGenericType_ObjectType_ReturnsFalse()
    {
        typeof(object).IsAssignableToGenericType(typeof(IGeneric<>))
            .Should().BeFalse();
    }

    [Fact]
    public void IsAssignableToGenericType_MultipleGenericInterfaces_MatchesCorrectOne()
    {
        typeof(MultiInterface).IsAssignableToGenericType(typeof(IGenericTwo<,>))
            .Should().BeTrue();
    }
}
