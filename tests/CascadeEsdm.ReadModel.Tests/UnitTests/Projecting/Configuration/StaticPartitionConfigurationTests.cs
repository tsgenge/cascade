using AutoMapper;
using CascadeEsdm.ReadModel.Projecting.Configuration;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.SharedKernel.ValueObjects;
using FluentAssertions;

namespace CascadeEsdm.ReadModel.UnitTests.UnitTests.Projecting.Configuration;

public class StaticPartitionConfigurationTests
{
    private static EventEnvelope CreateEnvelope(IDomainEvent @event)
    {
        return new EventEnvelope(
            source: new EventSource("TestAssembly/TestAggregate", Guid.NewGuid(), "TestCommand"),
            subject: new Subject(Guid.NewGuid(), "TestAggregate"),
            securityContext: new AuthenticatedContext(
                new UserIdentity(Guid.NewGuid()),
                new Tenant(Guid.NewGuid())),
            channel: ClientChannel.Empty,
            @event: @event,
            sequence: 1);
    }

    private static IMapper BuildMapper(ViewProfileConfiguration<ItemView> configuration)
    {
        var register = new ViewEventRegister<ItemView>();
        var rootProfile = new RootViewProfile<ItemView>(register, [configuration]);
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new GlobalViewProfile());
            cfg.AddProfile(rootProfile);
        });
        configuration.Validate();
        return mapperConfig.CreateMapper();
    }

    [Fact]
    public void AddEvent_MapsProperties_ToNewView()
    {
        var config = new ItemAddedConfiguration();
        var mapper = BuildMapper(config);

        var evt = new ItemAddedEvent { ItemId = Guid.NewGuid(), Name = "Widget", Price = 9.99m };
        var envelope = CreateEnvelope(evt);

        var view = mapper.Map<ItemView>(evt, opts => opts.State = envelope);

        view.Name.Should().Be("Widget");
        view.Price.Should().Be(9.99m);
        view.Modified.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        view.Created.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ChangeEvent_MapsProperty_ToExistingView()
    {
        var config = new ItemRenamedConfiguration();
        var mapper = BuildMapper(config);

        var evt = new ItemRenamedEvent { NewName = "Super Widget" };
        var envelope = CreateEnvelope(evt);

        var view = mapper.Map<ItemView>(evt, opts => opts.State = envelope);

        view.Name.Should().Be("Super Widget");
        view.Modified.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void RemoveEvent_CreatesRowRemover()
    {
        var config = new ItemRemovedConfiguration();
        var mapper = BuildMapper(config);

        var evt = new ItemRemovedEvent();
        var envelope = CreateEnvelope(evt);

        var remover = mapper.Map<RowRemover<ItemView>>(evt, opts => opts.State = envelope);

        remover.Deletes.Should().BeTrue();
    }

    [Fact]
    public void AddEvent_CreatesRowAdder_WithCorrectId()
    {
        var config = new ItemAddedConfiguration();
        var mapper = BuildMapper(config);

        var expectedId = Guid.NewGuid();
        var evt = new ItemAddedEvent { ItemId = expectedId, Name = "Test", Price = 1m };
        var envelope = CreateEnvelope(evt);

        var adder = mapper.Map<RowAdder<ItemView>>(evt, opts => opts.State = envelope);

        adder.Creates.Should().BeTrue();
        adder.NewRowId.Should().Be(expectedId);
    }

    [Fact]
    public void AddEvent_CreatesRowLocator_WithCorrectProperty()
    {
        var config = new ItemAddedConfiguration();
        var mapper = BuildMapper(config);

        var itemId = Guid.NewGuid();
        var evt = new ItemAddedEvent { ItemId = itemId, Name = "Test", Price = 1m };
        var envelope = CreateEnvelope(evt);

        var locator = mapper.Map<RowLocator<ItemView>>(evt, opts => opts.State = envelope);

        locator.PropertySelector.Key.Should().Be(nameof(ItemView.Id));
        locator.PropertySelector.Value.Should().Be(itemId);
    }
}

internal class ItemAddedConfiguration : ViewProfileConfiguration<ItemView>
{
    protected override void Configure(ViewEventBuilder<ItemView> builder)
    {
        var config = builder.UsesStaticPartitionKey();

        config.For<ItemAddedEvent>()
            .UsingRowLocator((e, o) => new(nameof(ItemView.Id), e.ItemId))
            .AddsNewRow((e, o) => e.ItemId);
    }
}

internal class ItemRenamedConfiguration : ViewProfileConfiguration<ItemView>
{
    protected override void Configure(ViewEventBuilder<ItemView> builder)
    {
        var config = builder.UsesStaticPartitionKey();

        config.For<ItemRenamedEvent>()
            .UsingRowLocator((e, o) => new(nameof(ItemView.Id), o?.Subject.Id ?? Guid.Empty))
            .ChangesRows()
            .ForMember(v => v.Name, x => x.MapFrom(e => e.NewName));
    }
}

internal class ItemRemovedConfiguration : ViewProfileConfiguration<ItemView>
{
    protected override void Configure(ViewEventBuilder<ItemView> builder)
    {
        var config = builder.UsesStaticPartitionKey();

        config.For<ItemRemovedEvent>()
            .UsingRowLocator((e, o) => new(nameof(ItemView.Id), o?.Subject.Id ?? Guid.Empty))
            .RemovesRows();
    }
}
