using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace SOFT_DB_EXAM;

public class ApplicationContextFactory
{
    public static ApplicationDbContext CreateDbContext()
    {
        // Load configuration from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory()) // Required to locate appsettings.json correctly
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Read connection string
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}