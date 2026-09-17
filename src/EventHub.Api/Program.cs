using System.Diagnostics;
using System.Net;
using System.Text;
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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
    builder.Services.AddScoped<IEventsRepository, EventsRepository>();
    builder.Services.AddScoped<IEventsService, EventsService>();
    builder.Services.AddScoped<IBookingsService, BookingsService>();
    builder.Services.AddScoped<ITransactionRunner, EfTransactionRunner>();
    builder.Services.AddScoped<IBookingRepository, BookingsRepository>();
    builder.Services.AddSingleton<ChannelNotificationQueue>();
    builder.Services.AddSingleton<INotificationQueue>(sp => sp.GetRequiredService<ChannelNotificationQueue>());
    builder.Services.AddScoped<INotificationQueue, OutboxNotificationQueue>();
    builder.Services.AddScoped<INotificationSender, LoggingNotificationSender>();
    builder.Services.AddHostedService<OutboxWorker>();
    builder.Services.AddScoped<INotificationSender, LoggingNotificationSender>();
    builder.Services.AddHostedService<NotificationWorker>();
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
        app.UseHttpsRedirection();
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.UseHttpsRedirection();
    app.UseAuthentication();
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