using Microsoft.Extensions.DependencyInjection;
using Shared.Services;

namespace ApiTemplate.Shared.Services
{
    /// <summary>
    /// Extension methods for registering Permission Service
    /// </summary>
    public static class PermissionServiceExtensions
    {
        /// <summary>
        /// Registers the Permission Service for dependency injection
        /// </summary>
        public static IServiceCollection AddPermissionService(this IServiceCollection services)
        {
            services.AddScoped<IPermissionService, PermissionService>();
            return services;
        }
    }
}
