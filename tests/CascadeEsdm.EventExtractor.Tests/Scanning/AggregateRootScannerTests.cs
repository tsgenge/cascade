using CascadeEsdm.EventExtractor.Scanning;
using FluentAssertions;

namespace CascadeEsdm.EventExtractor.Tests.Scanning;

public class AggregateRootScannerTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public AggregateRootScannerTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private void Write(string relativePath, string content)
    {
        var full = Path.Combine(_tempDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }

    [Fact]
    public void FindAggregateRoots_FindsClassImplementingIAggregateRoot()
    {
        Write("Orders/OrderAggregate.cs", """
            using CascadeEsdm.SharedKernel.Aggregates;
            namespace Acme.Orders;
            public class OrderAggregate : IAggregateRoot
            {
                public Guid Id { get; set; }
                public int LastSequence { get; set; }
            }
            """);

        var result = AggregateRootScanner.FindAggregateRoots(_tempDir);

        result.Should().ContainSingle()
              .Which.Should().BeEquivalentTo(
                  new AggregateRootInfo("OrderAggregate", "Acme.Orders"));
    }

    [Fact]
    public void FindAggregateRoots_IgnoresClassesWithoutIAggregateRoot()
    {
        Write("Commands/PlaceOrder.cs", """
            namespace Acme.Orders.Commands;
            public class PlaceOrder { }
            """);

        AggregateRootScanner.FindAggregateRoots(_tempDir).Should().BeEmpty();
    }

    [Fact]
    public void FindAggregateRoots_FindsMultipleAggregateRoots()
    {
        Write("Orders/OrderAggregate.cs", """
            using CascadeEsdm.SharedKernel.Aggregates;
            namespace Acme.Orders;
            public class OrderAggregate : IAggregateRoot
            {
                public Guid Id { get; set; }
                public int LastSequence { get; set; }
            }
            """);

        Write("People/PersonAggregate.cs", """
            using CascadeEsdm.SharedKernel.Aggregates;
            namespace Acme.People;
            public class PersonAggregate : IAggregateRoot
            {
                public Guid Id { get; set; }
                public int LastSequence { get; set; }
            }
            """);

        var result = AggregateRootScanner.FindAggregateRoots(_tempDir);

        result.Should().HaveCount(2);
        result.Should().Contain(r => r.ClassName == "OrderAggregate" && r.Namespace == "Acme.Orders");
        result.Should().Contain(r => r.ClassName == "PersonAggregate" && r.Namespace == "Acme.People");
    }

    [Fact]
    public void FindAggregateRoots_SkipsFilesWithoutIAggregateRootText()
    {
        // This file has no "IAggregateRoot" text so it should be skipped by the pre-filter
        Write("Events/OrderPlaced.cs", """
            namespace Acme.Orders.Events;
            public record OrderPlaced(Guid Id);
            """);

        AggregateRootScanner.FindAggregateRoots(_tempDir).Should().BeEmpty();
    }

    [Fact]
    public void FindAggregateRoots_HandlesQualifiedIAggregateRoot()
    {
        Write("Orders/OrderAggregate.cs", """
            namespace Acme.Orders;
            public class OrderAggregate : CascadeEsdm.SharedKernel.Aggregates.IAggregateRoot
            {
                public Guid Id { get; set; }
                public int LastSequence { get; set; }
            }
            """);

        var result = AggregateRootScanner.FindAggregateRoots(_tempDir);

        result.Should().ContainSingle()
              .Which.ClassName.Should().Be("OrderAggregate");
    }
}
