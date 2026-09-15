using System.Text;
using EventHub.Domain.Interfaces;
using EventHub.Domain.Services;
using EventHub.Infrastructure.Authentication;
using EventHub.Infrastructure.Caching;
using EventHub.Infrastructure.Entities;
using EventHub.Infrastructure.Persistence;
using EventHub.Infrastructure.Repositories;
using EventHub.Infrastructure.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);


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

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
          ?? throw new InvalidOperationException(
              "The ‘Jwt’ section is missing from the configuration.");


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

builder.Services.AddAuthorization();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
builder.Services.AddScoped<ICacheService, RedisCacheService>();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseHttpsRedirection();

app.Run();