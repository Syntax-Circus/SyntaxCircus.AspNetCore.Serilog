using Serilog;

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
    public async Task AddStandardSerilog_MultipleConcurrentHostsInOneProcess_DoesNotThrow()
    {
        var originalStaticLogger = Log.Logger;
        var hostsReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var readyHostCount = 0;

        try
        {
            var tasks = Enumerable.Range(0, 3)
                .Select(_ => Task.Run(async () =>
                {
                    var builder = Host.CreateApplicationBuilder();
                    builder.AddStandardSerilog();

                    using var host = builder.Build();
                    if (Interlocked.Increment(ref readyHostCount) == 3)
                    {
                        hostsReady.TrySetResult();
                    }

                    await start.Task;

                    var logger = host.Services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SerilogBootstrapExtensionsTests>>();
                    logger.LogInformation("Hello from a concurrent host.");
                }))
                .ToArray();

            await hostsReady.Task.WaitAsync(TestContext.Current.CancellationToken);
            start.TrySetResult();
            await Task.WhenAll(tasks);
        }
        finally
        {
            Log.Logger = originalStaticLogger;
        }
    }

    [Fact]
    public void AddStandardSerilog_ResolvingHostLogger_PreservesStaticBootstrapLogger()
    {
        var originalStaticLogger = Log.Logger;

        try
        {
            var builder = Host.CreateApplicationBuilder();
            builder.AddStandardSerilog();
            var bootstrapLogger = Log.Logger;

            using var host = builder.Build();
            _ = host.Services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SerilogBootstrapExtensionsTests>>();

            Log.Logger.ShouldBeSameAs(bootstrapLogger);
        }
        finally
        {
            Log.Logger = originalStaticLogger;
        }
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

    [Fact]
    public void AddStandardSerilog_FileLoggingWithRetainedFileCountLimit_HostStillBuildsSuccessfully()
    {
        var builder = Host.CreateApplicationBuilder();
        var logPath = Path.Combine(Path.GetTempPath(), "sc-serilog-tests", Guid.NewGuid().ToString("N"), "log-.txt");

        try
        {
            builder.AddStandardSerilog(options =>
            {
                options.Enabled = true;
                options.Path = logPath;
                options.RetainedFileCountLimit = 7;
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

    [Fact]
    public void AddStandardSerilog_FileLoggingWithOutputTemplate_WritesConfiguredFormat()
    {
        var builder = Host.CreateApplicationBuilder();
        var logPath = Path.Combine(Path.GetTempPath(), "sc-serilog-tests", Guid.NewGuid().ToString("N"), "log.txt");

        try
        {
            builder.AddStandardSerilog(options =>
            {
                options.Enabled = true;
                options.Path = logPath;
                options.RollingInterval = RollingInterval.Infinite;
                options.OutputTemplate = "MESSAGE:{Message:lj}{NewLine}";
            });

            using (var host = builder.Build())
            {
                var logger = host.Services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SerilogBootstrapExtensionsTests>>();
                logger.LogInformation("custom file template");
            }

            File.ReadAllText(logPath).ShouldBe("MESSAGE:custom file template" + Environment.NewLine);
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

    [Fact]
    public void AddStandardSerilog_FileLoggingWithSharedAccess_AllowsConcurrentWriter()
    {
        var builder = Host.CreateApplicationBuilder();
        var logPath = Path.Combine(Path.GetTempPath(), "sc-serilog-tests", Guid.NewGuid().ToString("N"), "log.txt");

        try
        {
            builder.AddStandardSerilog(options =>
            {
                options.Enabled = true;
                options.Path = logPath;
                options.RollingInterval = RollingInterval.Infinite;
                options.Shared = true;
            });

            using (var host = builder.Build())
            {
                var logger = host.Services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SerilogBootstrapExtensionsTests>>();
                logger.LogInformation("before concurrent writer");

                using var concurrentWriter = new FileStream(logPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                logger.LogInformation("while concurrent writer is open");
            }

            File.ReadAllText(logPath).ShouldContain("while concurrent writer is open");
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

    [Fact]
    public void AddStandardSerilog_ConfigureEnrichmentCallback_IsInvoked()
    {
        var builder = Host.CreateApplicationBuilder();
        var invoked = false;

        builder.AddStandardSerilog(configureEnrichment: _ => invoked = true);

        // configureEnrichment runs inside the deferred AddSerilog callback, unlike
        // configureFileLogging (invoked eagerly, synchronously) — it only fires once the host
        // builds and the logger is actually constructed.
        using var host = builder.Build();
        _ = host.Services.GetService<Microsoft.Extensions.Logging.ILogger<SerilogBootstrapExtensionsTests>>();

        invoked.ShouldBeTrue();
    }

    [Fact]
    public void AddStandardSerilog_ConfigureEnrichmentCallback_HostStillBuildsSuccessfully()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.AddStandardSerilog(configureEnrichment: cfg => cfg.Enrich.WithProperty("Application", "test"));

        using var host = builder.Build();

        host.Services.ShouldNotBeNull();
    }
}
