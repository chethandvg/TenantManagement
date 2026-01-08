using TentMan.Domain.Common;
using TentMan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TentMan.Infrastructure.Repositories;

/// <summary>
/// Base repository providing common data access patterns and concurrency control.
/// </summary>
/// <typeparam name="TEntity">The entity type that inherits from BaseEntity.</typeparam>
public abstract class BaseRepository<TEntity> where TEntity : BaseEntity
{
    protected readonly ApplicationDbContext Context;
    protected readonly DbSet<TEntity> DbSet;

    protected BaseRepository(ApplicationDbContext context)
    {
        Context = context;
        DbSet = context.Set<TEntity>();
    }

    /// <summary>
    /// Sets the original RowVersion for concurrency control.
    /// This must be called before updating an entity to enable optimistic concurrency detection.
    /// </summary>
    /// <param name="entity">The entity being updated.</param>
    /// <param name="originalRowVersion">The RowVersion from the client.</param>
    protected void SetOriginalRowVersion(TEntity entity, byte[] originalRowVersion)
    {
        Context.Entry(entity).Property(e => e.RowVersion).OriginalValue = originalRowVersion;
    }

    /// <summary>
    /// Performs a soft delete on the entity.
    /// The actual soft delete transformation is handled by ApplicationDbContext.ApplySoftDeleteTransform().
    /// </summary>
    /// <param name="entity">The entity to soft delete.</param>
    protected void SoftDelete(TEntity entity)
    {
        // Simply remove the entity - the DbContext will transform it to a soft delete
        DbSet.Remove(entity);
    }
}
