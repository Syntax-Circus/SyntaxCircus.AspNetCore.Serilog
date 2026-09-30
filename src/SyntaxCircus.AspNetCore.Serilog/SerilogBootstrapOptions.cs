using Serilog;

namespace SyntaxCircus.AspNetCore.Serilog;

/// <summary>Configures the process-global startup logger independently of each host's DI logger.</summary>
public sealed class SerilogBootstrapOptions
{
    /// <summary>Enables the built-in bootstrap console sink. Defaults to true.</summary>
    public bool ConsoleEnabled { get; set; } = true;

    /// <summary>Routes every bootstrap level to stderr when enabled. Defaults to stdout.</summary>
    public bool ConsoleToStandardError { get; set; }

    /// <summary>
    /// Runs synchronously after the optional console sink and before creating or assigning the
    /// bootstrap logger. Configure trusted sinks, levels and enrichment here; this does not
    /// configure the host's full logger or automatically sanitize event data.
    /// </summary>
    public Action<LoggerConfiguration>? ConfigureLogger { get; set; }
}
