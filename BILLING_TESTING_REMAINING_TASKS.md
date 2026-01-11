# Billing Engine Testing Suite - Remaining Tasks

## Overview

This document tracks the remaining testing tasks that were not completed in the initial billing engine testing suite implementation. The initial implementation delivered 177 tests (166 unit + 11 integration) covering core functionality, but several areas remain to be addressed.

## Completed in Previous Work ✅

- ✅ All proration calculator tests (28 tests)
- ✅ All calculation service tests (39 tests)
- ✅ All invoice lifecycle tests (70 tests)
- ✅ InvoicesController API tests (10 tests)
- ✅ All 10 edge case scenarios (19 tests)
- ✅ Integration tests - E2E, batch, concurrency (11 tests)
- ✅ Performance tests with 50+ leases

**Total Completed: 177 tests**

## Remaining Tasks

### 1. API Controller Tests (6 controllers remaining)

**Priority: High**

The following billing API controllers need comprehensive unit tests:

#### BillingSettingsController
- [ ] Get billing settings endpoint
- [ ] Update billing settings endpoint
- [ ] Validation tests (billing day 1-28, payment terms)
- [ ] Error handling (lease not found, etc.)
- [ ] Proration method selection validation

#### CreditNotesController
- [ ] Create credit note endpoint
- [ ] List credit notes endpoint (with filters)
- [ ] Get credit note by ID endpoint
- [ ] Issue credit note endpoint
- [ ] Void credit note endpoint
- [ ] Authorization checks

#### InvoiceRunsController
- [ ] Create monthly invoice run endpoint
- [ ] Create utility invoice run endpoint
- [ ] Get invoice run details endpoint
- [ ] List invoice runs endpoint
- [ ] Status tracking validation

#### ChargeTypesController
- [ ] List charge types endpoint
- [ ] Get charge type endpoint
- [ ] Create charge type endpoint
- [ ] Update charge type endpoint
- [ ] Validation tests

#### RecurringChargesController
- [ ] Create recurring charge endpoint
- [ ] List recurring charges endpoint (by lease)
- [ ] Update recurring charge endpoint
- [ ] Delete recurring charge endpoint
- [ ] Frequency validation (monthly/annual)

#### UtilityStatementsController
- [ ] Create utility statement endpoint
- [ ] List utility statements endpoint
- [ ] Get utility statement endpoint
- [ ] Finalize utility statement endpoint
- [ ] Versioning validation

**Estimated Tests**: ~50-60 tests (8-10 tests per controller)

### 2. Performance Tests

**Priority: Medium**

#### Load Testing
- [ ] **Batch generation with 1000+ leases**
  - Batch invoice generation performance benchmark
  - Memory usage validation
  - Response time thresholds (< 60 seconds)
  - Database connection pool handling
  - Concurrent batch runs

#### Database Performance
- [ ] **Query performance analysis**
  - Index effectiveness validation
  - Query execution plan analysis
  - N+1 query detection
  - Pagination performance (large datasets)
  - Join optimization verification

**Estimated Tests**: ~5-10 performance tests

### 3. UI Tests

**Priority: Low** (Requires infrastructure setup)

These tests require Playwright or Selenium infrastructure which was out of scope for the initial implementation:

#### Billing Settings UI
- [ ] Form field validation (billing day, payment terms)
- [ ] Proration method selection
- [ ] Save and cancel workflows
- [ ] Error message display
- [ ] Default value population

#### Invoice List UI
- [ ] Status filter functionality
- [ ] Date range filtering
- [ ] Organization filtering
- [ ] Pagination navigation
- [ ] Sort by column
- [ ] Search functionality

#### Invoice Workflows UI
- [ ] Issue invoice button and confirmation
- [ ] Void invoice modal with reason
- [ ] Status update reflection in UI
- [ ] Permission-based button visibility
- [ ] Optimistic UI updates

#### Utility Statement UI
- [ ] Form validation
- [ ] Meter reading input
- [ ] Amount calculation preview
- [ ] Finalize statement workflow
- [ ] Version history display

#### Credit Note UI
- [ ] Invoice selection dropdown
- [ ] Line item selection (partial/full)
- [ ] Amount validation
- [ ] Reason dropdown selection
- [ ] Submit workflow

#### Tenant Portal UI
- [ ] Invoice detail display (read-only)
- [ ] Line items table display
- [ ] Payment status indicator
- [ ] Download invoice PDF
- [ ] Payment history

**Estimated Tests**: ~30-40 UI tests

## Implementation Recommendations

### API Controller Tests

Follow the pattern established in `InvoicesControllerTests.cs`:

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "Billing")]
[Trait("Component", "Controller")]
public class BillingSettingsControllerTests
{
    private readonly Mock<ILeaseBillingSettingRepository> _mockRepository;
    private readonly Mock<ILeaseRepository> _mockLeaseRepository;
    private readonly BillingSettingsController _controller;
    
    // Constructor setup
    
    [Fact]
    public async Task GetBillingSettings_WithValidLeaseId_ReturnsOk()
    {
        // Arrange
        // Act
        // Assert
    }
    
