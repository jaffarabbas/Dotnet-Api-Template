using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using System.Collections.ObjectModel;
using System.Data;

namespace Shared.Services
{
    /// <summary>
    /// Centralized Serilog logging configuration service
    /// </summary>
    public static class LoggingConfiguration
    {
        /// <summary>
        /// Configures Serilog for the application with multiple sinks (Console, File, Database)
        /// </summary>
        public static void ConfigureSerilog(this WebApplicationBuilder builder)
        {
            var configuration = builder.Configuration;
            var environment = builder.Environment;

            // Create the logger configuration
            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "ApiTemplate")
                .Enrich.WithProperty("Environment", environment.EnvironmentName)
                .Enrich.WithProperty("MachineName", Environment.MachineName);

            // Console Sink (for Development)
            if (environment.IsDevelopment())
            {
                loggerConfig.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}"
                );
            }

            // File Sink (JSON format for structured logging)
            var logsPath = configuration["Serilog:FilePath"] ?? "Logs/log-.json";
            loggerConfig.WriteTo.File(
                new Serilog.Formatting.Compact.CompactJsonFormatter(),
                logsPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                fileSizeLimitBytes: 10_485_760, // 10MB
                rollOnFileSizeLimit: true
            );

            // Plain text file sink for easy reading
            loggerConfig.WriteTo.File(
                configuration["Serilog:TextFilePath"] ?? "Logs/log-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
            );

            // SQL Server Sink (for production logging to database)
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrEmpty(connectionString))
            {
                var columnOptions = new ColumnOptions
                {
                    AdditionalColumns = new Collection<SqlColumn>
                    {
                        new SqlColumn { ColumnName = "UserName", DataType = SqlDbType.NVarChar, DataLength = 100, AllowNull = true },
                        new SqlColumn { ColumnName = "IPAddress", DataType = SqlDbType.NVarChar, DataLength = 50, AllowNull = true },
                        new SqlColumn { ColumnName = "RequestPath", DataType = SqlDbType.NVarChar, DataLength = 500, AllowNull = true },
                        new SqlColumn { ColumnName = "ActionName", DataType = SqlDbType.NVarChar, DataLength = 200, AllowNull = true },
                        new SqlColumn { ColumnName = "Application", DataType = SqlDbType.NVarChar, DataLength = 100, AllowNull = true }
                    }
                };

                // Remove the default Id column to use auto-increment
                columnOptions.Store.Remove(StandardColumn.Properties);
                columnOptions.Store.Add(StandardColumn.LogEvent);

                loggerConfig.WriteTo.MSSqlServer(
                    connectionString: connectionString,
                    sinkOptions: new MSSqlServerSinkOptions
                    {
                        TableName = "ApplicationLogs",
                        SchemaName = "dbo",
                        AutoCreateSqlTable = true,
                        BatchPostingLimit = 50,
                        BatchPeriod = TimeSpan.FromSeconds(5)
                    },
                    columnOptions: columnOptions,
                    restrictedToMinimumLevel: LogEventLevel.Information
                );
            }

            // Create the logger
            Log.Logger = loggerConfig.CreateLogger();

            // Add Serilog to the application
            builder.Host.UseSerilog();

            Log.Information("Logging system initialized - Environment: {Environment}", environment.EnvironmentName);
        }
    }
}
