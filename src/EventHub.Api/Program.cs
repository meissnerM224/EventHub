using EventHub.Domain.Interfaces;
using EventHub.Domain.Services;
using EventHub.Infrastructure.Persistence;
using EventHub.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<EventHubDbContext>(options =>
    options.UseNpgsql(
            builder.Configuration
                .GetConnectionString("Default"))
        .LogTo(Console.WriteLine));
builder.Services.AddScoped<IEventsRepository, EventsRepository>();
builder.Services.AddScoped<IEventsService, EventsService>();

var app = builder.Build();

// if (app.Environment.IsDevelopment())
// {
    app.MapOpenApi();
    app.MapScalarApiReference();
// }

app.UseHttpsRedirection();
app.MapControllers();

app.Run();