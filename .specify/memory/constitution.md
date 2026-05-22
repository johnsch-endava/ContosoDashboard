<!--
Sync Impact Report
Version change: template -> 1.0.0
Modified principles:
- Template principle 1 -> I. Spec-First Delivery
- Template principle 2 -> II. Training-Safe Runtime Boundaries
- Template principle 3 -> III. Security-by-Default Training Flow
- Template principle 4 -> IV. Independently Verifiable Slices
- Template principle 5 -> V. Cross-Platform Local Operations
Added sections:
- Operating Constraints
- Delivery Workflow
Removed sections:
- None
Templates requiring updates:
- ✅ .specify/templates/plan-template.md
- ✅ .specify/templates/spec-template.md
- ✅ .specify/templates/tasks-template.md
- ✅ README.md
- ⚠ pending: .specify/templates/commands/*.md (directory not present in this repository)
Follow-up TODOs:
- None
-->

# ContosoDashboard Constitution

## Core Principles

### I. Spec-First Delivery
Every material change to product behavior, architecture, or operating guidance MUST
start with Spec Kit artifacts that describe the user outcome, implementation plan,
and execution tasks before code is treated as complete. Plans and tasks MUST point
to concrete repository paths and MUST be updated when implementation decisions
change. This keeps training exercises auditable and prevents silent drift between
requirements and the delivered application.

### II. Training-Safe Runtime Boundaries
The default developer experience MUST remain local, offline-capable, and safe for
training use. New features MUST run without paid cloud dependencies, MUST keep the
mock authentication model clearly separated from production identity guidance, and
MUST preserve a migration seam for production infrastructure through configuration
or abstractions rather than hard-coded cloud assumptions. Local development MUST
continue to work with SQLite as the default datastore.

### III. Security-by-Default Training Flow
Security controls demonstrated by the training application are non-negotiable.
Protected pages MUST require authorization, service methods that expose or mutate
user-scoped data MUST enforce requester checks, and security-sensitive behavior
MUST not rely on UI-only safeguards. Training-only shortcuts are allowed only when
they are explicit, documented, and do not present themselves as production-ready
security.

### IV. Independently Verifiable Slices
Each feature specification MUST be decomposed into independently testable user
stories with measurable acceptance scenarios. Implementation work MUST preserve a
path to validate the smallest affected slice first, whether through focused manual
checks, targeted tests, or scoped build validation. Quickstart and operator-facing
documentation MUST stay current enough for another contributor to reproduce the
intended flow.

### V. Cross-Platform Local Operations
Local setup and runtime instructions MUST account for the supported development
environments used in training, including Windows and WSL/Linux. Configuration
changes MUST prefer deterministic local defaults, MUST document non-obvious host
differences such as HTTP versus HTTPS behavior, and MUST avoid introducing a
single-platform dependency unless it is optional and clearly isolated.

## Operating Constraints

ContosoDashboard is an ASP.NET Core 10 Blazor Server application intended for
training only. The repository standard stack is Blazor Server, Entity Framework
Core, SQLite for local development, and optional SQL Server configuration for
non-development deployments. Changes MUST preserve the existing layered structure
across Pages, Services, Models, and Data, and MUST update README guidance when
tooling, SDK, database, or startup behavior changes. Production-only concerns may
be described, but the implementation default MUST remain suitable for offline,
single-repository training exercises.

## Delivery Workflow

Contributors MUST begin with a current constitution-aligned spec, then plan, then
tasks before broad implementation. Reviews MUST verify that new work preserves the
training-safe runtime boundary, enforces service-level authorization where needed,
and leaves behind reproducible setup or validation instructions. Before merge,
contributors MUST run at least one executable validation scoped to the touched
surface and MUST record any intentional deviations from the constitution in the
relevant plan or review notes.

## Governance

This constitution supersedes ad hoc local practices for the ContosoDashboard
repository. Amendments require updating this document, adjusting any affected
templates or runtime guidance in the same change, and recording the semantic
version bump rationale in the sync impact report. Versioning follows semantic
rules: MAJOR for incompatible governance changes or principle removals, MINOR for
new principles or materially expanded obligations, and PATCH for clarifications
that do not change contributor duties. Compliance review is required for every
spec, plan, and implementation review; unresolved constitution violations MUST be
explicitly justified before work proceeds.

**Version**: 1.0.0 | **Ratified**: 2026-05-22 | **Last Amended**: 2026-05-22
