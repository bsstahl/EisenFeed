# EisenFeed Testing Standards

**Purpose**: Establish consistent conventions for unit and integration tests across all test projects.

---

## Naming & Variable Conventions

### Interface Naming

All domain contract interfaces MUST:

- Start with `I`
- Use a generic responsibility-oriented **verb phrase** name
- Avoid implementation details (technology, mechanism, or hosting model)

Examples:

```csharp
IAgeFeedItems
IPerformFeedIngestion
IRetrieveFeedItems
IWriteFeedItems
```

### Test Class Naming

Test classes **MUST** follow this pattern:

`[TestTargetClassName]_[TestMethodName]_Should`

Examples:

```csharp
public sealed class FeedRepository_RetrieveAsync_Should
public sealed class FeedTransformStrategySelector_Select_Should
public sealed class FeedRepository_PublishAsync_Should
```

**Rationale**:

- Makes target class and target method explicit in every class name
- Improves scanability in Test Explorer and stack traces
- Keeps happy-path and failure-path scenarios together under the same method-focused class

### Test File Naming

Test file names **MUST** match the test class name exactly:

`[TestClassName].cs`

Examples:

```csharp
FeedRepository_RetrieveAsync_Should.cs
FeedTransformStrategySelector_Select_Should.cs
FeedRepository_PublishAsync_Should.cs
```

### Class Under Test Variable

The class or component being tested **MUST** always use the variable name `target`.

```csharp
[Fact]
public async Task RetrieveAsync_WhenFeedIsReachable_ReturnsCanonicalFeedItems()
{
    var target = CreateTestTarget();

    var items = await target.RetrieveAsync(CancellationToken.None);

    Assert.NotEmpty(items);
}
```

**Rationale**:

- Universally recognizable across all test files
- Reduces cognitive overhead when scanning tests
- Makes test intent clear: setup → act on target → assert on outcome
- "target" more directly conveys "what we're testing" than "sut" or domain-specific names like "repository"

### Factory Method Naming

Use `CreateTestTarget()` as the factory method name for creating instances of the class under test.

```csharp
private static IRetrieveFeedItems CreateTestTarget()
{
    var feedUrl = new Uri("https://example.com/feed.xml");
    var handler = new DelegateHttpMessageHandler((_, _) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<rss></rss>")
        }));

    var httpClient = new HttpClient(handler);
    return new FeedRepository(httpClient, feedUrl);
}
```

**Rationale**:

- Parallel to `target` variable naming
- Explicit about intent: creating a target for testing
- Consistent factory naming across all tests

### Dependencies and Fixtures

Use domain-specific names for dependencies and test fixtures:

```csharp
private static IWriteFeedItems CreateMockItemStore()
{
    // Return mock or test double
}

private static object CreateTestFeedConfiguration(string feedId = "test-feed")
{
    // Return fixture
}
```

---

## Test Method Naming

### Pattern: `BehaviorDescription`

Because the test class already encodes `[TestTargetClassName]_[TestMethodName]_Should`, test methods
MUST be behavior-only descriptions with no repeated method prefix.

```csharp
public async Task ReturnCanonicalFeedItemsWhenTheFeedIsReachable()
public async Task ThrowOperationCanceledExceptionWhenCancellationIsRequested()
```

---

## Arrange-Act-Assert Structure

```csharp
[Fact]
public async Task ReturnCanonicalFeedItemsWhenTheFeedIsReachable()
{
    // Arrange: Setup target and dependencies
    var target = CreateTestTarget();

    // Act: Execute the method under test
    var items = await target.RetrieveAsync(CancellationToken.None);

    // Assert: Verify expected outcome
    Assert.NotEmpty(items);
}
```

---

## Test Output Logging

- Every unit test class MUST accept `ITestOutputHelper` via constructor injection and store it in a field.
- Every unit test MUST log the values used during Arrange and key values produced during Act.
- Tests that rely on randomly generated defaults (for example `GetRandom()`) MUST log the realized values used in that test run.
- On failures, tests MUST log exception details to `ITestOutputHelper` before rethrowing.

Example:

