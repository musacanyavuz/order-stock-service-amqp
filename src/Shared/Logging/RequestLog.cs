using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Shared.Logging
{
    public class RequestLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; } // RequestId

        public List<LogEntry> Logs { get; set; } = new();
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public class LogEntry
    {
        public string Service { get; set; } // Order.API, Stock.API
        public string Message { get; set; }
        public string Type { get; set; } // Info, Warning, Error
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
