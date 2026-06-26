# Tasks: Single RSS Feed Ingestion to Kafka

**Input**: Design documents from `/specs/002-ingest-rss-kafka/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`

**Tests**: Strict test-first sequencing is required for this feature. Each user story's tests are written and confirmed RED before that story's production code is written.

**Organization**: Tasks are paired by story (tests then implementation) and include an explicit approval gate that blocks all `src/` production code tasks until user approval is granted.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: User story label (`[US1]`, `[US2]`, `[US3]`) for user-story phase tasks
- Every task includes an exact file path

## Phase 1: Setup (Test Harness Only)

**Purpose**: Prepare test infrastructure under `tst/` without adding production code.

- [x] T001 Create ingestion test project scaffolds in tst/EisenFeed.Ingestion.Consume.Rss.Tests/EisenFeed.Ingestion.Consume.Rss.Tests.csproj, tst/EisenFeed.Ingestion.Produce.Kafka.Tests/EisenFeed.Ingestion.Produce.Kafka.Tests.csproj, and tst/EisenFeed.Ingestion.Transform.Rules.Tests/EisenFeed.Ingestion.Transform.Rules.Tests.csproj
- [x] T002 Add test projects to solution in EisenFeed.slnx
- [x] T003 [P] Create per-project test fixtures in tst/EisenFeed.Ingestion.Produce.Kafka.Tests/Common/IngestionTestFixture.cs and tst/EisenFeed.Ingestion.Transform.Rules.Tests/Common/IngestionTestFixture.cs
- [x] T004 [P] Add XML/canonical test payload fixtures for transform rules tests in tst/EisenFeed.Ingestion.Transform.Rules.Tests/TestData/Rss/
- [x] T005 [P] Add canonical FeedItem fixture builders for stage-specific tests in tst/EisenFeed.Ingestion.Produce.Kafka.Tests/Common/CanonicalFeedItemFactory.cs and tst/EisenFeed.Ingestion.Transform.Rules.Tests/Common/CanonicalFeedItemFactory.cs

---

## Phase 2: User Story 1 Tests (RED) - Ingest New Feed Items Reliably (Priority: P1)

**Goal**: Define failing tests for consume/transform/produce stage behavior for new-item ingestion.

**Independent Test**: Tests fail initially and encode expected behavior for retrieve repository abstraction with source-to-canonical mapping, transform strategy over canonical `FeedItem` inputs, and producer repository behavior with canonical `FeedItem` inputs.

- [x] T006 [P] [US1] Create fetch repository unit tests for successful RSS retrieval in tst/EisenFeed.Ingestion.Consume.Rss.Tests/FeedRepository_RetrieveAsync_Should.cs
- [x] T007 [P] [US1] Create fetch repository unit tests for feed-level failures/timeouts in tst/EisenFeed.Ingestion.Consume.Rss.Tests/FeedRepository_RetrieveAsync_Should.cs
- [x] T008 [P] [US1] Create retrieve repository unit tests for valid XML item mapping in tst/EisenFeed.Ingestion.Consume.Rss.Tests/FeedRepository_RetrieveAsync_Should.cs
- [x] T009 [P] [US1] Create retrieve repository unit tests for malformed XML handling in tst/EisenFeed.Ingestion.Consume.Rss.Tests/FeedRepository_RetrieveAsync_Should.cs
- [x] T010 [P] [US1] Create transform strategy selector tests for canonical-item strategy dispatch in tst/EisenFeed.Ingestion.Transform.Rules.Tests/FeedTransformStrategySelector_Select_Should.cs
- [x] T011 [P] [US1] Create message mapper unit tests with canonical FeedItems for key/payload mapping in tst/EisenFeed.Ingestion.Produce.Kafka.Tests/FeedIdItemIdMessageMapper_MapMessagesAsync_Should.cs
- [x] T012 [P] [US1] Create producer repository unit tests with canonical FeedItems for ack/error handling in tst/EisenFeed.Ingestion.Produce.Kafka.Tests/FeedRepository_PublishAsync_Should.cs
- [x] T013 [US1] **GATE**: Confirm all US1 tests fail (RED) before proceeding to Phase 5 — record results in specs/002-ingest-rss-kafka/checklists/requirements.md

