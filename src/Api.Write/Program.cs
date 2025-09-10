using Azure.Storage.Queues;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ProjectZenith.Api.Write.Behaviors;
using ProjectZenith.Api.Write.Data;
using ProjectZenith.Api.Write.Services.AppDomain.BackgroundServices;
using ProjectZenith.Api.Write.Services.AppDomain.DomainServices;
using ProjectZenith.Api.Write.Services.AppDomain.Validators;
using ProjectZenith.Api.Write.Services.DeveloperDomain.Validators;
using ProjectZenith.Api.Write.Services.PurchaseDomain.BackgroundServices;
using ProjectZenith.Api.Write.Services.PurchaseDomain.Validators;
using ProjectZenith.Api.Write.Services.UserDomain.CommandHandlers;
using ProjectZenith.Api.Write.Services.UserDomain.DomainServices.Email;
using ProjectZenith.Api.Write.Services.UserDomain.DomainServices.Security;
using ProjectZenith.Api.Write.Services.UserDomain.Validators;
using ProjectZenith.Contracts.Configuration;
using ProjectZenith.Contracts.Infrastructure;
using ProjectZenith.Contracts.Infrastructure.MessageQueue;
using ProjectZenith.Contracts.Validation;
using Stripe;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

    // Add JWT auth definition
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter JWT token like: Bearer {your token}",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});


builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "DataProtectionKeys")))
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);

    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
});

builder.Services.AddHttpContextAccessor();


builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("ReviewActionsPolicy", httpContext =>
    {
        // Get UserId from claims to apply rate limiting per user
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: userId ?? httpContext.Request.Host.ToString(), // Use UserId or fallback to host
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 1, // Just 1 request
                Window = TimeSpan.FromSeconds(10) // In a 10-second window
            });
    });
    // Return 429 Too Many Requests when rate limit is exceeded
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection("Kafka"));
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection("Redis"));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<DeveloperOptions>(builder.Configuration.GetSection("Developer"));
builder.Services.Configure<BlobStorageOptions>(builder.Configuration.GetSection("BlobStorage"));
builder.Services.Configure<AzureStorageQueueOptions>(builder.Configuration.GetSection("Queue"));



// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>();
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key))
    };
});



builder.Services.Configure<DatabaseOptions>(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("WriteDb")
        ?? throw new InvalidOperationException("CRITICAL ERROR: Connection string 'WriteDb' not found. Have you set it using 'dotnet user-secrets set' for this project?");
});



builder.Services.AddSingleton<ConfigService>();
builder.Services.AddDbContext<WriteDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("WriteDb"),
        b => b.MigrationsAssembly("ProjectZenith.Api.Write")
    );
});
builder.Services.AddSingleton<IEventPublisher, KafkaEventPublisher>();





builder.Services.AddScoped<IPasswordService, PasswordService>();

builder.Services.AddScoped<ITokenService, ProjectZenith.Api.Write.Services.UserDomain.DomainServices.Security.TokenService>();

builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddScoped<IBlobStorageService>(sp =>
{

    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<BlobStorageService>>();
    var accountName = config["BlobStorage:AccountName"];
    return new BlobStorageService(accountName, logger);
});

builder.Services.AddSingleton<QueueServiceClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<BlobStorageOptions>>().Value;
    var uri = new Uri($"https://{options.AccountName}.queue.core.windows.net");
    return new QueueServiceClient(uri, new Azure.Identity.EnvironmentCredential());
});


// Register Queue service
builder.Services.AddSingleton<IQueueService, AzureQueueService>();
builder.Services.AddScoped<IAppStatusService, AppStatusService>();


builder.Services.AddValidatorsFromAssemblyContaining<RegisterUserCommandValidator>();

builder.Services.AddScoped<VerifyEmailCommandHandler>();
builder.Services.AddValidatorsFromAssemblyContaining<VerifyEmailCommandValidator>();

builder.Services.AddValidatorsFromAssemblyContaining<LoginCommandValidator>();

builder.Services.AddValidatorsFromAssemblyContaining<RefreshTokenCommandValidator>();

builder.Services.AddValidatorsFromAssemblyContaining<LogoutCommandValidator>();

builder.Services.AddScoped<RevokeAllSessionsCommandHandler>();
builder.Services.AddValidatorsFromAssemblyContaining<RevokeAllSessionsCommandValidator>();

builder.Services.AddScoped<RequestPasswordResetCommandHandler>();
builder.Services.AddValidatorsFromAssemblyContaining<RequestPasswordResetValidator>();

builder.Services.AddScoped<ResetPasswordCommandHandler>();
builder.Services.AddValidatorsFromAssemblyContaining<ResetPasswordCommandValidator>();

builder.Services.AddScoped<UpdateUserProfileCommandHandler>();
builder.Services.AddScoped<UpdateUserAvatarService>();
builder.Services.AddValidatorsFromAssemblyContaining<UpdateUserProfileCommandValidator>();

