using TentMan.Application.Abstractions;
using TentMan.Application.Abstractions.Repositories;
using TentMan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace TentMan.Infrastructure.Repositories;

/// <summary>
/// Implementation of Unit of Work pattern using Entity Framework Core.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly ITimeProvider _timeProvider;
    private readonly ILoggerFactory _loggerFactory;
    private IDbContextTransaction? _currentTransaction;
    private IProductRepository? _productRepository;
    private IOrganizationRepository? _organizationRepository;
    private IBuildingRepository? _buildingRepository;
    private IUnitRepository? _unitRepository;
    private IOwnerRepository? _ownerRepository;
    private IUserRepository? _userRepository;
    private IRoleRepository? _roleRepository;
    private IUserRoleRepository? _userRoleRepository;
    private IEmailConfirmationTokenRepository? _emailConfirmationTokenRepository;
    private IPasswordResetTokenRepository? _passwordResetTokenRepository;

    public UnitOfWork(
        ApplicationDbContext context,
        ITimeProvider timeProvider,
        ILoggerFactory loggerFactory)
    {
        _context = context;
        _timeProvider = timeProvider;
        _loggerFactory = loggerFactory;
    }

    public IProductRepository Products => _productRepository ??= new ProductRepository(_context);
    public IOrganizationRepository Organizations => _organizationRepository ??= new OrganizationRepository(_context);
    public IBuildingRepository Buildings => _buildingRepository ??= new BuildingRepository(_context);
    public IUnitRepository Units => _unitRepository ??= new UnitRepository(_context);
    public IOwnerRepository Owners => _ownerRepository ??= new OwnerRepository(_context);
    public IUserRepository Users => _userRepository ??= new UserRepository(_context);
    public IRoleRepository Roles => _roleRepository ??= new RoleRepository(_context);
    public IUserRoleRepository UserRoles => _userRoleRepository ??= new UserRoleRepository(_context);
    public IEmailConfirmationTokenRepository EmailConfirmationTokens =>
        _emailConfirmationTokenRepository ??= new EmailConfirmationTokenRepository(
            _context,
            _timeProvider,
            _loggerFactory.CreateLogger<EmailConfirmationTokenRepository>());
    public IPasswordResetTokenRepository PasswordResetTokens =>
        _passwordResetTokenRepository ??= new PasswordResetTokenRepository(
            _context,
            _timeProvider,
            _loggerFactory.CreateLogger<PasswordResetTokenRepository>());

    public async Task<TResult> ExecuteWithRetryAsync<TResult>(Func<Task<TResult>> operation, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(
            state: operation,
            operation: async (_, op, ct) => await op(),
            verifySucceeded: null,
            cancellationToken: cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress");
        }

        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No transaction in progress");
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            _currentTransaction?.Dispose();
            _currentTransaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No transaction in progress");
        }

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            _currentTransaction?.Dispose();
            _currentTransaction = null;
        }
    }

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
