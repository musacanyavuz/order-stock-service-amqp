using System;
using System.Collections.Generic;

namespace Order.API.Models
{
    public enum OrderStatus
    {
        Suspend,
        Completed,
        Failed
    }

    public class Order
    {
        public Guid Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid BuyerId { get; set; }
        public OrderStatus Status { get; set; }
        public required Address Address { get; set; }
        public string? FailMessage { get; set; }

        public required ICollection<OrderItem> Items { get; set; }
    }

    public class Address
    {
        public required string Line { get; set; }
        public required string Province { get; set; }
        public required string District { get; set; }
    }
}
