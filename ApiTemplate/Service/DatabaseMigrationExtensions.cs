using DBLayer.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApiTemplate.Services
{
    /// <summary>
    /// Extension methods for applying database migrations during application startup.
    /// </summary>
    public static class DatabaseMigrationExtensions
    {
        /// <summary>
        /// Applies pending migrations to the database. Should be called before UseApplicationPipeline().
        /// </summary>
        /// <param name="app">The WebApplication instance</param>
        /// <param name="applyMigrations">Whether to apply migrations (default: true)</param>
        /// <returns>The WebApplication for method chaining</returns>
        public static WebApplication ApplyDatabaseMigrations(this WebApplication app, bool applyMigrations = true)
        {
            if (!applyMigrations)
            {
                return app;
            }

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<TestContext>();
                    var logger = services.GetRequiredService<ILogger<Program>>();

                    logger.LogInformation("Checking for pending database migrations...");

                    // Check if there are pending migrations
                    var pendingMigrations = context.Database.GetPendingMigrations().ToList();

                    if (pendingMigrations.Any())
                    {
                        logger.LogInformation("Found {Count} pending migration(s): {Migrations}",
                            pendingMigrations.Count,
                            string.Join(", ", pendingMigrations));

                        // Apply migrations
                        context.Database.Migrate();

                        logger.LogInformation("Database migrations applied successfully");
                    }
                    else
                    {
                        logger.LogInformation("Database is up to date. No migrations needed.");
                    }
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while migrating the database.");

                    // Optionally rethrow to prevent app startup with a broken database
                    // throw;
                }
            }

            return app;
        }

        /// <summary>
        /// Ensures the database exists (creates if missing) without applying migrations.
        /// Useful for development scenarios.
        /// </summary>
        /// <param name="app">The WebApplication instance</param>
        /// <returns>The WebApplication for method chaining</returns>
        public static WebApplication EnsureDatabaseCreated(this WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<TestContext>();
                    var logger = services.GetRequiredService<ILogger<Program>>();

                    logger.LogInformation("Ensuring database exists...");
                    context.Database.EnsureCreated();
                    logger.LogInformation("Database verification completed");
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while ensuring database exists.");
                }
            }

            return app;
        }
    }
}
