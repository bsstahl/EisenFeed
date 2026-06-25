# EisenFeed Ubiquitous Language

**Purpose**: Canonical definitions for all terms used in code, documentation, tests, and conversation.  
**Status**: Draft — please edit and approve before terms are locked into implementation.

---

## Domain Concepts

### User

The account-level actor in EisenFeed. A User may hold one or more roles depending on context.

### Persona

The human perspective used to frame product behavior and user stories. Persona language is
separate from role/permission language.

### Reader

The primary persona for EisenFeed. A Reader consumes feed-driven information and relies on
ingestion outcomes to review what is new, skipped, or failed.

### Role

A responsibility or permission context assigned to a User (for example run operator, admin, or
analyst). Roles are implementation and access concerns, not primary persona labels.

### Eisenhower Matrix

The two-axis prioritization framework that underpins all scoring. Every feed item is placed into one
of four quadrants by combining its Urgency score and Importance score.

### Urgency

A normalized score [0, 1] representing how time-sensitive a feed item is. Urgency decays quickly
unless reinforced by a modifier or a re-scoring event.

### Importance

A normalized score [0, 1] representing how aligned a feed item is with the reader's goals,
interests, or trusted sources. Importance decays slowly or not at all for evergreen or VIP content.

### Score / FeedScore

The pair of (Urgency, Importance) values attached to a FeedItem. Scores are always in the range
[0, 1]. Scores may only be adjusted multiplicatively (never by addition to the base value).

### Quadrant

The classification derived from a FeedItem's Score pair:

- **Q1** — Urgent & Important — act now
- **Q2** — Not Urgent & Important — act later
- **Q3** — Urgent & Not Important — optional
- **Q4** — Not Urgent & Not Important — ignore or auto-filter

### Content Aging

The deterministic, time-based process by which a FeedItem's Urgency and/or Importance scores decay
over time according to rules configured per Feed Modifier. Aging is never stochastic.

### Feed Modifier

A composable coefficient applied multiplicatively to a FeedItem's Score during scoring and aging.
Modifiers express the character of the feed source (e.g. VIP, Owned, Emergency, Entertainment).
They MUST NOT encode domain logic — they only adjust coefficients.

---

## Feed & Item Concepts

### Feed / RSS Feed

An external RSS or Atom source from which EisenFeed ingests content. Identified by a `FeedId` and
a `Url`.

### Feed Source

The configuration record for one Feed: `FeedId`, `Url`, enabled/disabled state, and last
successful ingestion timestamp. One logical source per unique `FeedId`.

### Feed Item / FeedItem

A single article, post, or entry parsed from a Feed. Each FeedItem carries both feed-level data
(the FeedId that identifies its source) and item-level data (ItemId, PublishedAt, Title, Content).
Scores (Urgency, Importance) are tracked separately and are mutable. In code: `FeedItem`.

### Feed Item Identity / ItemId

A stable, deterministic string that uniquely identifies one logical FeedItem within a Feed.
Derived in priority order: RSS GUID → canonical link → normalized title + published-date fallback.
Never changes for the same logical item regardless of metadata edits.

### Canonical Feed Item

A `FeedItem` instance that has been fully normalized and identity-stamped, ready for idempotency
checking, scoring, or publication. The output of the Parse step.

---

## Ingestion Pipeline

### Ingestion

The end-to-end process of fetching a Feed, parsing it into FeedItems, checking each item for
prior ingestion, and publishing new items to the downstream topic.

### Ingestion Run

One execution of the ingestion pipeline for a single Feed. Produces a FeedIngestion.

### Fetch / Fetch Step

The **Consume** stage of the CTP pipeline for the Ingestion Service. Retrieves the raw Feed
payload (RSS/XML) from the external source. Implemented via a Fetch Repository.

### Parse / Parse Step

The **Transform** stage of the CTP pipeline for the Ingestion Service. Accepts a raw Feed payload
and emits a collection of Canonical Feed Items — one per entry in the feed. Each FeedItem produced
carries both feed-level data (FeedId) and item-level data (ItemId, Title, PublishedAt, Content).
Implemented via a Parser Strategy.

