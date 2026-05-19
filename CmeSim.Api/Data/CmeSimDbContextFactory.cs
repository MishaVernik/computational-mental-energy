using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CmeSim.Api.Data;

/// <summary>
/// Design-time factory for EF tools (migrations, database update).
/// </summary>
public class CmeSimDbContextFactory : IDesignTimeDbContextFactory<CmeSimDbContext>
{
    public CmeSimDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required");
        var optionsBuilder = new DbContextOptionsBuilder<CmeSimDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new CmeSimDbContext(optionsBuilder.Options);
    }
}
