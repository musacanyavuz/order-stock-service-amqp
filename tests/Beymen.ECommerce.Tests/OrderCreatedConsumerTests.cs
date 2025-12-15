using Xunit;
using Shared.Messages;
using Moq;
using MassTransit;
using Stock.API.Consumers;
using Stock.API.Infrastructure;
using Stock.API.Models;
using Shared.Events;
using Shared.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Beymen.ECommerce.Tests
{
    public class OrderCreatedConsumerTests
    {
        private readonly OrderCreatedConsumer _consumer;
        private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
        private readonly Mock<IMongoLogService> _mockLogger;
        private readonly StockDbContext _dbContext;

        public OrderCreatedConsumerTests()
        {
            _mockPublishEndpoint = new Mock<IPublishEndpoint>();
            _mockLogger = new Mock<IMongoLogService>();
            
            var options = new DbContextOptionsBuilder<StockDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _dbContext = new StockDbContext(options);
            
            _consumer = new OrderCreatedConsumer(_dbContext, _mockPublishEndpoint.Object);
        }

        [Fact]
        public async Task Consume_ShouldFail_WhenStockIsInsufficient()
        {
            // Arrange
            var productId = Guid.NewGuid();
            _dbContext.Stocks.Add(new Stock.API.Models.Stock { Id = Guid.NewGuid(), ProductId = productId, Count = 5 });
            await _dbContext.SaveChangesAsync();

            var message = new TestOrderCreatedEvent
            {
                OrderId = Guid.NewGuid(),
                BuyerId = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow,
                TotalPrice = 100,
                OrderItems = new List<OrderItemMessage>
                {
                    new OrderItemMessage { ProductId = productId, Count = 10, Price = 10 } // Requesting 10, have 5
                }
            };

            var contextMock = new Mock<ConsumeContext<IOrderCreated>>();
            contextMock.Setup(x => x.Message).Returns(message);
            contextMock.Setup(x => x.Headers.Get<string>("X-Request-ID", null)).Returns(Guid.NewGuid().ToString());

            // Act
            await _consumer.Consume(contextMock.Object);

            // Assert
            _mockPublishEndpoint.Verify(x => x.Publish(
                It.Is<IStockReservationFailed>(e => e.OrderId == message.OrderId && e.Message == "Stock not sufficient"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Consume_ShouldReserveStock_WhenStockIsSufficient()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var initialStock = 20;
            _dbContext.Stocks.Add(new Stock.API.Models.Stock { Id = Guid.NewGuid(), ProductId = productId, Count = initialStock });
            await _dbContext.SaveChangesAsync();

            var message = new TestOrderCreatedEvent
            {
                OrderId = Guid.NewGuid(),
                BuyerId = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow,
                TotalPrice = 100,
                OrderItems = new List<OrderItemMessage>
                {
                    new OrderItemMessage { ProductId = productId, Count = 5, Price = 10 }
                }
            };

            var contextMock = new Mock<ConsumeContext<IOrderCreated>>();
            contextMock.Setup(x => x.Message).Returns(message);
            contextMock.Setup(x => x.Headers.Get<string>("X-Request-ID", null)).Returns(Guid.NewGuid().ToString());

            // Act
            await _consumer.Consume(contextMock.Object);

            // Assert
            var updatedStock = await _dbContext.Stocks.FirstOrDefaultAsync(x => x.ProductId == productId);
            Assert.Equal(initialStock - 5, updatedStock.Count);

            _mockPublishEndpoint.Verify(x => x.Publish(
                It.Is<IStockReserved>(e => e.OrderId == message.OrderId),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
    public class TestOrderCreatedEvent : IOrderCreated
    {
        public Guid OrderId { get; set; }
        public Guid BuyerId { get; set; }
        public List<OrderItemMessage> OrderItems { get; set; } = new();
        public decimal TotalPrice { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
