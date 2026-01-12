using TentMan.Application.Abstractions;
using TentMan.Domain.Abstractions;
using TentMan.Domain.Entities;
using TentMan.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Reflection;

namespace TentMan.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUser _currentUser;
    private readonly ITimeProvider _time;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUser currentUser,
        ITimeProvider time) : base(options)
    {
        _currentUser = currentUser;
        _time = time;
    }

    // Business DbSets
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<BuildingAddress> BuildingAddresses => Set<BuildingAddress>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<BuildingOwnershipShare> BuildingOwnershipShares => Set<BuildingOwnershipShare>();
    public DbSet<UnitOwnershipShare> UnitOwnershipShares => Set<UnitOwnershipShare>();
    public DbSet<UnitMeter> UnitMeters => Set<UnitMeter>();
    public DbSet<FileMetadata> Files => Set<FileMetadata>();
    public DbSet<BuildingFile> BuildingFiles => Set<BuildingFile>();
    public DbSet<UnitFile> UnitFiles => Set<UnitFile>();

    // Tenant Management DbSets
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantAddress> TenantAddresses => Set<TenantAddress>();
    public DbSet<TenantEmergencyContact> TenantEmergencyContacts => Set<TenantEmergencyContact>();
    public DbSet<TenantDocument> TenantDocuments => Set<TenantDocument>();
    public DbSet<TenantInvite> TenantInvites => Set<TenantInvite>();

    // Lease Management DbSets
    public DbSet<Lease> Leases => Set<Lease>();
    public DbSet<LeaseParty> LeaseParties => Set<LeaseParty>();
    public DbSet<LeaseTerm> LeaseTerms => Set<LeaseTerm>();
    public DbSet<DepositTransaction> DepositTransactions => Set<DepositTransaction>();
    public DbSet<UnitHandover> UnitHandovers => Set<UnitHandover>();
    public DbSet<HandoverChecklistItem> HandoverChecklistItems => Set<HandoverChecklistItem>();
    public DbSet<MeterReading> MeterReadings => Set<MeterReading>();
    public DbSet<UnitOccupancy> UnitOccupancies => Set<UnitOccupancy>();

    // Billing Engine DbSets
    public DbSet<ChargeType> ChargeTypes => Set<ChargeType>();
    public DbSet<LeaseBillingSetting> LeaseBillingSettings => Set<LeaseBillingSetting>();
    public DbSet<LeaseRecurringCharge> LeaseRecurringCharges => Set<LeaseRecurringCharge>();
    public DbSet<UtilityRatePlan> UtilityRatePlans => Set<UtilityRatePlan>();
    public DbSet<UtilityRateSlab> UtilityRateSlabs => Set<UtilityRateSlab>();
    public DbSet<UtilityStatement> UtilityStatements => Set<UtilityStatement>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<CreditNote> CreditNotes => Set<CreditNote>();
    public DbSet<CreditNoteLine> CreditNoteLines => Set<CreditNoteLine>();
    public DbSet<InvoiceRun> InvoiceRuns => Set<InvoiceRun>();
    public DbSet<InvoiceRunItem> InvoiceRunItems => Set<InvoiceRunItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentConfirmationRequest> PaymentConfirmationRequests => Set<PaymentConfirmationRequest>();

    // Identity DbSets
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<ApplicationRole> Roles => Set<ApplicationRole>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    
    // Token DbSets for secure authentication flows
    public DbSet<EmailConfirmationToken> EmailConfirmationTokens => Set<EmailConfirmationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    // Audit DbSets
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Scan & apply IEntityTypeConfiguration<T> from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Global query filter for soft delete on all entities implementing ISoftDeletable
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(ApplicationDbContext)
                    .GetMethod(nameof(SetSoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);

                method.Invoke(null, new object[] { modelBuilder });
            }
        }
    }

    private static void SetSoftDeleteFilter<TEntity>(ModelBuilder builder)
        where TEntity : class, ISoftDeletable
        => builder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditing();
        ApplySoftDeleteTransform();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditing()
    {
        var now = _time.UtcNow;
        var user = _currentUser.UserId;

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.CreatedBy = user;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedAtUtc = now;
                entry.Entity.ModifiedBy = user;
            }
        }
    }

    private void ApplySoftDeleteTransform()
    {
        var now = _time.UtcNow;
        var user = _currentUser.UserId;

        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>()
                     .Where(e => e.State == EntityState.Deleted))
        {
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAtUtc = now;
            entry.Entity.DeletedBy = user;
        }
    }
}
