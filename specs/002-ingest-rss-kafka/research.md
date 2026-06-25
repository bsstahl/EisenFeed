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
- Rationale: Matches FR-009 and FR-010 while preserving throughput and operational visibility.
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

- Decision: Split ingestion into three explicit stages: retrieve/canonicalize, transform, and produce.
- Rationale: Stage boundaries allow highly focused tests, including source-adapter retrieval tests, canonical transformation tests, and producer tests driven by canonical `FeedItem` inputs.
- Alternatives considered:
  - Single monolithic ingestion method: rejected because it makes parser and producer testing brittle and highly coupled.

## Decision 8: Architectural Patterns per Stage

- Decision: Use repository pattern for retrieve and produce stages, and strategy pattern for transform stage.
- Rationale: Repositories isolate external I/O concerns and make retrieve/produce mockable; transform strategies allow controlled variation in canonical item processing behavior while keeping orchestration stable.
- Responsibility split: Transform owns canonical-to-canonical data shaping; Orchestration owns stage sequencing, idempotency checks, retry behavior, stop/continue decisions, and run accounting.
- Alternatives considered:
  - Use repository for all stages: rejected because transform variation is behavioral and better modeled as strategies.
  - Use strategy for retrieve/produce: rejected because these stages are integration boundaries and better represented as repositories.

## Decision 9: Packaging and Hosting Model

- Decision: Package the first CTP feature as four separate C# libraries (consume, transform, produce, orchestration) and run them through a single Aspire-hosted ingestion service.
- Rationale: Library boundaries enforce clear responsibilities and test isolation, while one node keeps operational complexity low for the first increment.
- Alternatives considered:
  - Single library with folders only: rejected because package-level boundaries improve ownership and deployment clarity.
  - Multiple Aspire nodes for each stage: rejected for this increment due to unnecessary operational complexity.

## Decision 10: Dual-Write Consistency Strategy (Kafka + Idempotency Store)

- Decision: Use per-item processing with explicit publish-state transitions in the idempotency store:
  - record publish attempt state,
  - publish one item to Kafka,
  - confirm publish state with Kafka metadata.
- Rationale: Cross-system transactional commit is not available between Kafka and the ACID idempotency store. A per-item workflow bounds inconsistency and duplicate exposure to at most one in-flight item rather than a whole feed batch.
- Operational rules:
  - Process items one-at-a-time for produce plus idempotency confirmation.
  - Publish to Kafka before marking the item fully published to preserve at-least-once semantics.
  - If confirmation write fails after Kafka publish, stop the run and surface failure for controlled retry.
- Alternatives considered:
  - Batch publish then batch DB confirmation: rejected because confirmation failure can duplicate an entire feed on retry.
  - Kafka-only idempotency with no store: rejected because pre-publish existence checks and durable run accounting are not reliable enough.
  - Exactly-once cross-system transactions: rejected for this increment due to high implementation and operational complexity.

---

## Future Enhancements (Beyond MVP)

### FE-001: Content Fingerprinting for Feeds Without Consistent Item Identity

- **Problem**: Some RSS feeds lack stable item identities (missing GUID, reused links, unstable titles). Current `FeedIdItemId` strategy is insufficient for these feeds, leading to duplicate ingestion or missed items on retries.
- **Proposed Solution**: Introduce a pluggable identity strategy system with support for `ContentFingerprint` strategy alongside the current `FeedIdItemId` strategy.
  - Compute a deterministic content hash (e.g., SHA-256 of normalized title + content) for identity when GUID/link are unreliable.
  - Make strategy selection configurable per feed or auto-detected based on feed characteristics.
  - Maintain backward compatibility by keeping `FeedIdItemId` as the default strategy.
- **Expected Benefits**:
  - Handles low-quality feeds that change item metadata arbitrarily.
  - Reduces duplicate ingestion in unreliable feed sources.
  - Extensible for future identity strategies (e.g., author+date fingerprint).
- **Considerations**:
  - Content fingerprinting is slower than GUID-based identity; may require batching or caching.
  - Collisions (different items with identical normalized content) are rare but possible; requires monitoring.
  - Migration path needed for existing feeds switching identity strategies.

### FE-002: LLM-Based Normalization for Unparseable Feed Items

- **Problem**: Some feed items contain malformed XML, encoding errors, or non-standard structures that fail to parse into canonical `FeedItem` objects. Current error handling skips these items entirely, losing potentially valuable content.
- **Proposed Solution**: Introduce a [Behavioral Layer](https://cognitiveinheritance.com/Posts/introducing-the-behavioral-layer.html) pattern with optional LLM-based item recovery and normalization.
  - When an item fails to parse, capture the raw content and error context.
  - Route to an LLM service (e.g., Azure OpenAI) with a prompt to attempt structured extraction of Title, Content, PublishedAt.
  - If LLM succeeds, synthesize a `FeedItem` with LLM-extracted fields and a `normalized: true` metadata flag.
  - If LLM fails or is disabled, fall back to current skip/error behavior.
  - Track recovery success rate and confidence score from LLM for observability.
- **Expected Benefits**:
  - Recovers content from malformed but semantically parseable items.
  - Reduces loss of feed data due to encoding or structural irregularities.
  - Enables feed sources with legacy or non-compliant XML to be ingested reliably.
- **Considerations**:
  - LLM calls introduce latency and cost; requires careful rate-limiting and caching of results.
  - Behavioral Layer must be optional and degradable; system works without it.
  - Confidence scoring needed to prevent propagation of low-quality LLM extractions downstream.
  - Privacy and data residency concerns if feeding raw content to external LLM services; consider on-device models.
  - Testing: synthetic malformed XML fixtures needed to validate recovery behavior and confidence thresholds.
