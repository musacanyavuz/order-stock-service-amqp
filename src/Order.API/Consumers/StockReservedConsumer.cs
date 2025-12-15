using MassTransit;
using Order.API.Infrastructure;
using Shared.Events;
using Microsoft.EntityFrameworkCore;

namespace Order.API.Consumers
{
    public sealed class StockReservedConsumer : IConsumer<Shared.Events.IStockReserved>
    {
        private readonly OrderDbContext _context;

        public StockReservedConsumer(OrderDbContext context)
        {
            _context = context;
        }

        public async Task Consume(ConsumeContext<IStockReserved> context)
        {
            var orderId = context.Message.OrderId;
            
            var order = await _context.Orders.FirstOrDefaultAsync(x => x.Id == orderId);
            if (order != null)
            {
                order.Status = Models.OrderStatus.Completed;
                await _context.SaveChangesAsync();
                
                Console.WriteLine($"[Order.API] Order {orderId} Completed. Stock Reserved.");
            }
            else 
            {
                 Console.WriteLine($"[Order.API] Order {orderId} not found while processing StockReserved.");
            }
        }
    }
}
