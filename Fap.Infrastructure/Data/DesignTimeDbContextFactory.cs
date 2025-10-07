using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;


namespace Fap.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FapDbContext>
{
    public FapDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var conn = config.GetConnectionString("Default")
                 ?? "Server=localhost,1433;Database=FapDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<FapDbContext>()
            .UseSqlServer(conn)
            .Options;

        return new FapDbContext(options);
    }
}
