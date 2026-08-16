namespace SyntaxCircus.AspNetCore.Serilog.Tests;

public class SerilogBootstrapExtensionsTests
{
    [Fact]
    public void AddStandardSerilog_NullBuilder_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SerilogBootstrapExtensions.AddStandardSerilog(null!));
    }

    [Fact]
    public void AddStandardSerilog_ReturnsSameBuilderInstance()
    {
        var builder = Host.CreateApplicationBuilder();

        var result = builder.AddStandardSerilog();

        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void AddStandardSerilog_WithoutFileLoggingCallback_HostBuildsAndResolvesLogger()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.AddStandardSerilog();

        using var host = builder.Build();
        var logger = host.Services.GetService<Microsoft.Extensions.Logging.ILogger<SerilogBootstrapExtensionsTests>>();

        logger.ShouldNotBeNull();
    }

    [Fact]
    public void AddStandardSerilog_ConfigureFileLoggingCallback_IsInvoked()
    {
        var builder = Host.CreateApplicationBuilder();
        var invoked = false;

        builder.AddStandardSerilog(options =>
        {
            invoked = true;
            options.Enabled = false;
        });

        invoked.ShouldBeTrue();
    }

    [Fact]
    public void AddStandardSerilog_FileLoggingEnabled_HostStillBuildsSuccessfully()
    {
        var builder = Host.CreateApplicationBuilder();
        var logPath = Path.Combine(Path.GetTempPath(), "sc-serilog-tests", Guid.NewGuid().ToString("N"), "log-.txt");

        try
        {
            builder.AddStandardSerilog(options =>
            {
                options.Enabled = true;
                options.Path = logPath;
            });

            using var host = builder.Build();

            host.Services.ShouldNotBeNull();
        }
        finally
        {
            var directory = Path.GetDirectoryName(logPath);
            if (directory is not null && Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
