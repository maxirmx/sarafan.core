// Copyright (C) 2026 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
// This file is a part of the Sarafan application

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Sarafan.Core.Tests;

[SetUpFixture]
public sealed class IntegrationTestEnvironment
{
    private static string _databaseName = string.Empty;
    private static string _adminConnectionString = string.Empty;
    private static readonly Dictionary<string, string?> PreviousEnvironment = new(StringComparer.Ordinal);

    public static WebApplicationFactory<Program> Factory { get; private set; } = null!;

    [OneTimeSetUp]
    public async Task SetUp()
    {
        _adminConnectionString = Environment.GetEnvironmentVariable("SARAFAN_TEST_POSTGRES")
            ?? "Host=localhost;Port=5433;Database=postgres;Username=postgres;Password=postgres";
        _databaseName = $"sarafan_test_{Guid.NewGuid():N}";

        var adminBuilder = new NpgsqlConnectionStringBuilder(_adminConnectionString)
        {
            Database = "postgres",
            Pooling = false
        };
        await using (var connection = new NpgsqlConnection(adminBuilder.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE \"{_databaseName}\"";
            await command.ExecuteNonQueryAsync();
        }

        var applicationBuilder = new NpgsqlConnectionStringBuilder(_adminConnectionString)
        {
            Database = _databaseName,
            Pooling = false
        };
        SetEnvironment("ASPNETCORE_ENVIRONMENT", "Testing");
        SetEnvironment("ConnectionStrings__DefaultConnection", applicationBuilder.ConnectionString);
        SetEnvironment("Database__ApplyMigrations", "true");
        SetEnvironment("Authentication__Issuer", "sarafan.core.tests");
        SetEnvironment("Authentication__Audience", "sarafan.ui.tests");
        SetEnvironment(
            "Authentication__SigningKey",
            "sarafan-tests-signing-key-with-at-least-thirty-two-characters");
        SetEnvironment("Authentication__AccessTokenMinutes", "15");
        SetEnvironment("Authentication__RefreshTokenDays", "30");
        SetEnvironment("Authentication__RefreshCookieName", "sarafan.refresh");
        SetEnvironment("Authentication__SecureCookies", "false");
        SetEnvironment("Authentication__AllowFixedCode", "true");
        SetEnvironment("Authentication__TermsVersion", "test-terms");
        SetEnvironment("Authentication__PersonalDataVersion", "test-personal-data");
        Factory = new TestWebApplicationFactory();
        using var client = Factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/status/status");
        response.EnsureSuccessStatusCode();
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        Factory?.Dispose();
        NpgsqlConnection.ClearAllPools();

        if (string.IsNullOrEmpty(_databaseName))
        {
            return;
        }

        var builder = new NpgsqlConnectionStringBuilder(_adminConnectionString)
        {
            Database = "postgres",
            Pooling = false
        };
        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)";
        await command.ExecuteNonQueryAsync();

        foreach (var item in PreviousEnvironment)
        {
            Environment.SetEnvironmentVariable(item.Key, item.Value);
        }
    }

    private sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            });
        }
    }

    private static void SetEnvironment(string name, string value)
    {
        PreviousEnvironment[name] = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);
    }
}
