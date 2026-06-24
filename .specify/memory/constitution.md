# EisenFeed Constitution

## Core Principles

### I. Priority-Driven Evaluation (NON-NEGOTIABLE)
EisenFeed MUST evaluate every feed item using the Eisenhower Matrix (Urgent vs. Important).
All scoring logic MUST be transparent, testable, and explainable. Every item's quadrant position
MUST be derivable from explicit urgency and importance values. Opaque scoring is forbidden.

### II. Dynamic Content Aging
Scores decay over time according to explicit aging rules. Urgency decays quickly unless reinforced;
importance decays slowly or not at all for evergreen/VIP sources. Aging MUST be deterministic and
configurable per feed modifier. Temporal changes in quadrant position MUST be predictable and
observable through audit trails.

### III. Composable Feed Modifiers
Feed characteristics (owned, VIP, emergency, entertainment) MUST influence scoring via composable,
independently testable modifiers. Modifiers MUST NOT hardcode business logic; they MUST only adjust
coefficients. Adding a new modifier MUST NOT require changes to core scoring logic.

### IV. Integration-Ready Architecture
The core domain/library MUST remain framework-agnostic. Infrastructure dependencies MUST be
isolated behind interfaces and adapters, with dependencies flowing toward core abstractions.
Every subsystem (ingestion, scoring, aging, persistence, execution/output) MUST expose a clear,
swappable interface. EchoDrop integration MUST work via data contracts, not procedural coupling.
Feed items MUST serialize/deserialize losslessly. Storage backends (SQL, cache, file) MUST be
swappable.

### V. Testable Scoring (MANDATORY)
Scoring changes MUST be accompanied by test cases that document expected behavior before
implementation. Scoring disputes MUST be resolved by reference to test coverage and audit trails.
Deterministic business logic (including scoring, aging, and routing policy evaluation) MUST be
unit-testable without I/O. Non-deterministic scoring is forbidden. All numeric changes to
urgency/importance MUST include rationale in code comments or documentation.

### VI. CTP Pipeline Architecture (MANDATORY)
All operations MUST follow a consume-transform-produce (CTP) pattern. For feeds, consume from
external sources and produce to Kafka. For internal processing, consume from Kafka and produce to
Kafka. For external interfaces (for example EchoDrop), consume from Kafka and produce to the
external API. Core business logic MUST live in the transform step. External systems MUST only be
accessed at system edges. Kafka is the default internal handoff mechanism, and direct
component-to-component calls SHOULD be avoided when Kafka-based handoff is appropriate.

## Design Constraints

- **Technology Stack**: .NET 10+, layered architecture (ingestion → scoring → persistence → output)
- **Feed Item Contract**: Immutable core (id, source, timestamp); mutable scores (urgency, importance, modifiers)
- **Scoring Range**: All scores normalized to [0, 1] range
- **Modifier Application**: Multiplicative only; no addition to base scores
- **Data Persistence**: ACID transactions required for feed item mutations
- **Policy Configuration**: Weights, thresholds, decay, routing, and retention policies SHOULD be
  configuration-driven when practical
- **Internal Handoff**: Kafka is the default internal transport between processing stages
- **Contract Evolution**: Public contracts MUST evolve additively and avoid unnecessary breaking changes
- **Repository Layout**: Production code MUST live under `src/` and tests MUST live under `tst/`

## Development Workflow

1. **Specification First**: Before coding a new subsystem or modifier, specify its interface and scoring behavior
2. **Test-Driven Implementation**: Tests written → User approval → Test fails → Implement → Refactor → Mutation Testing
3. **Code Review Gate**: All PRs MUST verify principle compliance (especially priority-driven evaluation, composability, CTP boundaries, and testability)
4. **Breaking Changes**: Scoring changes affecting quadrant classification or public contracts MUST be versioned and documented
5. **Observability**: Audit trails required for all score changes; logging MUST capture before/after states

## Governance

This constitution supersedes all other development practices and architectural guidance. Changes to
principles require ratification by the project lead and MUST include migration paths for affected code.

All PRs and pull requests MUST verify compliance with Core Principles before merge. Architecture review
MUST confirm that new features maintain composability, CTP boundaries, and testability. Scoring logic
changes MUST include regression test suites.

Amendments follow semantic versioning:
- **MAJOR**: Principle removal or redefinition (requires migration plan)
- **MINOR**: New principle or expanded guidance
- **PATCH**: Clarifications, wording, non-semantic refinements

**Version**: 1.2.1 | **Ratified**: 2026-05-19 | **Last Amended**: 2026-06-24
