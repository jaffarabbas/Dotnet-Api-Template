using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Shared.Services;

namespace ApiTemplate.Services
{
    /// <summary>
    /// Extension methods for registering Serilog logging services
    /// </summary>
    public static class SerilogServiceExtensions
    {
        /// <summary>
        /// Adds Serilog logging configuration to the application builder.
        /// This should be called early in the application startup pipeline.
        /// </summary>
        /// <param name="builder">The WebApplicationBuilder instance</param>
        /// <returns>The WebApplicationBuilder for method chaining</returns>
        public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
        {
            builder.ConfigureSerilog();
            return builder;
        }
    }
}
