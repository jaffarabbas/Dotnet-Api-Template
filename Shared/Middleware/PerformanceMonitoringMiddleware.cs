using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ApiTemplate.Middleware
{
    /// <summary>
    /// Middleware to monitor application performance and detect slow operations
    /// </summary>
    public class PerformanceMonitoringMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PerformanceMonitoringMiddleware> _logger;
        private const int SlowRequestThresholdMs = 1000; // 1 second
        private const int VerySlowRequestThresholdMs = 3000; // 3 seconds

        public PerformanceMonitoringMiddleware(RequestDelegate next, ILogger<PerformanceMonitoringMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var path = context.Request.Path;
            var method = context.Request.Method;

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                var elapsedMs = stopwatch.ElapsedMilliseconds;

                // Log performance metrics
                if (elapsedMs >= VerySlowRequestThresholdMs)
                {
                    _logger.LogWarning(
                        "PERFORMANCE: Very slow request detected | {Method} {Path} | Duration: {Duration}ms | Status: {StatusCode}",
                        method, path, elapsedMs, context.Response.StatusCode);
                }
                else if (elapsedMs >= SlowRequestThresholdMs)
                {
                    _logger.LogInformation(
                        "PERFORMANCE: Slow request | {Method} {Path} | Duration: {Duration}ms | Status: {StatusCode}",
                        method, path, elapsedMs, context.Response.StatusCode);
                }

                // Log all requests with detailed timing
                _logger.LogDebug(
                    "PERFORMANCE: Request completed | {Method} {Path} | Duration: {Duration}ms | Status: {StatusCode}",
                    method, path, elapsedMs, context.Response.StatusCode);

                // Add performance header for client-side monitoring
                context.Response.Headers.TryAdd("X-Response-Time-Ms", elapsedMs.ToString());
            }
        }
    }
}
