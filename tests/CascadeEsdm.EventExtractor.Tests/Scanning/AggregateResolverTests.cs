using CascadeEsdm.EventExtractor.Scanning;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CascadeEsdm.EventExtractor.Tests.Scanning;

public class AggregateResolverTests
{
    private static ScannedEventFile CreateScannedFile(
        string sourceNamespace,
        string[] eventNames,
        (string EventName, string AggregateName)[]? appliers = null)
    {
        var usings = SyntaxFactory.List<UsingDirectiveSyntax>([
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("CascadeEsdm.SharedKernel"))
        ]);

        var namespaceDecl = SyntaxFactory.FileScopedNamespaceDeclaration(
            SyntaxFactory.ParseName(sourceNamespace));

        var members = new List<MemberDeclarationSyntax>();

        foreach (var eventName in eventNames)
        {
            var recordDecl = SyntaxFactory.RecordDeclaration(
                SyntaxFactory.Token(SyntaxKind.RecordKeyword),
                eventName)
                .WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                    SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("IDomainEvent")))));
            members.Add(recordDecl);
        }

        if (appliers != null)
        {
            foreach (var (eventName, aggregateName) in appliers)
            {
                var applierDecl = SyntaxFactory.ClassDeclaration($"{eventName}Applier")
                    .WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                        SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(
                            $"IEventApplier<{eventName}, {aggregateName}>")))));
                members.Add(applierDecl);
            }
        }

        var unit = SyntaxFactory.CompilationUnit()
            .WithUsings(usings)
            .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                namespaceDecl.WithMembers(SyntaxFactory.List(members))));

        return new ScannedEventFile(
            FilePath: $"/test/{sourceNamespace.Replace('.', '/')}/Events.cs",
            SourceNamespace: sourceNamespace,
            SyntaxRoot: unit,
            EventRecords: members.OfType<RecordDeclarationSyntax>().ToList(),
            EnumDeclarations: [],
            ApplierClasses: members.OfType<ClassDeclarationSyntax>().ToList());
    }

    [Fact]
    public void BuildEventToAggregateMap_ExtractsEventAndAggregateFromApplier()
    {
        var file = CreateScannedFile(
            "TestDomain.People.Events",
            ["PersonAdded", "PersonRemoved"],
            [("PersonAdded", "PersonAggregate"), ("PersonRemoved", "PersonAggregate")]);

        var map = AggregateResolver.BuildEventToAggregateMap([file]);

        map.Should().ContainKey("PersonAdded").WhoseValue.Should().Be("PersonAggregate");
        map.Should().ContainKey("PersonRemoved").WhoseValue.Should().Be("PersonAggregate");
    }

    [Fact]
    public void BuildEventToAggregateMap_HandlesDifferentAggregates()
    {
        var file = CreateScannedFile(
            "TestDomain.Orders.Events",
            ["OrderPlaced", "PaymentReceived"],
            [("OrderPlaced", "OrderAggregate"), ("PaymentReceived", "PaymentAggregate")]);

        var map = AggregateResolver.BuildEventToAggregateMap([file]);

        map["OrderPlaced"].Should().Be("OrderAggregate");
        map["PaymentReceived"].Should().Be("PaymentAggregate");
    }

    [Fact]
    public void GetAggregateForEvent_ReturnsAggregateFromMap_WhenApplierFound()
    {
        var file = CreateScannedFile(
            "TestDomain.People.Events",
            ["PersonAdded"],
            [("PersonAdded", "PersonAggregate")]);

        var map = AggregateResolver.BuildEventToAggregateMap([file]);
        var eventRecord = file.EventRecords[0];

        var aggregate = AggregateResolver.GetAggregateForEvent(
            eventRecord, file.SourceNamespace, "TestDomain", map);

        aggregate.Should().Be("Person");  // "Aggregate" suffix stripped
    }

    [Fact]
    public void GetAggregateForEvent_StripsAggregateSuffix()
    {
        var cases = new[]
        {
            ("PersonAggregate", "Person"),
            ("OrderAggregate", "Order"),
            ("SomeEntityAggregate", "SomeEntity"),
            ("NoSuffix", "NoSuffix"),
        };

        foreach (var (input, expected) in cases)
        {
            var file = CreateScannedFile(
                "TestDomain.Test.Events",
                ["TestEvent"],
                [("TestEvent", input)]);

            var map = AggregateResolver.BuildEventToAggregateMap([file]);
            var aggregate = AggregateResolver.GetAggregateForEvent(
                file.EventRecords[0], file.SourceNamespace, "TestDomain", map);

            aggregate.Should().Be(expected, $"for input {input}");
        }
    }

    [Fact]
    public void GetAggregateForEvent_FallsBackToNamespace_WhenNoApplierFound()
    {
        var file = CreateScannedFile(
            "TestDomain.Orders.Events",
            ["OrderPlaced"],
            appliers: null);  // No appliers

        var map = AggregateResolver.BuildEventToAggregateMap([file]);
        var eventRecord = file.EventRecords[0];

        var aggregate = AggregateResolver.GetAggregateForEvent(
            eventRecord, file.SourceNamespace, "TestDomain", map);

        aggregate.Should().Be("Orders");  // Second segment of namespace
    }

    [Fact]
    public void GetAggregateForEvent_HandlesQualifiedIEventApplier()
    {
        // Some code uses fully qualified names like WriteModel.Hydration.IEventApplier<TEvent, TAggregate>
        var source = """
            using CascadeEsdm.SharedKernel;
            namespace TestDomain.Orders.Events;
            public record OrderPlaced : IDomainEvent;
            public class OrderPlacedApplier : CascadeEsdm.WriteModel.Hydration.IEventApplier<OrderPlaced, OrderAggregate>
            {
                public void Apply(OrderAggregate aggregate, OrderPlaced @event, EventEnvelope envelope) { }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source, path: "/test/Events.cs");
        var root = tree.GetCompilationUnitRoot();

        var namespaceDecl = root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().First();
        var eventRecords = root.DescendantNodes().OfType<RecordDeclarationSyntax>().ToList();
        var applierClasses = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();

        var file = new ScannedEventFile(
            "/test/Events.cs",
            namespaceDecl.Name.ToString(),
            root,
            eventRecords,
            [],
            applierClasses);

        var map = AggregateResolver.BuildEventToAggregateMap([file]);

        map.Should().ContainKey("OrderPlaced").WhoseValue.Should().Be("OrderAggregate");
    }
}
