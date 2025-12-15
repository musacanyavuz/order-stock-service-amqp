using System;
using System.Collections.Generic;

namespace Shared.Events
{
    public interface IStockReservationFailed
    {
        Guid OrderId { get; set; }
        Guid BuyerId { get; set; }
        List<Guid> ProductIds { get; set; }
        string Message { get; set; }
    }
}
