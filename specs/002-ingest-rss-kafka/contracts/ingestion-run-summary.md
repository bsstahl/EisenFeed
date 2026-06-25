# Contract: Ingestion Run Summary

## Purpose

Defines the run-level output contract used by readers and tests to validate ingestion outcomes.

## Shape

```json
{
  "runId": "3f8fef3f-8ee9-4e6b-9c11-2cded18f47fa",
  "feedId": "string",
  "startedAt": "2026-06-24T12:00:00Z",
  "completedAt": "2026-06-24T12:00:05Z",
  "status": "Succeeded",
  "discoveredCount": 100,
  "ingestedCount": 80,
  "skippedCount": 18,
  "failedCount": 2
}
```

## Validation Rules

- `status` in: `Succeeded`, `PartiallySucceeded`, `Failed`
- All counts are integers >= 0
- `discoveredCount = ingestedCount + skippedCount + failedCount`
- `completedAt >= startedAt`

## Usage

- Used for operational visibility
- Used as assertion target in integration tests for retry/idempotency behavior
