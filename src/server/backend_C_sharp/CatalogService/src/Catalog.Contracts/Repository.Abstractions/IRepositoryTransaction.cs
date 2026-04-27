namespace Catalog.Contracts.Repository.Abstractions;

public interface IRepositoryTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct = default);
}

