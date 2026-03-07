using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Environment;

public class WriteContext
{
    public IServiceProvider ServiceProvider { get; }
    public WriteContext()
    {
        var builder = new HostBuilder()
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureContainer<ContainerBuilder>((h, b) => { })
            .ConfigureAppConfiguration((context, config) => { })
            .ConfigureServices((b, services) => { });
        
        var app= builder.Build();
        ServiceProvider = app.Services;
    }
}