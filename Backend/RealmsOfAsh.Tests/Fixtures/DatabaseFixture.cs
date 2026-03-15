using Testcontainers.PostgreSql;

namespace RealmsOfAsh.Tests.Fixtures;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16")
        .Build();

    public string ConnectionString { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        ConnectionString = _postgres.GetConnectionString();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
}
