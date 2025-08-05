using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Contracts.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 1. Configure DatabaseOptions to use the "ReadDb" connection string from User Secrets.
// We are using the simple .Configure<T>() method which is perfect when you only need one
// instance of a particular options type.
builder.Services.Configure<DatabaseOptions>(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("WriteDb")
        ?? throw new InvalidOperationException("CRITICAL ERROR: Connection string 'WriteDb' not found. Have you set it using 'dotnet user-secrets set' for this project?");
});

builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection("Kafka"));
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection("Redis"));

builder.Services.AddSingleton<ConfigService>();
builder.Services.AddDbContext<WriteDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("WriteDb"),
        b => b.MigrationsAssembly("ProjectZenith.Api.Write")
    );
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(builder =>
{
    builder.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader();
});
app.MapControllers();



app.Run();


public class ConfigService
{
    private readonly DatabaseOptions _dbOptions;
    private readonly KafkaOptions _kafkaOptions;
    private readonly RedisOptions _redisOptions;

    public ConfigService(IOptions<DatabaseOptions> dbOptions, IOptions<KafkaOptions> kafkaOptions, IOptions<RedisOptions> redisOptions)
    {
        _dbOptions = dbOptions.Value ?? throw new ArgumentNullException(nameof(dbOptions));
        _kafkaOptions = kafkaOptions.Value ?? throw new ArgumentNullException(nameof(kafkaOptions));
        _redisOptions = redisOptions.Value ?? throw new ArgumentNullException(nameof(redisOptions));
    }

    public string GetConfigSummary()
    {
        return $"Database Connection: {_dbOptions.ConnectionString}, " +
               $"Kafka Brokers: {string.Join(", ", _kafkaOptions.Brokers)}, " +
               $"Redis Connection: {_redisOptions.ConnectionString}";
    }
}
