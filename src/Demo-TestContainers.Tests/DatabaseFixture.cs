using System.Data;
using Demo_TestContainers.Repositories;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.MySql;
using Xunit;

namespace Demo_TestContainers.Tests;

public class DatabaseFixture : IAsyncLifetime
{
    private const int MariaDbPort = 3306;
    private const string DatabaseUser = "root";
    private const string DatabasePassword = "some-password";
    private readonly string _databaseName = GetRandomTestDatabaseName();

    private IContainer _container = null!;

    public IDbConnection Connection { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _container = BuildContainer();
        //_container = BuildMySqlContainer();
        await _container.StartAsync();

        var server = _container.Hostname;
        var port = _container.GetMappedPublicPort(MariaDbPort);
        SetEnvironmentVariables(server, port, _databaseName);

        Connection = await ConnectionProvider.GetOpenDatabaseConnection(connectionTimeout: 3);
        await Database.Seed(Connection, _databaseName);
    }

    public async Task DisposeAsync()
    {
        await Database.DeleteDatabase(Connection, _databaseName);
        await _container.StopAsync();
    }

    public static string GetRandomTestDatabaseName()
    {
        return $"test_{Guid.NewGuid():N}";
    }

    private static void SetEnvironmentVariables(string server, int port, string databaseName)
    {
        Environment.SetEnvironmentVariable("DB_SERVER", server);
        Environment.SetEnvironmentVariable("DB_PORT", port.ToString());
        Environment.SetEnvironmentVariable("DB_NAME", databaseName);
        Environment.SetEnvironmentVariable("DB_USER", DatabaseUser);
        Environment.SetEnvironmentVariable("DB_PASSWORD", DatabasePassword);
    }

    private IContainer BuildContainer()
    {
        var env = new Dictionary<string, string>
        {
            { "MYSQL_ROOT_PASSWORD", DatabasePassword },
            { "MYSQL_DATABASE", _databaseName },
            { "MYSQL_USER", "orders" },
            { "MYSQL_PASSWORD", "another-password" }
        };

        var builder = new ContainerBuilder()
            .WithImage("mariadb:11.3")
            .WithPortBinding(MariaDbPort, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(MariaDbPort))
            .WithHostname("mariadb-test-container")
            .WithEnvironment(env);
        return builder.Build();
    }

    private IContainer BuildMySqlContainer()
    {
        var builder = new MySqlBuilder()
            .WithImage("mariadb:11.3")
            .WithPortBinding(MariaDbPort, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(MariaDbPort))
            .WithHostname("mariadb-test-container")
            .WithUsername(DatabaseUser)
            .WithPassword(DatabasePassword)
            .WithDatabase(_databaseName);
        return builder.Build();
    }
}