# Event Extractor

## Why It Exists

In an event-sourced system, domain events are the shared language between bounded contexts. They are the facts other systems subscribe to — not commands, not aggregates, not internal state.

A common problem is **write-model leakage**: the events assembly consumers reference starts pulling in write-model concerns — command handlers, appliers, hydration logic, infrastructure dependencies. The consumer now has a transitive dependency on your internal domain machinery.

The alternative — duplicating event definitions into a separate project by hand — works initially but drifts. Two copies of the same event diverge silently. The write model moves on; the published contract doesn't.

The `CascadeEsdm.EventExtractor` solves this by treating your write-model source as the **single source of truth** and generating the events assembly automatically at build time. You write your events once, in context, alongside their appliers. The extractor lifts only the publishable parts — the event records — into a clean, dependency-light assembly.

---

## What It Does

At pre-build time the tool:

1. **Scans** all `.cs` files under your project root for `record` types implementing `IDomainEvent`
2. **Strips** write-model-only concerns from each file — `IEventApplier` classes, and `using` directives for write-model namespaces
3. **Rewrites** namespaces from your write-model root (e.g. `Acme.Orders.WriteModel`) to a schema root (e.g. `Acme.Orders.Schema`)
4. **Resolves** any external enum dependencies referenced by event records but defined in non-event files, and copies them into an `Enums/` subfolder
5. **Generates** a standalone `.csproj` referencing only `CascadeEsdm.SharedKernel.Abstractions` (on first run; never overwritten thereafter)
6. **Reports** what was found and written to stdout

The result is a compilable events-only project your consumers can reference without pulling in any write-model code.

---

## Setup

### 1. Install the tool

```bash
dotnet tool install -g CascadeEsdm.EventExtractor
```

### 2. Add `CascadeEsdm.WriteModel.Abstractions` to your write-model project

The MSBuild targets are bundled in the `CascadeEsdm.WriteModel.Abstractions` NuGet package and activate automatically. No further configuration is required for a default setup.

### 3. Build

On the next build, the extractor runs before compilation and writes the events project alongside your write-model project:

```
MyApp.WriteModel/
MyApp.Schema/           ← generated
  MyApp.Schema.csproj
  Orders/
    Events/
      OrderPlaced.cs
      OrderFulfilled.cs
  Enums/
    OrderStatus.cs
```

Add `MyApp.Schema/` to source control. Add it to your solution. Maybe build and publish to your own private nuget feed. Reference it from consumer projects.

---

## Configurable Properties

Set these in your write-model project's `<PropertyGroup>`:

| Property | Default | Description |
|---|---|---|
| `CascadeEventsEnabled` | `true` | Set to `false` to disable extraction entirely |
| `CascadeEventsOutputDir` | `$(MSBuildProjectDirectory)\..\AssemblyName.Schema` | Where the generated project is written |
| `CascadeEventsAssemblyName` | RootNamespace with write-model suffix stripped, + `.Schema` | Assembly name of the generated project |
| `CascadeEventsOverwrite` | `false` | When `true`, regenerates all files on every build; the `.csproj` is still never overwritten |
| `CascadeEventsRequireExtractor` | `false` | When `true`, a missing tool is a build error instead of a warning |

### Assembly name defaulting

If `CascadeEventsAssemblyName` is not set, the tool strips a recognised write-model suffix from `RootNamespace` and appends `.Schema`:

| `RootNamespace` | Resolved assembly name |
|---|---|
| `Acme.Orders.WriteModel` | `Acme.Orders.Schema` |
| `Acme.Orders.Domain` | `Acme.Orders.Schema` |
| `Acme.Orders.Write` | `Acme.Orders.Schema` |
| `Acme.Orders.Application` | `Acme.Orders.Schema` |
| `Acme.Orders` | `Acme.Orders.Schema` |

