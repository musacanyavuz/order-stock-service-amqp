using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Events;
using Stock.API.Infrastructure;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Stock.API.Consumers
{
    public class OrderCreatedConsumer : IConsumer<Shared.Events.IOrderCreated>
    {
        private readonly StockDbContext _context;
        private readonly IPublishEndpoint _publishEndpoint;


        public OrderCreatedConsumer(StockDbContext context, IPublishEndpoint publishEndpoint)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;
        }

        public async Task Consume(ConsumeContext<IOrderCreated> context)
        {
            Console.WriteLine($"[Stock.API] HIT! Raw Consume Method called for Order {context.Message.OrderId}");
            var requestId = context.Headers.Get<string>("X-Request-ID") ?? Guid.NewGuid().ToString();

            var message = context.Message;
            var productIds = message.OrderItems.Select(x => x.ProductId).ToList();

            var stocks = await _context.Stocks
                .Where(x => productIds.Contains(x.ProductId))
                .ToListAsync();

            var insufficientStocks = new List<Guid>();

            foreach (var item in message.OrderItems)
            {
                var stock = stocks.FirstOrDefault(x => x.ProductId == item.ProductId);
                Console.WriteLine($"[StockCheck] Checking Product: {item.ProductId}. Requested: {item.Count}. Found Stock Object: {(stock == null ? "NULL" : "YES")}. Available: {stock?.Count}");
                
                if (stock == null || stock.Count < item.Count)
                {
                    Console.WriteLine($"[StockCheck] FAILURE. Stock was {(stock == null ? "NULL" : "Insufficient")}");
                    insufficientStocks.Add(item.ProductId);
                }
            }

            if (insufficientStocks.Any())
            {
                await _publishEndpoint.Publish<IStockReservationFailed>(new StockReservationFailedEvent
                {
                    OrderId = message.OrderId,
                    BuyerId = message.BuyerId,
                    ProductIds = insufficientStocks,
                    Message = "Stok Yetersiz (Insufficient Stock)"
                });

                return;
            }

            foreach (var item in message.OrderItems)
            {
                var stock = stocks.FirstOrDefault(x => x.ProductId == item.ProductId);
                if (stock != null)
                {
                    stock.Count -= item.Count;
                }
            }

            try
            {
                await _publishEndpoint.Publish<IStockReserved>(new StockReservedEvent
                {
                    OrderId = message.OrderId,
                    BuyerId = message.BuyerId,
                    TotalPrice = message.TotalPrice
                });

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                Console.WriteLine($"[Stock.API] Concurrency Exception for Order {message.OrderId}. Retrying...");
                throw;
            }
        }
    }

    public class StockReservedEvent : IStockReserved
    {
        public Guid OrderId { get; set; }
        public Guid BuyerId { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class StockReservationFailedEvent : IStockReservationFailed
    {
        public Guid OrderId { get; set; }
        public Guid BuyerId { get; set; }
        public required List<Guid> ProductIds { get; set; }
        public required string Message { get; set; }
    }
}
