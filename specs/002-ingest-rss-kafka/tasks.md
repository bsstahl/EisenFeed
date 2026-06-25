# Tasks: Single RSS Feed Ingestion to Kafka

**Input**: Design documents from `/specs/002-ingest-rss-kafka/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`

**Tests**: Strict test-first sequencing is required for this feature. All test tasks are defined before any production code tasks.

**Organization**: Tasks are grouped by user story and include an explicit approval gate that blocks all `src/` production code tasks until user approval is granted.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: User story label (`[US1]`, `[US2]`, `[US3]`) for user-story phase tasks
- Every task includes an exact file path

## Phase 1: Setup (Test Harness Only)

**Purpose**: Prepare test infrastructure under `tst/` without adding production code.

- [x] T001 Create ingestion test project scaffold in tst/EisenFeed.Ingestion.Tests/EisenFeed.Ingestion.Tests.csproj
- [x] T002 Add test project to solution in EisenFeed.slnx
- [x] T003 [P] Create shared test fixtures folder and base fixture in tst/EisenFeed.Ingestion.Tests/Common/IngestionTestFixture.cs
- [x] T004 [P] Add XML test payload fixtures for parser strategy tests in tst/EisenFeed.Ingestion.Tests/TestData/Rss/
- [x] T005 [P] Add canonical FeedItem fixture builders for producer tests in tst/EisenFeed.Ingestion.Tests/Common/CanonicalFeedItemFactory.cs

---

## Phase 2: User Story 1 Tests - Ingest New Feed Items Reliably (Priority: P1)

**Goal**: Define failing tests for consume/transform/produce stage behavior for new-item ingestion.

**Independent Test**: Tests fail initially and encode expected behavior for retrieve repository abstraction with source-to-canonical mapping, transform strategy over canonical `FeedItem` inputs, and producer repository behavior with canonical `FeedItem` inputs.

- [x] T006 [P] [US1] Create fetch repository unit tests for successful RSS retrieval in tst/EisenFeed.Ingestion.Tests/Consume/FeedRepositoryTests.cs
- [x] T007 [P] [US1] Create fetch repository unit tests for feed-level failures/timeouts in tst/EisenFeed.Ingestion.Tests/Consume/FeedRepositoryFailureTests.cs
- [x] T008 [P] [US1] Create retrieve repository unit tests for valid XML item mapping in tst/EisenFeed.Ingestion.Tests/Consume/FeedRepository_RetrieveAsync_Should.cs
- [x] T009 [P] [US1] Create retrieve repository unit tests for malformed XML handling in tst/EisenFeed.Ingestion.Tests/Consume/FeedRepository_RetrieveAsync_Should.cs
- [x] T010 [P] [US1] Create transform strategy selector tests for canonical-item strategy dispatch in tst/EisenFeed.Ingestion.Tests/Transform/FeedTransformStrategySelector_Select_Should.cs
- [x] T011 [P] [US1] Create message mapper unit tests with canonical FeedItems for key/payload mapping in tst/EisenFeed.Ingestion.Tests/Produce/FeedIdItemIdMessageMapperTests.cs
- [x] T012 [P] [US1] Create producer repository unit tests with canonical FeedItems for ack/error handling in tst/EisenFeed.Ingestion.Tests/Produce/FeedRepositoryDeliveryTests.cs
- [x] T013 [US1] Add initial red-test execution notes for US1 in specs/002-ingest-rss-kafka/checklists/requirements.md

---

## Phase 3: User Story 2 Tests - Safe Re-Runs After Failures (Priority: P2)

**Goal**: Define failing tests for at-least-once rerun semantics and idempotent skip behavior.

**Independent Test**: Tests fail initially and verify safe re-runs after partial completion and transient publish failures.

- [ ] T014 [P] [US2] Create orchestration unit tests for skipping already-ingested identities across reruns in tst/EisenFeed.Ingestion.Tests/Orchestration/IngestionOrchestratorIdempotencyTests.cs
- [ ] T015 [P] [US2] Create orchestration unit tests for continue-on-item-failure semantics in tst/EisenFeed.Ingestion.Tests/Orchestration/IngestionOrchestratorContinueOnFailureTests.cs
- [ ] T016 [P] [US2] Create integration tests for at-least-once publish behavior on retry in tst/EisenFeed.Ingestion.Tests/Integration/AtLeastOnceRetryIntegrationTests.cs
- [ ] T017 [P] [US2] Create integration tests for duplicate-minimization with persistent ingested records in tst/EisenFeed.Ingestion.Tests/Integration/DuplicateMinimizationIntegrationTests.cs
- [ ] T018 [US2] Add initial red-test execution notes for US2 in specs/002-ingest-rss-kafka/checklists/requirements.md

