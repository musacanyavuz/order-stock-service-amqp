using MassTransit;
using Shared.Events;
using Notification.API.Infrastructure;
using Notification.API.Models;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;

namespace Notification.API.Consumers
{
    public class NotificationConsumer : 
        IConsumer<IOrderCreated>,
        IConsumer<IStockReserved>,
        IConsumer<IStockReservationFailed>
    {
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<Hubs.NotificationHub> _hubContext;
        private readonly NotificationDbContext _context;

        public NotificationConsumer(Microsoft.AspNetCore.SignalR.IHubContext<Hubs.NotificationHub> hubContext, NotificationDbContext context)
        {
            _hubContext = hubContext;
            _context = context;
        }

        public async Task Consume(ConsumeContext<IOrderCreated> context)
        {
            var requestId = context.Headers.Get<string>("X-Request-ID") ?? Guid.NewGuid().ToString();
            var message = context.Message;
            var log = $"Sipariş oluşturuldu. Müşteri ID: {message.BuyerId}";

            var exists = await _context.NotificationRecords.AnyAsync(x => x.OrderId == message.OrderId && x.Type == "OrderCreated");
            if (exists)
            {
                return;
            }

            Console.WriteLine($"[NotificationService] {log}");

            await _context.NotificationRecords.AddAsync(new NotificationRecord
            {
                Id = Guid.NewGuid(),
                OrderId = message.OrderId,
                Message = log,
                Type = "OrderCreated",
                SentDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", new 
            {
                Type = "OrderCreated",
                Source = "NotificationService",
                OrderId = message.OrderId,
                Message = log,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task Consume(ConsumeContext<IStockReserved> context)
        {
            var requestId = context.Headers.Get<string>("X-Request-ID") ?? Guid.NewGuid().ToString();
            
            var message = context.Message;
            var log = $"Stok başarıyla ayrıldı. Toplam Tutar: {message.TotalPrice:C2}";
            Console.WriteLine($"[NotificationService] {log}");

            await _context.NotificationRecords.AddAsync(new NotificationRecord
            {
                Id = Guid.NewGuid(),
                OrderId = message.OrderId,
                Message = log,
                Type = "StockReserved",
                SentDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();


            await _hubContext.Clients.All.SendAsync("ReceiveNotification", new 
            {
                Type = "StockReserved",
                Source = "NotificationService",
                OrderId = message.OrderId,
                Message = log,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task Consume(ConsumeContext<IStockReservationFailed> context)
        {
            var message = context.Message;
            var log = $"Stok işlemi başarısız! Sebep: {message.Message}";
            Console.WriteLine($"[NotificationService] {log}");

            await _context.NotificationRecords.AddAsync(new NotificationRecord
            {
                Id = Guid.NewGuid(),
                OrderId = message.OrderId,
                Message = log,
                Type = "StockFailed",
                SentDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("ReceiveNotification", new 
            {
                Type = "StockFailed",
                Source = "NotificationService",
                OrderId = message.OrderId,
                Message = log,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
