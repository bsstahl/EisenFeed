# Data Model: Single RSS Feed Ingestion to Kafka

## Entity: FeedSource

- Purpose: Configuration for the single RSS feed being ingested.
- Fields:
  - `FeedId` (string, required, immutable)
  - `Url` (string/URI, required)
  - `IsEnabled` (bool, required)
  - `LastSuccessfulIngestionAt` (datetimeoffset, optional)
- Validation Rules:
  - `Url` must be absolute HTTP/HTTPS.
  - `FeedId` must be unique in configured scope.

## Entity: FeedItem

- Purpose: Canonical representation of an item discovered from RSS.
- Fields:
  - `FeedId` (string, required)
  - `ItemId` (string, required, stable identity)
  - `PublishedAt` (datetimeoffset, required)
  - `Title` (string, required)
  - `Content` (string, optional)
- Validation Rules:
  - `ItemId` must be deterministic for equivalent source item.
  - Required text fields must be trimmed and non-empty where required.

## Entity: IngestedItemRecord

- Purpose: Persistent idempotency record proving a feed item has already been ingested.
- Fields:
  - `FeedId` (string, required)
  - `ItemId` (string, required)
  - `IngestedAt` (datetimeoffset, required)
  - `KafkaMessageKey` (string, required)
  - `RunId` (string/UUID, required)
- Validation Rules:
  - Unique constraint on `(FeedId, ItemId)`.
  - `KafkaMessageKey` must match `FeedId:ItemId` format.

## Entity: IngestionRunResult

- Purpose: Operational summary for one ingestion execution.
- Fields:
  - `RunId` (string/UUID, required)
  - `FeedId` (string, required)
  - `StartedAt` (datetimeoffset, required)
  - `CompletedAt` (datetimeoffset, required)
  - `DiscoveredCount` (int >= 0)
  - `IngestedCount` (int >= 0)
  - `SkippedCount` (int >= 0)
  - `FailedCount` (int >= 0)
  - `Status` (enum: `Succeeded`, `PartiallySucceeded`, `Failed`)
- Validation Rules:
  - `DiscoveredCount = IngestedCount + SkippedCount + FailedCount`.
  - `CompletedAt >= StartedAt`.

## Relationships

- `FeedSource` 1-to-many `FeedItem` (discovery context).
- `FeedItem` 1-to-1 `IngestedItemRecord` by `(FeedId, ItemId)` for idempotency.
- `IngestionRunResult` 1-to-many `IngestedItemRecord` via `RunId`.

## State Transitions

- Feed item lifecycle:
  - `Discovered` -> `Skipped` (already ingested)
  - `Discovered` -> `Ingested` -> `Published`
  - `Discovered` -> `Failed` (with retry in later run)

- Run lifecycle:
  - `Started` -> `Succeeded` when `FailedCount = 0`
  - `Started` -> `PartiallySucceeded` when `FailedCount > 0` and `IngestedCount > 0`
  - `Started` -> `Failed` on terminal feed-level failure
