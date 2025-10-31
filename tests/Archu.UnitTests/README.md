# Archu.UnitTests

Comprehensive unit tests for Archu's Domain, Application, and business logic layers using xUnit, Moq, AutoFixture, and FluentAssertions.

## Overview

This project contains **37 unit test classes** covering the core business logic, CQRS handlers, validators, domain entities, and common behaviors. Tests are isolated from external dependencies using mocks and test fixtures to ensure fast, reliable execution.

---

## Test Framework & Tools

| Tool | Version | Purpose |
|------|---------|---------|
| **xUnit** | 2.9.3 | Test framework |
| **Moq** | 4.20.72 | Mocking framework |
| **AutoFixture** | 4.18.1 | Test data generation |
| **AutoFixture.AutoMoq** | 4.18.1 | Auto-mocking with AutoFixture |
| **AutoFixture.Xunit2** | 4.18.1 | xUnit integration for AutoFixture |
| **FluentAssertions** | 7.0.0 | Fluent assertion library |
| **Coverlet** | 6.0.3 | Code coverage collection |

---

## Test Structure

```
Archu.UnitTests/
├── Application/
│ ├── Admin/
│   │   └── Commands/  # Admin command handler tests (6 test classes)
│   ├── Auth/
│   │   ├── Commands/      # Authentication command tests (8 test classes)
│   │   ├── Queries/          # Authentication query tests (1 test class)
│   │   └── Validators/              # Password validation tests (3 test classes)
│   ├── Common/
│   │   ├── Behaviors/                 # MediatR pipeline behaviors (2 test classes)
│   │   ├── BaseCommandHandlerTests.cs     # Base command handler tests
│   │   └── ResultTests.cs          # Result pattern tests
│   └── Products/
│       ├── Commands/        # Product command tests (3 test classes)
│       ├── Queries/        # Product query tests (2 test classes)
│       └── Validators/     # Product validation tests (3 test classes)
├── Domain/
│   ├── Common/
│   │   └── BaseEntityTests.cs          # Base entity tests (audit, soft delete, concurrency)
│   └── Entities/
│       ├── ApplicationRoleTests.cs        # ApplicationRole entity tests
│       ├── ApplicationUserTests.cs        # ApplicationUser entity tests
│       ├── EmailConfirmationTokenTests.cs # Email confirmation token tests
│    ├── PasswordResetTokenTests.cs     # Password reset token tests
│       ├── ProductTests.cs       # Product entity tests
│       └── UserRoleTests.cs    # UserRole entity tests
└── TestHelpers/
    ├── Builders/      # Test data builders (4 builders)
    ├── Exceptions/       # Test exception types
    └── Fixtures/          # Test fixtures and attributes (3 fixtures)
```

---

## Code Coverage Configuration

The project is configured with **80% coverage threshold** for both line and branch coverage:

```xml
<PropertyGroup>
  <CollectCoverage>true</CollectCoverage>
  <CoverletOutputFormat>opencover,cobertura</CoverletOutputFormat>
  <CoverletOutput>./TestResults/</CoverletOutput>
  <Threshold>80</Threshold>
  <ThresholdType>line,branch</ThresholdType>
</PropertyGroup>
```

---

## Test Coverage by Layer

### Domain Layer Tests (7 test classes)

**Purpose**: Verify domain entity behavior, business rules, and invariants.

| Test Class | Focus | Coverage |
|------------|-------|----------|
| `BaseEntityTests` | Audit tracking, soft delete, concurrency (RowVersion) | ✅ |
| `ApplicationRoleTests` | Role entity behavior and validation | ✅ |
| `ApplicationUserTests` | User entity behavior, email, password | ✅ |
| `EmailConfirmationTokenTests` | Email confirmation token generation and validation | ✅ |
| `PasswordResetTokenTests` | Password reset token generation and expiration | ✅ |
| `ProductTests` | Product entity business rules | ✅ |
| `UserRoleTests` | User-role relationship entity | ✅ |

**Key Tests**:
- ✅ Audit field tracking (CreatedAt, ModifiedAt, CreatedBy, ModifiedBy)
- ✅ Soft delete functionality (IsDeleted, DeletedAt, DeletedBy)
- ✅ RowVersion concurrency control
- ✅ Entity validation rules
- ✅ Domain invariants

