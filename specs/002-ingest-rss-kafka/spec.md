# Feature Specification: Single RSS Feed Ingestion to Kafka

**Feature Branch**: `002-ingest-rss-kafka`

**Created**: 2026-06-24

**Status**: Draft

**Input**: User description: "ingest a single RSS feed into a Kafka topic, creating an idempotent collection of feed items processed later, skipping already-ingested items"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ingest New Feed Items Reliably (Priority: P1)

As a reader, I can run ingestion for one configured RSS feed so that newly published items are captured and made available for downstream processing with at-least-once delivery semantics.

**Why this priority**: This is the core business outcome: collecting feed items reliably for later processing while minimizing duplicate work through idempotency safeguards.

**Independent Test**: Can be fully tested by running ingestion against an RSS feed with known items and verifying newly discovered items are emitted for later processing while already ingested items are skipped on best effort.

**Acceptance Scenarios**:

1. **Given** a configured RSS feed and an empty ingested-item collection, **When** ingestion runs, **Then** all currently available feed items are stored as ingested and published to the target Kafka topic for later processing.
2. **Given** a configured RSS feed where some items are already recorded as ingested, **When** ingestion runs again, **Then** only newly discovered items are stored and published, and already-ingested items are skipped.

---

### User Story 2 - Safe Re-Runs After Failures (Priority: P2)

As a reader, I can re-run ingestion after interruptions or transient failures so that ingestion completes with at-least-once delivery while minimizing duplicates.

**Why this priority**: Operational resilience is required for scheduled or manually retried ingestion jobs.

**Independent Test**: Can be fully tested by interrupting or failing an ingestion run, then re-running it and confirming valid items are eventually delivered while duplicate deliveries are tracked and minimized.

**Acceptance Scenarios**:

1. **Given** ingestion started and partially completed, **When** ingestion is re-run, **Then** previously ingested items are skipped and only remaining eligible items are ingested.
2. **Given** a temporary fetch or publish failure, **When** ingestion is retried, **Then** items are eventually published with at-least-once semantics and duplicate publication is reduced by idempotency checks.

---

### User Story 3 - Observable Ingestion Outcome (Priority: P3)

As a reader, I can view ingestion results for each run so that I can confirm how many items were discovered, ingested, skipped, and failed.

**Why this priority**: Visibility improves trust, troubleshooting speed, and operational readiness.

**Independent Test**: Can be fully tested by executing a run with mixed outcomes and verifying that run-level summary data reports discovered, ingested, skipped, and failed counts.

**Acceptance Scenarios**:

1. **Given** an ingestion run completes, **When** the reader checks run results, **Then** a summary is available showing total discovered items, ingested items, skipped already-ingested items, and failures.

### Edge Cases

