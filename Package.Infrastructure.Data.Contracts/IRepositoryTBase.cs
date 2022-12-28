using System.Linq.Expressions;

namespace Package.Infrastructure.Data.Contracts;

public interface IRepositoryTBase : IRepositoryQBase
{
    Task<bool> Exists<T>(Expression<Func<T, bool>> filter) where T : class;

    void Save<T>(ref T entity) where T : EntityBase;

    void Create<T>(ref T entity) where T : class;

    void PrepareForUpdate<T>(ref T entity) where T : EntityBase;

    void UpdateFull<T>(ref T entity) where T : EntityBase;

    void Delete<T>(T entity) where T : EntityBase;

    Task Delete<T>(params object[] keys) where T : class;

    Task Delete<T>(Expression<Func<T, bool>> filter) where T : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(OptimisticConcurrencyWinner winner, CancellationToken cancellationToken = default);
}
