using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProjectZenith.Api.Write.Data
{
    public class WriteDbContextFactory : IDesignTimeDbContextFactory<WriteDbContext>
    {
        public WriteDbContext CreateDbContext(string[] args)
        {
            // Build config (appsettings.json + user secrets + env vars)
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddUserSecrets<WriteDbContextFactory>(optional: true)  // 👈 this line adds user secrets
                .AddEnvironmentVariables()
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<WriteDbContext>();

            // Use the same name you’re using in Program.cs
            var connectionString = configuration.GetConnectionString("WriteDb");

            optionsBuilder.UseSqlServer(connectionString,
                b => b.MigrationsAssembly("ProjectZenith.Api.Write"));

            return new WriteDbContext(optionsBuilder.Options);
        }
    }
}
