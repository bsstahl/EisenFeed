# Research: Single RSS Feed Ingestion to Kafka

## Decision 1: Stable Feed Item Identity

- Decision: Use a deterministic identity key `FeedId + ItemId`, where `ItemId` is derived from RSS item GUID when present, otherwise canonical link, and finally a normalized title+published fallback.
- Rationale: Ensures repeatable identity across reruns and supports idempotent skip behavior required by FR-003 through FR-008.
- Alternatives considered:
  - Hash full payload: rejected because harmless content changes would break identity stability.
  - Timestamp-only identity: rejected because collisions and feed edits make it unreliable.

## Decision 2: Idempotency Persistence Strategy

- Decision: Persist ingested item identities in an ACID-backed store with a uniqueness constraint on `(FeedId, ItemId)`.
- Rationale: Uniqueness constraint guarantees at-most-one ingestion record for a logical feed item across retries and reruns.
- Alternatives considered:
  - In-memory deduplication only: rejected because it fails across process restarts.
  - Kafka-compacted topic as source of truth: rejected for now due to added operational complexity for first increment.

## Decision 3: Kafka Message Contract and Partitioning

- Decision: Publish canonical `feed-item-ingested` messages to one Kafka topic using `FeedId:ItemId` as message key.
- Rationale: Stable keys improve downstream dedupe and partition consistency while preserving order per item identity under at-least-once delivery.
- Alternatives considered:
  - Null Kafka key: rejected because partitioning would be non-deterministic and downstream dedupe harder.
  - Separate topics per feed: rejected because scope is single feed and one topic is sufficient.

## Decision 4: Partial Failure and Retry Semantics

- Decision: Continue processing when an individual item fails and report per-run counters (`discovered`, `ingested`, `skipped`, `failed`); retries reprocess only items without ingested records.
- Rationale: Matches FR-009 and FR-010 while preserving throughput and operator visibility.
- Alternatives considered:
  - Fail-fast on first item error: rejected because one malformed item should not block valid items.
  - Best-effort logging without counters: rejected because it weakens observability and acceptance testability.

## Decision 6: Delivery Guarantee

- Decision: Use at-least-once Kafka delivery as the explicit guarantee.
- Rationale: Exactly-once end-to-end delivery is operationally complex; this feature prioritizes reliable eventual delivery with deterministic keys for downstream idempotency.
- Alternatives considered:
  - Exactly-once delivery: rejected for this increment due to higher implementation and operational complexity.

## Decision 5: Test Strategy

- Decision: Add unit tests for identity derivation/idempotent skipping and integration tests for persistence plus Kafka publish behavior under `tst/`.
- Rationale: Verifies the most failure-prone logic (identity and retries) and aligns with constitution testability mandates.
- Alternatives considered:
  - Unit tests only: rejected because idempotency guarantees depend on persistence semantics.
  - Integration tests only: rejected because pinpointing identity-rule defects becomes harder.

## Decision 7: Pipeline Stage Separation

- Decision: Split ingestion into three explicit stages: fetch, parse/itemize, and produce.
- Rationale: Stage boundaries allow highly focused tests, including synthetic XML parser tests and producer tests driven by canonical `FeedItem` inputs.
- Alternatives considered:
  - Single monolithic ingestion method: rejected because it makes parser and producer testing brittle and highly coupled.

## Decision 8: Architectural Patterns per Stage

- Decision: Use repository pattern for fetch and produce stages, and strategy pattern for parse/itemize stage.
- Rationale: Repositories isolate external I/O concerns and make fetch/produce mockable; parser strategies allow controlled variation in parsing behavior while keeping orchestration stable.
- Alternatives considered:
  - Use repository for all stages: rejected because parser variation is behavioral and better modeled as strategies.
  - Use strategy for fetch/produce: rejected because these stages are integration boundaries and better represented as repositories.

## Decision 9: Packaging and Hosting Model

- Decision: Package the first CTP feature as four separate C# libraries (consume, transform, produce, orchestration) and run them through a single Aspire-hosted ingestion service.
- Rationale: Library boundaries enforce clear responsibilities and test isolation, while one node keeps operational complexity low for the first increment.
- Alternatives considered:
  - Single library with folders only: rejected because package-level boundaries improve ownership and deployment clarity.
  - Multiple Aspire nodes for each stage: rejected for this increment due to unnecessary operational complexity.