---

## Phase 3: Approval Gate - Required Before Production Code

**Purpose**: Enforce explicit user approval before creating or editing production code under `src/`.

- [x] T023 Obtain explicit user approval in specs/002-ingest-rss-kafka/tasks.md before starting any production code tasks in src/

**Checkpoint**: APPROVED 2026-06-24 — user requested stub implementations to produce meaningful NotImplementedException test failures.

---

## Phase 4: Foundational Production Scaffolding (Blocked by T023)

**Purpose**: Create the stage libraries required by FR-019. Service scaffolding is deferred to Phase 7 where it is first needed.

- [x] T024 Create consume library project in src/EisenFeed.Ingestion.Consume.Rss/EisenFeed.Ingestion.Consume.Rss.csproj
- [x] T025 [P] Create transform library project in src/EisenFeed.Ingestion.Transform.Rules/EisenFeed.Ingestion.Transform.Rules.csproj
- [x] T026 [P] Create produce library project in src/EisenFeed.Ingestion.Produce.Kafka/EisenFeed.Ingestion.Produce.Kafka.csproj
- [x] T027 [P] Create orchestration library project in src/EisenFeed.Ingestion.Orchestration/EisenFeed.Ingestion.Orchestration.csproj

---

## Phase 5: User Story 1 Implementation (GREEN) - Ingest New Feed Items Reliably (Blocked by T023)

**Goal**: Implement retrieve, transform strategy, and produce repository behavior to satisfy US1 tests.

**Independent Test**: All US1 tests in Phase 2 pass.

- [x] T030 [P] [US1] Implement retrieve repository abstraction in src/EisenFeed.Core/Contracts/IRetrieveFeedItems.cs and RSS implementation wiring in src/EisenFeed.Ingestion.Consume.Rss/FeedRepository.cs
- [x] T031 [P] [US1] Implement RSS retrieve repository behavior in src/EisenFeed.Ingestion.Consume.Rss/FeedRepository.cs
- [x] T032 [P] [US1] Implement transform strategy interface in src/EisenFeed.Core/Contracts/ITransformFeedItems.cs
- [x] T033 [P] [US1] Implement transform strategy selector in src/EisenFeed.Ingestion.Transform.Rules/FeedTransformStrategySelector.cs
- [x] T034 [P] [US1] Implement rules-based canonical-item transformer and rule contract in src/EisenFeed.Ingestion.Transform.Rules/FeedItemTransformer.cs and src/EisenFeed.Ingestion.Transform.Rules/ITransformFeedItemRule.cs
- [x] T035 [P] [US1] Implement producer repository abstraction in src/EisenFeed.Core/Contracts/IWriteFeedItems.cs and Kafka implementation wiring in src/EisenFeed.Ingestion.Produce.Kafka/FeedRepository.cs
- [x] T036 [P] [US1] Implement Kafka producer repository mapping/delivery logic in src/EisenFeed.Ingestion.Produce.Kafka/FeedRepository.cs
- [x] T037 [US1] **GATE**: Confirm all US1 tests pass (GREEN) before proceeding to Phase 6 — record results in specs/002-ingest-rss-kafka/checklists/requirements.md

---

## Phase 6: User Story 2 Tests (RED) - Safe Re-Runs After Failures (Priority: P2)

**Goal**: Define failing unit tests for orchestration behavior and failing Aspire integration tests for end-to-end pipeline execution.

**Independent Test**: All unit tests fail initially against missing orchestration types. All integration tests fail initially because the service host and run summary do not yet exist.

