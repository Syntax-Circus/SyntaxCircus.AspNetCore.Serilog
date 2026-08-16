namespace SyntaxCircus.AspNetCore.Serilog;

public static class SerilogFilePathResolver
{
    /// <summary>
    /// Resolves the log file path: an explicit <see cref="SerilogFileLoggingOptions.Path"/> wins
    /// (normalized relative to the host's content root if not already absolute); otherwise
    /// defaults to <c>%LocalAppData%/{ApplicationName}/logs/log-.txt</c>.
    /// </summary>
    public static string Resolve(IHostEnvironment environment, SerilogFileLoggingOptions options)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(options);

        if (!string.IsNullOrWhiteSpace(options.Path))
        {
            return Path.IsPathRooted(options.Path)
                ? options.Path
                : Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.Path));
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, environment.ApplicationName, "logs", "log-.txt");
    }
}
