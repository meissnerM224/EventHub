using System.Diagnostics;
using System.Net;
using System.Text;
using Amazon.S3;
using EventHub.Api.Authentication;
using EventHub.Api.ErrorHandling;
using EventHub.Api.Extensions;
using EventHub.Api.Logging;
using EventHub.Domain.Interfaces;
using EventHub.Domain.Services;
using EventHub.Infrastructure.Authentication;
using EventHub.Infrastructure.BackgroundJobs;
using EventHub.Infrastructure.Caching;
using EventHub.Infrastructure.Entities;
using EventHub.Infrastructure.Messaging;
using EventHub.Infrastructure.Persistence;
using EventHub.Infrastructure.Repositories;
using EventHub.Infrastructure.Service;
using EventHub.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using StackExchange.Redis;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();
try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services).Enrich.FromLogContext()
        .Enrich.With<ActivityEnricher>()
        .Destructure.ToMaximumDepth(3));
    builder.WebHost.ConfigureKestrel(o => o.AddServerHeader = false);
    builder.Services.AddControllers();
    builder.Services.AddOpenApi();
    var connection = builder.Configuration.GetConnectionString("Default");
    if (string.IsNullOrWhiteSpace(connection))
        throw new InvalidOperationException("ConnectionStrings:Default is not configured.");
    builder.Services.AddDbContext<EventHubDbContext>(options =>
    {
        options.UseNpgsql(connection);
        if (builder.Environment.IsDevelopment()) options.EnableSensitiveDataLogging();
    });
    builder.Services.AddSingleton(TimeProvider.System);
    builder.Services.AddScoped<IUserRoleService, UserRoleService>();
    builder.Services.AddScoped<IEventsRepository, EventsRepository>();
    builder.Services.AddScoped<IEventsService, EventsService>();
    builder.Services.AddScoped<IBookingsService, BookingsService>();
    builder.Services.AddScoped<ITransactionRunner, EfTransactionRunner>();
    builder.Services.AddScoped<IBookingRepository, BookingsRepository>();
    builder.Services.AddScoped<INotificationQueue, OutboxNotificationQueue>();
    builder.Services.AddScoped<INotificationSender, LoggingNotificationSender>();
    builder.Services.AddHostedService<OutboxWorker>();
    builder.Services.Configure<OutboxOptions>(
        builder.Configuration.GetSection(OutboxOptions.SectionName));
    builder.Services.Configure<JwtOptions>(
        builder.Configuration.GetSection(JwtOptions.SectionName));

    var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
              ?? throw new InvalidOperationException(
                  "The ‘Jwt’ section is missing from the configuration.");
    if (string.IsNullOrWhiteSpace(jwt.Key))
        throw new InvalidOperationException("Jwt:Key is not configured.");

    builder.Services
        .AddIdentityCore<AppUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<EventHubDbContext>();

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };

            options.Events = new JwtBearerEvents()
            {
                OnMessageReceived = context =>
                {
                    if (!context.Request.Headers.ContainsKey("Authorization") &&
                        context.Request.Cookies.TryGetValue(AuthCookie.Name, out var token))
                    {
                        context.Token = token;
                    }

                    return Task.CompletedTask;
                },
                // A valid signature is not enough: after logout or a role change the user's security
                // stamp is new, and tokens issued before are rejected right away instead of at expiry.
                OnTokenValidated = async context =>
                {
                    var stamp = context.Principal?.FindFirst(JwtTokenGenerator.SecurityStampClaim)?.Value;
                    var id = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (stamp is null || !Guid.TryParse(id, out var userId))
                    {
                        context.Fail("Token has no security stamp.");
                        return;
                    }

                    var db = context.HttpContext.RequestServices.GetRequiredService<EventHubDbContext>();
                    var current = await db.Users.Where(u => u.Id == userId)
                        .Select(u => u.SecurityStamp)
                        .SingleOrDefaultAsync(context.HttpContext.RequestAborted);
                    if (current != stamp) context.Fail("Session is no longer valid.");
                }
            };
        });
    var redisConnection = builder.Configuration.GetConnectionString("Redis");
    if (string.IsNullOrWhiteSpace(redisConnection))
        throw new InvalidOperationException("Redis connection string is not configured.");
    builder.Services.AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Instance =
                $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
            context.ProblemDetails.Extensions["traceId"] =
                Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
        };
    });

    builder.Services.AddAuthRateLimit();
    builder.Services.AddExceptionHandler<DomainExceptionHandler>();
    builder.Services.AddExceptionHandler<UnhandledExceptionHandler>();
    builder.Services.AddAuthorization();
    builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        return ConnectionMultiplexer.Connect(config.GetConnectionString("Redis")!);
    });
    builder.Services.AddScoped<ICacheService, RedisCacheService>();
    builder.Services.AddHealthChecks().AddDbContextCheck<EventHubDbContext>();
    builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));

    builder.Services.AddSingleton<IAmazonS3>(sp =>
    {
        var o = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
        return new AmazonS3Client(o.AccessKey, o.SecretKey, new AmazonS3Config
        {
            ServiceURL = o.Endpoint,
            ForcePathStyle = true,
            AuthenticationRegion = "garage"
        });
    });

    builder.Services.AddScoped<IImageStorage, S3ImageStorage>();
    builder.Services.AddScoped<OrphanImageCleaner>();
    builder.Services.AddHostedService<ImageCleanupWorker>();
    builder.Services.Configure<ImageCleanupOptions>(
        builder.Configuration.GetSection(ImageCleanupOptions.SectionName));


    var app = builder.Build();

    if (args.Contains("--migrate-only"))
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EventHubDbContext>();
        await db.Database.MigrateAsync();
        Log.Information("Migrations applied");
        return 0;
    }

    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        KnownIPNetworks = { new System.Net.IPNetwork(IPAddress.Parse("172.16.0.0"), 12) },
    });
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate =
            "{RequestMethod} {RequestPath} → {StatusCode} in {Elapsed:0.0} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            if (httpContext.User.Identity?.IsAuthenticated == true)
                diagnosticContext.Set("UserId", httpContext.User.GetUserId());
        };
    });
    app.UseExceptionHandler();
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        // API reference only where it is needed – in production it would describe the attack surface.
        app.MapScalarApiReference();
    }
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseRateLimiter();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

    app.Run();
    return 0;
}
catch (Exception e) when (e is not HostAbortedException)
{
    Log.Fatal(e, "EventHub terminated Unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}