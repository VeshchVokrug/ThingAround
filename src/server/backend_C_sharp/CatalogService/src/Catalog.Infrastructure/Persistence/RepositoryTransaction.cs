using Catalog.Contracts.Repository.Abstractions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Persistence;

public sealed class RepositoryTransaction : IRepositoryTransaction
{
    private readonly IDbContextTransaction _transaction;

    public RepositoryTransaction(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public Task CommitAsync(CancellationToken ct = default)
    {
        return _transaction.CommitAsync(ct);
    }

    public ValueTask DisposeAsync()
    {
        return _transaction.DisposeAsync();
    }
}

