# EisenFeed Testing Standards

**Purpose**: Establish consistent conventions for unit and integration tests across all test projects.

---

## Naming & Variable Conventions

### Class Under Test Variable

The class or component being tested **MUST** always use the variable name `target`.

```csharp
[Fact]
public async Task FetchAsync_WhenFeedIsReachable_ReturnsRawRssPayload()
{
    var target = CreateTestTarget();
    
    string payload = await target.FetchAsync(new Uri("https://example.com/feed.xml"), CancellationToken.None);
    
    Assert.False(string.IsNullOrWhiteSpace(payload));
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
private static IReadRssFeeds CreateTestTarget()
{
    return new FeedRepository();
}
```

**Rationale**:
- Parallel to `target` variable naming
- Explicit about intent: creating a target for testing
- Consistent factory naming across all tests

### Dependencies and Fixtures

Use domain-specific names for dependencies and test fixtures:

```csharp
private static IFeedItemStore CreateMockItemStore()
{
    // Return mock or test double
}

private static FeedSource CreateTestFeed(string feedId = "test-feed")
{
    // Return fixture
}
```

---

## Test Method Naming

### Pattern: `MethodName_WhenCondition_ThenExpectation`

```csharp
public async Task FetchAsync_WhenFeedIsReachable_ReturnsRawRssPayload()
public async Task PublishAsync_WhenKafkaAckReceived_ReturnsSuccessfulDeliveryResult()
public async Task ParseAsync_WhenXmlIsMalformed_ThrowsFormatException()
```

---

## Arrange-Act-Assert Structure

```csharp
[Fact]
public async Task FetchAsync_WhenFeedIsReachable_ReturnsRawRssPayload()
{
    // Arrange: Setup target and dependencies
    var target = CreateTestTarget();
    var feedUrl = new Uri("https://example.com/feed.xml");
    
    // Act: Execute the method under test
    string payload = await target.FetchAsync(feedUrl, CancellationToken.None);
    
    // Assert: Verify expected outcome
    Assert.False(string.IsNullOrWhiteSpace(payload));
    Assert.Contains("<rss", payload, StringComparison.OrdinalIgnoreCase);
}
```

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

---

## Test Class Organization

- One test class per class or interface being tested
- Happy-path tests in primary test class (e.g., `FeedRepositoryTests`)
- Failure/error-path tests in dedicated failure test class (e.g., `FeedRepositoryFailureTests`)
- Use `sealed` modifier on test classes to prevent inheritance

```csharp
public sealed class FeedRepositoryTests
{
    [Fact]
    public async Task FetchAsync_WhenFeedIsReachable_ReturnsRawRssPayload() { ... }
}

public sealed class FeedRepositoryFailureTests
{
    [Fact]
    public async Task FetchAsync_WhenFeedIsUnavailable_ThrowsHttpRequestException() { ... }
}
```

---

## Async Testing

- Use `async Task` for all async test methods
- Use `await` consistently; never use `.Result` or `.Wait()` to block on tasks
- Prefer `Assert.ThrowsAsync<T>` for exception testing in async contexts

```csharp
[Fact]
public async Task FetchAsync_WhenCancellationIsRequested_ThrowsOperationCanceledException()
{
    var target = CreateTestTarget();
    using var cts = new CancellationTokenSource();
    cts.Cancel();
    
    await Assert.ThrowsAnyAsync<OperationCanceledException>(
        () => target.FetchAsync(new Uri("https://example.com/feed.xml"), cts.Token));
}
```

---

## Stub & Mock Patterns

- Use `NotImplementedException` for stub implementations during TDD
- For mock/test-double needs, prefer `Moq` or similar only when necessary
- Inline simple test doubles when possible; use factories for complex ones

---

## Test Execution

- All tests MUST be runnable via `dotnet test` from the test project root
- Tests MUST pass consistently (no flaky tests)
- Integration tests MAY require external dependencies but SHOULD document requirements