- [x] T014 [P] [US2] Create orchestration unit tests for skipping already-ingested identities across reruns in tst/EisenFeed.Ingestion.Orchestration.Tests/IngestionOrchestrator_RunOnceAsync_Should.cs
- [x] T015 [P] [US2] Create orchestration unit tests for continue-on-item-failure semantics in tst/EisenFeed.Ingestion.Orchestration.Tests/IngestionOrchestrator_RunOnceAsync_Should.cs
- [x] T016 [P] [US2] Create re-run delivery tests for at-least-once behavior in tst/EisenFeed.Ingestion.Orchestration.Tests/IngestionOrchestrator_RunOnceAsync_Should.cs
- [x] T017 [P] [US2] Create re-run delivery tests for duplicate-minimization via idempotency store in tst/EisenFeed.Ingestion.Orchestration.Tests/IngestionOrchestrator_RunOnceAsync_Should.cs
- [x] T050 [P] [US2] Create integration test project scaffold in tst/EisenFeed.Ingestion.Service.Tests/EisenFeed.Ingestion.Service.Tests.csproj with reference to EisenFeed.AppHost
- [x] T051 [P] [US2] Create Aspire integration test for full pipeline run: items retrieved from RSS stub, published to Kafka, skipped on re-run in tst/EisenFeed.Ingestion.Service.Tests/IngestionPipeline_RunOnceAsync_Should.cs
- [x] T018 [US2] **GATE**: Confirm all US2 unit and integration tests fail (RED) before proceeding to Phase 7 — record results in specs/002-ingest-rss-kafka/checklists/requirements.md

---

## Phase 7: User Story 2 Implementation (GREEN) - Safe Re-Runs After Failures (Blocked by T023)

**Goal**: Implement orchestration, idempotency, and host wiring sufficient to pass all US2 unit and Aspire integration tests.

**Independent Test**: All US2 unit tests in Phase 6 pass and Aspire integration tests execute end-to-end.

- [x] T028 Create ingestion host service project in src/EisenFeed.Ingestion.Service/EisenFeed.Ingestion.Service.csproj
- [x] T029 Wire ingestion service project references and solution entries in EisenFeed.slnx
- [x] T038 [P] [US2] Implement ingestion orchestration flow in src/EisenFeed.Ingestion.Orchestration/IngestionOrchestrator.cs
- [x] T039 [P] [US2] Implement persistent Feed Item Ingestion store adapter usage in src/EisenFeed.Ingestion.Orchestration/IngestionOrchestrator.cs
- [x] T040 [P] [US2] Implement retry-aware at-least-once produce orchestration in src/EisenFeed.Ingestion.Orchestration/IngestionOrchestrator.cs
- [x] T041 [US2] Integrate orchestration with host startup wiring in src/EisenFeed.Ingestion.Service/Program.cs
- [x] T042 [US2] **GATE**: Confirm all US2 unit and integration tests pass (GREEN) before proceeding to Phase 8 — record results in specs/002-ingest-rss-kafka/checklists/requirements.md

---

## Phase 8: User Story 3 Tests (RED) - Observable Ingestion Outcome (Priority: P3)

**Goal**: Define failing unit tests for run summary behavior and failing Aspire integration tests that assert observable run outcome data from a full pipeline execution.

**Independent Test**: All unit tests fail initially against missing run summary types. Aspire integration tests fail initially because summary output does not yet exist on the service boundary.

- [ ] T019 [P] [US3] Create run summary builder unit tests for counter invariants in tst/EisenFeed.Ingestion.Orchestration.Tests/RunSummaryBuilder_BuildAsync_Should.cs
- [ ] T020 [P] [US3] Create run summary contract serialization tests in tst/EisenFeed.Ingestion.Orchestration.Tests/IngestionRunSummary_Serialization_Should.cs
- [ ] T021 [P] [US3] Create orchestration run-outcome unit tests for mixed item results in tst/EisenFeed.Ingestion.Orchestration.Tests/IngestionOrchestrator_RunOnceAsync_Should.cs
- [ ] T052 [P] [US3] Create Aspire integration test asserting run summary (discovered/ingested/skipped/failed counts) is returned after a full pipeline run in tst/EisenFeed.Ingestion.Service.Tests/IngestionPipeline_RunOnceAsync_Should.cs
- [ ] T022 [US3] **GATE**: Confirm all US3 unit and integration tests fail (RED) before proceeding to Phase 9 — record results in specs/002-ingest-rss-kafka/checklists/requirements.md

