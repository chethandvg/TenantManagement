# Query Test Improvements - Recommendations

## Overview
After analyzing the comprehensive patterns established in command handler tests, significant improvements can be applied to query handler tests. The query tests are currently basic and missing many patterns that make command tests robust and maintainable.

## Current State Analysis

### Command Tests (Excellent) ✅
- **Comprehensive coverage**: Happy path, error scenarios, edge cases, authentication
- **Structured logging verification**: Validates log fields and messages
- **Cancellation token flow**: Verifies token propagation
- **AutoFixture integration**: Reduces boilerplate, increases variability
- **Test fixture pattern**: Fluent API for setup, comprehensive verification methods
- **Organized with regions**: Clear test categorization
- **Theory-based tests**: Parameterized tests for multiple scenarios

### Query Tests (Basic - Needs Improvement) ⚠️
- **Limited coverage**: Mostly happy path scenarios
- **No structured logging tests**: Only basic information log checks
- **Basic cancellation tests**: Limited verification
- **No AutoFixture usage**: Manual test data creation
- **Limited fixture usage**: Basic setup, missing many helper methods
- **No test organization**: No regions, harder to navigate
- **Few theory tests**: Limited parameterization

## Recommended Improvements

### 1. Add Comprehensive Structured Logging Tests (HIGH PRIORITY) 🔥

**Current State**: Query tests only verify basic log messages.

```csharp
// Current - Only string matching
fixture.VerifyInformationLogged("Retrieving products");
```

**Improvement Needed**: Verify structured log fields like command tests do.

```csharp
// Recommended - Structured field verification
fixture.VerifyStructuredInformationLogged(new Dictionary<string, object?>
{
    { "PageNumber", 1 },
    { "PageSize", 10 },
    { "TotalCount", 100 }
});
```

**Benefits**:
- Ensures structured logging survives refactoring
- Validates proper field names for monitoring/alerting
- Better production debugging capabilities

**Required Changes**:
- Add structured logging verification methods to `QueryHandlerTestFixture`
- Add test section for structured logging verification
- Create tests that verify field names and values

---

### 2. Enhance Cancellation Token Verification (HIGH PRIORITY) 🔥

**Current State**: Basic cancellation test that only checks exception is thrown.

```csharp
// Current - Only verifies exception thrown
await Assert.ThrowsAsync<OperationCanceledException>(
    async () => await handler.Handle(query, cts.Token));
```

**Improvement Needed**: Verify actual token flow like command tests.

```csharp
// Recommended - Verify token propagation
fixture.VerifyGetPagedCalledWithToken(pageNumber: 1, pageSize: 10, expectedToken: cancellationToken);
```

**Benefits**:
- Verifies cancellation token properly propagates to repository
- Ensures handlers respect cancellation throughout the call chain
- Catches bugs where cancellation token is ignored

**Required Changes**:
- Add token verification methods to `QueryHandlerTestFixture`:
  - `VerifyGetPagedCalledWithToken(int pageNumber, int pageSize, CancellationToken token)`
  - `VerifyGetByIdCalledWithToken(Guid id, CancellationToken token)`
  - `VerifyGetByOwnerIdCalledWithToken(Guid ownerId, CancellationToken token)`
- Create dedicated "Cancellation Token Flow Tests" region
- Add tests for each repository method

---

### 3. Apply AutoFixture Pattern Consistently (MEDIUM PRIORITY) 📦

**Current State**: All query tests use `[Fact]` with hardcoded values.

```csharp
// Current - Manual data creation
[Fact]
public async Task Handle_ShouldReturnPagedProducts_WhenProductsExist()
{
    var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
        .WithPagedProducts(totalCount: 5, pageSize: 10, currentPage: 1);
    var query = new GetProductsQuery(PageNumber: 1, PageSize: 10);
    // ...
}
```

**Improvement Needed**: Use `[Theory, AutoMoqData]` for variable test data.

```csharp
// Recommended - AutoFixture with parameterization
[Theory, AutoMoqData]
public async Task Handle_ShouldReturnPagedProducts_WhenProductsExist(
    int pageNumber,
    int pageSize,
    int totalCount)
{
    var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
        .WithPagedProducts(totalCount, pageSize, pageNumber);
    var query = new GetProductsQuery(pageNumber, pageSize);
    // ...
}
```

**Benefits**:
- Reduces hardcoded test values
- Increases test data variability per run
- Catches edge cases with different data
- More maintainable tests