---

### Application Layer Tests (30 test classes)

#### Admin Command Handlers (6 test classes)

| Test Class | Command | Coverage |
|------------|---------|----------|
| `AssignRoleCommandHandlerTests` | Assign role to user | ✅ |
| `CreateRoleCommandHandlerTests` | Create new role | ✅ |
| `CreateUserCommandHandlerTests` | Create new user | ✅ |
| `DeleteUserCommandHandlerTests` | Delete user (soft delete) | ✅ |
| `InitializeSystemCommandHandlerTests` | System initialization | ✅ |
| `RemoveRoleCommandHandlerTests` | Remove role from user | ✅ |

#### Authentication Command Handlers (8 test classes)

| Test Class | Command | Coverage |
|------------|---------|----------|
| `ChangePasswordCommandHandlerTests` | Change user password | ✅ |
| `ConfirmEmailCommandHandlerTests` | Email confirmation | ✅ |
| `ForgotPasswordCommandHandlerTests` | Password reset request | ✅ |
| `LoginCommandHandlerTests` | User login with JWT token generation | ✅ |
| `LogoutCommandHandlerTests` | User logout and token revocation | ✅ |
| `RefreshTokenCommandHandlerTests` | JWT refresh token flow | ✅ |
| `RegisterCommandHandlerTests` | User registration | ✅ |
| `ResetPasswordCommandHandlerTests` | Password reset with token | ✅ |

#### Authentication Query Handlers (1 test class)

| Test Class | Query | Coverage |
|------------|-------|----------|
| `ValidateTokenQueryHandlerTests` | JWT token validation | ✅ |

#### Authentication Validators (3 test classes)

| Test Class | Focus | Coverage |
|------------|-------|----------|
| `ChangePasswordRequestPasswordValidatorTests` | Password change validation | ✅ |
| `RegisterRequestPasswordValidatorTests` | Registration password validation | ✅ |
| `ResetPasswordRequestPasswordValidatorTests` | Password reset validation | ✅ |

**Password Policy Tests**:
- ✅ Minimum length (8 characters)
- ✅ Maximum length (100 characters)
- ✅ Uppercase letter required
- ✅ Lowercase letter required
- ✅ Digit required
- ✅ Special character required
- ✅ Empty/whitespace validation

#### Product Command Handlers (3 test classes)

| Test Class | Command | Coverage |
|------------|---------|----------|
| `CreateProductCommandHandlerTests` | Create product | ✅ |
| `DeleteProductCommandHandlerTests` | Delete product (soft delete) | ✅ |
| `UpdateProductCommandHandlerTests` | Update product with concurrency control | ✅ |

#### Product Query Handlers (2 test classes)

| Test Class | Query | Coverage |
|------------|-------|----------|
| `GetProductByIdQueryHandlerTests` | Get single product | ✅ |
| `GetProductsQueryHandlerTests` | Get paginated products | ✅ |

#### Product Validators (3 test classes)

| Test Class | Focus | Coverage |
|------------|-------|----------|
| `CreateProductCommandValidatorTests` | Create product validation | ✅ |
| `UpdateProductCommandValidatorTests` | Update product validation | ✅ |
| `ProductBoundaryValueTests` | Product boundary value testing | ✅ |

**Product Validation Tests**:
- ✅ Name required and length validation
- ✅ Price range validation
- ✅ Boundary values (min/max prices)
- ✅ RowVersion validation for updates

#### Common Tests (4 test classes)

| Test Class | Focus | Coverage |
|------------|-------|----------|
| `BaseCommandHandlerTests` | Base command handler functionality | ✅ |
| `ResultTests` | Result pattern (Success/Failure) | ✅ |
| `PerformanceBehaviorTests` | Performance monitoring pipeline | ✅ |
| `ValidationBehaviorTests` | FluentValidation pipeline | ✅ |

**MediatR Pipeline Tests**:
- ✅ Performance monitoring and logging
- ✅ Automatic validation before handler execution
- ✅ Validation error aggregation
- ✅ Performance threshold warnings

---

## Test Helpers

### Builders (Test Data Construction)

