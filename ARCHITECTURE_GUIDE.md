# Archu - Architecture & Improvements Guide

> **Last Updated:** 2025-01-19  
> **Status:** Repository Pattern Fixed ✅ | Build Passing ✅

---

## 📖 Table of Contents

1. [Executive Summary](#executive-summary)
2. [Critical Fix Implemented](#critical-fix-implemented)
3. [Implementation Roadmap](#implementation-roadmap)
4. [High-Priority Improvements](#high-priority-improvements)
5. [Medium-Priority Improvements](#medium-priority-improvements)
6. [Quick Reference](#quick-reference)
7. [Checklist](#checklist)

---

## Executive Summary

### ✅ What's Excellent

Your Archu application demonstrates **excellent architectural principles**:

- ✅ **Clean Architecture** - Proper layer separation with dependency inversion
- ✅ **CQRS with MediatR** - Commands/queries separated with pipeline behaviors
- ✅ **Modern .NET** - .NET 9, nullable types, source generators
- ✅ **Cloud-Native** - .NET Aspire orchestration with OpenTelemetry
- ✅ **EF Core Best Practices** - Configurations, soft delete, audit trails
- ✅ **API Design** - Versioning, health checks, OpenAPI/Scalar docs
- ✅ **Error Handling** - Global exception middleware

### ⚠️ Areas Needing Attention

- ❌ No test projects
- ❌ No authentication (placeholder only)
- ❌ No caching layer
- ❌ Anemic domain model

---

## Critical Fix Implemented

### 🔴 Repository Pattern Violation → ✅ FIXED

**Problem:** Repositories were calling `SaveChangesAsync()` directly, violating Unit of Work pattern.

**Before (Wrong):**
```csharp
public async Task<Product> AddAsync(Product product, CancellationToken ct)
{
    _context.Products.Add(product);
    await _context.SaveChangesAsync(ct); // ❌ Repository shouldn't save
    return product;
}
```

**After (Fixed):**
```csharp
// Repository only tracks changes
public Task<Product> AddAsync(Product product, CancellationToken ct)
{
    _context.Products.Add(product);
    return Task.FromResult(product); // ✅
}

// Handler controls persistence via Unit of Work
public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken ct)
{
    var product = new Product { Name = request.Name, Price = request.Price };
    await _unitOfWork.Products.AddAsync(product, ct);
    await _unitOfWork.SaveChangesAsync(ct); // ✅ Explicit save
    
    return MapToDto(product);
}
```

**Files Modified:**
- `src/Archu.Infrastructure/Repositories/ProductRepository.cs`
- `src/Archu.Application/Abstractions/IProductRepository.cs`
- All command handlers (Create, Update, Delete)

**Benefits:**
- ✅ Proper transaction control
- ✅ Can batch multiple operations atomically
- ✅ Better testability
- ✅ Aligns with Unit of Work pattern

---

## Implementation Roadmap

### Phase 1: Critical Fixes ✅ DONE
- [x] Fix Repository Pattern
- [x] Update handlers to use Unit of Work
- [x] Verify build succeeds

### Phase 2: Testing (Week 1-2)
- [ ] Create test projects (Domain, Application, API, Architecture)
- [ ] Add unit tests for handlers
- [ ] Add integration tests for repositories
- [ ] Target: >70% code coverage

### Phase 3: Security (Week 3)
- [ ] Implement JWT authentication
- [ ] Add authorization policies
- [ ] Update `HttpContextCurrentUser`
- [ ] Secure endpoints

### Phase 4: Performance (Week 4-5)
- [ ] Add Redis caching
- [ ] Implement caching behavior
- [ ] Add rate limiting
- [ ] Add response compression

### Phase 5: Domain Richness (Week 6-7)
- [ ] Convert to domain-rich entities
- [ ] Add domain events
- [ ] Implement specification pattern

### Phase 6: Polish (Week 8+)
- [ ] Add pagination
- [ ] AutoMapper integration
- [ ] Performance optimization

---

## High-Priority Improvements

### 1. Testing Infrastructure 🧪

**Create test projects:**
```bash
# Application tests
dotnet new xunit -n Archu.Application.Tests -o tests/Archu.Application.Tests
dotnet add tests/Archu.Application.Tests package FluentAssertions
dotnet add tests/Archu.Application.Tests package Moq
dotnet add tests/Archu.Application.Tests package Microsoft.EntityFrameworkCore.InMemory
dotnet add tests/Archu.Application.Tests reference src/Archu.Application
dotnet add tests/Archu.Application.Tests reference src/Archu.Infrastructure

# Architecture tests
dotnet new xunit -n Archu.ArchitectureTests -o tests/Archu.ArchitectureTests
dotnet add tests/Archu.ArchitectureTests package NetArchTest.Rules
```

**Example Unit Test:**
```csharp
public class CreateProductCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options, 
            new MockCurrentUser(), 
            new MockTimeProvider());
        _unitOfWork = new UnitOfWork(_context);
    }

    [Fact]
    public async Task Handle_ValidProduct_CreatesSuccessfully()
    {
        // Arrange
        var handler = new CreateProductCommandHandler(
            _unitOfWork, 
            NullLogger<CreateProductCommandHandler>.Instance);
        var command = new CreateProductCommand("Test Product", 99.99m);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test Product");
        
        var products = await _context.Products.ToListAsync();
        products.Should().HaveCount(1);
    }

    public void Dispose() => _context.Dispose();
}
```

**Architecture Tests:**
```csharp
[Fact]
public void Domain_Should_Not_DependOn_OtherLayers()
{
    var result = Types.InAssembly(typeof(Product).Assembly)
        .Should()
        .NotHaveDependencyOn("Archu.Application")
        .And().NotHaveDependencyOn("Archu.Infrastructure")
        .And().NotHaveDependencyOn("Archu.Api")
        .GetResult();

    result.IsSuccessful.Should().BeTrue();
}
```

---

### 2. JWT Authentication 🔒

**Add package:**
```bash
dotnet add src/Archu.Api package Microsoft.AspNetCore.Authentication.JwtBearer
```

**Configuration:**
```csharp
// appsettings.json
{
  "Jwt": {
    "SecretKey": "your-256-bit-secret-key-minimum-32-characters",
    "Issuer": "https://archu-api",
    "Audience": "https://archu-api",
    "ExpirationMinutes": 60
  }
}

// Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
});

app.UseAuthentication();
app.UseAuthorization();
```

**Secure endpoints:**
```csharp
[Authorize]
[ApiController]
public class ProductsController : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> CreateProduct(...) { }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult> GetProducts(...) { }
}
```

---

### 3. Redis Caching 🚀

**Update AppHost:**
```csharp
var redis = builder.AddRedis("cache");

var api = builder.AddProject<Projects.Archu_Api>("api")
    .WithReference(sql)
    .WithReference(redis) // ✅
    .WithExternalHttpEndpoints();
```

**Create abstraction:**
```csharp
// src/Archu.Application/Abstractions/ICacheService.cs
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
}
```

**Implement:**
```csharp
// src/Archu.Infrastructure/Caching/RedisCacheService.cs
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var data = await _cache.GetStringAsync(key, ct);
        return data is null ? default : JsonSerializer.Deserialize<T>(data);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration, CancellationToken ct)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5)
        };
        
        await _cache.SetStringAsync(key, JsonSerializer.Serialize(value), options, ct);
    }
}
```

**Add caching behavior:**
```csharp
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICacheService _cache;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!typeof(TRequest).Name.EndsWith("Query"))
            return await next();

        var cacheKey = $"{typeof(TRequest).Name}:{JsonSerializer.Serialize(request)}";
        var cached = await _cache.GetAsync<TResponse>(cacheKey, ct);
        
        if (cached is not null)
            return cached;

        var response = await next();
        await _cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5), ct);
        
        return response;
    }
}

// Register in Program.cs
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(AssemblyReference).Assembly);
    cfg.AddOpenBehavior(typeof(CachingBehavior<,>)); // ✅ Add first
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
});
```

---

### 4. Rate Limiting ⏱️

```csharp
// Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var userId = context.User.Identity?.Name 
            ?? context.Connection.RemoteIpAddress?.ToString() 
            ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(userId, _ => 
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

app.UseRateLimiter();
```

---

### 5. Response Compression 📦

```csharp
// Program.cs
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
    options.Providers.Add<BrotliCompressionProvider>();
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

app.UseResponseCompression(); // Early in pipeline
```

---

## Medium-Priority Improvements

### 6. Domain-Rich Entities

**Convert anemic entities to domain-rich:**
```csharp
public class Product : BaseEntity
{
    private Product() { } // EF Core constructor

    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }

    // Factory method with validation
    public static Product Create(string name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty", nameof(name));

        if (price <= 0)
            throw new ArgumentException("Price must be positive", nameof(price));

        return new Product { Name = name, Price = price };
    }

    // Business logic in entity
    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice <= 0)
            throw new ArgumentException("Price must be positive", nameof(newPrice));

        Price = newPrice;
    }
}
```

---

### 7. Domain Events

```csharp
// Domain Event
public record ProductCreatedEvent(Guid ProductId, string Name, decimal Price) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

// Entity raises event
public static Product Create(string name, decimal price)
{
    var product = new Product { Name = name, Price = price };
    product.RaiseDomainEvent(new ProductCreatedEvent(product.Id, name, price));
    return product;
}

// Dispatch in DbContext
public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
{
    var events = ChangeTracker.Entries<BaseEntity>()
        .SelectMany(e => e.Entity.DomainEvents)
        .ToList();

    var result = await base.SaveChangesAsync(ct);

    foreach (var domainEvent in events)
        await _publisher.Publish(domainEvent, ct);

    return result;
}
```

---

### 8. Specification Pattern

```csharp
public interface ISpecification<T>
{
    Expression<Func<T, bool>>? Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
    Expression<Func<T, object>>? OrderBy { get; }
}

// Example specification
public class ActiveProductsSpecification : BaseSpecification<Product>
{
    public ActiveProductsSpecification(string? searchTerm, int page, int pageSize)
        : base(p => !p.IsDeleted && (searchTerm == null || p.Name.Contains(searchTerm)))
    {
        ApplyOrderBy(p => p.Name);
        ApplyPaging((page - 1) * pageSize, pageSize);
    }
}

// Usage
var spec = new ActiveProductsSpecification("laptop", 1, 10);
var products = await _repository.GetAsync(spec, cancellationToken);
```

---

### 9. AutoMapper

```bash
dotnet add src/Archu.Application package AutoMapper.Extensions.Microsoft.DependencyInjection
```

```csharp
// Mapping profile
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Product, ProductDto>();
        CreateMap<CreateProductCommand, Product>();
    }
}

// Registration
builder.Services.AddAutoMapper(typeof(AssemblyReference).Assembly);

// Usage
var product = _mapper.Map<Product>(request);
return _mapper.Map<ProductDto>(product);
```

---

### 10. Outbox Pattern

```csharp
public class OutboxMessage : BaseEntity
{
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime? ProcessedOnUtc { get; set; }
}

// Background processor
public class OutboxProcessor : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await ProcessPendingMessagesAsync(ct);
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
        }
    }
}
```

---

## Quick Reference

### Common Commands

**Run Application:**
```bash
cd src/Archu.AppHost
dotnet run
```

**Create Migration:**
```bash
dotnet ef migrations add MigrationName \
  --project src/Archu.Infrastructure \
  --startup-project src/Archu.Api
```

**Apply Migration:**
```bash
dotnet ef database update \
  --project src/Archu.Infrastructure \
  --startup-project src/Archu.Api
```

**Run Tests:**
```bash
dotnet test
```

---

### Key Patterns

**Command Handler (Create):**
```csharp
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var product = new Product { Name = request.Name, Price = request.Price };
        await _unitOfWork.Products.AddAsync(product, ct);
        await _unitOfWork.SaveChangesAsync(ct); // ✅
        
        // ✅ Return created product with generated ID and RowVersion
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            RowVersion = product.RowVersion
        };
    }
}
```

**Command Handler (Update):**
```csharp
public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result<ProductDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<ProductDto>> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(request.Id, ct);
        
        if (product is null)
            return Result<ProductDto>.Failure("Product not found");

        product.Name = request.Name;
        product.Price = request.Price;

        await _unitOfWork.Products.UpdateAsync(product, ct);
        await _unitOfWork.SaveChangesAsync(ct); // ✅ Gets new RowVersion
        
        // ✅ Return updated product with new RowVersion
        return Result<ProductDto>.Success(new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            RowVersion = product.RowVersion // Critical for next update!
        });
    }
}
```

**Query Handler:**
```csharp
public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, IEnumerable<ProductDto>>
{
    private readonly IProductRepository _repository;

    public async Task<IEnumerable<ProductDto>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        var products = await _repository.GetAllAsync(ct);
        return products.Select(MapToDto);
    }
}
```

---

### Pro Tips

✅ **DO:**
- Always use `IUnitOfWork.SaveChangesAsync()` in handlers
- **Return data after POST/PUT** - Clients need generated/updated values (ID, RowVersion)
- Write tests before adding features
- Cache read-heavy queries
- Secure all non-public endpoints
- Use structured logging
- Version your APIs

❌ **DON'T:**
- Call `SaveChangesAsync` in repository
- **Return null after POST/PUT** - Breaks optimistic concurrency and requires extra GET
- Expose domain entities in API (use DTOs)
- Put business logic in controllers
- Skip tests
- Deploy without authentication

---

### REST API Best Practices

**POST (Create):**
- ✅ Return **201 Created** with Location header
- ✅ Return created resource in body (ID, RowVersion, audit fields)
- ✅ Use `CreatedAtAction()` to include resource URI

**PUT (Update):**
- ✅ Return **200 OK** with updated resource in body
- ✅ Include new RowVersion (critical for optimistic concurrency!)
- ❌ Don't return 204 No Content (client loses RowVersion)

**GET:**
- ✅ Return **200 OK** with resource(s)

**DELETE:**
- ✅ Return **204 No Content** or **200 OK** with no body

**See:** `REST_API_IMPROVEMENTS_IMPLEMENTED.md` for detailed explanation.
