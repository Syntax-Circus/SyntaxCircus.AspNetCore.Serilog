namespace SyntaxCircus.AspNetCore.Serilog;

public sealed class SerilogFileLoggingOptions
{
    public bool Enabled { get; set; }

    /// <summary>Overrides the default LocalAppData-based log file path when set. Relative paths are resolved against the host's content root.</summary>
    public string? Path { get; set; }

    public global::Serilog.RollingInterval RollingInterval { get; set; } = global::Serilog.RollingInterval.Day;

    /// <summary>Maximum number of rolled log files to retain on disk. <c>null</c> (default) retains all files, matching Serilog's own <c>File</c> sink default.</summary>
    public int? RetainedFileCountLimit { get; set; }
}