builder.Services.AddValidatorsFromAssemblyContaining<RequestDeveloperStatusCommandValidator>();

builder.Services.AddScoped<IFileSignatureValidator, FileSignatureValidator>();

builder.Services.AddScoped<PrepareAppFileUploadCommandValidator>();
builder.Services.AddScoped<FinalizeAppSubmissionCommandValidator>();

builder.Services.AddSingleton<SubmitNewVersionCommandValidator>();
builder.Services.AddSingleton<ApproveAppCommandValidator>();
builder.Services.AddSingleton<MarkAppAsPendingApprovalCommandValidator>();
builder.Services.AddSingleton<RejectAppCommandValidator>();
builder.Services.AddSingleton<MarkScreenshotProcessedCommandValidator>();

builder.Services.AddSingleton<MarkVersionAsPendingApprovalCommandValidator>();
builder.Services.AddSingleton<RejectVersionCommandValidator>();
builder.Services.AddSingleton<UnpublishVersionCommandValidator>();
builder.Services.AddSingleton<SetAppPriceCommandValidator>();
builder.Services.AddSingleton<CreatePurchaseCommandValidator>();
builder.Services.AddSingleton<SchedulePayoutCommandValidator>();
builder.Services.AddSingleton<ProcessSinglePayoutCommandValidator>();
builder.Services.AddSingleton<CreateStripeConnectOnboardingLinkCommandValidator>();
builder.Services.AddSingleton<ProcessStripeAccountUpdateCommandValidator>();
builder.Services.AddSingleton<ReconcilePayoutStatusCommandValidator>();
builder.Services.AddSingleton<DeleteAppCommandValidator>();
builder.Services.AddSingleton<SubmitReviewCommandValidator>();
builder.Services.AddSingleton<SubmitReviewReplyCommandValidator>();
builder.Services.AddSingleton<UpdateReviewCommandValidator>();
builder.Services.AddSingleton<DeleteReviewCommandValidator>();
builder.Services.AddSingleton<RecalculateAppRatingCommandValidator>();


builder.Services.AddHostedService<FileProcessingBackgroundService>();
builder.Services.AddHostedService<PayoutProcessingBackgroundService>();
builder.Services.AddHostedService<ReviewProcessingBackgroundService>();

builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection(StripeOptions.SectionName));
builder.Services.AddSingleton<PaymentIntentService>();
builder.Services.AddSingleton<TransferService>();
builder.Services.AddSingleton<AccountService>();
builder.Services.AddSingleton<AccountLinkService>();




var app = builder.Build();

var stripeOptions = app.Services.GetRequiredService<IOptions<StripeOptions>>().Value;
StripeConfiguration.ApiKey = stripeOptions.SecretKey;

//using (var scope = app.Services.CreateScope())
//{
//    var dbContext = scope.ServiceProvider.GetRequiredService<WriteDbContext>();
//    // Ensure the database is created and apply any pending migrations.
//    try
//    {
//        dbContext.Database.Migrate();
//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine($"Migration failed: {ex.Message}");
//        throw;
//    }
//}

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
    private readonly JwtOptions _jwtOptions;
    private readonly DeveloperOptions _developerOptions;
    private readonly BlobStorageOptions _blobStorageOptions;

    public ConfigService(
        IOptions<DatabaseOptions> dbOptions,
        IOptions<KafkaOptions> kafkaOptions,
        IOptions<RedisOptions> redisOptions,
        IOptions<JwtOptions> jwtOptions,
        IOptions<DeveloperOptions> developerOptions,
        IOptions<BlobStorageOptions> blobStorageOptions)
    {
        _dbOptions = dbOptions.Value ?? throw new ArgumentNullException(nameof(dbOptions));
        _kafkaOptions = kafkaOptions.Value ?? throw new ArgumentNullException(nameof(kafkaOptions));
        _redisOptions = redisOptions.Value ?? throw new ArgumentNullException(nameof(redisOptions));
        _jwtOptions = jwtOptions.Value ?? throw new ArgumentNullException(nameof(jwtOptions));
        _developerOptions = developerOptions.Value ?? throw new ArgumentNullException(nameof(developerOptions));
        _blobStorageOptions = blobStorageOptions.Value ?? throw new ArgumentNullException(nameof(blobStorageOptions));
    }

    public string GetConfigSummary()
    {
        return $"Database Connection: {_dbOptions.ConnectionString}, " +
               $"Kafka Brokers: {string.Join(", ", _kafkaOptions.BootstrapServers)}, " +
               $"Redis Connection: {_redisOptions.ConnectionString}, " +
               $"JWT Secret: {_jwtOptions.Key}, " +
               $"Developer Mode: {_developerOptions.ApprovalPolicy}, " +
               $"Blob Storage Connection: {_blobStorageOptions.AccountName}";
    }
}
