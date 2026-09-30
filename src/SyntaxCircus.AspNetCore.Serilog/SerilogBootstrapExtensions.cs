using Serilog;

namespace SyntaxCircus.AspNetCore.Serilog;

public static class SerilogBootstrapExtensions
{
    private const string DefaultFileOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

    /// <summary>Configures startup logging while preserving independent per-host DI logging.</summary>
    public static IHostApplicationBuilder AddStandardSerilog(
        this IHostApplicationBuilder builder,
        Action<SerilogFileLoggingOptions>? configureFileLogging,
        Action<LoggerConfiguration>? configureEnrichment,
        SerilogBootstrapOptions bootstrapOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(bootstrapOptions);

        var bootstrapConfiguration = new LoggerConfiguration();
        if (bootstrapOptions.ConsoleEnabled)
        {
            bootstrapConfiguration.WriteTo.Console(
                standardErrorFromLevel: bootstrapOptions.ConsoleToStandardError
                    ? global::Serilog.Events.LogEventLevel.Verbose
                    : null);
        }
        bootstrapOptions.ConfigureLogger?.Invoke(bootstrapConfiguration);
        global::Serilog.Log.Logger = bootstrapConfiguration.CreateBootstrapLogger();

        var fileLoggingOptions = new SerilogFileLoggingOptions();
        configureFileLogging?.Invoke(fileLoggingOptions);

        builder.Services.AddSerilog(
            (services, loggerConfiguration) =>
            {
                loggerConfiguration
                    .ReadFrom.Configuration(builder.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext();

                configureEnrichment?.Invoke(loggerConfiguration);

                if (fileLoggingOptions.Enabled)
                {
                    var path = SerilogFilePathResolver.Resolve(builder.Environment, fileLoggingOptions);
                    loggerConfiguration.WriteTo.File(
                        path,
                        rollingInterval: fileLoggingOptions.RollingInterval,
                        retainedFileCountLimit: fileLoggingOptions.RetainedFileCountLimit,
                        outputTemplate: fileLoggingOptions.OutputTemplate ?? DefaultFileOutputTemplate,
                        shared: fileLoggingOptions.Shared);
                }
            },
            preserveStaticLogger: true);

        return builder;
    }

    /// <summary>
    /// Installs a process-global console bootstrap logger and an independent full DI logger.
    /// File options run immediately; enrichment runs during DI logger construction after
    /// configuration, services and LogContext enrichment. The static logger is preserved so
    /// concurrent hosts do not share its reload/freeze lifecycle. Use ILogger&lt;T&gt; for host logging.
    /// </summary>
    public static IHostApplicationBuilder AddStandardSerilog(
        this IHostApplicationBuilder builder,
        Action<SerilogFileLoggingOptions>? configureFileLogging = null,
        Action<LoggerConfiguration>? configureEnrichment = null)
        => builder.AddStandardSerilog(configureFileLogging, configureEnrichment, new SerilogBootstrapOptions());
}
