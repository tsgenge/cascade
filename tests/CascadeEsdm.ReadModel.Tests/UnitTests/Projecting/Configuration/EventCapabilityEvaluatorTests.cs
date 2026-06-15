using AutoMapper;
using CascadeEsdm.ReadModel.Projecting.Configuration;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.SharedKernel.ValueObjects;
using FluentAssertions;

namespace CascadeEsdm.ReadModel.UnitTests.UnitTests.Projecting.Configuration;

public class EventCapabilityEvaluatorTests
{
    private static EventEnvelope CreateEnvelope(IDomainEvent @event) =>
        new(
            source: new EventSource("TestAssembly/TestAggregate", Guid.NewGuid(), "TestCommand"),
            subject: new Subject(Guid.NewGuid(), "TestAggregate"),
            securityContext: new AuthenticatedContext(
                new UserIdentity(Guid.NewGuid()),
                new Tenant(Guid.NewGuid())),
            channel: ClientChannel.Empty,
            @event: @event,
            sequence: 1);

    private static (EventCapabilityEvaluator<ItemView> evaluator, IMapper mapper) BuildEvaluator(ViewProfileConfiguration<ItemView> configuration)
    {
        var register = new ViewEventRegister<ItemView>();
        var rootProfile = new RootViewProfile<ItemView>(register, [configuration]);
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new GlobalViewProfile());
            cfg.AddProfile(rootProfile);
        });
        var evaluator = new EventCapabilityEvaluator<ItemView>(register);
        return (evaluator, mapperConfig.CreateMapper());
    }

    [Fact]
    public void Supports_RegisteredEvent_ReturnsTrue()
    {
        var (evaluator, _) = BuildEvaluator(new FullItemConfiguration());
        var envelope = CreateEnvelope(new ItemAddedEvent { ItemId = Guid.NewGuid(), Name = "Test", Price = 1m });

        evaluator.Supports(envelope).Should().BeTrue();
    }

    [Fact]
    public void Supports_UnregisteredEvent_ReturnsFalse()
    {
        var (evaluator, _) = BuildEvaluator(new FullItemConfiguration());
        var envelope = CreateEnvelope(new UnregisteredEvent());

        evaluator.Supports(envelope).Should().BeFalse();
    }

    [Fact]
    public void AddsRow_ForAddEvent_ReturnsTrue()
    {
        var (evaluator, _) = BuildEvaluator(new FullItemConfiguration());
        var envelope = CreateEnvelope(new ItemAddedEvent { ItemId = Guid.NewGuid(), Name = "Test", Price = 1m });

        evaluator.AddsRow(envelope).Should().BeTrue();
    }

    [Fact]
    public void AddsRow_ForChangeEvent_ReturnsFalse()
    {
        var (evaluator, _) = BuildEvaluator(new FullItemConfiguration());
        var envelope = CreateEnvelope(new ItemRenamedEvent { NewName = "New" });

        evaluator.AddsRow(envelope).Should().BeFalse();
    }

    [Fact]
    public void RemovesRow_ForRemoveEvent_ReturnsTrue()
    {
        var (evaluator, _) = BuildEvaluator(new FullItemConfiguration());
        var envelope = CreateEnvelope(new ItemRemovedEvent());

        evaluator.RemovesRow(envelope).Should().BeTrue();
    }

    [Fact]
    public void RemovesRow_ForAddEvent_ReturnsFalse()
    {
        var (evaluator, _) = BuildEvaluator(new FullItemConfiguration());
        var envelope = CreateEnvelope(new ItemAddedEvent { ItemId = Guid.NewGuid(), Name = "Test", Price = 1m });

        evaluator.RemovesRow(envelope).Should().BeFalse();
    }
}

public class UnregisteredEvent : IDomainEvent;

internal class FullItemConfiguration : ViewProfileConfiguration<ItemView>
{
    protected override void Configure(ViewEventBuilder<ItemView> builder)
    {
        var config = builder.UsesStaticPartitionKey();

        config.For<ItemAddedEvent>()
            .UsingRowLocator((e, o) => new(nameof(ItemView.Id), e.ItemId))
            .AddsNewRow((e, o) => e.ItemId);

        config.For<ItemRenamedEvent>()
            .UsingRowLocator((e, o) => new(nameof(ItemView.Id), o?.Subject.Id ?? Guid.Empty))
            .ChangesRows()
            .ForMember(v => v.Name, x => x.MapFrom(e => e.NewName));

        config.For<ItemRemovedEvent>()
            .UsingRowLocator((e, o) => new(nameof(ItemView.Id), o?.Subject.Id ?? Guid.Empty))
            .RemovesRows();
    }
}
