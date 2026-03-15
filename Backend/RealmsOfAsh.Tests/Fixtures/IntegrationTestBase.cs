using Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace RealmsOfAsh.Tests.Fixtures;

[Collection("Database tests")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly CustomWebApplicationFactory Factory;
    protected readonly HttpClient Client;
    private IDbContextTransaction _transaction = default!;
    private AppDbContext _dbContext = default!;

    protected IntegrationTestBase(DatabaseFixture fixture)
    {
        Factory = new CustomWebApplicationFactory(fixture);
        Client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    public async Task InitializeAsync()
    {
        _dbContext = Factory.Services.CreateScope()
            .ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
    }

    public async Task DisposeAsync()
    {
        await _transaction.RollbackAsync();
        _dbContext.ChangeTracker.Clear();
        await Factory.DisposeAsync();
    }
}