### Produce / Produce Step

The **Produce** stage of the CTP pipeline for the Ingestion Service. Publishes Canonical Feed Items
to the downstream Kafka Topic as Feed Item Ingested messages. Implemented via a Produce Repository.

### Orchestration / Orchestrator

The component that sequences Fetch → Parse → idempotency check → Produce, accumulates run counters,
and returns the Feed Ingestion summary. Does not perform I/O directly.

### CTP Pipeline / Consume-Transform-Produce

The mandatory architectural pattern for all EisenFeed operations. External consumption happens at
the Consume edge. Business logic lives in Transform. Publication to downstream systems happens at
the Produce edge. Direct component-to-component calls bypass this pattern and are forbidden.

---

## Idempotency

### Idempotency Check

The lookup that determines whether a FeedItem has already been ingested in a prior run. Uses the
(FeedId, ItemId) pair as the key.

### Feed Item Ingestion / FeedItemIngestion

The persisted record that proves a specific (FeedId, ItemId) has been ingested. Carries the RunId
and the Kafka message key used at publication time. Backed by an ACID store with a uniqueness
constraint on (FeedId, ItemId).

### Idempotency Store

The ACID-backed persistence layer that stores Feed Item Ingestions and enforces the uniqueness
constraint. The concrete provider is swappable.

### Publish State

The state tracked per item in the Idempotency Store to support recovery across the Kafka plus
database dual-write boundary. States are `Publishing`, `Published`, and `Failed`.

### Dual Write

The operation boundary where one logical ingestion action writes to Kafka and to the Idempotency
Store. Since this cannot be made transactionally atomic across systems in this increment, failures
are managed with per-item sequencing and publish-state transitions.

### Per-Item Produce Sequencing

The required workflow for produce and idempotency confirmation:

1. Write/update item state to `Publishing`.
2. Publish one item to Kafka.
3. Confirm item state as `Published` with Kafka metadata.

If step 3 fails after Kafka publish, the run must stop and report failure so retries have bounded
duplicate exposure.

### Skip

The outcome for a FeedItem that is already present in the Idempotency Store. Skipped items are
counted but not re-published.

### At-Least-Once Delivery

The explicit delivery guarantee for Kafka publication in this feature. Items will be delivered at
least once; duplicate delivery under retry is tolerated and handled downstream via stable Message
Keys.

---

## Kafka Concepts

### Topic / Kafka Topic

The Kafka destination for Feed Item Ingested messages. Name is configurable; default is
`feed-items-ingested`.

### Message Key

The string `<FeedId>:<ItemId>` used as the Kafka partition key. Stable across retries; supports
downstream deduplication.

### Feed Item Ingested Message / `feed-item-ingested`

The Kafka message produced for each newly ingested FeedItem. Schema includes `schemaVersion`,
`eventType`, `runId`, `occurredAt`, `feedId`, `itemId`, `publishedAt`, `title`, and `content`.

### Delivery Result / ProduceDeliveryResult

The outcome record from one Produce Stage invocation: AttemptedCount, DeliveredCount, FailedCount.

---

## Run Accounting

### Feed Ingestion / FeedIngestion

The summary of one Ingestion Run: RunId, FeedId, start/completion timestamps, status, and
discovery/ingestion/skip/failure counts. Invariant: `DiscoveredCount = IngestedCount + SkippedCount + FailedCount`.

### Discovered

A FeedItem that was returned by the Fetch Stage for a given run, before idempotency check.

### Ingested

A FeedItem that passed the idempotency check and was successfully published in this run.

### Skipped

A FeedItem that failed the idempotency check (already Ingested in a prior run) and was not
re-published.

### Failed

A FeedItem that was not successfully ingested or published in this run due to an error. Eligible
for retry in a subsequent run.

### Run Status

The terminal state of an Ingestion Run:

- **Succeeded** — FailedCount = 0
- **PartiallySucceeded** — FailedCount > 0 and IngestedCount > 0
- **Failed** — terminal feed-level failure prevented meaningful progress

---

## Architectural Patterns

### Fetch Repository / IReadRssFeeds

