# Cascade ESDM

An opinionated C# framework for building **Event Sourced Domain Model** systems — without building the framework yourself.

> *"In the companies I've worked in over the last 10 years I've not seen a single one implement ESDM or even event sourcing. It's just CRUD-based entity obsession and all the quadratic complexity and regret this generates."*
> — [cascade-esdm.org](https://cascade-esdm.org)

---

## The Problem

ESDM is one of the most powerful approaches to system architecture. Events as the source of truth. Commands that express intent. Aggregates that protect invariants. Read models built from facts, not guesses.

The problem is the framework. To implement ESDM correctly you need opinions on dozens of decisions: how commands are dispatched, how aggregates are hydrated, how events are stored, how concurrency is handled, how read models are projected. Every team that tries makes different mistakes, usually without realising they are mistakes until they're baked in.

Cascade removes those decisions. Engineers implement commands, emit events, add policies and build view projections. The framework handles everything else.

---

## Features

- ** AI Agent Friendly Structure ** - implement tightly focused ICommandExecutors and IEventAppliers. Packages providing agent context markdown for common providers.
- ** All the benefits of ESDM ** - traceability, temporal queries, strong bounded context. A balance of cohesion and loose coupling.
- ** Out of the box event extraction ** - extract events from your write model into a clean, publishable events assembly.
- ** Clear direction for inexperienced teams ** - ESDM is hard. Let us do the heavy lifting, while you implement (or orchestrate AI!) behaviour.

## What's in the Box

### Core packages

| Package | Description |
|---|---|
| `CascadeEsdm.SharedKernel.Abstractions` | Core interfaces — `IDomainEvent`, `IAggregateRoot`, value object contracts |
| `CascadeEsdm.SharedKernel` | Base implementations for aggregates, value objects, and shared kernel types |
| `CascadeEsdm.WriteModel.Abstractions` | Write-side interfaces — `ICommand`, `ICommandExecutor`, `ICommandEnvelope`, `IEventApplier` |
| `CascadeEsdm.WriteModel` | Command dispatch, aggregate hydration, event stream writing, concurrency, MSBuild integration |
| `CascadeEsdm.ReadModel.Abstractions` | Read-side interfaces for projections and queries - `IViewProjector`, `IView` |
| `CascadeEsdm.ReadModel` | Read model implementations |

### Infrastructure packages

| Package | Description |
|---|---|
| `CascadeEsdm.Storage.CosmosDb` | Azure Cosmos DB event stream and read model storage |
| `CascadeEsdm.DistributedLocks` | Azure Storage distributed lock provider for aggregate-level concurrency |
| `CascadeEsdm.Logging.OpenTelemetry` | OpenTelemetry-based structured logging and Application Insights integration |
| `CascadeEsdm.SignalR` | Azure SignalR real-time view change notifications |

### Tools

| Package | Description |
|---|---|
| `CascadeEsdm.EventExtractor` | Pre-build tool that extracts `IDomainEvent` records from your write model into a clean, publishable events assembly |
| `CascadeEsdm.AIContext` | Installs AI agent context and best practices into your IDE's rules directory on build |

---

## AI Agent Context

Cascade ships a package that gives AI agents in your IDE (Windsurf, Cursor, GitHub Copilot) automatic context about the framework — patterns, conventions, composition, exceptions, and the event extractor.

```bash
dotnet add package CascadeEsdm.AIContext
```

On your next build, a `cascade-esdm.md` rules file is written into your project's AI agent configuration directory:

- **`.devin/rules/`** — Windsurf
- **`.cursor/rules/`** — Cursor
- **`AGENTS.md`** — fallback for other agents

Add the generated file to source control so all team members and CI agents share the same context. The file is only updated when the package version changes.

---

## Example project
We've put together an example project to show how to use Cascade in a real-world scenario. Well, real world ish.

https://github.com/tsgenge/cascade-example

---

## Design Principles

**Opinionated by intent.** Cascade has opinions so your engineers don't need to. The right decisions are already made — concurrency strategy, hydration, command dispatch, event storage.

**Technology Abstraction.** Azure today, something else tomorrow. Storage and lock providers are pluggable. The domain code doesn't change.

**Engineers focus on function.** Write commands, emit events, build projections. The framework handles the rest.

**Cohesion over unnecessary abstraction.** Events and their appliers live together. The extractor publishes only what belongs in the contract. No artificial splits to satisfy infrastructure concerns.

---

## Status

Alpha — initial release Q2 2026. The core write model and infrastructure packages are stable. The event extractor is in active development.

Packages are available on [NuGet](https://www.nuget.org/packages?q=cascadeesdm).

---

## Further Reading

- [cascade-esdm.org](https://cascade-esdm.org) — the thinking behind the framework
- [docs/BoundedContexts.md](docs/BoundedContexts.md) — bounded context conventions and what they are
- [docs/Aggregates.md](docs/Aggregates.md) — aggregate conventions and folder structure
- [docs/WriteModel.md](docs/WriteModel.md) — creating and configuring aggregates
- [docs/ReadModel.md](docs/ReadModel.md) — creating and configuring views in the read model
- [docs/Commands.md](docs/Commands.md) — command and executor conventions
- [docs/Events.md](docs/Events.md) — event and applier conventions
- [docs/Entities.md](docs/Entities.md) — entity conventions and folder structure
- [docs/ValueObjects.md](docs/ValueObjects.md) — value object conventions
- [docs/Policies.md](docs/Policies.md) — reactive policies triggered by domain events
- [docs/CompositionUsage.md](docs/CompositionUsage.md) — composition and registration patterns
- [docs/EventExtractor.md](docs/EventExtractor.md) — the event extractor in detail
- [docs/Exceptions.md](docs/Exceptions.md) — exception handling conventions

---

## Contributing

Issues and discussions welcome via [GitHub](https://github.com/tsgenge/cascade). Pull requests considered — open an issue first to discuss intent.

---

*Copyright © Tim Genge / Mindfish 2026. BSD-3-Clause.*
