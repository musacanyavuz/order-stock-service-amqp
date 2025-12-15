using MassTransit;
using Microsoft.EntityFrameworkCore;
using Stock.API.Consumers;
using Stock.API.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<StockDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCreatedConsumer>();

    x.AddEntityFrameworkOutbox<StockDbContext>(o =>
    {
        o.QueryDelay = TimeSpan.FromSeconds(1);
        o.UsePostgres();
        o.UseBusOutbox();
        o.DuplicateDetectionWindow = TimeSpan.FromSeconds(30);
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMQ"));

        cfg.Publish<Shared.Events.IStockReserved>(x => x.ExchangeType = RabbitMQ.Client.ExchangeType.Fanout);
        cfg.Publish<Shared.Events.IStockReservationFailed>(x => x.ExchangeType = RabbitMQ.Client.ExchangeType.Fanout);
        
        cfg.UsePublishFilter(typeof(Shared.Logging.MongoLogPublishFilter<>), context);
        
        cfg.ReceiveEndpoint("stock-service", e =>
        {
            e.UseConsumeFilter(typeof(Shared.Logging.MongoLogConsumeFilter<>), context);
            
            e.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1)));

            e.UseEntityFrameworkOutbox<StockDbContext>(context);
            e.ConfigureConsumer<OrderCreatedConsumer>(context);
        });
    });
});

// Logging
builder.Services.AddSingleton<Shared.Logging.IMongoLogService, Shared.Logging.MongoLogService>();

var app = builder.Build();

// Middleware
app.UseMiddleware<Shared.Logging.LoggingMiddleware>("Stock.API");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseCors();

app.MapControllers();

// Apply migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<StockDbContext>();
    context.Database.EnsureCreated();
}

// Apply migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<StockDbContext>();
    
    try 
    {
        context.Database.EnsureCreated();
        
        // Reset Stocks for Demo
        var stocksToReset = context.Stocks.ToList();
        foreach (var stock in stocksToReset)
        {
            stock.Count = 100;
            context.Stocks.Update(stock);
        }
        
        var demoProductId = Guid.Parse("d8d47424-0c5a-4e2b-b5d1-93335555d444");
        if (!stocksToReset.Any(x => x.ProductId == demoProductId))
        {
            context.Stocks.Add(new Stock.API.Models.Stock 
            { 
                Id = Guid.NewGuid(), 
                ProductId = demoProductId, 
                Count = 100
            });
        }

        context.SaveChanges();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Stock.API] Infrastructure Setup Error: {ex.Message}");
    }
}

app.Run();
