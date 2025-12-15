using System;

namespace Notification.API.Models
{
    public class NotificationRecord
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public required string Message { get; set; }
        public required string Type { get; set; }
        public DateTime SentDate { get; set; }
    }
}
