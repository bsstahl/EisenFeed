# Quickstart: Single RSS Feed Ingestion to Kafka

## 1. Prerequisites

- .NET 10 SDK installed
- Kafka broker reachable
- One RSS feed URL selected for ingestion

## 2. Project Placement Rules

- Place production implementation under `src/`
- Place tests under `tst/`

## 3. Implement Core Flow

1. Create four libraries under `src/`:

 - `EisenFeed.Ingestion.Consume.Rss`
 - `EisenFeed.Ingestion.Transform.Rules`
 - `EisenFeed.Ingestion.Produce.Kafka`
 - `EisenFeed.Ingestion.Orchestration`

2. Create `EisenFeed.Ingestion.Service` as the single Aspire-hosted runtime service.
3. Implement retrieve repository (`IRetrieveFeedItems`) in the consume library to retrieve and canonicalize source feed items.
4. Implement transform strategy (`ITransformFeedItems`) in the transform library to apply canonical-to-canonical processing to retrieved feed items.
5. Derive deterministic `ItemId` for each item and perform idempotency checks in orchestration.
6. Implement produce repository (`IWriteFeedItems`) in the produce library to publish to Kafka with key `FeedId:ItemId`.
7. Persist feed item ingestion record and run counters in orchestration.
8. Return run summary with discovered/ingested/skipped/failed counts.

## 4. Validate Locally

```powershell
dotnet restore
dotnet build EisenFeed.slnx
```

Run unit tests and integration tests (after test projects exist):

```powershell
dotnet test
```

Recommended unit-test grouping:

- Retrieve repository tests: verify retrieval and source-to-canonical mapping behaviors via repository abstraction.
- Transform tests: provide canonical feed items and validate canonical-to-canonical processing outcomes.
- Transform strategy tests: ensure strategy selection and transform behavior are correct for provided canonical items.
- Produce repository tests: pass canonical `FeedItem` instances and validate message key/payload publishing behavior.

## 5. Acceptance Checks

- First run ingests and publishes all feed items.
- Second run with unchanged feed publishes zero new messages.
- Retry after partial failure achieves eventual delivery with at-least-once semantics, and duplicate deliveries are bounded and observable.
- Run summary exposes discovered, ingested, skipped, and failed counts.
