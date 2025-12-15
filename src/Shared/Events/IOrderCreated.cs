using System;
using System.Collections.Generic;
using Shared.Messages;

namespace Shared.Events
{
    public interface IOrderCreated
    {
        Guid OrderId { get; set; }
        Guid BuyerId { get; set; }
        List<OrderItemMessage> OrderItems { get; set; }
        decimal TotalPrice { get; set; }
        DateTime CreatedDate { get; set; }
    }
}
