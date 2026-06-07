---
trigger: always_on
---

# Documentation Organization

Cascade ESDM documentation is maintained across five locations. Each has a distinct audience and purpose. When adding or updating documentation, place content in the correct location(s) and ensure consistency across them.

## 1. Root `README.md`

**Audience:** Anyone landing on the GitHub project for the first time.
**Purpose:** Welcome page — high-level overview, package table, quick start, design principles, links to detailed docs.
**Guidelines:**
- Keep concise and scannable
- Link to `/docs/` for detailed breakouts rather than inlining long content
- Include installation and minimal usage examples
- Reference the AIContext package for AI agent integration

## 2. Per-package `docs/README.md`

**Audience:** NuGet consumers browsing the package on nuget.org.
**Purpose:** Package-specific overview shown on the NuGet gallery page.
**Guidelines:**
- Title must match the NuGet package name (e.g. `CascadeEsdm.WriteModel`, not legacy names)
- Keep to a short, stable summary: package name, one-liner purpose, install command, link to the GitHub project
- Do NOT duplicate detailed documentation here — the root README and `/docs/` are the canonical sources
- These should rarely need updating, reducing maintenance burden

## 3. `/docs/` directory

**Audience:** Developers adopting or using the framework.
**Purpose:** Detailed breakout documentation referenced from the root README — conventions, patterns, tool usage.
**Guidelines:**
- One file per major topic (composition, event extractor, aggregates, commands, events, value objects, exceptions)
- Include code examples, naming conventions, folder structures, and detailed guidance
- These are the canonical source of truth for consumer-facing conventions
- Root README links to these files in the "Further Reading" section

## 4. `AIContext/ai-context/` directory

**Audience:** AI agents (Copilot, Cursor, Windsurf, Devin) working in projects that consume the framework.
**Purpose:** Machine-readable context file installed into consumer repos via the `CascadeEsdm.AIContext` NuGet package.
**Guidelines:**
- Content must be self-contained — AI agents will not follow links to `/docs`
- Cover all key patterns: composition, commands, events, aggregates, value objects, exceptions, event extractor, code style
- Keep examples concise but complete enough for an agent to generate correct code
- Mirror the conventions from `/docs/` but in a denser, more prescriptive format
- Changes here affect all consumers on next package update

## 5. `.devin/rules/` directory

**Audience:** AI agents (Devin) maintaining the Cascade ESDM framework itself.
**Purpose:** Rules and conventions specific to developing and maintaining this repository.
**Guidelines:**
- Only include rules relevant to maintaining the framework codebase
- Do NOT include consumer-facing conventions (aggregates, commands, events, value objects) — those belong in `/docs/` and `AIContext/ai-context/`
- Examples of appropriate content: code style rules, PR conventions, CI/CD guidelines, documentation organization
- Use frontmatter triggers (`always_on`, `glob`) to control when rules are activated

## Content Distribution Matrix

Primary maintenance effort is on the root README, `/docs/`, and `AIContext/ai-context/`. Per-package READMEs are stable summaries that rarely change.

| Topic | Root README | Package README | /docs/ | AIContext | .devin/rules/ |
|---|---|---|---|---|---|
| Framework overview | brief | — | — | brief | — |
| Package descriptions | table | stable summary | — | table | — |
| Quick start / composition | minimal | — | detailed | detailed | — |
| Aggregate conventions | — | — | detailed | detailed | — |
| Command conventions | — | — | detailed | detailed | — |
| Event conventions | — | — | detailed | detailed | — |
| Value object conventions | — | — | detailed | detailed | — |
| Exception conventions | — | — | detailed | detailed | — |
| Event extractor | summary | — | detailed | summary | — |
| Code style | — | — | — | yes | yes |
| CI/CD and workflows | — | — | — | — | if needed |
| Documentation organization | — | — | — | — | yes |
