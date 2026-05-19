using Microsoft.EntityFrameworkCore;
using RentalService.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Catalog.Tests;

public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("test_db")
        .WithUsername("test_user")
        .WithPassword("test_pass")
        .Build();

    private string ConnectionString => _container.GetConnectionString();

    public RentalDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<RentalDbContext>()
            .UseNpgsql(ConnectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(RentalDbContext).Assembly.GetName().Name);
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
            })
            .UseSnakeCaseNamingConvention()
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging()
            .Options;

        return new RentalDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = CreateContext();

        if (!await context.Database.CanConnectAsync())
        {
            throw new Exception("Can't connect to database");
        }

        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}

[CollectionDefinition("PostgresCollection")]
public class PostgresCollection : ICollectionFixture<PostgresFixture>
{
}

