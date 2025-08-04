using ProjectZenith.Contracts.Configuration;
using ProjectZenith.Kafka.Consumer;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                IConfiguration configuration = hostContext.Configuration;

                // --- Register Your Options Classes ---

                services.Configure<KafkaOptions>(configuration.GetSection("Kafka"));
                services.Configure<RedisOptions>(configuration.GetSection("Redis"));

                // --- THIS IS THE CORRECTED PART ---
                // This is now simpler, more consistent, and less error-prone.
                // It looks for the "ReadDb" key inside the "ConnectionStrings" section.
                services.Configure<DatabaseOptions>(options =>
                {
                    options.ConnectionString = configuration.GetConnectionString("ReadDb")
                        ?? throw new InvalidOperationException("CRITICAL ERROR: Connection string 'ReadDb' not found. Have you set it using 'dotnet user-secrets set' for the Kafka.Consumer project?");
                });

                // --- Register Your Worker Service ---
                services.AddHostedService<Worker>();
            })
            .Build();

        await host.RunAsync();
    }
}