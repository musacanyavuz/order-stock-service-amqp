using Microsoft.AspNetCore.Mvc;
using MassTransit;
using Order.API.Infrastructure;
using Order.API.Models;
using Order.API.DTOs;
using Shared.Events;
using Shared.Messages;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace Order.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class OrdersController : ControllerBase
    {
        private readonly OrderDbContext _context;
        private readonly IPublishEndpoint _publishEndpoint;


        public OrdersController(OrderDbContext context, IPublishEndpoint publishEndpoint)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(OrderCreateRequest request)
        {
            var newOrder = new Models.Order
            {
                Id = Guid.NewGuid(),
                BuyerId = request.BuyerId,
                Status = OrderStatus.Suspend,
                Address = new Address { Line = request.Address.Line, District = request.Address.District, Province = request.Address.Province },
                CreatedDate = DateTime.UtcNow,
                Items = request.OrderItems.Select(x => new OrderItem
                {
                    Count = x.Count,
                    Price = x.Price,
                    ProductId = x.ProductId
                }).ToList()
            };

            await _context.Orders.AddAsync(newOrder);

            var orderCreatedEvent = new OrderCreatedEvent
            {
                BuyerId = newOrder.BuyerId,
                OrderId = newOrder.Id,
                TotalPrice = newOrder.Items.Sum(x => x.Price * x.Count),
                CreatedDate = newOrder.CreatedDate,
                OrderItems = newOrder.Items.Select(x => new OrderItemMessage
                {
                    ProductId = x.ProductId,
                    Count = x.Count,
                    Price = x.Price
                }).ToList()
            };

            var requestId = HttpContext.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString();

            await _publishEndpoint.Publish<IOrderCreated>(orderCreatedEvent, context => {
                context.Headers.Set("X-Request-ID", requestId);
            });
            await _context.SaveChangesAsync();
            


            return Ok(new { OrderId = newOrder.Id });
        }
    }

    public class OrderCreatedEvent : IOrderCreated
    {
        public Guid OrderId { get; set; }
        public Guid BuyerId { get; set; }
        public required List<OrderItemMessage> OrderItems { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