---

## Phase 9: User Story 3 Implementation (GREEN) - Observable Ingestion Outcome (Blocked by T023)

**Goal**: Implement run summary accounting and contract output sufficient to pass all US3 unit and Aspire integration tests.

**Independent Test**: All US3 unit and integration tests in Phase 8 pass.

- [ ] T043 [P] [US3] Implement run summary aggregation in src/EisenFeed.Ingestion.Orchestration/RunSummaryBuilder.cs
- [ ] T044 [P] [US3] Implement run status/counter invariants in src/EisenFeed.Ingestion.Orchestration/RunSummaryBuilder.cs
- [ ] T045 [US3] Expose run summary output from orchestration service boundary in src/EisenFeed.Ingestion.Service/Program.cs
- [ ] T046 [US3] **GATE**: Confirm all US3 unit and integration tests pass (GREEN) before proceeding to Phase 10 — record results in specs/002-ingest-rss-kafka/checklists/requirements.md

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Final consistency, documentation, and full-suite validation.

- [ ] T047 [P] Update quickstart verification steps for strict test-first workflow in specs/002-ingest-rss-kafka/quickstart.md
- [ ] T048 [P] Update implementation notes and architectural decisions in specs/002-ingest-rss-kafka/plan.md
- [ ] T049 Run full test suite and record final results in specs/002-ingest-rss-kafka/checklists/requirements.md

---

## Dependencies & Execution Order

### Phase Dependencies

- Phase 1 (Setup) must complete before Phase 2.
- Phase 2 (US1 Tests) must complete before Phase 3 (Approval Gate).
- Phase 3 (Approval Gate) blocks Phase 4 and all implementation phases.
- Phase 4 (Foundational Scaffolding) must complete before Phase 5.
- Phase 5 (US1 Implementation) must complete before Phase 6 (US2 Tests). Gate: T037.
- Phase 6 (US2 Tests) must complete before Phase 7 (US2 Implementation). Gate: T018.
- Phase 7 (US2 Implementation) must complete before Phase 8 (US3 Tests). Gate: T042.
- Phase 8 (US3 Tests) must complete before Phase 9 (US3 Implementation). Gate: T022.
- Phase 10 depends on completion of all story phases. Gate: T046.

### User Story Dependencies

- US1 (P1): Tests in Phase 2; implementation in Phase 5 after T023 and Phase 4.
- US2 (P2): Tests in Phase 6 after US1 implementation; implementation in Phase 7.
- US3 (P3): Tests in Phase 8 after US2 implementation; implementation in Phase 9.

### Approval Gate Rule

- No task touching any file under `src/` may begin before T023 is explicitly completed by user approval.

---

## Parallel Opportunities

- Phase 1: T003-T005 can run in parallel.
- Phase 2 (US1 tests): T006-T012 can run in parallel once fixtures are in place.
- Phase 4 (Scaffolding): T025-T027 can run in parallel after T024 starts.
- Phase 5 (US1 implementation): T030-T036 are largely parallel by library boundary.
- Phase 6 (US2 tests): T014-T017, T050-T051 can run in parallel.
- Phase 8 (US3 tests): T019-T021, T052 can run in parallel.

---

## Implementation Strategy

### Story-Paired TDD

For each user story: write tests (RED), then implement (GREEN), then validate before moving to the next story.

1. Phase 1: Setup test infrastructure.
2. Phase 2: Write US1 tests (RED).
3. Phase 3: Approval gate.
4. Phase 4: Foundational scaffolding.
5. Phase 5: Implement US1 (GREEN) — validate before continuing.
6. Phase 6: Write US2 tests (RED).
7. Phase 7: Implement US2 (GREEN) — validate before continuing.
8. Phase 8: Write US3 tests (RED).
9. Phase 9: Implement US3 (GREEN) — validate before continuing.
10. Phase 10: Polish and full-suite validation.

### Blocker Policy

- If T023 is not approved, all production tasks T024-T046 remain blocked and must not be executed.
