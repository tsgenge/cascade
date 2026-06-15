using AutoMapper;
using CascadeEsdm.ReadModel.Projecting;
using CascadeEsdm.ReadModel.Projecting.Configuration;
using FluentAssertions;

namespace CascadeEsdm.ReadModel.UnitTests.UnitTests.Projecting.Configuration;

public class ValidationTests
{
    [Fact]
    public void Validate_WithNoPartitionStrategy_Throws()
    {
        var config = new NoPartitionConfiguration();
        var register = new ViewEventRegister<ItemView>();
        var rootProfile = new RootViewProfile<ItemView>(register, [config]);
        _ = new MapperConfiguration(cfg => cfg.AddProfile(rootProfile));

        var act = () => config.Validate();

        act.Should().Throw<ProjectionConfigurationException<ItemView>>();
    }

    [Fact]
    public void Validate_WithNoEvents_Throws()
    {
        var config = new NoEventsConfiguration();
        var register = new ViewEventRegister<ItemView>();
        var rootProfile = new RootViewProfile<ItemView>(register, [config]);
        _ = new MapperConfiguration(cfg => cfg.AddProfile(rootProfile));

        var act = () => config.Validate();

        act.Should().Throw<ProjectionConfigurationException<ItemView>>();
    }

    [Fact]
    public void Validate_WithNoMutationStrategy_Throws()
    {
        var config = new NoMutationConfiguration();
        var register = new ViewEventRegister<ItemView>();
        var rootProfile = new RootViewProfile<ItemView>(register, [config]);
        _ = new MapperConfiguration(cfg => cfg.AddProfile(rootProfile));

        var act = () => config.Validate();

        act.Should().Throw<ProjectionConfigurationException<ItemView, ItemAddedEvent>>();
    }

    [Fact]
    public void Validate_WithValidConfiguration_DoesNotThrow()
    {
        var config = new FullItemConfiguration();
        var register = new ViewEventRegister<ItemView>();
        var rootProfile = new RootViewProfile<ItemView>(register, [config]);
        _ = new MapperConfiguration(cfg => cfg.AddProfile(rootProfile));

        var act = () => config.Validate();

        act.Should().NotThrow();
    }
}

internal class NoPartitionConfiguration : ViewProfileConfiguration<ItemView>
{
    protected override void Configure(ViewEventBuilder<ItemView> builder)
    {
    }
}

internal class NoEventsConfiguration : ViewProfileConfiguration<ItemView>
{
    protected override void Configure(ViewEventBuilder<ItemView> builder)
    {
        builder.UsesStaticPartitionKey();
    }
}

internal class NoMutationConfiguration : ViewProfileConfiguration<ItemView>
{
    protected override void Configure(ViewEventBuilder<ItemView> builder)
    {
        var config = builder.UsesStaticPartitionKey();
        config.For<ItemAddedEvent>()
            .UsingRowLocator((e, o) => new(nameof(ItemView.Id), e.ItemId));
    }
}
