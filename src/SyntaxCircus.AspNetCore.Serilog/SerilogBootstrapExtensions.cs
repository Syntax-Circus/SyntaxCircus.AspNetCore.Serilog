using Serilog;

namespace SyntaxCircus.AspNetCore.Serilog;

public static class SerilogBootstrapExtensions
{
    private const string DefaultFileOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// Bootstraps Serilog with a console-only static logger for pre-DI startup logging, and configures
    /// an independent full logger (from <paramref name="builder"/>'s configuration + DI services) via
    /// <c>Services.AddSerilog(...)</c> — the registration path that works for both
    /// <c>WebApplicationBuilder</c> and a plain worker-service <c>HostApplicationBuilder</c>,
    /// unlike <c>Host.UseSerilog(...)</c> which only <c>WebApplicationBuilder</c> exposes.
    /// The static bootstrap logger is preserved so multiple hosts in the same process do not share
    /// a reloadable logger lifecycle; use <c>ILogger&lt;T&gt;</c> for the host's full logger.
    /// <paramref name="configureEnrichment"/> is an optional hook to layer additional enrichers
    /// (e.g. <c>Enrich.WithMachineName()</c>) onto the logger, invoked after
    /// <c>Enrich.FromLogContext()</c> and before the file sink (if any) is wired up.
    /// </summary>
    public static IHostApplicationBuilder AddStandardSerilog(
        this IHostApplicationBuilder builder,
        Action<SerilogFileLoggingOptions>? configureFileLogging = null,
        Action<LoggerConfiguration>? configureEnrichment = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        global::Serilog.Log.Logger = new global::Serilog.LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

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
}
