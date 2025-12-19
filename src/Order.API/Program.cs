using MassTransit;
using Microsoft.EntityFrameworkCore;
using Order.API.Infrastructure;
using System.Reflection;
using System.Data;
using Shared.Monitoring;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient",
        builder => builder
            .WithOrigins("http://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader());
});


builder.Services.AddDbContext<OrderDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddMassTransit(x =>
{
    // Register all consumers in the current assembly
    x.AddConsumers(Assembly.GetExecutingAssembly());

    x.AddEntityFrameworkOutbox<OrderDbContext>(o =>
    {
        o.QueryDelay = TimeSpan.FromSeconds(1);
        o.UsePostgres();
        o.UseBusOutbox();
        o.DuplicateDetectionWindow = TimeSpan.FromSeconds(30);
        o.IsolationLevel = IsolationLevel.ReadCommitted;
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMQ"));
        cfg.Publish<Shared.Events.IOrderCreated>(x => x.ExchangeType = RabbitMQ.Client.ExchangeType.Fanout);
        cfg.UsePublishFilter(typeof(Shared.Logging.MongoLogPublishFilter<>), context);
        cfg.ReceiveEndpoint("order-service", e =>
        {
            e.UseConsumeFilter(typeof(Shared.Logging.MongoLogConsumeFilter<>), context);
            e.ConfigureConsumers(context);
        });
    });
});

// Logging
builder.Services.AddSingleton<Shared.Logging.IMongoLogService, Shared.Logging.MongoLogService>();
builder.Services.AddServiceMonitoring("Order.API");

var app = builder.Build();

// Middleware
app.UseMiddleware<Shared.Logging.LoggingMiddleware>("Order.API");

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
app.MapPrometheusScrapingEndpoint();

// Apply migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    context.Database.EnsureCreated();
}

app.Run();
