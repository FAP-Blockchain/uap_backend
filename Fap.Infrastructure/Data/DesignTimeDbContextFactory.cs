using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Fap.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FapDbContext>
{
    public FapDbContext CreateDbContext(string[] args)
    {

        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "../Fap.Api");
        
        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();


        var conn = config.GetConnectionString("DefaultConnection")
                 ?? "Server=localhost,1433;Database=FapDb;User Id=sa;Password=12345;TrustServerCertificate=True;Encrypt=False;";

        var options = new DbContextOptionsBuilder<FapDbContext>()
            .UseSqlServer(conn)
            .Options;

        return new FapDbContext(options);
    }
}
