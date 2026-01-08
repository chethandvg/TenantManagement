# Contributing to TentMan

This document provides guidelines for contributors and coding agents working on the TentMan project. Following these standards ensures maintainable, modular, and reusable code.

---

## 📋 Table of Contents

- [Code Organization Guidelines](#code-organization-guidelines)
- [Backend (.NET/C#) Guidelines](#backend-netc-guidelines)
- [Frontend (Blazor) Guidelines](#frontend-blazor-guidelines)
- [Documentation Requirements](#documentation-requirements)
- [Testing Requirements](#testing-requirements)
- [Pull Request Guidelines](#pull-request-guidelines)

---

## 🏗️ Code Organization Guidelines

### File Size Limits

To maintain readability and modularity:

| File Type | Maximum Lines | Acceptable Variance | Action When Exceeded |
|-----------|--------------|---------------------|---------------------|
| `.cs` files | **300 lines** | +30 lines (max 330) | Use partial classes or refactor |
| `.razor` files | **200 lines** | +20 lines (max 220) | Extract components |
| `.razor.cs` code-behind | **300 lines** | +30 lines (max 330) | Use partial classes |

### When to Use Partial Classes

Use partial classes when:
- A class exceeds 300 lines of code
- A class has distinct logical sections (e.g., properties, methods, event handlers)
- You need to separate generated code from hand-written code
- A component has complex UI and complex logic

**Example Structure:**
```
MyService.cs                 # Main class definition, core methods
MyService.Validation.cs      # Validation logic partial
MyService.Mapping.cs         # Mapping logic partial
```

---

## 🔧 Backend (.NET/C#) Guidelines

### File Naming Conventions

| Item | Convention | Example |
|------|------------|---------|
| Entity | `{Name}.cs` | `Product.cs`, `Building.cs` |
| Repository Interface | `I{Name}Repository.cs` | `IProductRepository.cs` |
| Repository Implementation | `{Name}Repository.cs` | `ProductRepository.cs` |
| Command | `{Action}{Entity}Command.cs` | `CreateProductCommand.cs` |
| Command Handler | `{Action}{Entity}CommandHandler.cs` | `CreateProductCommandHandler.cs` |
| Query | `Get{Entity/Entities}Query.cs` | `GetProductsQuery.cs` |
| Query Handler | `Get{Entity/Entities}QueryHandler.cs` | `GetProductsQueryHandler.cs` |
| DTO | `{Entity}Dto.cs` | `ProductDto.cs` |
| Request | `{Action}{Entity}Request.cs` | `CreateProductRequest.cs` |
| Response | `{Entity}Response.cs` | `ProductResponse.cs` |
| Controller | `{Entity}Controller.cs` | `ProductsController.cs` |
| Validator | `{Command}Validator.cs` | `CreateProductCommandValidator.cs` |

### Class Organization (300 LOC Limit)

Each `.cs` file should contain:

```csharp
// 1. File header (if required)
// 2. Using statements
// 3. Namespace declaration
// 4. Single class/interface/record/enum

namespace TentMan.Domain.Entities;

/// <summary>
/// Brief description of the class purpose.
/// </summary>
public class MyEntity
{
    // Properties first (grouped logically)
    
    // Constructors
    
    // Public methods
    
    // Private/protected methods
}
```

### Partial Class Structure

When a class exceeds 300 lines, split it logically:

**Main file (e.g., `AuthenticationService.cs`):**
```csharp
namespace TentMan.Infrastructure.Authentication;

/// <summary>
/// Handles user authentication operations.
/// </summary>
public partial class AuthenticationService : IAuthenticationService
{
    private readonly ILogger<AuthenticationService> _logger;
    
    public AuthenticationService(ILogger<AuthenticationService> logger)
    {
        _logger = logger;
    }
    
    // Core authentication methods here
}
```

**Partial file (e.g., `AuthenticationService.TokenManagement.cs`):**
```csharp
namespace TentMan.Infrastructure.Authentication;

/// <summary>
/// Token management methods for AuthenticationService.
/// </summary>
public partial class AuthenticationService
{
    // Token-related methods here
}
```

### CQRS Pattern

Follow the Command Query Responsibility Segregation pattern:

- **Commands**: Modify state, return result (success/failure)
- **Queries**: Read data, never modify state
- Use MediatR for handling requests

### Repository Pattern

- One repository per aggregate root
- Repositories return domain entities, not DTOs
- Use specification pattern for complex queries

### Dependency Injection

- Register services in `DependencyInjection.cs` within each project
- Use interfaces for all services
- Prefer constructor injection

---

## 🎨 Frontend (Blazor) Guidelines

### Component Organization

Maintain modular, reusable components:

| Component Type | Location | Max Lines | Purpose |
|---------------|----------|-----------|---------|
| Pages | `Pages/` | 200 | Route-based views |
| Shared Components | `Components/` | 150 | Reusable UI elements |
| Layouts | `Layouts/` | 150 | Page structure templates |
| State Containers | `State/` | 200 | State management |

> **Note**: Create a `State/` folder at the same level as `Pages/`, `Components/`, and `Layouts/` in the Blazor project if it does not already exist. This folder is used to store state container services.

### Component File Structure

Each component should have:

```
Components/
├── MyComponent/
│   ├── MyComponent.razor        # Markup only (max 100 lines preferred)
│   ├── MyComponent.razor.cs     # Code-behind (max 300 lines (+30 max))
│   └── MyComponent.razor.css    # Scoped styles (optional)
```

### Code-Behind Pattern (Required)

**Always** use code-behind files for components with logic:

**MyComponent.razor:**
```razor
<div class="my-component">
    <h3>@Title</h3>
    @if (IsLoading)
    {
        <MudProgressCircular Indeterminate="true" />
    }
    else
    {
        <MudText>@Content</MudText>
        <MudButton OnClick="HandleClick">Click Me</MudButton>
    }
</div>
```

**MyComponent.razor.cs:**
```csharp
namespace TentMan.Ui.Components;

public partial class MyComponent : ComponentBase
{
    [Parameter] public string Title { get; set; } = string.Empty;
    [Parameter] public string Content { get; set; } = string.Empty;
    
    private bool IsLoading { get; set; }
    
    private async Task HandleClick()
    {
        IsLoading = true;
        // Logic here
        IsLoading = false;
    }
}
```

### Component Design Principles

1. **Single Responsibility**: Each component does one thing well
2. **Reusability**: Design components to be used in multiple contexts
3. **Composability**: Build complex UIs from simple components
4. **Isolation**: Components should not depend on parent implementation details

### State Management

- Use cascading parameters for shared state
- Use state containers (`State/`) for complex state
- Avoid passing too many parameters (max 5-7 per component)
- Use EventCallback for child-to-parent communication

### Component Extraction Guidelines

Extract a new component when:
- UI section is reused in multiple places
- A section has independent state/logic
- The parent component exceeds line limits
- Logic can be tested independently

**Before (monolithic):**
```razor
<div class="product-list">
    @foreach (var product in Products)
    {
        <div class="product-card">
            <img src="@product.ImageUrl" />
            <h4>@product.Name</h4>
            <p>@product.Description</p>
            <span>@product.Price.ToString("C")</span>
            <button @onclick="() => AddToCart(product)">Add</button>
        </div>
    }
</div>
```

**After (modular):**
```razor
<div class="product-list">
    @foreach (var product in Products)
    {
        <ProductCard Product="@product" OnAddToCart="AddToCart" />
    }
</div>
```

### MudBlazor Component Usage

- Prefer MudBlazor components over raw HTML
- Use MudBlazor theming for consistent styling
- Follow MudBlazor accessibility guidelines

---

## 📚 Documentation Requirements

### README Files

Every project folder must have a `README.md` containing:

1. **Purpose**: What the project/folder contains
2. **Structure**: Folder organization
3. **Dependencies**: What this project depends on
4. **Usage**: How to use/integrate
5. **Guidelines**: Specific coding guidelines for this area

### Code Comments

- Use XML documentation for public APIs
- Avoid obvious comments; write self-documenting code
- Document complex algorithms and business rules
- Keep comments up-to-date with code changes

### Inline Documentation

```csharp
/// <summary>
/// Creates a new product in the system.
/// </summary>
/// <param name="command">The product creation command.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>The created product DTO.</returns>
/// <exception cref="ValidationException">Thrown when validation fails.</exception>
public async Task<ProductDto> Handle(
    CreateProductCommand command, 
    CancellationToken cancellationToken)
{
    // Implementation
}
```

---

## 🧪 Testing Requirements

### Test File Organization

```
tests/
├── TentMan.UnitTests/
│   ├── Application/
│   │   └── Products/
│   │       └── GetProductsQueryHandlerTests.cs
│   └── Domain/
├── TentMan.IntegrationTests/
│   └── Endpoints/
│       └── ProductsEndpointTests.cs
└── TentMan.ApiClient.Tests/
    └── ProductsApiClientTests.cs
```

### Test Naming Convention

```
[MethodName]_Should[ExpectedBehavior]_When[Condition]
```

Examples:
- `Handle_ShouldReturnProducts_WhenProductsExist`
- `CreateProduct_ShouldThrowValidationException_WhenNameIsEmpty`

### Test Structure (AAA Pattern)

```csharp
[Fact]
public async Task MethodName_ShouldExpectedBehavior_WhenCondition()
{
    // Arrange
    var sut = CreateSystemUnderTest();
    var input = CreateTestInput();
    
    // Act
    var result = await sut.Method(input);
    
    // Assert
    result.Should().NotBeNull();
    result.Value.Should().Be(expected);
}
```

---

## 🔄 Pull Request Guidelines

### Before Submitting

- [ ] Code follows the 300 LOC limit (or uses partial classes)
- [ ] Frontend components use code-behind pattern
- [ ] All public APIs have XML documentation
- [ ] Tests are included for new functionality
- [ ] README files are updated if structure changes
- [ ] No commented-out code
- [ ] No TODO comments without issue references

### PR Description Template

```markdown
## Summary
Brief description of changes

## Changes
- Change 1
- Change 2

## Testing
How to test these changes

## Checklist
- [ ] Code follows style guidelines
- [ ] Tests added/updated
- [ ] Documentation updated
```

---

## 🤖 For Coding Agents

When working on this codebase:

1. **Always check file sizes** before editing. If a file is near 300 lines, consider refactoring.
2. **Use partial classes** proactively when adding significant code.
3. **Follow existing patterns** in the codebase.
4. **Create README.md** files when adding new folders.
5. **Document decisions** in comments when logic is complex.
6. **Maintain modularity** - prefer multiple small files over one large file.
7. **Use code-behind** for all Blazor components with logic.
8. **Extract components** when UI sections are reusable.

### Quick Reference

| Rule | Limit | Action |
|------|-------|--------|
| C# file size | 300 lines (+30 max) | Use partial classes |
| Razor file size | 200 lines (+20 max) | Extract components |
| Component parameters | 5-7 max | Use parameter objects |
| Method length | 30 lines suggested | Extract helper methods |
| Class dependencies | 5-7 injected | Use facade pattern |

---

**Last Updated**: 2026-01-08  
**Version**: 1.0  
**Maintainer**: TentMan Development Team
