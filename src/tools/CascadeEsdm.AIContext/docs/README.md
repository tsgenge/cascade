# CascadeEsdm.AIContext

Provides AI agent context and best practices for projects using the [Cascade ESDM](https://cascade-esdm.org) framework.

## What this package does

On every build, this package writes a `cascade-esdm.md` rules file into your project's AI agent configuration directory:

- **`.windsurf/rules/cascade-esdm.md`** — if a `.windsurf/` directory exists (Windsurf IDE)
- **`.cursor/rules/cascade-esdm.md`** — if a `.cursor/` directory exists (Cursor IDE)
- **`AGENTS.md`** — fallback if neither directory is detected (GitHub Copilot, generic agents)

The file covers Cascade ESDM patterns, composition, commands, events, aggregates, value objects, exceptions, and the Event Extractor. AI agents in your IDE will automatically pick it up as project context.

## Usage

```bash
dotnet add package CascadeEsdm.AIContext
```

No further configuration is required. Add the generated file to source control so all team members and CI agents benefit from the same context.

## Keeping it up to date

The rules file is only written when its content changes (`SkipUnchangedFiles`). Update the package version to pick up revised guidance.
