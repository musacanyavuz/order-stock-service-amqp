using MassTransit;
using Order.API.Infrastructure;
using Shared.Events;
using Microsoft.EntityFrameworkCore;

namespace Order.API.Consumers
{
    public class StockReservationFailedConsumer : IConsumer<Shared.Events.IStockReservationFailed>
    {
        private readonly OrderDbContext _context;

        public StockReservationFailedConsumer(OrderDbContext context)
        {
            _context = context;
        }

        public async Task Consume(ConsumeContext<IStockReservationFailed> context)
        {
            var orderId = context.Message.OrderId;
            
            var order = await _context.Orders.FirstOrDefaultAsync(x => x.Id == orderId);
            if (order != null)
            {
                order.Status = Models.OrderStatus.Failed;
                order.FailMessage = context.Message.Message;
                await _context.SaveChangesAsync();
                
                 Console.WriteLine($"[Order.API] Order {orderId} Failed. Reason: {context.Message.Message}");
            }
            else 
            {
                 Console.WriteLine($"[Order.API] Order {orderId} not found while processing StockReservationFailed.");
            }
        }
    }
}
