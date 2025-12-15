using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Logging
{
    public sealed class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _serviceName;

        public LoggingMiddleware(RequestDelegate next, string serviceName)
        {
            _next = next;
            _serviceName = serviceName;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requestId = context.Request.Headers["X-Request-ID"].ToString();
            if (string.IsNullOrEmpty(requestId))
            {
                requestId = Guid.NewGuid().ToString();
                context.Request.Headers["X-Request-ID"] = requestId; // Propagate downstream via HTTP if needed
            }

            // Store in HttpContext.Items for inner services to access
            context.Items["RequestId"] = requestId;

            var logger = context.RequestServices.GetService<IMongoLogService>();
            if (logger != null)
            {
                try 
                {
                    await logger.AddLog(requestId, _serviceName, $"Request Started: {context.Request.Path}", "Info");
                }
                catch { /* Fail Safe */ }
            }

            try
            {
                await _next(context);

                if (logger != null)
                {
                     await logger.AddLog(requestId, _serviceName, $"Request Finished. Status: {context.Response.StatusCode}", "Info");
                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    await logger.AddLog(requestId, _serviceName, $"Request Failed: {ex.Message}", "Error");
                }
                throw;
            }
        }
    }
}
