using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace ApiTemplate.Middleware
{
    /// <summary>
    /// Middleware to log HTTP requests and responses with performance metrics
    /// </summary>
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
        private static readonly string[] SensitiveHeaders = { "authorization", "cookie", "x-api-key" };
        private static readonly string[] SensitivePaths = { "/api/auth/login", "/api/auth/register", "/api/auth/change-password" };

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip logging for static files and health checks
            if (ShouldSkipLogging(context))
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString();
            context.Items["RequestId"] = requestId;

            // Log request
            await LogRequest(context, requestId);

            // Capture response
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context);
                stopwatch.Stop();

                // Log response
                await LogResponse(context, requestId, stopwatch.ElapsedMilliseconds);

                // Copy response back to original stream
                await responseBody.CopyToAsync(originalBodyStream);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "Unhandled exception in request {RequestId} | Path: {Path} | Duration: {Duration}ms",
                    requestId, context.Request.Path, stopwatch.ElapsedMilliseconds);
                throw;
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }

        private async Task LogRequest(HttpContext context, string requestId)
        {
            try
            {
                var request = context.Request;
                var ipAddress = GetClientIpAddress(context);
                var userName = context.User?.Identity?.Name ?? "Anonymous";

                var requestLog = new
                {
                    RequestId = requestId,
                    Timestamp = DateTime.UtcNow,
                    Method = request.Method,
                    Path = request.Path.Value,
                    QueryString = request.QueryString.Value,
                    IPAddress = ipAddress,
                    UserName = userName,
                    UserAgent = request.Headers["User-Agent"].ToString(),
                    ContentType = request.ContentType,
                    ContentLength = request.ContentLength,
                    Headers = GetSafeHeaders(request.Headers)
                };

                // Log request body for POST/PUT/PATCH (but not for sensitive endpoints)
                if (ShouldLogBody(context, request.Method))
                {
                    request.EnableBuffering();
                    var bodyAsText = await ReadBodyAsync(request.Body);
                    request.Body.Position = 0;

                    _logger.LogInformation(
                        "HTTP Request {RequestId} | {Method} {Path} | IP: {IPAddress} | User: {UserName} | Body: {Body}",
                        requestId, request.Method, request.Path, ipAddress, userName,
                        MaskSensitiveData(bodyAsText, request.Path));
                }
                else
                {
                    _logger.LogInformation(
                        "HTTP Request {RequestId} | {Method} {Path} | IP: {IPAddress} | User: {UserName}",
                        requestId, request.Method, request.Path, ipAddress, userName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging request {RequestId}", requestId);
            }
        }

        private async Task LogResponse(HttpContext context, string requestId, long durationMs)
        {
            try
            {
                var response = context.Response;

                response.Body.Seek(0, SeekOrigin.Begin);
                var responseBodyText = await new StreamReader(response.Body).ReadToEndAsync();
                response.Body.Seek(0, SeekOrigin.Begin);

                var logLevel = response.StatusCode >= 500 ? LogLevel.Error
                    : response.StatusCode >= 400 ? LogLevel.Warning
                    : LogLevel.Information;

                _logger.Log(logLevel,
                    "HTTP Response {RequestId} | Status: {StatusCode} | Duration: {Duration}ms | ContentType: {ContentType} | Size: {Size} bytes",
                    requestId, response.StatusCode, durationMs, response.ContentType, responseBodyText.Length);

                // Log performance warnings
                if (durationMs > 3000)
                {
                    _logger.LogWarning(
                        "Slow Request Detected {RequestId} | Path: {Path} | Duration: {Duration}ms",
                        requestId, context.Request.Path, durationMs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging response {RequestId}", requestId);
            }
        }

        private bool ShouldSkipLogging(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";
            return path.StartsWith("/swagger")
                || path.StartsWith("/health")
                || path.StartsWith("/favicon.ico")
                || path.Contains("signalr");
        }

        private bool ShouldLogBody(HttpContext context, string method)
        {
            if (method != "POST" && method != "PUT" && method != "PATCH")
                return false;

            var path = context.Request.Path.Value?.ToLower() ?? "";
            return !SensitivePaths.Any(sp => path.Contains(sp.ToLower()));
        }

        private string GetClientIpAddress(HttpContext context)
        {
            return context.Connection.RemoteIpAddress?.ToString()
                ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                ?? "Unknown";
        }

        private Dictionary<string, string> GetSafeHeaders(IHeaderDictionary headers)
        {
            var safeHeaders = new Dictionary<string, string>();
            foreach (var header in headers)
            {
                if (!SensitiveHeaders.Contains(header.Key.ToLower()))
                {
                    safeHeaders[header.Key] = header.Value.ToString();
                }
                else
                {
                    safeHeaders[header.Key] = "***REDACTED***";
                }
            }
            return safeHeaders;
        }

        private async Task<string> ReadBodyAsync(Stream body)
        {
            body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(body, Encoding.UTF8, leaveOpen: true);
            var bodyText = await reader.ReadToEndAsync();
            body.Seek(0, SeekOrigin.Begin);
            return bodyText;
        }

        private string MaskSensitiveData(string body, PathString path)
        {
            if (string.IsNullOrWhiteSpace(body))
                return body;

            // Mask password fields in JSON
            var sensitiveFields = new[] { "password", "currentPassword", "newPassword", "resetToken" };
            foreach (var field in sensitiveFields)
            {
                body = System.Text.RegularExpressions.Regex.Replace(
                    body,
                    $"\"{field}\"\\s*:\\s*\"[^\"]*\"",
                    $"\"{field}\": \"***MASKED***\"",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            return body;
        }
    }
}