    [Fact]
    public async Task UpdateBillingSettings_WithInvalidBillingDay_ReturnsBadRequest()
    {
        // Test billing day > 28
    }
}
```

**Guidelines:**
- Use Moq for service/repository mocking
- Test successful operations (200 OK)
- Test validation failures (400 Bad Request)
- Test not found scenarios (404 Not Found)
- Test authorization checks (401/403)
- Test error handling (500 Internal Server Error)

### Performance Tests

Create `BillingPerformanceTests.cs` in the integration test project:

```csharp
[Collection("Integration Tests")]
[Trait("Category", "Performance")]
[Trait("Feature", "Billing")]
public class BillingPerformanceTests
{
    [Fact]
    public async Task MonthlyInvoiceRun_With1000Leases_CompletesInUnder60Seconds()
    {
        // Seed 1000 leases
        // Measure execution time
        // Assert completion time < 60 seconds
        // Assert memory usage within limits
    }
}
```

**Considerations:**
- Use BenchmarkDotNet for detailed metrics
- Set baseline performance thresholds
- Monitor memory usage and garbage collection
- Test with realistic data volumes
- Validate database connection pooling

### UI Tests

Set up infrastructure first, then create `TentMan.UiTests` project:

```csharp
public class BillingSettingsPageTests : IAsyncLifetime
{
    private IPlaywright _playwright;
    private IBrowser _browser;
    private IPage _page;
    
    [Fact]
    public async Task UpdateBillingDay_WithValidValue_SavesSuccessfully()
    {
        // Navigate to billing settings
        // Update billing day
        // Click save
        // Verify success message
        // Verify updated value persists
    }
}
```

**Setup Requirements:**
1. Install Playwright NuGet package
2. Configure browser automation
3. Implement Page Object Model
4. Add screenshot capture on failure
5. Integrate with CI/CD pipeline

## Acceptance Criteria

### API Controller Tests
- [ ] All 6 remaining controllers have comprehensive unit tests
- [ ] Minimum 8 tests per controller
- [ ] All tests pass successfully
- [ ] Code coverage maintained at 80%+
- [ ] Tests follow AAA pattern (Arrange-Act-Assert)
- [ ] Descriptive test names: `Method_Scenario_ExpectedResult`

### Performance Tests
- [ ] 1000+ lease test completes in < 60 seconds
- [ ] Memory usage documented and within acceptable limits
- [ ] Database query performance baselines established
- [ ] N+1 query issues identified and documented
- [ ] Performance test results documented

### UI Tests
- [ ] Playwright/Selenium infrastructure configured
- [ ] All 6 UI workflow categories have tests
- [ ] Tests run successfully in headless mode
- [ ] Screenshots captured on test failures
- [ ] Tests integrated into CI/CD pipeline
- [ ] Page Object Model pattern implemented

## Out of Scope

The following were intentionally excluded or determined to be infeasible:

- ❌ **Database index performance testing** - Requires specialized database analysis tools (SQL Server Query Store, execution plan analysis tools) not available in standard test environment
- ❌ **Custom agent testing** - No custom agents exist for the billing domain
- ❌ **Manual exploratory testing** - Should be done separately as part of QA process

## Priority and Sequencing

### Phase 1 (High Priority)
1. API Controller Tests - BillingSettingsController
2. API Controller Tests - CreditNotesController
3. API Controller Tests - InvoiceRunsController

### Phase 2 (Medium Priority)
4. API Controller Tests - ChargeTypesController
5. API Controller Tests - RecurringChargesController
6. API Controller Tests - UtilityStatementsController
7. Performance Tests - 1000+ lease load test

### Phase 3 (Low Priority - Separate Epic)
8. UI Test Infrastructure Setup
9. UI Tests - All workflows

## Estimated Effort

| Task Category | Estimated Time | Complexity |
|---------------|----------------|------------|
| API Controller Tests (6 controllers) | 2-3 days | Medium |
| Performance Tests | 1-2 days | Medium-High |
| UI Test Infrastructure | 1-2 days | High |
| UI Tests Implementation | 2-3 days | Medium |
| **Total** | **6-10 days** | - |

## Success Metrics

Upon completion of all remaining tasks:

- **Unit Tests**: 210-220 tests (current 166 + 50-60 new controller tests)
- **Integration Tests**: 16-21 tests (current 11 + 5-10 performance tests)
- **UI Tests**: 30-40 tests (new)
- **Total Tests**: 260-280 tests
- **Code Coverage**: Maintained at 80%+ across all billing code
- **Performance**: Documented baselines for 1000+ lease scenarios

## Related Documentation

- `BILLING_TEST_SUITE_SUMMARY.md` - Current test coverage summary
- `tests/TESTING_GUIDE.md` - Testing infrastructure and patterns
- Original Issue: Unit, integration, and edge case testing for billing engine
- Parent Epic: #45 - Billing Engine Feature

## Notes

- All new tests should follow the established patterns in existing test files
- Use FluentAssertions for readable assertions
- Maintain consistent test naming conventions
- Add appropriate test traits for categorization
- Document any test data setup requirements
- Consider test execution time and avoid slow tests where possible
