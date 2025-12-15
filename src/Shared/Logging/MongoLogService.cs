using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;

namespace Shared.Logging
{
    public class MongoLogService : IMongoLogService
    {
        private readonly IMongoCollection<RequestLog> _collection;

        public MongoLogService(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("MongoDb");
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase("LogDb");
            _collection = database.GetCollection<RequestLog>("RequestLogs");
        }

        public async Task AddLog(string requestId, string service, string message, string type = "Info")
        {
            if (!Guid.TryParse(requestId, out var requestGuid)) return;

            var logEntry = new LogEntry
            {
                Service = service,
                Message = message,
                Type = type,
                Timestamp = DateTime.UtcNow
            };

            var filter = Builders<RequestLog>.Filter.Eq(x => x.Id, requestGuid);
            var update = Builders<RequestLog>.Update
                .Push(x => x.Logs, logEntry)
                .SetOnInsert(x => x.CreatedDate, DateTime.UtcNow);

            await _collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
        }
    }
}