The repository abstraction over the Fetch Stage. Decouples HTTP/network concerns from orchestration
and parsing. Concrete implementation: `FeedRepository` in `EisenFeed.Ingestion.Consume.Rss`.

### Parser Strategy / IFeedParserStrategy

The strategy abstraction for the Parse Step. A single strategy accepts a raw Feed payload and a
FeedId and returns Canonical Feed Items. Concrete implementation: `RssXmlParserStrategy`.

### Parser Strategy Selector / FeedParserStrategySelector

The component that selects the appropriate Parser Strategy for a given content type. Throws
`NotSupportedException` for unknown content types.

### Produce Repository / IWriteFeedItems

The repository abstraction over the Produce Stage. Decouples Kafka client concerns from
orchestration logic. Concrete implementation: `FeedRepository` in `EisenFeed.Ingestion.Produce.Kafka`.

---

## Integration & External Systems

### EchoDrop

The external publishing and scheduling system that consumes high-value FeedItems from EisenFeed.
Integration is via data contracts through the CTP pipeline, not procedural coupling.

### Downstream Consumer / Downstream Processor

Any system or service that consumes messages from the Kafka Topic after EisenFeed publishes them.
Expected to handle at-least-once delivery via the stable Message Key.

---

## Project & Code Structure

### EisenFeed.Core

The framework-agnostic domain library. Contains the canonical `FeedItem`, `FeedScore`, and
interface definitions for all subsystems. Has no infrastructure dependencies.

### EisenFeed.Ingestion.Consume.Rss

The library implementing the Fetch Stage. Depends only on `EisenFeed.Core`.

### EisenFeed.Ingestion.Transform.Parser

The library implementing the Parse Step (the Transform stage of the CTP pipeline). Contains
`IFeedParserStrategy`, `RssXmlParserStrategy`, and `FeedParserStrategySelector`. Depends on
`EisenFeed.Core`.

### EisenFeed.Ingestion.Produce.Kafka

The library implementing the Produce Stage. Contains `IWriteFeedItems`,
`FeedRepository`, `FeedIdItemIdMessageMapper`, and `ProduceDeliveryResult`. Depends on `EisenFeed.Core`.

### EisenFeed.Ingestion.Orchestration

The library implementing the Orchestrator. Sequences all stages, manages idempotency checks,
and builds the Feed Ingestion summary. Depends on Consume, Transform, Produce, and Core.

### EisenFeed.Ingestion.Service

The single Aspire-hosted runtime process that composes all four ingestion libraries and runs
the pipeline. Entry point for the ingestion feature.

### Ingestion Node

Informal synonym for `EisenFeed.Ingestion.Service`. Prefer **Ingestion Service** in all formal
documentation and code.

---

## Terms Pending Clarification

The following terms appear inconsistently across code and docs and require owner decision before
being locked:

| Term in use | Where used | Question |
|---|---|---|
| `parse/itemize` | spec FRs, research | ✅ **Resolved**: The step is called **Parse**. It is the Transform stage of CTP. The action is parsing the raw feed into individual FeedItem entities, each carrying feed-level and item-level data. |
| `FetchAsync` | `IReadRssFeeds` | ✅ **Resolved**: Use `FetchAsync` as the canonical method name for feed retrieval via `IReadRssFeeds`. |
| `MapMessagesAsync` | `FeedIdItemIdMessageMapper` | ✅ **Resolved**: The mapper returns correlated key/payload pairs (`FeedKafkaMessage`) to avoid positional mismatch across separate collections. |
| `ProduceAsyncWithForcedFailure` | test + stub | ✅ **Resolved**: This is test-only and must never exist in production repositories. Failure-path testing should use test doubles in the test project. |
| `IngestedItemRecord` | data model | ✅ **Resolved**: Canonical name is `FeedItemIngestion`. |
| `IngestionRunResult` | data model, contracts | ✅ **Resolved**: Canonical name is `FeedIngestion`. |
| `Operator` | spec user stories | ✅ **Resolved**: Primary persona is **Reader**. `User` is the account actor and may have multiple roles; `Operator` is a role label, not the persona. |