**Required Changes**:
- Convert `[Fact]` tests to `[Theory, AutoMoqData]` where appropriate
- Use method parameters instead of hardcoded values
- Keep `[InlineData]` for specific boundary value tests

---

### 4. Add Authentication Tests for Secured Queries (HIGH PRIORITY) 🔐

**Current State**: Query tests have NO authentication/authorization tests.

**Improvement Needed**: Add authentication tests for queries that require user context.

```csharp
// Recommended - Authentication verification
[Theory, AutoMoqData]
public async Task Handle_WhenUserNotAuthenticated_ThrowsException(
    int pageNumber,
    int pageSize)
{
    var fixture = new QueryHandlerTestFixture<GetMyProductsQueryHandler>()
        .WithUnauthenticatedUser();

    var handler = fixture.CreateHandler();
    var query = new GetMyProductsQuery(pageNumber, pageSize);

    await Assert.ThrowsAsync<UnauthorizedAccessException>(
        () => handler.Handle(query, CancellationToken.None));
}

[Theory]
[InlineAutoMoqData("")]
[InlineAutoMoqData("   ")]
[InlineAutoMoqData("invalid-guid")]
public async Task Handle_WhenUserIdHasInvalidFormat_ThrowsException(
    string invalidUserId,
    int pageNumber,
    int pageSize)
{
    var fixture = new QueryHandlerTestFixture<GetMyProductsQueryHandler>()
        .WithInvalidUserIdFormat(invalidUserId);

    var handler = fixture.CreateHandler();
    var query = new GetMyProductsQuery(pageNumber, pageSize);

    await Assert.ThrowsAsync<UnauthorizedAccessException>(
        () => handler.Handle(query, CancellationToken.None));
}
```

**Benefits**:
- Ensures authorization logic works correctly
- Prevents unauthorized data access
- Critical for security-sensitive queries

**Required Changes**:
- Add `ICurrentUser` mock to `QueryHandlerTestFixture`
- Add authentication setup methods:
  - `WithAuthenticatedUser(Guid userId)`
  - `WithUnauthenticatedUser()`
  - `WithInvalidUserIdFormat(string invalidUserId)`
- Create "Authentication & Authorization Tests" region
- Add tests for user-scoped queries

---

### 5. Organize Tests with Regions (LOW PRIORITY BUT VALUABLE) 📋

**Current State**: Tests are flat, no organization.

**Improvement Needed**: Group tests by concern like command tests.

```csharp
#region Happy Path Tests
// Standard success scenarios
#endregion

#region Authentication & Authorization Tests  
// User authentication/authorization scenarios (if applicable)
#endregion

#region Error Handling Tests
// Edge cases and error scenarios
#endregion

#region Logging Verification Tests
// Log message and structured logging tests
#endregion

#region Structured Logging Tests
// Structured field verification
#endregion

#region Cancellation Token Flow Tests
// Token propagation verification
#endregion

#region Pagination Tests
// Pagination-specific scenarios
#endregion

#region Repository Interaction Tests
// Verify correct repository method calls
#endregion
```

**Benefits**:
- Easier navigation in large test files
- Clear separation of concerns
- Easier to identify coverage gaps
- Better maintainability

---

### 6. Enhance QueryHandlerTestFixture (HIGH PRIORITY) 🛠️

**Current State**: Basic fixture with limited helper methods.

**Improvements Needed**:

```csharp
/// <summary>
/// Configures an authenticated user for queries requiring user context.
/// </summary>
public QueryHandlerTestFixture<THandler> WithAuthenticatedUser(Guid userId)
{
    MockCurrentUser.Setup(x => x.UserId).Returns(userId.ToString());
    MockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
    return this;
}

/// <summary>
/// Configures an unauthenticated user.
/// </summary>
public QueryHandlerTestFixture<THandler> WithUnauthenticatedUser()
{
    MockCurrentUser.Setup(x => x.UserId).Returns((string?)null);
    MockCurrentUser.Setup(x => x.IsAuthenticated).Returns(false);
    return this;
}

/// <summary>
/// Configures an invalid user ID format.
/// </summary>
public QueryHandlerTestFixture<THandler> WithInvalidUserIdFormat(string invalidUserId)
{
    MockCurrentUser.Setup(x => x.UserId).Returns(invalidUserId);
    MockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
    return this;
}

/// <summary>
/// Verifies GetPagedAsync was called with specific cancellation token.
/// </summary>
public void VerifyGetPagedCalledWithToken(int pageNumber, int pageSize, CancellationToken expectedToken)
{
    MockProductRepository.Verify(
        r => r.GetPagedAsync(pageNumber, pageSize, It.Is<CancellationToken>(t => t == expectedToken)),
        Times.Once());
}

/// <summary>
/// Verifies GetByIdAsync was called with specific cancellation token.
/// </summary>
public void VerifyGetByIdCalledWithToken(Guid productId, CancellationToken expectedToken)
{
    MockProductRepository.Verify(
        r => r.GetByIdAsync(productId, It.Is<CancellationToken>(t => t == expectedToken)),
        Times.Once());
}

/// <summary>
/// Verifies structured log fields in Information logs.
/// </summary>
public void VerifyStructuredInformationLogged(Dictionary<string, object?> expectedFields, Times? times = null)
{
    MockLogger.Verify(
        x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => VerifyLogState(v, expectedFields)),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        times ?? Times.Once());
}

/// <summary>
/// Verifies structured log fields in Warning logs.
/// </summary>
public void VerifyStructuredWarningLogged(Dictionary<string, object?> expectedFields, Times? times = null)
{
    // Similar implementation
}

/// <summary>
/// Creates a query handler instance with configured mocks.
/// </summary>
public THandler CreateHandler()
{
    // Use reflection to find constructor or custom factory
    // Similar to CommandHandlerTestFixture.CreateHandler()
}

/// <summary>
/// Helper method to verify structured log state.
/// </summary>
private static bool VerifyLogState(object state, Dictionary<string, object?> expectedFields)
{
    // Implementation from CommandHandlerTestFixture
}
```

**Benefits**:
- Parity with CommandHandlerTestFixture capabilities
- Reduces test code duplication
- Makes query tests as robust as command tests

---

### 7. Add Query-Specific Test Scenarios (MEDIUM PRIORITY) 📊

**Current State**: Limited scenario coverage.

**Improvements Needed**:

#### Empty Result Tests
```csharp
[Theory, AutoMoqData]
public async Task Handle_WhenNoProductsExist_ReturnsEmptyResult(
    int pageNumber,
    int pageSize)
{
    var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
        .WithEmptyProductList();

    var handler = fixture.CreateHandler();
    var query = new GetProductsQuery(pageNumber, pageSize);

    var result = await handler.Handle(query, CancellationToken.None);

    result.Should().NotBeNull();
    result.Items.Should().BeEmpty();
    result.TotalCount.Should().Be(0);
}
```

#### Boundary Value Tests for Pagination
```csharp
[Theory]
[InlineAutoMoqData(0, 10)]    // Page 0
[InlineAutoMoqData(-1, 10)]   // Negative page
[InlineAutoMoqData(1, 0)]     // Zero page size
[InlineAutoMoqData(1, -1)]    // Negative page size
[InlineAutoMoqData(1, 1001)]  // Exceeds max page size
public async Task Handle_WithInvalidPaginationParameters_ReturnsValidationError(
    int pageNumber,
    int pageSize)
{
    // Test validation logic
}
```

#### Large Dataset Tests
```csharp
[Theory]
[InlineAutoMoqData(1, 100, 10000)]   // Large dataset
[InlineAutoMoqData(100, 100, 10000)] // Last page
[InlineAutoMoqData(101, 100, 10000)] // Beyond last page
public async Task Handle_WithLargeDatasets_CalculatesPaginationCorrectly(
    int pageNumber,
    int pageSize,
    int totalCount)
{
    // Test pagination math with large numbers
}
```

#### Sorting and Filtering Tests (if applicable)
```csharp
[Theory, AutoMoqData]
public async Task Handle_WithSortParameter_ReturnsCorrectlySortedResults(
    string sortField,
    string sortDirection)
{
    // Test query with sorting
}

[Theory, AutoMoqData]
public async Task Handle_WithFilterParameter_ReturnsFilteredResults(
    string filterField,
    string filterValue)
{
    // Test query with filtering
}
```

---

### 8. Add Performance/Efficiency Tests (LOW PRIORITY) ⚡

