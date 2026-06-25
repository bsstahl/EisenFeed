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

## Entity: FeedItemIngestion

- Purpose: Persistent idempotency record proving a feed item has already been ingested.
- Fields:
  - `FeedId` (string, required)
  - `ItemId` (string, required)
  - `Status` (enum: `Publishing`, `Published`, `Failed`, required)
  - `PublishAttemptedAt` (datetimeoffset, required)
  - `IngestedAt` (datetimeoffset, optional, required when `Status = Published`)
  - `AttemptCount` (int >= 1, required)
  - `LastError` (string, optional)
  - `KafkaMessageKey` (string, required)
  - `KafkaTopic` (string, optional, required when `Status = Published`)
  - `KafkaPartition` (int, optional, required when `Status = Published`)
  - `KafkaOffset` (long, optional, required when `Status = Published`)
  - `RunId` (string/UUID, required)
- Validation Rules:
  - Unique constraint on `(FeedId, ItemId)`.
  - `KafkaMessageKey` must match `FeedId:ItemId` format.
  - `Status = Published` requires `IngestedAt`, `KafkaTopic`, `KafkaPartition`, and `KafkaOffset`.
  - `AttemptCount` increments on each publish attempt.

## Entity: FeedIngestion

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
- `FeedItem` 1-to-1 `FeedItemIngestion` by `(FeedId, ItemId)` for idempotency.
- `FeedIngestion` 1-to-many `FeedItemIngestion` via `RunId`.

## State Transitions

- Feed item lifecycle:
  - `Discovered` -> `Skipped` (already ingested)
  - `Discovered` -> `Ingested` -> `Published`
  - `Discovered` -> `Failed` (with retry in later run)

- Idempotency record lifecycle:
  - `Publishing` -> `Published` after Kafka ack and confirmation write.
  - `Publishing` -> `Failed` when publish or confirmation fails.
  - `Failed` -> `Publishing` on retry.

- Run lifecycle:
  - `Started` -> `Succeeded` when `FailedCount = 0`
  - `Started` -> `PartiallySucceeded` when `FailedCount > 0` and `IngestedCount > 0`
  - `Started` -> `Failed` on terminal feed-level failure