---

## Phase 4: User Story 3 Tests - Observable Ingestion Outcome (Priority: P3)

**Goal**: Define failing tests for run summary contract and reporting counters.

**Independent Test**: Tests fail initially and verify discovered/ingested/skipped/failed counts and status transitions.

- [ ] T019 [P] [US3] Create run summary builder unit tests for counter invariants in tst/EisenFeed.Ingestion.Tests/Orchestration/RunSummaryBuilderTests.cs
- [ ] T020 [P] [US3] Create run summary contract serialization tests in tst/EisenFeed.Ingestion.Tests/Contracts/IngestionRunSummaryContractTests.cs
- [ ] T021 [P] [US3] Create integration tests for mixed outcomes reporting in tst/EisenFeed.Ingestion.Tests/Integration/IngestionRunSummaryIntegrationTests.cs
- [ ] T022 [US3] Add initial red-test execution notes for US3 in specs/002-ingest-rss-kafka/checklists/requirements.md

---

## Phase 5: Approval Gate - Required Before Production Code

**Purpose**: Enforce explicit user approval before creating or editing production code under `src/`.

- [x] T023 Obtain explicit user approval in specs/002-ingest-rss-kafka/tasks.md before starting any production code tasks in src/

**Checkpoint**: APPROVED 2026-06-24 — user requested stub implementations to produce meaningful NotImplementedException test failures.

---

## Phase 6: Foundational Production Scaffolding (Blocked by T023)

**Purpose**: Create the four-library architecture and the service host required by FR-019 and FR-020.

- [x] T024 Create consume library project in src/EisenFeed.Ingestion.Consume.Rss/EisenFeed.Ingestion.Consume.Rss.csproj
- [x] T025 [P] Create transform library project in src/EisenFeed.Ingestion.Transform.Parser/EisenFeed.Ingestion.Transform.Parser.csproj
- [x] T026 [P] Create produce library project in src/EisenFeed.Ingestion.Produce.Kafka/EisenFeed.Ingestion.Produce.Kafka.csproj
- [ ] T027 [P] Create orchestration library project in src/EisenFeed.Ingestion.Orchestration/EisenFeed.Ingestion.Orchestration.csproj
- [ ] T028 Create ingestion host service project in src/EisenFeed.Ingestion.Service/EisenFeed.Ingestion.Service.csproj
- [ ] T029 Wire project references and solution entries in EisenFeed.slnx

---

## Phase 7: User Story 1 Implementation - Ingest New Feed Items Reliably (Blocked by T023)

**Goal**: Implement retrieve, transform strategy, and produce repository behavior to satisfy US1 tests.

**Independent Test**: All US1 tests in Phase 2 pass.

- [ ] T030 [P] [US1] Implement fetch repository abstraction and RSS implementation in src/EisenFeed.Ingestion.Consume.Rss/IRetrieveFeedItems.cs
- [ ] T031 [P] [US1] Implement RSS fetch repository behavior in src/EisenFeed.Ingestion.Consume.Rss/FeedRepository.cs
- [ ] T032 [P] [US1] Implement transform strategy interface and selector in src/EisenFeed.Ingestion.Transform.Parser/ITransformFeedItems.cs
- [ ] T033 [P] [US1] Implement transform strategy selector in src/EisenFeed.Ingestion.Transform.Parser/FeedTransformStrategySelector.cs
- [ ] T034 [P] [US1] Implement canonical-item transform strategy in src/EisenFeed.Ingestion.Transform.Parser/FeedItemTransformer.cs
- [ ] T035 [P] [US1] Implement producer repository abstraction and Kafka implementation in src/EisenFeed.Ingestion.Produce.Kafka/IWriteFeedItems.cs
- [ ] T036 [P] [US1] Implement Kafka producer repository mapping/delivery logic in src/EisenFeed.Ingestion.Produce.Kafka/FeedRepository.cs
- [ ] T037 [US1] Run US1 tests and capture green results in specs/002-ingest-rss-kafka/checklists/requirements.md

---

## Phase 8: User Story 2 Implementation - Safe Re-Runs After Failures (Blocked by T023)

**Goal**: Implement orchestration and idempotency behavior for at-least-once reruns.

**Independent Test**: All US2 tests in Phase 3 pass.

