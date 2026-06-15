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
    public void GetAggregateForEvent_ReturnsPluralisedAggregateName_WhenApplierFound()
    {
        var file = CreateScannedFile(
            "TestDomain.People.Events",
            ["PersonAdded"],
            [("PersonAdded", "PersonAggregate")]);

        var map = AggregateResolver.BuildEventToAggregateMap([file]);
        var eventRecord = file.EventRecords[0];

        var aggregate = AggregateResolver.GetAggregateForEvent(
            eventRecord, file.SourceNamespace, "TestDomain", map, []);

        aggregate.Should().Be("People");  // "Aggregate" suffix stripped and pluralised
    }

    [Fact]
    public void GetAggregateForEvent_PluralisesAggregateName()
    {
        var cases = new[]
        {
            ("PersonAggregate", "People"),
            ("OrderAggregate", "Orders"),
            ("DoorAggregate", "Doors"),
            ("CompanyAggregate", "Companies"),
            ("BusAggregate", "Buses"),
            ("ChildAggregate", "Children"),
            ("StatusAggregate", "Statuses"),
        };

        foreach (var (input, expected) in cases)
        {
            var file = CreateScannedFile(
                "TestDomain.Test.Events",
                ["TestEvent"],
                [("TestEvent", input)]);

            var map = AggregateResolver.BuildEventToAggregateMap([file]);
            var aggregate = AggregateResolver.GetAggregateForEvent(
                file.EventRecords[0], file.SourceNamespace, "TestDomain", map, []);

            aggregate.Should().Be(expected, $"for input {input}");
        }
    }

    [Fact]
    public void GetAggregateForEvent_PluralisesAggregateWithoutSuffix()
    {
        // Even when the name doesn't end with "Aggregate", it should still be pluralised
        var file = CreateScannedFile(
            "TestDomain.Test.Events",
            ["TestEvent"],
            [("TestEvent", "Door")]);

        var map = AggregateResolver.BuildEventToAggregateMap([file]);
        var aggregate = AggregateResolver.GetAggregateForEvent(
            file.EventRecords[0], file.SourceNamespace, "TestDomain", map, []);

        aggregate.Should().Be("Doors");
    }

    [Fact]
    public void GetAggregateForEvent_FallsBackToClosestAggregateRoot_WhenNoApplierFound()
    {
        var file = CreateScannedFile(
            "TestDomain.Orders.Events",
            ["OrderPlaced"],
            appliers: null);

        var map = AggregateResolver.BuildEventToAggregateMap([file]);
        var eventRecord = file.EventRecords[0];

        // Simulate IAggregateRoot class found in the source assembly
        var aggregateRoots = new[]
        {
            new AggregateRootInfo("OrderAggregate", "TestDomain.Orders"),
            new AggregateRootInfo("PersonAggregate", "TestDomain.People"),
        };

        var aggregate = AggregateResolver.GetAggregateForEvent(
            eventRecord, file.SourceNamespace, "TestDomain", map, aggregateRoots);

        // "TestDomain.Orders.Events" is closest to "TestDomain.Orders" (OrderAggregate)
        aggregate.Should().Be("Orders");  // Pluralised from "Order"
    }

    [Fact]
    public void GetAggregateForEvent_ClosestAggregateRoot_PicksHigherInNamespaceTree()
    {
        var file = CreateScannedFile(
            "TestDomain.Shipping.Tracking.Events",
            ["PackageShipped"],
            appliers: null);

        var map = AggregateResolver.BuildEventToAggregateMap([file]);
        var eventRecord = file.EventRecords[0];

        // Two aggregate roots: one in a deeper namespace, one higher up
        var aggregateRoots = new[]
        {
            new AggregateRootInfo("TrackingAggregate", "TestDomain.Shipping.Tracking.Model"),
            new AggregateRootInfo("ShipmentAggregate", "TestDomain.Shipping"),
        };

        var aggregate = AggregateResolver.GetAggregateForEvent(
            eventRecord, file.SourceNamespace, "TestDomain", map, aggregateRoots);

        // "TestDomain.Shipping" is higher up the namespace tree from "TestDomain.Shipping.Tracking.Events"
        aggregate.Should().Be("Shipments");  // Pluralised from "Shipment"
    }

    [Fact]
    public void GetAggregateForEvent_ClosestAggregateRoot_PrefersDirectParentNamespace()
    {
        var file = CreateScannedFile(
            "TestDomain.Accounts.Events",
            ["AccountCreated"],
            appliers: null);

        var map = AggregateResolver.BuildEventToAggregateMap([file]);
        var eventRecord = file.EventRecords[0];

        var aggregateRoots = new[]
        {
            new AggregateRootInfo("AccountAggregate", "TestDomain.Accounts"),
            new AggregateRootInfo("UserAggregate", "TestDomain.Users"),
        };

        var aggregate = AggregateResolver.GetAggregateForEvent(
            eventRecord, file.SourceNamespace, "TestDomain", map, aggregateRoots);

        // "TestDomain.Accounts" is a direct parent of "TestDomain.Accounts.Events"
        aggregate.Should().Be("Accounts");
    }

    [Fact]
    public void GetAggregateForEvent_FallsBackToNamespace_WhenNoApplierAndNoAggregateRoot()
    {
        var file = CreateScannedFile(
            "TestDomain.Orders.Events",
            ["OrderPlaced"],
            appliers: null);

        var map = AggregateResolver.BuildEventToAggregateMap([file]);
        var eventRecord = file.EventRecords[0];

        var aggregate = AggregateResolver.GetAggregateForEvent(
            eventRecord, file.SourceNamespace, "TestDomain", map, []);

        aggregate.Should().Be("Orders");  // Second segment of namespace (already plural in this case)
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

    [Fact]
    public void GetAggregateForEvent_ClosestAggregateRoot_UsesFirstDiscoveredWhenEquidistant()
    {
        var file = CreateScannedFile(
            "TestDomain.Sales.Events",
            ["SaleCompleted"],
            appliers: null);

        var map = AggregateResolver.BuildEventToAggregateMap([file]);
        var eventRecord = file.EventRecords[0];

        // Two aggregate roots at same distance — should pick first discovered
        var aggregateRoots = new[]
        {
            new AggregateRootInfo("InvoiceAggregate", "TestDomain.Billing"),
            new AggregateRootInfo("SaleAggregate", "TestDomain.Sales"),
        };

        var aggregate = AggregateResolver.GetAggregateForEvent(
            eventRecord, file.SourceNamespace, "TestDomain", map, aggregateRoots);

        // "TestDomain.Sales" is a direct parent namespace of "TestDomain.Sales.Events"
        // while "TestDomain.Billing" is not, so SaleAggregate is closest
        aggregate.Should().Be("Sales");
    }
}
