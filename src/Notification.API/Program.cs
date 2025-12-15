using MassTransit;

using Notification.API.Consumers;
using Microsoft.EntityFrameworkCore;
using Notification.API.Infrastructure;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<NotificationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient",
        builder => builder
            .WithOrigins("http://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<NotificationConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMQ"));
        
        cfg.ReceiveEndpoint("notification-service", e =>
        {
            e.UseConsumeFilter(typeof(Shared.Logging.MongoLogConsumeFilter<>), context);
            e.ConfigureConsumer<NotificationConsumer>(context);
        });
    });
});

// Logging
builder.Services.AddSingleton<Shared.Logging.IMongoLogService, Shared.Logging.MongoLogService>();

var app = builder.Build();

// Middleware
app.UseMiddleware<Shared.Logging.LoggingMiddleware>("Notification.API");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowClient");

app.UseAuthorization();

app.MapControllers();
app.MapHub<Notification.API.Hubs.NotificationHub>("/notificationHub");

// Apply migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    context.Database.EnsureCreated();
}

app.Run();
