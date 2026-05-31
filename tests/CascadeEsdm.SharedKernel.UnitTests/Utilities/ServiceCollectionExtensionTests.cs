using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CascadeEsdm.SharedKernel.UnitTests.Utilities;

public class ServiceCollectionExtensionTests
{
    [Fact]
    public void Adding_Decorator_Runs_In_Order()
    {
        var services = new ServiceCollection();
        services.AddTransient<IExampleInterface<ExampleType1>, BaseImplementation<ExampleType1>>();
        services.AddGenericDecorator(typeof(IExampleInterface<>), typeof(DecoratorImplementation1<>));
        services.AddGenericDecorator(typeof(IExampleInterface<>), typeof(DecoratorImplementation2<>));
        var serviceProvider = services.BuildServiceProvider();

        var implementation = serviceProvider.GetRequiredService<IExampleInterface<ExampleType1>>();
        var callOrder = new List<string>();
        implementation.DoIt(new ExampleType1(), callOrder);
        callOrder.Should().HaveCount(3);
        callOrder.Should().BeEquivalentTo("DecoratorImplementation1`1", "DecoratorImplementation2`1",
            "BaseImplementation`1");
    }

    [Fact]
    public void Adding_Decorator_For_Different_Versions_Runs_In_Order_For_Each()
    {
        var services = new ServiceCollection();
        services.AddTransient<IExampleInterface<ExampleType1>, BaseImplementation<ExampleType1>>();
        services.AddGenericDecorator(typeof(IExampleInterface<>), typeof(DecoratorImplementation1<>));
        services.AddGenericDecorator(typeof(IExampleInterface<>), typeof(DecoratorImplementation2<>));

        services
            .AddTransient<IExampleInterface<ExampleType1, ExampleType2>,
                BaseImplementation<ExampleType1, ExampleType2>>();

        services.AddGenericDecorator(typeof(IExampleInterface<,>), typeof(DecoratorImplementation1<,>));
        services.AddGenericDecorator(typeof(IExampleInterface<,>), typeof(DecoratorImplementation2<,>));

        var serviceProvider = services.BuildServiceProvider();

        var implementation1 = serviceProvider.GetRequiredService<IExampleInterface<ExampleType1>>();
        var callOrder1 = new List<string>();
        implementation1.DoIt(new ExampleType1(), callOrder1);
        callOrder1.Should().HaveCount(3);
        callOrder1.Should().BeEquivalentTo("DecoratorImplementation1`1", "DecoratorImplementation2`1",
            "BaseImplementation`1");

        var implementation2 = serviceProvider.GetRequiredService<IExampleInterface<ExampleType1, ExampleType2>>();
        var callOrder2 = new List<string>();
        implementation2.DoIt(new ExampleType1(), callOrder2);
        callOrder2.Should().HaveCount(3);
        callOrder2.Should().BeEquivalentTo("DecoratorImplementation1`2", "DecoratorImplementation2`2",
            "BaseImplementation`2");
    }

    [Fact]
    public void Adding_Decorator_For_Different_Generics_Decorates_All()
    {
        var services = new ServiceCollection();
        services.AddTransient<IExampleInterface<ExampleType1>, BaseImplementation<ExampleType1>>();
        services.AddTransient<IExampleInterface<ExampleType2>, BaseImplementation<ExampleType2>>();

        services.AddGenericDecorator(typeof(IExampleInterface<>), typeof(DecoratorImplementation1<>));
        services.AddGenericDecorator(typeof(IExampleInterface<>), typeof(DecoratorImplementation2<>));


        var serviceProvider = services.BuildServiceProvider();

        var implementation1 = serviceProvider.GetRequiredService<IExampleInterface<ExampleType1>>();
        var callOrder1 = new List<string>();
        implementation1.DoIt(new ExampleType1(), callOrder1);
        callOrder1.Should().HaveCount(3);
        callOrder1.Should().BeEquivalentTo("DecoratorImplementation1`1", "DecoratorImplementation2`1",
            "BaseImplementation`1");

        var implementation2 = serviceProvider.GetRequiredService<IExampleInterface<ExampleType2>>();
        var callOrder2 = new List<string>();
        implementation2.DoIt(new ExampleType2(), callOrder2);
        callOrder2.Should().HaveCount(3);
        callOrder2.Should().BeEquivalentTo("DecoratorImplementation1`1", "DecoratorImplementation2`1",
            "BaseImplementation`1");
    }
}

public interface IExampleInterface<TType>
{
    void DoIt(TType t, List<string> callOrder);
}

public interface IExampleInterface<TType1, TType2>
{
    void DoIt(TType1 t, List<string> callOrder);
}

public class BaseImplementation<TType> : IExampleInterface<TType>
{
    public void DoIt(TType t, List<string> callOrder)
    {
        callOrder.Add(GetType().Name);
    }
}

public class BaseImplementation<TType, TType2> : IExampleInterface<TType, TType2>
{
    public void DoIt(TType t, List<string> callOrder)
    {
        callOrder.Add(GetType().Name);
    }
}

public class DecoratorImplementation1<TType> : IExampleInterface<TType>
{
    private readonly IExampleInterface<TType> _inner;

    public DecoratorImplementation1(IExampleInterface<TType> inner)
    {
        _inner = inner;
    }

    public void DoIt(TType t, List<string> callOrder)
    {
        callOrder.Add(GetType().Name);
        _inner.DoIt(t, callOrder);
    }
}

public class DecoratorImplementation2<TType> : IExampleInterface<TType>
{
    private readonly IExampleInterface<TType> _inner;

    public DecoratorImplementation2(IExampleInterface<TType> inner)
    {
        _inner = inner;
    }

    public void DoIt(TType t, List<string> callOrder)
    {
        callOrder.Add(GetType().Name);
        _inner.DoIt(t, callOrder);
    }
}

public class DecoratorImplementation1<TType, TType2> : IExampleInterface<TType, TType2>
{
    private readonly IExampleInterface<TType, TType2> _inner;

    public DecoratorImplementation1(IExampleInterface<TType, TType2> inner)
    {
        _inner = inner;
    }

    public void DoIt(TType t, List<string> callOrder)
    {
        callOrder.Add(GetType().Name);
        _inner.DoIt(t, callOrder);
    }
}

public class DecoratorImplementation2<TType, TType2> : IExampleInterface<TType, TType2>
{
    private readonly IExampleInterface<TType, TType2> _inner;

    public DecoratorImplementation2(IExampleInterface<TType, TType2> inner)
    {
        _inner = inner;
    }

    public void DoIt(TType t, List<string> callOrder)
    {
        callOrder.Add(GetType().Name);
        _inner.DoIt(t, callOrder);
    }
}

public class ExampleType1 { }

public class ExampleType2 { }