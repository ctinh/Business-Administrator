using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using QTKD.Api.Data;

namespace QTKD.Tests.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private string m_TestConnectionString = string.Empty;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // retrieve the existing configuration (including user-secrets from QTKD.Api)
            var ServiceProvider = services.BuildServiceProvider();
            using var scope = ServiceProvider.CreateScope();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            var BaseConnString = configuration.GetConnectionString("DefaultConnection")
                ?? "Host=localhost;Port=5432;Database=qtkd_db;Username=postgres;Password=postgres";

            // switch to an isolated PostgreSQL test database named 'qtkd_test_db'
            var ConnStringBuilder = new NpgsqlConnectionStringBuilder(BaseConnString)
            {
                Database = "qtkd_test_db"
            };
            m_TestConnectionString = ConnStringBuilder.ConnectionString;

            // replace QtkdDbContext registration to point strictly to qtkd_test_db
            services.RemoveAll(typeof(DbContextOptions<QtkdDbContext>));
            services.RemoveAll(typeof(QtkdDbContext));

            services.AddDbContext<QtkdDbContext>(options =>
                options.UseNpgsql(m_TestConnectionString));
        });
    }

    public async Task InitializeAsync()
    {
        // create client to trigger web host configuration
        using var scope = Services.CreateScope();
        var DbContext = scope.ServiceProvider.GetRequiredService<QtkdDbContext>();

        // reset and apply migrations on the dedicated qtkd_test_db before test suite runs
        await DbContext.Database.EnsureDeletedAsync();
        await DbContext.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }
}