```csharp
[Theory, AutoMoqData]
public async Task Handle_ShouldCallRepositoryOnlyOnce(
    int pageNumber,
    int pageSize)
{
    var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
        .WithPagedProducts(totalCount: 100, pageSize, pageNumber);

    var handler = fixture.CreateHandler();
    var query = new GetProductsQuery(pageNumber, pageSize);

    await handler.Handle(query, CancellationToken.None);

    // Verify no extra repository calls
    fixture.VerifyGetPagedCalled(pageNumber, pageSize, Times.Once());
}

[Theory, AutoMoqData]
public async Task Handle_ShouldNotLoadEntireDataset_WhenPaginating(
    int pageNumber,
    int pageSize)
{
    // Verify that only requested page is fetched, not all data
    var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
        .WithPagedProducts(totalCount: 10000, pageSize, pageNumber);

    var handler = fixture.CreateHandler();
    var query = new GetProductsQuery(pageNumber, pageSize);

    var result = await handler.Handle(query, CancellationToken.None);

    result.Items.Count.Should().BeLessOrEqualTo(pageSize);
    fixture.VerifyGetAllCalled(Times.Never()); // Should not call GetAll
}
```

---

## Priority Implementation Order

### Phase 1: Critical Improvements (Do First) 🔥
1. **Add Structured Logging Tests** - Essential for production monitoring
2. **Enhance Cancellation Token Verification** - Important for responsive operations
3. **Add Authentication Tests** - Critical for security-sensitive queries
4. **Enhance QueryHandlerTestFixture** - Foundation for other improvements

### Phase 2: Quality Improvements (Do Second) 📦
5. **Apply AutoFixture Pattern** - Reduces maintenance burden
6. **Add Query-Specific Scenarios** - Improves coverage
7. **Organize Tests with Regions** - Better navigation

### Phase 3: Optional Enhancements (Nice to Have) ⚡
8. **Add Performance Tests** - Validates efficiency

---

## Expected Impact

### Before Improvements
- **Test Count**: ~15 basic tests
- **Code Coverage**: Basic happy path and simple error cases
- **Maintainability**: Low - hardcoded values, no structure
- **Robustness**: Low - missing critical scenarios

### After Improvements
- **Test Count**: ~40-60 comprehensive tests
- **Code Coverage**: Happy path, error cases, authentication, logging, cancellation, pagination edge cases
- **Maintainability**: High - AutoFixture, fixture helpers, organized regions
- **Robustness**: High - comprehensive scenario coverage

### Metrics
- **~4x increase in test coverage**
- **~40% reduction in boilerplate** (AutoFixture + fixture helpers)
- **100% parity with command test patterns**
- **Better production debugging** (structured logging tests)
- **Enhanced security** (authentication tests)

---

## Example: Complete Refactored Query Test File

