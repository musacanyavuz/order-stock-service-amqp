using System;

namespace Shared.Events
{
    public interface IStockReserved
    {
        Guid OrderId { get; set; }
        Guid BuyerId { get; set; }
        decimal TotalPrice { get; set; }
    }
}
