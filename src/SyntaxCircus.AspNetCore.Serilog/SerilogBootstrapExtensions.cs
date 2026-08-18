using Serilog;

namespace SyntaxCircus.AspNetCore.Serilog;

public static class SerilogBootstrapExtensions
{
    /// <summary>
    /// Bootstraps Serilog: a console-only bootstrap logger active until the host is built, then
    /// full configuration (from <paramref name="builder"/>'s configuration + DI services) via
    /// <c>Services.AddSerilog(...)</c> — the registration path that works for both
    /// <c>WebApplicationBuilder</c> and a plain worker-service <c>HostApplicationBuilder</c>,
    /// unlike <c>Host.UseSerilog(...)</c> which only <c>WebApplicationBuilder</c> exposes.
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

        builder.Services.AddSerilog((services, loggerConfiguration) =>
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
                    retainedFileCountLimit: fileLoggingOptions.RetainedFileCountLimit);
            }
        });

        return builder;
    }
}
