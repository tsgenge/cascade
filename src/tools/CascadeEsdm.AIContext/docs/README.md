# CascadeEsdm.AIContext

Provides AI agent context and best practices for projects using the [Cascade ESDM](https://cascade-esdm.org) framework.

## What this package does

When the package is added or restored, this package writes a `cascade-esdm.md` rules file into your repository's AI agent configuration directory. The file is only written once — if it already exists it will not be overwritten, allowing you to customise it freely after installation:

- **`.windsurf/rules/cascade-esdm.md`** — if a `.windsurf/` directory exists (Windsurf IDE)
- **`.devin/rules/cascade-esdm.md`** — if a `.devin/` directory exists (Devin IDE)
- **`.cursor/rules/cascade-esdm.md`** — if a `.cursor/` directory exists (Cursor IDE)
- **`AGENTS.md`** — fallback if neither directory is detected (GitHub Copilot, generic agents)

The file covers Cascade ESDM patterns, composition, commands, events, aggregates, value objects, exceptions, and the Event Extractor. AI agents in your IDE will automatically pick it up as project context.

## Installation

> **Important:** This package must be added to a project that is part of a solution (`.sln` or `.slnx`), and the solution must be opened from its file when building. The package uses MSBuild's `$(SolutionDir)` to locate the repository root where your `.windsurf/`, `.cursor/`, or `.devin/` directories reside. If built outside of a solution context, the rules file will be written relative to the project directory instead.

Add the package to one project in your solution — typically your API, host, or a dedicated tooling project:

```bash
dotnet add package CascadeEsdm.AIContext
```

Then build the solution:

```bash
dotnet build MySolution.sln
```

Add the generated rules file to source control so all team members and CI agents benefit from the same context.

## Keeping it up to date

The rules file is never overwritten once it exists. To pick up revised guidance from a newer package version, delete the generated file and run `dotnet restore` again.
