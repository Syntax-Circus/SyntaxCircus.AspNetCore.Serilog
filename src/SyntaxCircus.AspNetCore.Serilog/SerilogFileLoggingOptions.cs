namespace SyntaxCircus.AspNetCore.Serilog;

public sealed class SerilogFileLoggingOptions
{
    public bool Enabled { get; set; }

    /// <summary>Overrides the default LocalAppData-based log file path when set. Relative paths are resolved against the host's content root.</summary>
    public string? Path { get; set; }

    public global::Serilog.RollingInterval RollingInterval { get; set; } = global::Serilog.RollingInterval.Day;
}
