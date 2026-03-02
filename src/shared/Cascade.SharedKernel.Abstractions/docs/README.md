# Cascade.SharedKernel.Abstractions

Core abstractions and interfaces for the Cascade Event Sourcing framework.

## Overview

This package provides the foundational interfaces and base types used throughout the Cascade framework. It contains no implementation details, making it ideal for inclusion in your domain layer without introducing heavy dependencies.

## Key Interfaces

- **Domain Events**: Base interfaces for domain events in event-sourced systems
- **Aggregate Roots**: Abstractions for aggregate root entities
- **Value Objects**: Base types for immutable value objects
- **Repository Contracts**: Interfaces for event store repositories

## Installation

```bash
dotnet add package Cascade.SharedKernel.Abstractions
```

## Usage

Reference this package in your domain model projects to define your aggregates and events without coupling to infrastructure concerns.

```csharp
using Cascade.SharedKernel;

// Your domain code using Cascade abstractions
```

## Dependencies

This package has no external dependencies, ensuring minimal footprint in your domain layer.

## Related Packages

- **Cascade.SharedKernel**: Concrete implementations of these abstractions
- **Cascade.Commands.Abstractions**: Command handling abstractions
- **Cascade.Views.Abstractions**: Query/view abstractions

## Documentation

For complete documentation, visit: https://github.com/tsgenge/cascade

## License

BSD 3-Clause License - see LICENSE file for details
