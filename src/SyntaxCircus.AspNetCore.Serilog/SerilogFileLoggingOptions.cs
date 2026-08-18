namespace SyntaxCircus.AspNetCore.Serilog;

public sealed class SerilogFileLoggingOptions
{
    public bool Enabled { get; set; }

    /// <summary>Overrides the default LocalAppData-based log file path when set. Relative paths are resolved against the host's content root.</summary>
    public string? Path { get; set; }

    public global::Serilog.RollingInterval RollingInterval { get; set; } = global::Serilog.RollingInterval.Day;

    /// <summary>Maximum number of rolled log files to retain on disk. <c>null</c> (default) retains all files, matching Serilog's own <c>File</c> sink default.</summary>
    public int? RetainedFileCountLimit { get; set; }

    /// <summary>Optional template passed to Serilog's <c>File</c> sink. <c>null</c> (default) uses Serilog's standard file output template.</summary>
    public string? OutputTemplate { get; set; }

    /// <summary>Allows multiple processes to write to the same log file. Defaults to <c>false</c>, matching Serilog's <c>File</c> sink default.</summary>
    public bool Shared { get; set; }
}
