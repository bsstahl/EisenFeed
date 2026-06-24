# Contract: Kafka Message `feed-item-ingested`

## Purpose

Defines the message produced for each newly ingested feed item so downstream processors can consume an at-least-once event stream with stable keys for idempotent handling.

## Topic

- Name: `feed-items-ingested` (configurable)
- Delivery guarantee: at-least-once

## Key

- Format: `<FeedId>:<ItemId>`
- Required: yes
- Rationale: deterministic partitioning and downstream dedupe support

## Value Schema (JSON)

```json
{
  "schemaVersion": "1.0",
  "eventType": "feed-item-ingested",
  "runId": "3f8fef3f-8ee9-4e6b-9c11-2cded18f47fa",
  "occurredAt": "2026-06-24T12:00:00Z",
  "feedId": "string",
  "itemId": "string",
  "publishedAt": "2026-06-24T11:30:00Z",
  "title": "string",
  "content": "string"
}
```

## Validation Rules

- `schemaVersion` required, additive evolution only
- `eventType` must equal `feed-item-ingested`
- `feedId` and `itemId` required and non-empty
- `occurredAt` and `publishedAt` must be valid ISO-8601 timestamps
- `key` must match `<feedId>:<itemId>`

## Compatibility

- Backward-compatible changes: add optional fields only
- Breaking changes require new `schemaVersion` and migration plan
