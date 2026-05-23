using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CascadeEsdm.WriteModel.Tests.UnitTests.Utilities;

public class ServiceCollectionExtensionTests
{
    [Fact]
    public void Adding_Decorator_Runs_In_Order()
    {
        var services = new ServiceCollection();
        services.AddGeneric(typeof(IExampleInterface<>), typeof(BaseImplementation<>));
        services.AddTransientGenericDecorator(typeof(IExampleInterface<>), typeof(DecoratorImplementation<>));
        var serviceProvider = services.BuildServiceProvider();

        var implementation = serviceProvider.GetRequiredService<IExampleInterface<ExampleType1>>();
        var callOrder = new List<string>();
        implementation.DoIt(new ExampleType1(), callOrder);
        callOrder.Should().HaveCount(2);
    }

    private interface IExampleInterface<TType>
    {
        void DoIt(TType t, List<string> callOrder);
    }

    private class BaseImplementation<TType> : IExampleInterface<TType>
    {
        public void DoIt(TType t, List<string> callOrder)
        {
            callOrder.Add(GetType().Name);
        }
    }

    private class DecoratorImplementation<TType> : IExampleInterface<TType>
    {
        private readonly IExampleInterface<TType> _inner;

        public DecoratorImplementation(IExampleInterface<TType> inner)
        {
            _inner = inner;
        }

        public void DoIt(TType t, List<string> callOrder)
        {
            callOrder.Add(GetType().Name);
            _inner.DoIt(t, callOrder);
        }
    }


    private class ExampleType1 { }
}