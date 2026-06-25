# Implementation Plan: Single RSS Feed Ingestion to Kafka

**Branch**: `002-ingest-rss-kafka` | **Date**: 2026-06-24 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-ingest-rss-kafka/spec.md`

## Summary

Ingest one configured RSS feed and publish newly discovered feed items to a Kafka topic using
at-least-once delivery semantics. The approach is to derive a stable item identity, persist
ingestion state with a uniqueness constraint for best-effort skip behavior, and publish a canonical
feed-item message contract to Kafka using the item identity as the message key for downstream
idempotent processing. Because Kafka plus idempotency store writes are a dual-write boundary,
produce plus idempotency confirmation is processed per item to bound duplicate exposure.

## Technical Context

**Language/Version**: C# / .NET 10

**Primary Dependencies**: `System.ServiceModel.Syndication` for RSS parsing, `Confluent.Kafka` for Kafka producer, existing EisenFeed.Core abstractions

**Storage**: Persistent idempotency store for ingested identities (ACID-backed; concrete provider added in implementation)

**Testing**: xUnit for unit tests by pipeline step (fetch repository, parse strategy, produce repository); integration tests for at-least-once publish behavior and duplicate handling under `tst/`

**Target Platform**: .NET worker/service process on Windows/Linux

**Project Type**: Backend ingestion pipeline composed of four libraries plus one Aspire-hosted service

**Performance Goals**: Complete a single-feed ingestion run within 30 seconds for feeds up to 5,000 items; duplicate detection O(1) by key lookup/constraint

**Constraints**: At-least-once delivery guarantee; best-effort duplicate prevention; explicit fetch/parse/produce separation; repository pattern for fetch/produce; strategy pattern for parser; continue-on-item-failure; Kafka handoff contract stability; dual-write guardrails with per-item publish/confirm sequencing; production code under `src/`, tests under `tst/`

**Scale/Scope**: Single feed source, one Kafka topic, one ingestion pipeline with four internal libraries; multi-feed orchestration out of scope

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Priority-Driven Evaluation**: PASS. This feature is ingestion-only and does not alter scoring semantics.
- **Dynamic Content Aging**: PASS. No aging behavior is introduced or modified.
- **Composable Feed Modifiers**: PASS. No modifier behavior is introduced or modified.
- **Integration-Ready Architecture**: PASS. Design keeps transport and persistence behind abstractions and contracts.
- **Testable Scoring**: PASS. No scoring logic is changed; ingestion logic will be tested in isolation.
- **CTP Pipeline Architecture**: PASS. Feed items are consumed from RSS and produced to Kafka for downstream stages.
- **Repository Layout**: PASS. Planned source paths are under `src/`; tests under `tst/`.

## Project Structure

### Documentation (this feature)

```text
specs/002-ingest-rss-kafka/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── EisenFeed.Core/
│   ├── FeedIngestion/
│   ├── DataPersistence/
│   └── Models/
├── EisenFeed.Ingestion.Consume.Rss/
│   ├── IReadRssFeeds.cs
│   └── FeedRepository.cs
├── EisenFeed.Ingestion.Transform.Parser/
│   ├── IFeedParserStrategy.cs
│   ├── RssXmlParserStrategy.cs
│   └── FeedParserStrategySelector.cs
├── EisenFeed.Ingestion.Produce.Kafka/
│   ├── IWriteFeedItems.cs
│   ├── FeedRepository.cs
│   ├── FeedIdItemIdMessageMapper.cs
│   └── FeedKafkaMessage.cs
├── EisenFeed.Ingestion.Orchestration/
│   ├── IngestionOrchestrator.cs
│   └── RunSummaryBuilder.cs
├── EisenFeed.Ingestion.Service/
│   └── Program.cs
└── EisenFeed.AppHost/

tst/
├── EisenFeed.Core.Tests/
└── EisenFeed.Ingestion.Tests/
 ├── Consume/
 ├── Transform/
 ├── Produce/
 ├── Orchestration/
 └── Integration/
```

**Structure Decision**: Implement CTP as four separate libraries (`Consume.Rss`, `Transform.Parser`,
`Produce.Kafka`, `Orchestration`) and host them in a single Aspire service project (`EisenFeed.Ingestion.Service`).
Place all tests under `tst/` with per-library suites and integration coverage.

Pattern Decision: Use repository abstractions for fetch and produce boundaries and strategy abstractions
for parse/itemize behavior so each stage is independently testable and replaceable.

## Phase 0: Research Output

See [research.md](./research.md) for finalized decisions on identity derivation, at-least-once delivery,
idempotency strategy, Kafka contract shape, and retry behavior.

## Phase 1: Design Output

- Data model: [data-model.md](./data-model.md)
- Contracts: [contracts/kafka-feed-item-ingested.md](./contracts/kafka-feed-item-ingested.md)
- Quickstart: [quickstart.md](./quickstart.md)

## Constitution Check (Post-Design)

- **Priority-Driven Evaluation**: PASS (unchanged scoring semantics)
- **Dynamic Content Aging**: PASS (unchanged aging semantics)
- **Composable Feed Modifiers**: PASS (unchanged modifier semantics)
- **Integration-Ready Architecture**: PASS (adapter interfaces and message contract isolated)
- **Testable Scoring**: PASS (scoring untouched; ingestion tests isolated)
- **CTP Pipeline Architecture**: PASS (RSS consume -> Kafka produce with explicit handoff)
- **Repository Layout**: PASS (`src/` and `tst/` respected)

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | N/A |