```csharp
namespace Archu.UnitTests.Application.Products.Queries;

[Trait("Category", "Unit")]
[Trait("Feature", "Products")]
public class GetProductsQueryHandlerTests
{
    #region Happy Path Tests

    [Theory, AutoMoqData]
    public async Task Handle_ShouldReturnPagedProducts_WhenProductsExist(
        int pageNumber,
        int pageSize,
        int totalCount)
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount, pageSize, pageNumber);

        var handler = fixture.CreateHandler();
        var query = new GetProductsQuery(pageNumber, pageSize);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(totalCount);
        result.PageNumber.Should().Be(pageNumber);
        result.PageSize.Should().Be(pageSize);
    }

    [Theory, AutoMoqData]
    public async Task Handle_ShouldReturnEmptyPagedResult_WhenNoProductsExist(
        int pageNumber,
        int pageSize)
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithEmptyProductList();

        var handler = fixture.CreateHandler();
        var query = new GetProductsQuery(pageNumber, pageSize);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    #endregion

    #region Logging Verification Tests

    [Theory, AutoMoqData]
    public async Task Handle_LogsInformation_WhenRetrievingProducts(
        int pageNumber,
        int pageSize)
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount: 15, pageSize, pageNumber);

        var handler = fixture.CreateHandler();
        var query = new GetProductsQuery(pageNumber, pageSize);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        fixture.VerifyInformationLogged("Retrieving products");
    }

    [Theory, AutoMoqData]
    public async Task Handle_LogsInformation_AfterProductsRetrieved(
        int pageNumber,
        int pageSize,
        int totalCount)
    {
        // Arrange
        var itemsReturned = Math.Min(pageSize, totalCount);
        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount, pageSize, pageNumber);

        var handler = fixture.CreateHandler();
        var query = new GetProductsQuery(pageNumber, pageSize);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        fixture.VerifyInformationLogged($"Retrieved {itemsReturned} products");
    }

    #endregion

    #region Structured Logging Tests

    [Theory, AutoMoqData]
    public async Task Handle_LogsWithStructuredPaginationParameters(
        int pageNumber,
        int pageSize,
        int totalCount)
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount, pageSize, pageNumber);

        var handler = fixture.CreateHandler();
        var query = new GetProductsQuery(pageNumber, pageSize);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        fixture.VerifyStructuredInformationLogged(new Dictionary<string, object?>
        {
            { "PageNumber", pageNumber },
            { "PageSize", pageSize }
        });
    }

    [Theory, AutoMoqData]
    public async Task Handle_LogsWithStructuredResultCount(
        int pageNumber,
        int pageSize,
        int totalCount)
    {
        // Arrange
        var itemsReturned = Math.Min(pageSize, Math.Max(0, totalCount - (pageNumber - 1) * pageSize));
        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount, pageSize, pageNumber);

        var handler = fixture.CreateHandler();
        var query = new GetProductsQuery(pageNumber, pageSize);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        fixture.VerifyStructuredInformationLogged(new Dictionary<string, object?>
        {
            { "TotalCount", totalCount },
            { "ReturnedCount", itemsReturned }
        });
    }

    #endregion

    #region Cancellation Token Flow Tests

    [Theory, AutoMoqData]
    public async Task Handle_PassesCancellationTokenToGetPagedAsync(
        int pageNumber,
        int pageSize)
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount: 100, pageSize, pageNumber);

        var handler = fixture.CreateHandler();
        var query = new GetProductsQuery(pageNumber, pageSize);

        // Act
        await handler.Handle(query, cancellationToken);

        // Assert
        fixture.VerifyGetPagedCalledWithToken(pageNumber, pageSize, cancellationToken);
    }

    [Theory, AutoMoqData]
    public async Task Handle_RespectsCancellationToken(
        int pageNumber,
        int pageSize)
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithCancelledOperation();

        var handler = fixture.CreateHandler();
        var query = new GetProductsQuery(pageNumber, pageSize);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.Handle(query, cts.Token));
    }

    #endregion

    #region Pagination Tests

    [Theory]
    [InlineAutoMoqData(1, 10, 5)]
    [InlineAutoMoqData(2, 10, 25)]
    [InlineAutoMoqData(1, 20, 100)]
    [InlineAutoMoqData(5, 10, 50)]
    public async Task Handle_CalculatesPaginationCorrectly(
        int pageNumber,
        int pageSize,
        int totalCount)
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount, pageSize, pageNumber);

        var handler = fixture.CreateHandler();
        var query = new GetProductsQuery(pageNumber, pageSize);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        var expectedItems = Math.Min(pageSize, Math.Max(0, totalCount - (pageNumber - 1) * pageSize));
        result.Items.Should().HaveCount(expectedItems);
        result.TotalCount.Should().Be(totalCount);
        result.PageNumber.Should().Be(pageNumber);
        result.PageSize.Should().Be(pageSize);
    }

    [Theory, AutoMoqData]
    public async Task Handle_CalculatesTotalPagesCorrectly(
        int pageSize,
        int totalCount)
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount, pageSize, currentPage: 1);

        var handler = fixture.CreateHandler();
        var query = new GetProductsQuery(1, pageSize);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        var expectedPages = (int)Math.Ceiling((double)totalCount / pageSize);
        result.TotalPages.Should().Be(expectedPages);
    }

    #endregion

    #region Repository Interaction Tests

    [Theory, AutoMoqData]
    public async Task Handle_CallsRepositoryOnlyOnce(
        int pageNumber,
        int pageSize)
    {
        // Arrange
        var fixture = new QueryHandlerTestFixture<GetProductsQueryHandler>()
            .WithPagedProducts(totalCount: 100, pageSize, pageNumber);

        var handler = fixture.CreateHandler();
        var query = new GetProductsQuery(pageNumber, pageSize);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        fixture.VerifyGetPagedCalled(pageNumber, pageSize, Times.Once());
    }

    #endregion
}
```

---

## Conclusion

Applying these improvements will bring query tests to the same high standard as command tests:

✅ **Comprehensive coverage** - All scenarios tested
✅ **Structured logging** - Production-ready monitoring
✅ **Security** - Authentication/authorization verified
✅ **Robustness** - Cancellation and error handling
✅ **Maintainability** - AutoFixture, fixtures, organization
✅ **Consistency** - Same patterns as command tests

**Estimated effort**: 2-3 days for complete implementation
**Impact**: Significantly improved test quality and production confidence

The query tests will be just as robust, maintainable, and comprehensive as the command tests, ensuring consistency across your entire test suite.