| Builder | Purpose |
|---------|---------|
| `ProductBuilder` | Fluent builder for Product entities |
| `RoleBuilder` | Fluent builder for ApplicationRole entities |
| `UserBuilder` | Fluent builder for ApplicationUser entities |
| `UserTokenBuilder` | Fluent builder for user token entities |

**Example**:
```csharp
var product = new ProductBuilder()
    .WithName("Test Product")
    .WithPrice(99.99m)
    .Build();
```

### Fixtures (Test Infrastructure)

| Fixture | Purpose |
|---------|---------|
| `AutoMoqDataAttribute` | Combines AutoFixture + AutoMoq for xUnit |
| `InlineAutoMoqDataAttribute` | Inline data with AutoFixture |
| `CommandHandlerTestFixture` | Base fixture for command handler tests |
| `QueryHandlerTestFixture` | Base fixture for query handler tests |

**Example**:
```csharp
[Theory, AutoMoqData]
public async Task Handle_ShouldCreateProduct(
    [Frozen] Mock<IProductRepository> mockRepo,
    CreateProductCommand command)
{
    // Auto-generated mocks and test data
}
```

### Custom Exceptions

| Exception | Purpose |
|-----------|---------|
| `DbUpdateConcurrencyException` | Test EF Core concurrency conflicts |

---

## Running Tests

### Run All Tests

```bash
# From repository root
dotnet test tests/Archu.UnitTests

# From test project directory
cd tests/Archu.UnitTests
dotnet test
```

### Run Specific Test Class

```bash
dotnet test --filter "FullyQualifiedName~CreateProductCommandHandlerTests"
```

### Run Specific Test

```bash
dotnet test --filter "Handle_ShouldCreateProduct_WhenValidCommand"
```

### Run with Code Coverage

```bash
# Generate coverage report
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Generate coverage report with threshold enforcement
dotnet test /p:CollectCoverage=true /p:Threshold=80 /p:ThresholdType=line,branch
```

### Run with Detailed Output

```bash
dotnet test --verbosity detailed
```

---

## Test Patterns

### 1. Arrange-Act-Assert (AAA)

```csharp
[Fact]
public async Task Handle_ShouldCreateProduct_WhenValidCommand()
{
    // Arrange
    var command = new CreateProductCommand("Test Product", 99.99m);
    var mockRepo = new Mock<IProductRepository>();
    
    // Act
    var result = await _handler.Handle(command, CancellationToken.None);
    
    // Assert
    result.IsSuccess.Should().BeTrue();
    mockRepo.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Once);
}
```

### 2. Mock Setup and Verification

```csharp
// Setup
_mockProductRepository
  .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
    .ReturnsAsync(product);

// Verification
_mockProductRepository.Verify(
    r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()),
    Times.Once);
```

### 3. FluentAssertions

```csharp
result.Should().NotBeNull();
result.IsSuccess.Should().BeTrue();
result.Value.Should().NotBeNull();
result.Value!.Name.Should().Be("Test Product");
result.Value!.Price.Should().Be(99.99m);
```

### 4. Theory Tests (Data-Driven)

```csharp
[Theory]
[InlineData("", false)]  // Empty name
[InlineData("A", true)]  // Valid name
[InlineData("ValidProduct123", true)]
public async Task Validate_ProductName(string name, bool isValid)
{
    // Test implementation
}
```

### 5. AutoFixture + AutoMoq

```csharp
[Theory, AutoMoqData]
public async Task Handle_ShouldReturnSuccess(
    [Frozen] Mock<IProductRepository> mockRepo,
    Product product,
    GetProductByIdQuery query)
{
    // mockRepo, product, and query are auto-generated
    mockRepo.Setup(r => r.GetByIdAsync(query.Id, default))
        .ReturnsAsync(product);
    
    var result = await _handler.Handle(query, default);
    
    result.IsSuccess.Should().BeTrue();
}
```

---

## What's Tested

### ✅ Domain Layer Coverage

- **BaseEntity**: Audit tracking, soft delete, concurrency control
- **Product**: Business rules, validation
- **ApplicationUser**: User management, email confirmation, password reset
- **ApplicationRole**: Role management
- **UserRole**: User-role relationships
- **EmailConfirmationToken**: Token generation, validation, expiration
- **PasswordResetToken**: Token generation, validation, expiration

### ✅ Application Layer Coverage