```csharp
private readonly ITestOutputHelper _output;

public FeedRepository_RetrieveAsync_Should(ITestOutputHelper output)
{
    _output = output;
}

[Fact]
public async Task ReturnCanonicalFeedItemsWhenTheFeedIsReachable()
{
    try
    {
        var target = CreateTestTarget();
        _output.WriteLine("Input feedUrl configured in constructor");

        var items = await target.RetrieveAsync(CancellationToken.None);

        _output.WriteLine("Output item count: {0}", items.Count);
        Assert.NotEmpty(items);
    }
    catch (Exception ex)
    {
        _output.WriteLine(ex.ToString());
        throw;
    }
}
```

---

## Test Traits and Grouping

All test classes MUST declare xUnit `Trait` attributes for filtering and grouped execution.

Required traits:

- `TestType`: `Unit` or `Integration`
- `Phase`: `Consume`, `Transform`, `Produce`, `Orchestration`, or `All`
  - Use `All` for cross-stage integration tests that exercise the full pipeline end-to-end
- `Component`: concrete class or subsystem under test

Example (unit test):

```csharp
[Trait("TestType", "Unit")]
[Trait("Phase", "Produce")]
[Trait("Component", "FeedRepository")]
public sealed class FeedRepository_PublishAsync_Should
{
    ...
}
```

Example (cross-stage integration test):

```csharp
[Trait("TestType", "Integration")]
[Trait("Phase", "All")]
[Trait("Component", "IngestionPipeline")]
public sealed class IngestionPipeline_RunOnceAsync_Should
{
    ...
}
```

Filtering examples:

```powershell
dotnet test --filter "TestType=Unit"
dotnet test --filter "TestType=Integration"
dotnet test --filter "Phase=Transform"
dotnet test --filter "TestType=Unit&Phase=Produce"
dotnet test --filter "Component=FeedRepository"
```

Notes:

- Prefer class-level traits for consistent grouping across all tests in a class.
- Method-level traits are allowed only when a test needs additional categorization beyond class defaults.

---

## Assertion Style

- Prefer `Assert.*` methods from xUnit
- Keep assertions focused on one logical concern per test method
- Use descriptive assertion messages when needed:

  ```csharp
  Assert.Equal(expected, actual, "Item count should match discovered items");
  ```

---

## Test Data and Fixtures

- Place synthetic test data (canonical FeedItems, sample XML) in `TestData/` subdirectories under `tst/`
- Use factory classes like `CanonicalFeedItemFactory` to generate consistent test instances
- Keep fixtures immutable and reusable across multiple tests

### TestHelperExtensions and `GetRandom()` Usage

`TestHelperExtensions` is a **test-only** utility library and MAY be used in test projects for generating
non-essential fixture data.

Rules:

- `TestHelperExtensions` MUST only be referenced from test projects (for example under `tst/`).
- Production libraries (for example under `src/`) MUST NEVER reference `TestHelperExtensions`.
- `GetRandom()` SHOULD be used for filler/default values in test factories, not for values under direct assertion.
- If a test asserts a specific value, that value MUST be explicitly provided in the arrange step (not randomly generated).
- Tests MUST remain deterministic in intent: random defaults are acceptable only when assertions are invariant to those values.

Example (allowed in test factory):

```csharp
using TestHelperExtensions;

feedId ??= string.Empty.GetRandom();
itemId ??= string.Empty.GetRandom();
title ??= string.Empty.GetRandom();
content ??= string.Empty.GetRandom();
```

Example (required in explicit assertion tests):

```csharp
var item = CanonicalFeedItemFactory.Create(
    feedId: "feed-a",
    itemId: "item-42",
    title: "Expected Title",
    content: "Expected Content");
```

---

## Code Coverage Policy

- Test libraries MUST be excluded from code coverage reports.
- Coverage is intended to measure production code under `src/`, not test code under `tst/`.
- Every test library MUST declare the assembly-level exclusion attribute in source:

```csharp
using System.Diagnostics.CodeAnalysis;

[assembly: ExcludeFromCodeCoverage]
```

- New test libraries MUST include this declaration when created.

---

