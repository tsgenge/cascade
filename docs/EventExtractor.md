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
3. **Rewrites** namespaces from your write-model root (e.g. `Acme.Orders.WriteModel`) to an events root (e.g. `Acme.Orders.Events`)
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

### 2. Add `CascadeEsdm.WriteModel` to your write-model project

The MSBuild targets are bundled in the `CascadeEsdm.WriteModel` NuGet package and activate automatically. No further configuration is required for a default setup.

### 3. Build

On the next build, the extractor runs before compilation and writes the events project alongside your write-model project:

```
MyApp.WriteModel/
MyApp.Events/           ← generated
  MyApp.Events.csproj
  Orders/
    Events/
      OrderPlaced.cs
      OrderFulfilled.cs
  Enums/
    OrderStatus.cs
```

Add `MyApp.Events/` to source control. Add it to your solution. Maybe build and publish to your own private nuget feed. Reference it from consumer projects.

---

## Configurable Properties

Set these in your write-model project's `<PropertyGroup>`:

| Property | Default | Description |
|---|---|---|
| `CascadeEventsEnabled` | `true` | Set to `false` to disable extraction entirely |
| `CascadeEventsOutputDir` | `$(MSBuildProjectDirectory)\..\AssemblyName.Events` | Where the generated project is written |
| `CascadeEventsAssemblyName` | RootNamespace with write-model suffix stripped, + `.Events` | Assembly name of the generated project |
| `CascadeEventsNamespace` | Same as `CascadeEventsAssemblyName` | Root namespace used in generated files |
| `CascadeEventsOverwrite` | `false` | When `true`, regenerates all files on every build; the `.csproj` is still never overwritten |
| `CascadeEventsRequireExtractor` | `false` | When `true`, a missing tool is a build error instead of a warning |

### Assembly name defaulting

If `CascadeEventsAssemblyName` is not set, the tool strips a recognised write-model suffix from `RootNamespace` and appends `.Events`:

| `RootNamespace` | Resolved assembly name |
|---|---|
| `Acme.Orders.WriteModel` | `Acme.Orders.Events` |
| `Acme.Orders.Domain` | `Acme.Orders.Events` |
| `Acme.Orders.Write` | `Acme.Orders.Events` |
| `Acme.Orders.Application` | `Acme.Orders.Events` |
| `Acme.Orders` | `Acme.Orders.Events` |

---

## What Gets Extracted

### Event records

Any `record` whose base list contains `IDomainEvent` is included:

```csharp
// write-model source — Acme.Orders.WriteModel.Orders.Events
public record OrderPlaced(Guid OrderId, string Reference, OrderStatus Status) : IDomainEvent;
```

Becomes in the events assembly:

```csharp
// generated — Acme.Orders.Events.Orders.Events
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

### What is stripped

- `IEventApplier<TEvent, TAggregate>` classes
- `using` directives for write-model-only namespaces:
  - `CascadeEsdm.WriteModel.Hydration`
  - `CascadeEsdm.WriteModel.CommandHandling`
  - `CascadeEsdm.WriteModel.Security`
  - `CascadeEsdm.WriteModel.Composition`
  - `CascadeEsdm.WriteModel.EventStream`

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
    <AssemblyName>Acme.Orders.Events</AssemblyName>
    <RootNamespace>Acme.Orders.Events</RootNamespace>
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
  --output-dir       "/path/to/MyApp.Events" \
  --root-namespace   "Acme.Orders.WriteModel" \
  --assembly-name    "Acme.Orders.Events" \
  --events-namespace "Acme.Orders.Events" \
  --overwrite        false
```

| Flag | Required | Description |
|---|---|---|
| `--source-root` | Yes | Root directory of the source project to scan |
| `--output-dir` | Yes | Directory to write the generated events project into |
| `--root-namespace` | Yes | `RootNamespace` of the source project |
| `--assembly-name` | No | Override the generated assembly name |
| `--events-namespace` | No | Override the root namespace in generated files |
| `--overwrite` | No | Overwrite existing source files (default: `false`) |
