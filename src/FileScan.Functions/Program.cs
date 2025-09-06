using FileScan.Functions.Services.VirusTotal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Configuration;
using ProjectZenith.Contracts.Infrastructure;
using ProjectZenith.Contracts.Validation;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration((context, config) =>
     {
         if (context.HostingEnvironment.IsDevelopment())
         {
             config.AddUserSecrets<Program>();
         }
     })
    .ConfigureServices((context, services) =>
    {
        var storageAccountName = Environment.GetEnvironmentVariable("StorageAccountName")!;
        services.AddSingleton<IBlobStorageService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<BlobStorageService>>();
            return new BlobStorageService(storageAccountName, logger);
        });
        services.AddTransient<IFileSignatureValidator, FileSignatureValidator>();

        // --- Options Pattern Registration ---
        services.AddOptions<BlobStorageOptions>()
            .Bind(context.Configuration.GetSection(BlobStorageOptions.SectionName))
            .ValidateDataAnnotations() // Enable validation using Data Annotations ([Required], etc.)
            .ValidateOnStart(); // Fail fast on startup if validation fails

        // VirusTotal option
        services.AddOptions<VirusTotalOptions>()
            .Bind(context.Configuration.GetSection(VirusTotalOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // VirusTotal option
        services.AddOptions<KafkaOptions>()
            .Bind(context.Configuration.GetSection(KafkaOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IVirusScanService, VirusTotalService>();
        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

        services.AddDbContext<WriteDbContext>(options =>
        {
            options.UseSqlServer(
                context.Configuration.GetConnectionString("WriteDb"),
                b => b.MigrationsAssembly("ProjectZenith.Api.Write")
            );
        });
    })
    .Build();

host.Run();