- [ ] T038 [P] [US2] Implement ingestion orchestration flow in src/EisenFeed.Ingestion.Orchestration/IngestionOrchestrator.cs
- [ ] T039 [P] [US2] Implement persistent Feed Item Ingestion store adapter usage in src/EisenFeed.Ingestion.Orchestration/IngestionOrchestrator.cs
- [ ] T040 [P] [US2] Implement retry-aware at-least-once produce orchestration in src/EisenFeed.Ingestion.Orchestration/IngestionOrchestrator.cs
- [ ] T041 [US2] Integrate orchestration with host startup wiring in src/EisenFeed.Ingestion.Service/Program.cs
- [ ] T042 [US2] Run US2 tests and capture green results in specs/002-ingest-rss-kafka/checklists/requirements.md

---

## Phase 9: User Story 3 Implementation - Observable Ingestion Outcome (Blocked by T023)

**Goal**: Implement run summary accounting and contract output.

**Independent Test**: All US3 tests in Phase 4 pass.

- [ ] T043 [P] [US3] Implement run summary aggregation in src/EisenFeed.Ingestion.Orchestration/RunSummaryBuilder.cs
- [ ] T044 [P] [US3] Implement run status/counter invariants in src/EisenFeed.Ingestion.Orchestration/RunSummaryBuilder.cs
- [ ] T045 [US3] Expose run summary output from orchestration service boundary in src/EisenFeed.Ingestion.Service/Program.cs
- [ ] T046 [US3] Run US3 tests and capture green results in specs/002-ingest-rss-kafka/checklists/requirements.md

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Final consistency, documentation, and full-suite validation.

- [ ] T047 [P] Update quickstart verification steps for strict test-first workflow in specs/002-ingest-rss-kafka/quickstart.md
- [ ] T048 [P] Update implementation notes and architectural decisions in specs/002-ingest-rss-kafka/plan.md
- [ ] T049 Run full test suite and record final results in specs/002-ingest-rss-kafka/checklists/requirements.md

---

## Dependencies & Execution Order

### Phase Dependencies

- Phase 1 must complete before user story test phases.
- Phase 2, Phase 3, and Phase 4 are all test-only phases and must complete before Phase 5.
- Phase 5 (T023 approval gate) blocks every production phase.
- Phase 6 depends on T023 and blocks Phases 7 to 9.
- Phase 7 (US1 implementation) should complete before Phase 8 for minimum-risk delivery.
- Phase 8 should complete before Phase 9 because run-summary behavior depends on orchestration outcomes.
- Phase 10 depends on completion of desired user story phases.

### User Story Dependencies

- US1 (P1): Test phase can start after Phase 1; implementation can start only after T023 and Phase 6.
- US2 (P2): Test phase can start after Phase 1; implementation can start only after T023, Phase 6, and US1 implementation baseline.
- US3 (P3): Test phase can start after Phase 1; implementation can start only after T023 and orchestration baseline from US2.

### Approval Gate Rule

- No task touching any file under `src/` may begin before T023 is explicitly completed by user approval.

---

## Parallel Opportunities

- Phase 1: T003-T005 can run in parallel.
- US1 test phase: T006-T012 can run in parallel once fixtures are in place.
- US2 test phase: T014-T017 can run in parallel.
- US3 test phase: T019-T021 can run in parallel.
- Foundational production scaffolding: T025-T027 can run in parallel after T024 starts.
- US1 implementation: T030-T036 are largely parallel by library boundary.

---

## Parallel Example: Strict Test-First Batch

```bash
# US1 test batch (all before any src/ code)
Task: T006
Task: T007
Task: T008
Task: T009
Task: T010
Task: T011
Task: T012

# US2 at-least-once semantics tests (all before any src/ code)
Task: T014
Task: T015
Task: T016
Task: T017
```

---

## Implementation Strategy

### MVP First (US1 with Hard Test Gate)

1. Complete Phase 1 and all US1/US2/US3 test phases (Phase 2-4).
2. Complete Phase 5 approval gate (T023) with explicit user confirmation.
3. Complete Phase 6 scaffolding.
4. Complete Phase 7 (US1 implementation) and validate US1 independently.

### Incremental Delivery

1. Tests first for all stories (Phase 2-4).
2. Explicit gate approval (Phase 5).
3. Implement US1, then US2, then US3 with independent verification per phase.
4. Finish with Phase 10 cross-cutting validation.

### Blocker Policy

- If T023 is not approved, all production tasks T024-T046 remain blocked and must not be executed.