- The RSS feed is reachable but returns zero items; ingestion completes successfully with zero ingested and zero skipped.
- The RSS feed contains malformed or incomplete items; invalid items are not ingested and are reported as failures without stopping processing of valid items.
- The feed contains items with duplicate identifiers within the same fetch; the system SHOULD publish each logical item once per run, but duplicate delivery is tolerated under at-least-once semantics.
- The same feed item appears with metadata updates but unchanged stable identity; it is treated as already ingested and skipped for this feature scope.
- Publish to Kafka succeeds for some items and fails for others; failed items are retried in a subsequent run and may be delivered more than once, with duplicates handled by idempotency keys.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST support configuration of exactly one RSS feed source for this feature.
- **FR-002**: The system MUST fetch items from the configured RSS feed when ingestion is triggered.
- **FR-003**: The system MUST derive a stable item identity for each feed item and use it to determine whether the item has already been ingested.
- **FR-004**: The system MUST maintain a persistent collection of ingested item identities so prior ingestions are recognized across runs.
- **FR-005**: The system MUST provide at-least-once delivery for newly discovered feed items.
- **FR-006**: The system MUST publish each newly ingested item to the configured Kafka topic for later downstream processing.
- **FR-007**: The system MUST skip publishing and re-collecting items already present in the ingested-item collection.
- **FR-008**: The system MUST allow ingestion to be safely re-run after partial completion or failure, acknowledging that duplicate topic messages may occur under at-least-once delivery.
- **FR-011**: The system MUST include deterministic message keys and stable item identities so downstream consumers can perform idempotent processing.
- **FR-009**: The system MUST produce a run outcome summary containing counts of discovered, ingested, skipped, and failed items.
- **FR-010**: The system MUST continue processing remaining items when a single item fails, unless a terminal feed-level failure prevents further processing.
- **FR-012**: The ingestion pipeline MUST separate the following steps with explicit interfaces: fetch feed content, parse/itemize feed content, and produce feed items to Kafka.
- **FR-013**: The parse/itemize step MUST accept raw feed payload (for example XML text/stream) and return canonical `FeedItem` instances independent of network fetch logic.
- **FR-014**: The produce step MUST accept canonical `FeedItem` instances and publish them to Kafka independent of parsing concerns.
- **FR-015**: The system MUST provide unit tests for parse/itemize behavior using synthetic XML documents and unit/integration tests for produce behavior using canonical `FeedItem` inputs.
- **FR-016**: The fetch step MUST be implemented behind a repository abstraction so feed retrieval can be tested and replaced independently.
- **FR-017**: The parse/itemize step MUST use a strategy pattern so parser behavior can vary by feed format/profile without changing orchestration code.
- **FR-018**: The produce step MUST be implemented behind a repository abstraction so Kafka publishing behavior can be tested and replaced independently.
- **FR-019**: The implementation MUST be split into four C# libraries: consume (RSS), transform (parser/itemizer), produce (Kafka repository), and orchestration.
- **FR-020**: A single Aspire-hosted service MUST compose these four libraries into one executable ingestion pipeline for this feature increment.

### Key Entities *(include if feature involves data)*

- **Feed Source**: Represents the single RSS feed configuration used by ingestion, including location and ingestion enablement state.
- **Feed Item**: Represents one item discovered in the RSS feed, including a stable identity and content metadata needed for downstream processing.
- **Feed Item Ingestion**: Represents a persisted record proving a feed item identity has already been ingested, used for idempotency checks.
- **Feed Ingestion**: Represents per-run operational outcomes, including discovered, ingested, skipped, and failed counts plus run status.

### Architecture Components

- **Consume Library**: Fetches raw RSS payload through repository abstraction.
- **Transform Library**: Parses/itemizes raw payload into canonical `FeedItem` records via strategy pattern.
- **Produce Library**: Publishes canonical `FeedItem` messages to Kafka through repository abstraction.
- **Orchestration Library**: Coordinates consume -> transform -> produce flow, idempotency checks, and run-summary accounting.
- **Aspire Node**: Single host node that runs the orchestration pipeline and wires dependencies.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of valid newly discovered items are eventually delivered to Kafka (at-least-once).
- **SC-002**: For a stable feed with no new items, repeated ingestion runs produce zero new ingested records and expose duplicate-delivery counts when retries occur.
- **SC-003**: For feeds that contain new items, at least 99% of valid newly discovered items are available for downstream processing within one ingestion run.
- **SC-004**: Readers can confirm ingestion outcome from run summary data within 2 minutes of run completion.

## Assumptions

- Ingestion is triggered by an existing scheduler or manual run mechanism outside this feature.
- A Kafka topic for downstream processing already exists and is accessible.
- Delivery guarantee for Kafka publication is at-least-once, not exactly-once.
- Feed item identity can be derived from stable RSS item fields that are consistently present for target feeds.
- This feature scope covers a single feed only; multi-feed orchestration is out of scope.
- Downstream processing semantics are out of scope; this feature ends once new items are published for later processing.

## Testing Strategy

- **Fetch step tests**: Validate retrieval behavior, error handling, and timeout/retry logic independently from parsing.
- **Parse/itemize tests**: Provide synthetic RSS XML payloads directly to the parser and verify canonical itemization, identity derivation prerequisites, and malformed-entry handling.
- **Produce tests**: Provide canonical `FeedItem` instances directly to producer logic and validate key generation, payload mapping, and Kafka publish invocation/ack handling.
- **Cross-step integration tests**: Validate end-to-end behavior and run summary counts while preserving at-least-once semantics.
- **Pattern conformance tests**: Verify fetch and produce repositories are consumed via interfaces, and parser strategies are selected through strategy abstraction rather than conditional branching in orchestration.