> **Important:** The root namespace of generated files is always set equal to the resolved assembly name. The two cannot differ. This invariant is required for `$type`-based deserialisation — see [Service Bus serialisation](#service-bus-serialisation) below.
>
> If you override `CascadeEventsAssemblyName`, ensure your consumer's `SchemaTypeNameMapper` will see the same name. When in doubt, rely on the default.

---

## Service Bus Serialisation

When publishing an `EventEnvelope` to a service bus topic, the `IDomainEvent` stored in `Event` must carry a `$type` discriminator that consumers can resolve without access to the write-model assembly.

`DefaultSerialisationSettings.ForMessageBus()` provides serialiser options that rewrite the `$type` from the write-model identity to the schema assembly identity automatically — no configuration required:

```csharp
var options = DefaultSerialisationSettings.ForMessageBus();
var json = JsonSerializer.Serialize(envelope, options);
```

Given a write-model event `Acme.Orders.WriteModel.Orders.Events.OrderPlaced` in assembly `Acme.Orders.WriteModel`, the emitted `$type` will be:

```
Acme.Orders.Schema.Orders.Events.OrderPlaced, Acme.Orders.Schema
```

This is exactly what the schema assembly contains. A consumer that references `Acme.Orders.Schema` and uses the same `ForMessageBus()` options (or `UsingTypeQualifiedName()` with the schema assembly loaded) can deserialise the envelope without any additional wiring.

### How the mapping works

`SchemaTypeNameMapper` applies the same deterministic suffix-strip rule as the extractor to both the namespace prefix and the assembly component of the `$type` string:

1. Strip the recognised write-model suffix from the assembly name (`.WriteModel`, `.Domain`, `.Write`, `.Application`) and append `.Schema`
2. Replace the matching namespace prefix in the fully-qualified type name with the new assembly name

Because the rule is derived entirely from the type itself, the publisher needs no knowledge of the schema project — there is no configuration to keep in sync.

### Constraint

This mapping relies on the schema assembly name and root namespace being identical. The extractor enforces this: the root namespace of generated files always equals the resolved assembly name and cannot be overridden independently. If you override `CascadeEventsAssemblyName`, the same name must be used as the root namespace of the generated project (which the extractor sets automatically).

---

## What Gets Extracted

### Event records

Any `record` whose base list contains `IDomainEvent` is included:

```csharp
// write-model source — Acme.Orders.WriteModel.Orders.Events
public record OrderPlaced(Guid OrderId, string Reference, OrderStatus Status) : IDomainEvent;
```

Becomes in the schema assembly:

```csharp
// generated — Acme.Orders.Schema.Orders.Events
public record OrderPlaced(Guid OrderId, string Reference, OrderStatus Status) : IDomainEvent;
```

### Inheritance

The scanner is syntactic — it does not resolve types. A record is included **only if `IDomainEvent` appears literally in its own base list**. If you use a base record hierarchy, every level that should be extracted must declare `IDomainEvent` directly:

```csharp
// ✅ extracted — IDomainEvent is in the base list
public abstract record OrderEventBase(Guid OrderId) : IDomainEvent;

// ✅ extracted — IDomainEvent is in the base list
public record OrderPlaced(Guid OrderId, string Reference) : IDomainEvent;

// ❌ not extracted — IDomainEvent is not in the base list, only OrderEventBase is
public record OrderPlaced(Guid OrderId, string Reference) : OrderEventBase(OrderId);
```

If you want derived records extracted, either keep `IDomainEvent` on each record, or flatten the hierarchy — base record properties can be composed into each event directly.

### Co-located enums

Enums defined in the same file as event records are included verbatim.

### External enum dependencies

Enums referenced by event records but defined elsewhere in the project are detected and copied into an `Enums/` subfolder under the events project root, placed in a `<TargetRootNamespace>.Enums` namespace.

### Non-primitive parameter types

The extractor only copies enums automatically. Classes, records, structs, and interfaces referenced in event parameters are **not copied** — doing so would risk pulling in arbitrarily deep type graphs, including types from high-level assemblies that have no place in a minimal events contract.

The recommended approach is to **use primitives wherever possible** in event records:

```csharp
// ✅ prefer — portable, no external dependencies
public record OrderPlaced(Guid OrderId, string Reference, int StatusCode) : IDomainEvent;

// ⚠️ works but requires manual wiring — consumers must also reference the type
public record SecurityDescriptorSet(MySecurityDescriptor Descriptor) : IDomainEvent;
```

If a non-primitive type is genuinely part of the public event contract, add a reference to the assembly that defines it directly in the generated `.csproj`. Because the `.csproj` is never overwritten, this addition is stable across rebuilds:

```xml
<ItemGroup>
  <PackageReference Include="Acme.Shared.Contracts" Version="1.0.0" />
</ItemGroup>
```

### What is stripped

- `IEventApplier<TEvent, TAggregate>` classes
- `using` directives for write-model-only namespaces:
  - `CascadeEsdm.WriteModel.Hydration`
  - `CascadeEsdm.WriteModel.CommandHandling`
  - `CascadeEsdm.WriteModel.Security`
  - `CascadeEsdm.WriteModel.Composition`
  - `CascadeEsdm.WriteModel.EventStream`

### What is always added

- `using System;` — guaranteed present in every generated file regardless of whether the source includes it

---

## Cohesion vs Abstraction

The core tension this tool resolves is between two legitimate pressures:

**Cohesion** says: keep the event record and its applier together. The `PersonAdded` record and `PersonAddedApplier` belong side by side — they describe the same fact and its effect. Splitting them into separate projects fragments understanding and makes navigation harder.

**Abstraction** says: consumers should depend on the minimal contract. A read-model projection handler that subscribes to `OrderPlaced` should not compile against your command handlers, your aggregate hydration logic, or your CosmosDB infrastructure.

Without tooling you are forced to choose: either accept write-model leakage into the published contract, or split your event definitions out manually and maintain two copies.

The extractor removes the choice. **Write everything in one place, publish only what belongs in the contract.** The events assembly is generated, not authored — there is no second copy to drift.

---

## The Generated Project

On first run a `.csproj` is created with a single dependency:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>Acme.Orders.Schema</AssemblyName>
    <RootNamespace>Acme.Orders.Schema</RootNamespace>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="CascadeEsdm.SharedKernel.Abstractions" Version="*" />
  </ItemGroup>

</Project>
```

The `.csproj` is **never overwritten** on subsequent builds regardless of `CascadeEventsOverwrite`. This lets you pin the version, add additional references, or adjust the target framework without them being stomped. Source files are only rewritten when their content has changed.

---

## Missing Tool Behaviour

If `cascade-extract-events` is not installed:

- By default a **build warning** is emitted and extraction is skipped — your project still builds
- Set `CascadeEventsRequireExtractor=true` to promote this to a **build error**

```xml
<PropertyGroup>
  <CascadeEventsRequireExtractor>true</CascadeEventsRequireExtractor>
</PropertyGroup>
```

---

## Running Manually

The tool can be invoked directly outside of MSBuild:

```bash
cascade-extract-events \
  --source-root      "/path/to/MyApp.WriteModel" \
  --output-dir       "/path/to/MyApp.Schema" \
  --root-namespace   "Acme.Orders.WriteModel" \
  --assembly-name    "Acme.Orders.Schema" \
  --overwrite        false
```

| Flag | Required | Description |
|---|---|---|
| `--source-root` | Yes | Root directory of the source project to scan |
| `--output-dir` | Yes | Directory to write the generated events project into |
| `--root-namespace` | Yes | `RootNamespace` of the source project |
| `--assembly-name` | No | Override the generated assembly name |
| `--overwrite` | No | Overwrite existing source files (default: `false`) |
