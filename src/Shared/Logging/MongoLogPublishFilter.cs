using MassTransit;
using Shared.Logging;
using System;
using System.Threading.Tasks;

namespace Shared.Logging
{
    public sealed class MongoLogPublishFilter<T> : IFilter<PublishContext<T>> where T : class
    {
        private readonly IMongoLogService _logService;

        public MongoLogPublishFilter(IMongoLogService logService)
        {
            _logService = logService;
        }

        public async Task Send(PublishContext<T> context, IPipe<PublishContext<T>> next)
        {
            var requestId = context.Headers.Get<string>("X-Request-ID") ?? Guid.NewGuid().ToString();
            
            Console.WriteLine($"[MongoLogPublishFilter] Publishing {typeof(T).Name} (Req: {requestId})");

            // Log BEFORE execution
            await _logService.AddLog(requestId, "MassTransit", $"Publishing message: {typeof(T).Name}", "Info");

            await next.Send(context);
        }

        public void Probe(ProbeContext context)
        {
            context.CreateFilterScope("MongoLogPublishFilter");
        }
    }
}