## Test Class Organization

- One test class per target method under a target class
- Group both happy-path and failure-path scenarios in the same method-focused class
- Use `sealed` modifier on test classes to prevent inheritance
- Prefer behavior-focused tests over constructor-only tests. If object construction is broken, behavior tests will usually reveal it.
- Add constructor-specific tests only when the constructor itself contains meaningful behavior such as guard clauses, validation, or composition rules.

```csharp
public sealed class FeedRepository_RetrieveAsync_Should
{
    [Fact]
    public async Task ReturnCanonicalFeedItemsWhenTheFeedIsReachable() { ... }

    [Fact]
    public async Task ThrowHttpRequestExceptionWhenTheFeedIsUnavailable() { ... }
}
```

---

## Async Testing

- Use `async Task` for all async test methods
- Use `await` consistently; never use `.Result` or `.Wait()` to block on tasks
- Prefer `Assert.ThrowsAsync<T>` for exception testing in async contexts

```csharp
[Fact]
public async Task ThrowOperationCanceledExceptionWhenCancellationIsRequested()
{
    var target = CreateTestTarget();
    using var cts = new CancellationTokenSource();
    cts.Cancel();

    await Assert.ThrowsAnyAsync<OperationCanceledException>(
        () => target.RetrieveAsync(cts.Token));
}
```

---

## Stub & Mock Patterns

- Use `NotImplementedException` for stub implementations during TDD
- For mock/test-double needs, prefer `Moq` or similar only when necessary
- Inline simple test doubles when possible; use factories for complex ones

### Network-Dependent Unit Tests

Unit tests that exercise network paths MUST NOT call real network endpoints.

Production classes that perform network requests MUST require explicit `HttpClient` injection.
Do not add a parameterless constructor that internally creates `HttpClient`.

Rules:

- For `HttpClient` consumers, mock behavior by providing a custom/fake `HttpMessageHandler`.
- Unit tests MUST inject `HttpClient` with mocked handler into the target.
- Application code SHOULD construct these targets through the DI container.
- Real HTTP calls are allowed only in tests marked as `TestType=Integration`.
- Unit tests SHOULD cover success, transport failure, and cancellation using mocked handler responses.

Example:

```csharp
private static IRetrieveFeedItems CreateTestTarget(HttpMessageHandler handler)
{
    var feedUrl = new Uri("https://example.com/feed.xml");
    var httpClient = new HttpClient(handler);
    return new FeedRepository(httpClient, feedUrl);
}

private sealed class DelegateHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _sendAsync;

    public DelegateHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync)
    {
        _sendAsync = sendAsync;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _sendAsync(request, cancellationToken);
    }
}
```

---

### Kafka-Dependent Unit Tests

Unit tests for Kafka-producing components MUST use a mocked/fake Kafka producer abstraction and MUST NOT
attempt broker/network communication.

Rules:

- Do not use `HttpMessageHandler` for Kafka tests (Kafka is not HTTP-based).
- Production components SHOULD depend on an injectable Kafka producer abstraction (for example `IKafkaFeedProducer`) or the native Kafka producer interface.
- Unit tests MUST inject a fake/mock producer and control success/failure/cancellation behavior through that fake.
- Real broker communication belongs only in `TestType=Integration` tests.

Example:

```csharp
private static IWriteFeedItems CreateTestTarget(IKafkaFeedProducer kafkaProducer)
{
    return new FeedRepository(kafkaProducer, new FeedIdItemIdMessageMapper());
}

private sealed class FakeKafkaFeedProducer : IKafkaFeedProducer
{
    public required Func<string, string, CancellationToken, Task<KafkaProduceAck>> ProduceAsyncHandler { get; init; }

    public Task<KafkaProduceAck> ProduceAsync(string key, string payload, CancellationToken cancellationToken = default)
    {
        return ProduceAsyncHandler(key, payload, cancellationToken);
    }
}
```

---

## Test Execution

- All tests MUST be runnable via `dotnet test` from the test project root
- Tests MUST pass consistently (no flaky tests)
- Integration tests MAY require external dependencies but SHOULD document requirements
