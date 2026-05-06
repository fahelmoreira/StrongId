using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace StrongId.EntityFramework.Tests;

public class SqliteFixture : IAsyncLifetime
{
    private SqliteConnection _connection = null!;

    public string ConnectionString => _connection.ConnectionString;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    public TestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new TestDbContext(options);
    }

    public SqliteConnection RawConnection => _connection;
}

[CollectionDefinition(nameof(SqliteCollection))]
public class SqliteCollection : ICollectionFixture<SqliteFixture>;
