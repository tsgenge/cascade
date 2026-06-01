using CascadeEsdm.EventExtractor.Scanning;
using FluentAssertions;

namespace CascadeEsdm.EventExtractor.Tests.Scanning;

public class EnumDependencyScannerTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public EnumDependencyScannerTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private void Write(string relativePath, string content)
    {
        var full = Path.Combine(_tempDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }

    [Fact]
    public void FindExternalEnums_ReturnsEnum_DefinedInNonEventFile()
    {
        Write("Domain/OrderStatus.cs", """
            namespace Acme.Orders.Domain;
            public enum OrderStatus { Placed, Fulfilled }
            """);

        Write("Events/OrderPlaced.cs", """
            using CascadeEsdm.SharedKernel.Events;
            namespace Acme.Orders.Events;
            public record OrderPlaced(OrderStatus Status) : IDomainEvent;
            """);

        var eventFiles = ProjectScanner.FindEventFiles(_tempDir);
        var result = EnumDependencyScanner.FindExternalEnums(_tempDir, eventFiles);

        result.Should().ContainSingle()
              .Which.EnumName.Should().Be("OrderStatus");
    }

    [Fact]
    public void FindExternalEnums_DoesNotReturn_EnumAlreadyInEventFile()
    {
        Write("Events/OrderPlaced.cs", """
            using CascadeEsdm.SharedKernel.Events;
            namespace Acme.Orders.Events;
            public enum OrderStatus { Placed, Fulfilled }
            public record OrderPlaced(OrderStatus Status) : IDomainEvent;
            """);

        var eventFiles = ProjectScanner.FindEventFiles(_tempDir);
        var result = EnumDependencyScanner.FindExternalEnums(_tempDir, eventFiles);

        result.Should().BeEmpty();
    }

    [Fact]
    public void FindExternalEnums_ReturnsEmpty_WhenNoEnumsReferenced()
    {
        Write("Events/OrderPlaced.cs", """
            using CascadeEsdm.SharedKernel.Events;
            namespace Acme.Orders.Events;
            public record OrderPlaced(Guid Id, string Name) : IDomainEvent;
            """);

        var eventFiles = ProjectScanner.FindEventFiles(_tempDir);
        var result = EnumDependencyScanner.FindExternalEnums(_tempDir, eventFiles);

        result.Should().BeEmpty();
    }

    [Fact]
    public void FindExternalEnums_CapturesSourceNamespace_OfEnumFile()
    {
        Write("Domain/OrderStatus.cs", """
            namespace Acme.Orders.Domain;
            public enum OrderStatus { Placed }
            """);

        Write("Events/OrderPlaced.cs", """
            using CascadeEsdm.SharedKernel.Events;
            namespace Acme.Orders.Events;
            public record OrderPlaced(OrderStatus Status) : IDomainEvent;
            """);

        var eventFiles = ProjectScanner.FindEventFiles(_tempDir);
        var result = EnumDependencyScanner.FindExternalEnums(_tempDir, eventFiles);

        result.Single().SourceNamespace.Should().Be("Acme.Orders.Domain");
    }
}
