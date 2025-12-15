using MassTransit;
using Shared.Logging;
using System;
using System.Threading.Tasks;

namespace Shared.Logging
{
    public sealed class MongoLogConsumeFilter<T> : IFilter<ConsumeContext<T>> where T : class
    {
        private readonly IMongoLogService _logService;

        public MongoLogConsumeFilter(IMongoLogService logService)
        {
            _logService = logService;
        }

        public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
        {
            var requestId = context.Headers.Get<string>("X-Request-ID") ?? Guid.NewGuid().ToString();
            Console.WriteLine($"[MongoLogConsumeFilter] Consuming {typeof(T).Name} (Req: {requestId})");

            try
            {
                await _logService.AddLog(requestId, "MassTransit", $"Consuming message: {typeof(T).Name}", "Info");
                
                await next.Send(context);
                
                await _logService.AddLog(requestId, "MassTransit", $"Successfully consumed message: {typeof(T).Name}", "Info");
            }
            catch (Exception ex)
            {
                await _logService.AddLog(requestId, "MassTransit", $"Error consuming message: {typeof(T).Name}. Error: {ex.Message}", "Error");
                throw;
            }
        }

        public void Probe(ProbeContext context)
        {
            context.CreateFilterScope("MongoLogConsumeFilter");
        }
    }
}
