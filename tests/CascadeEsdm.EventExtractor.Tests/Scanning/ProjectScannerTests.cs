using CascadeEsdm.EventExtractor.Scanning;
using FluentAssertions;

namespace CascadeEsdm.EventExtractor.Tests.Scanning;

public class ProjectScannerTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public ProjectScannerTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private string Write(string relativePath, string content)
    {
        var full = Path.Combine(_tempDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
        return full;
    }

    [Fact]
    public void FindEventFiles_ReturnsFile_ContainingIDomainEventRecord()
    {
        Write("Events/OrderPlaced.cs", """
            using CascadeEsdm.SharedKernel.Events;
            namespace Acme.Orders.Events;
            public record OrderPlaced(Guid Id) : IDomainEvent;
            """);

        var result = ProjectScanner.FindEventFiles(_tempDir);

        result.Should().ContainSingle()
              .Which.SourceNamespace.Should().Be("Acme.Orders.Events");
    }

    [Fact]
    public void FindEventFiles_IgnoresFile_WithNoIDomainEventRecord()
    {
        Write("Commands/PlaceOrder.cs", """
            namespace Acme.Orders.Commands;
            public record PlaceOrder(Guid Id);
            """);

        ProjectScanner.FindEventFiles(_tempDir).Should().BeEmpty();
    }

    [Fact]
    public void FindEventFiles_CapturesApplierClasses_InSameFile()
    {
        Write("Events/OrderPlaced.cs", """
            using CascadeEsdm.SharedKernel.Events;
            using CascadeEsdm.WriteModel.Hydration;
            namespace Acme.Orders.Events;
            public record OrderPlaced(Guid Id) : IDomainEvent;
            internal class OrderPlacedApplier : IEventApplier<OrderPlaced, OrderAggregate>
            {
                public void Apply(OrderAggregate a, OrderPlaced e, EventEnvelope env) { }
            }
            """);

        var file = ProjectScanner.FindEventFiles(_tempDir).Single();

        file.ApplierClasses.Should().ContainSingle()
            .Which.Identifier.Text.Should().Be("OrderPlacedApplier");
    }

    [Fact]
    public void FindEventFiles_CapturesEnums_InSameFile()
    {
        Write("Events/OrderPlaced.cs", """
            using CascadeEsdm.SharedKernel.Events;
            namespace Acme.Orders.Events;
            public enum OrderStatus { Placed, Fulfilled }
            public record OrderPlaced(OrderStatus Status) : IDomainEvent;
            """);

        var file = ProjectScanner.FindEventFiles(_tempDir).Single();

        file.EnumDeclarations.Should().ContainSingle()
            .Which.Identifier.Text.Should().Be("OrderStatus");
    }

    [Fact]
    public void FindEventFiles_ScansSubdirectoriesRecursively()
    {
        Write("Aggregate/Events/OrderPlaced.cs", """
            using CascadeEsdm.SharedKernel.Events;
            namespace Acme.Orders.Aggregate.Events;
            public record OrderPlaced(Guid Id) : IDomainEvent;
            """);

        ProjectScanner.FindEventFiles(_tempDir).Should().ContainSingle();
    }
}
