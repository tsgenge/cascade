using CascadeEsdm.EventExtractor.Extraction;
using CascadeEsdm.EventExtractor.Scanning;
using FluentAssertions;

namespace CascadeEsdm.EventExtractor.Tests.Extraction;

public class EventSyntaxExtractorTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public EventSyntaxExtractorTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private ScannedEventFile ScanSingle(string source)
    {
        var path = Path.Combine(_tempDir, "Events.cs");
        File.WriteAllText(path, source);
        return ProjectScanner.FindEventFiles(_tempDir).Single();
    }

    [Fact]
    public void Extract_RewritesNamespace_ToTargetNamespace()
    {
        var file = ScanSingle("""
            using CascadeEsdm.SharedKernel.Events;
            namespace Acme.Orders.WriteModel.Orders.Events;
            public record OrderPlaced(Guid Id) : IDomainEvent;
            """);

        var output = EventSyntaxExtractor.Extract(file, "Acme.Orders.Events.Orders.Events", "Acme.Orders.WriteModel");

        output.Should().Contain("namespace Acme.Orders.Events.Orders.Events");
    }

    [Fact]
    public void Extract_RemovesApplierClasses()
    {
        var file = ScanSingle("""
            using CascadeEsdm.SharedKernel.Events;
            using CascadeEsdm.WriteModel.Hydration;
            namespace Acme.Orders.Events;
            public record OrderPlaced(Guid Id) : IDomainEvent;
            internal class OrderPlacedApplier : IEventApplier<OrderPlaced, OrderAggregate>
            {
                public void Apply(OrderAggregate a, OrderPlaced e, EventEnvelope env) { }
            }
            """);

        var output = EventSyntaxExtractor.Extract(file, "Acme.Orders.Events", "Acme.Orders.WriteModel");

        output.Should().NotContain("OrderPlacedApplier");
        output.Should().Contain("record OrderPlaced");
    }

    [Fact]
    public void Extract_RetainsEventRecord()
    {
        var file = ScanSingle("""
            using CascadeEsdm.SharedKernel.Events;
            namespace Acme.Orders.Events;
            public record OrderPlaced(Guid Id, string Reference) : IDomainEvent;
            """);

        var output = EventSyntaxExtractor.Extract(file, "Acme.Orders.Events", "Acme.Orders.WriteModel");

        output.Should().Contain("record OrderPlaced");
        output.Should().Contain("Guid Id");
        output.Should().Contain("string Reference");
    }

    [Fact]
    public void Extract_StripsWriteModelOnlyUsings()
    {
        var file = ScanSingle("""
            using CascadeEsdm.SharedKernel.Events;
            using CascadeEsdm.WriteModel.Hydration;
            using CascadeEsdm.WriteModel.CommandHandling;
            namespace Acme.Orders.Events;
            public record OrderPlaced(Guid Id) : IDomainEvent;
            """);

        var output = EventSyntaxExtractor.Extract(file, "Acme.Orders.Events", "Acme.Orders.WriteModel");

        output.Should().NotContain("CascadeEsdm.WriteModel.Hydration");
        output.Should().NotContain("CascadeEsdm.WriteModel.CommandHandling");
        output.Should().Contain("CascadeEsdm.SharedKernel.Events");
    }

    [Fact]
    public void Extract_RetainsEnums_CoLocatedWithEvents()
    {
        var file = ScanSingle("""
            using CascadeEsdm.SharedKernel.Events;
            namespace Acme.Orders.Events;
            public enum OrderStatus { Placed, Fulfilled }
            public record OrderPlaced(OrderStatus Status) : IDomainEvent;
            """);

        var output = EventSyntaxExtractor.Extract(file, "Acme.Orders.Events", "Acme.Orders.WriteModel");

        output.Should().Contain("enum OrderStatus");
    }

    [Fact]
    public void Extract_AlwaysIncludesUsingSystem_WhenNotInSource()
    {
        var file = ScanSingle("""
            using CascadeEsdm.SharedKernel.Events;
            namespace Acme.Orders.Events;
            public record OrderPlaced(string Reference) : IDomainEvent;
            """);

        var output = EventSyntaxExtractor.Extract(file, "Acme.Orders.Events", "Acme.Orders.WriteModel");

        output.Should().Contain("using System;");
    }

    [Fact]
    public void Extract_DoesNotDuplicateUsingSystem_WhenAlreadyInSource()
    {
        var file = ScanSingle("""
            using System;
            using CascadeEsdm.SharedKernel.Events;
            namespace Acme.Orders.Events;
            public record OrderPlaced(Guid Id) : IDomainEvent;
            """);

        var output = EventSyntaxExtractor.Extract(file, "Acme.Orders.Events", "Acme.Orders.WriteModel");

        var count = output.Split("using System;").Length - 1;
        count.Should().Be(1);
    }
}
