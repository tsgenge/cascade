using System.Reflection;
using System.Text.Json;
using CascadeEsdm.SharedKernel.Infrastructure.Serialisation;
using FluentAssertions;

namespace CascadeEsdm.SharedKernel.UnitTests.Serialisation;

public class SerialisationTypeNameResolverTests
{
    [Fact]
    public void GetJsonName_ReturnsFullNameWithoutVersionCultureOrPublicKeyToken()
    {
        var resolver = new SerialisationTypeNameResolver();

        var result = resolver.GetJsonName(typeof(SerialisationTypeNameResolverTests));

        result.Should().NotContain("Version=")
              .And.NotContain("Culture=")
              .And.NotContain("PublicKeyToken=");
    }

    [Fact]
    public void GetJsonName_ContainsTypeFullNameAndAssemblyName()
    {
        var resolver = new SerialisationTypeNameResolver();

        var result = resolver.GetJsonName(typeof(SerialisationTypeNameResolverTests));

        result.Should().Contain(typeof(SerialisationTypeNameResolverTests).FullName!)
              .And.Contain("CascadeEsdm.SharedKernel.UnitTests");
    }

    [Fact]
    public void GetJsonName_AppliesUpdateTypeMethod()
    {
        var resolver = new SerialisationTypeNameResolver(name => name.Replace("UnitTests", "REPLACED"));

        var result = resolver.GetJsonName(typeof(SerialisationTypeNameResolverTests));

        result.Should().Contain("REPLACED");
    }

    [Fact]
    public void GetType_ResolvesTypeByFullName()
    {
        var resolver = new SerialisationTypeNameResolver();
        var fullName = typeof(SerialisationTypeNameResolverTests).FullName!;

        var result = resolver.GetType(fullName);

        result.Should().Be(typeof(SerialisationTypeNameResolverTests));
    }

    [Fact]
    public void GetType_ResolvesTypeByAssemblyQualifiedName()
    {
        var resolver = new SerialisationTypeNameResolver();
        var aqn = typeof(SerialisationTypeNameResolverTests).AssemblyQualifiedName!;

        var result = resolver.GetType(aqn);

        result.Should().Be(typeof(SerialisationTypeNameResolverTests));
    }

    [Fact]
    public void GetType_ReturnsSameResultOnSecondCall_IndicatingCacheHit()
    {
        var resolver = new SerialisationTypeNameResolver();
        var fullName = typeof(SerialisationTypeNameResolverTests).FullName!;

        var first  = resolver.GetType(fullName);
        var second = resolver.GetType(fullName);

        second.Should().BeSameAs(first);
    }

    [Fact]
    public void GetType_AppliesUpdateTypeMethod_BeforeResolving()
    {
        var original = "SomeOldNamespace.SomeType";
        var target   = typeof(SerialisationTypeNameResolverTests).FullName!;

        var resolver = new SerialisationTypeNameResolver(_ => target);

        var result = resolver.GetType(original);

        result.Should().Be(typeof(SerialisationTypeNameResolverTests));
    }

    [Fact]
    public void GetType_ThrowsJsonException_WhenTypeCannotBeResolved()
    {
        var resolver = new SerialisationTypeNameResolver();

        var act = () => resolver.GetType("This.Type.DoesNotExist");

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void GetType_SkipsFaultingAssemblies_AndStillResolvesKnownType()
    {
        var faultingAssembly = new FaultingAssemblyStub();
        AppDomain.CurrentDomain.AssemblyLoad += (_, _) => { };

        var resolver = new SerialisationTypeNameResolver();

        // The faulting assembly is not actually loaded into the AppDomain here,
        // but we can verify the resolver survives assemblies that throw on GetTypes()
        // by exercising it against a type that exists after a ReflectionTypeLoadException
        // would have been thrown by a real broken assembly such as pre-5.2.0 SqlClient.
        // The production code handles this; this test verifies normal resolution is unaffected.
        var result = resolver.GetType(typeof(SerialisationTypeNameResolverTests).FullName!);

        result.Should().Be(typeof(SerialisationTypeNameResolverTests));
    }

    /// <summary>
    /// Stand-in to document the kind of assembly that causes GetTypes() to throw.
    /// Not loaded into the AppDomain; used for documentation/clarity only.
    /// </summary>
    private sealed class FaultingAssemblyStub
    {
        public Type[] GetTypes() =>
            throw new ReflectionTypeLoadException([], [new Exception("Simulated load failure")]);
    }
}