- **CQRS Handlers**: All commands and queries for Products, Auth, and Admin
- **Validators**: FluentValidation rules for commands
- **MediatR Behaviors**: Performance monitoring, validation pipeline
- **Result Pattern**: Success/failure handling
- **Repository Abstractions**: Mocked repository interactions
- **Password Policies**: Comprehensive password validation

### ✅ Common Patterns

- **Unit of Work**: Transaction management testing
- **Concurrency Control**: RowVersion handling and conflict detection
- **Soft Delete**: IsDeleted flag and query filtering
- **Audit Tracking**: Created/Modified timestamps and user tracking
- **Error Handling**: Exception scenarios and Result failures

---

## Best Practices

### ✅ DO

- Use **AutoFixture** for automatic test data generation
- Use **Moq** for dependency mocking
- Use **FluentAssertions** for readable assertions
- Test both **success** and **error** scenarios
- Verify **repository interactions** (Verify method calls)
- Test **validation rules** separately from handlers
- Use **Theory** tests for multiple input scenarios
- Keep tests **isolated** (no shared state)
- Test **concurrency conflicts** (RowVersion)
- Test **soft delete** behavior
- Test **audit tracking** (CreatedAt, ModifiedAt, etc.)

### ❌ DON'T

- Test implementation details (test behavior, not internals)
- Share state between tests
- Use real database in unit tests (use mocks)
- Skip exception scenarios
- Test multiple concerns in one test
- Ignore concurrency control tests
- Skip validation tests
- Hardcode test data (use AutoFixture or builders)
- Forget to verify mock interactions

---

## Known Issues

None currently identified. All 37 test classes are passing. ✅

---

## Future Improvements

### Test Coverage Expansion

1. **Additional Entity Tests** (Medium Priority)
   - Test more complex domain logic
   - Test entity relationships
   - Test domain events (if implemented)

2. **Additional Handler Tests** (Low Priority)
   - Test edge cases for existing handlers
   - Add more concurrency conflict scenarios
   - Test performance-critical paths

3. **Behavior Tests** (Low Priority)
   - Additional MediatR pipeline behaviors
   - Logging behavior tests
   - Caching behavior tests (if implemented)

### Code Quality

1. Increase code coverage to **90%+**
2. Add **mutation testing** with Stryker.NET
3. Add **performance benchmarks** with BenchmarkDotNet
4. Add **architecture tests** with NetArchTest

---

## Related Documentation

- 📖 **[Archu.Domain README](../../src/Archu.Domain/README.md)** - Domain layer documentation
- 📖 **[Archu.Application README](../../src/Archu.Application/README.md)** - Application layer documentation
- 📖 **[Integration Tests](../Archu.IntegrationTests/README.md)** - API integration tests
- 📖 **[API Client Tests](../Archu.ApiClient.Tests/README.md)** - API client unit tests
- 📖 **[Development Guide](../../docs/DEVELOPMENT_GUIDE.md)** - Development workflow
- 📖 **[Architecture Guide](../../docs/ARCHITECTURE.md)** - System architecture

---

## Statistics

| Metric | Value |
|--------|-------|
| **Total Test Classes** | 37 ✅ |
| **Domain Tests** | 7 test classes |
| **Application Tests** | 30 test classes |
| **Test Helpers** | 11 (builders, fixtures, exceptions) |
| **Passing Rate** | 100% |
| **Code Coverage Target** | 80% (line + branch) |
| **Test Execution Time** | ~5-10 seconds |
| **Framework** | xUnit 2.9.3 |
| **Target Framework** | .NET 9 |

### Test Distribution

```
Application Tests (30):
  ├── Admin Commands: 6 tests
  ├── Auth Commands: 8 tests
  ├── Auth Queries: 1 test
  ├── Auth Validators: 3 tests
  ├── Product Commands: 3 tests
  ├── Product Queries: 2 tests
  ├── Product Validators: 3 tests
  └── Common: 4 tests

Domain Tests (7):
  ├── Common: 1 test (BaseEntity)
  └── Entities: 6 tests
```

---

**Last Updated**: 2025-01-24
**Maintainer**: Archu Development Team  
**Status**: Active Development  
**Test Classes**: 37 (all passing)  
**Coverage**: Domain + Application layers fully tested
