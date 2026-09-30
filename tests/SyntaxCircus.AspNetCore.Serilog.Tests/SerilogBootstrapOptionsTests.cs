using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace SyntaxCircus.AspNetCore.Serilog.Tests;

[Collection("Console bootstrap")]
public sealed class SerilogBootstrapOptionsTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void BootstrapConsole_RoutesOnlyToSelectedStream(bool enabled, bool standardError)
    {
        var originalLogger = Log.Logger;
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var output = new StringWriter();
        using var error = new StringWriter();
        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            Host.CreateApplicationBuilder().AddStandardSerilog(null, null, new SerilogBootstrapOptions
            {
                ConsoleEnabled = enabled,
                ConsoleToStandardError = standardError
            });
            Log.Information("bootstrap-route-marker");
            output.ToString().Contains("bootstrap-route-marker").ShouldBe(enabled && !standardError);
            error.ToString().Contains("bootstrap-route-marker").ShouldBe(enabled && standardError);
        }
        finally
        {
            Log.CloseAndFlush();
            Log.Logger = originalLogger;
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void BootstrapCallback_EmitsEnrichedStartupEventWithoutSharingHostLogger()
    {
        var originalLogger = Log.Logger;
        var sink = new RecordingSink();
        try
        {
            var builder = Host.CreateApplicationBuilder();
            builder.AddStandardSerilog(null, null, new SerilogBootstrapOptions
            {
                ConsoleEnabled = false,
                ConfigureLogger = configuration => configuration
                    .Enrich.WithProperty("Startup", "safe")
                    .WriteTo.Sink(sink)
            });
            var bootstrap = Log.Logger;
            Log.Information("startup");
            using (var host = builder.Build())
            {
                host.Services.GetRequiredService<ILogger<SerilogBootstrapOptionsTests>>().LogInformation("host");
                Log.Logger.ShouldBeSameAs(bootstrap);
            }
            Log.Information("after host disposal");
            sink.Events.Count.ShouldBe(2);
            sink.Events[0].Properties["Startup"].ShouldBeOfType<ScalarValue>().Value.ShouldBe("safe");
        }
        finally
        {
            Log.CloseAndFlush();
            Log.Logger = originalLogger;
        }
    }

    [Fact]
    public void BootstrapReplacement_DisposesPackageOwnedSinksButPreservesExternalLogger()
    {
        var originalLogger = Log.Logger;
        var externalSink = new DisposableSink();
        using var externalLogger = new LoggerConfiguration().WriteTo.Sink(externalSink).CreateLogger();
        var firstSink = new DisposableSink();
        var secondSink = new DisposableSink();
        try
        {
            Log.Logger = externalLogger;
            Host.CreateApplicationBuilder().AddStandardSerilog(null, null, new SerilogBootstrapOptions
            { ConsoleEnabled = false, ConfigureLogger = configuration => configuration.WriteTo.Sink(firstSink) });
            externalSink.Disposals.ShouldBe(0);
            Host.CreateApplicationBuilder().AddStandardSerilog(null, null, new SerilogBootstrapOptions
            { ConsoleEnabled = false, ConfigureLogger = configuration => configuration.WriteTo.Sink(secondSink) });
            firstSink.Disposals.ShouldBe(1);
            secondSink.Disposals.ShouldBe(0);
            externalSink.Disposals.ShouldBe(0);
            Log.Information("second startup");
            secondSink.Events.ShouldBe(1);
            Log.CloseAndFlush();
            Host.CreateApplicationBuilder().AddStandardSerilog(null, null,
                new SerilogBootstrapOptions { ConsoleEnabled = false });
            firstSink.Disposals.ShouldBe(1);
            secondSink.Disposals.ShouldBe(1);
        }
        finally
        {
            Log.CloseAndFlush();
            Log.Logger = originalLogger;
        }
    }

    [Fact]
    public void BootstrapFailedReplacement_DoesNotDisposePreviouslyOwnedSink()
    {
        var originalLogger = Log.Logger;
        var sink = new DisposableSink();
        try
        {
            Host.CreateApplicationBuilder().AddStandardSerilog(null, null, new SerilogBootstrapOptions
            { ConsoleEnabled = false, ConfigureLogger = configuration => configuration.WriteTo.Sink(sink) });
            Should.Throw<InvalidOperationException>(() => Host.CreateApplicationBuilder().AddStandardSerilog(null, null,
                new SerilogBootstrapOptions { ConfigureLogger = _ => throw new InvalidOperationException("failure") }));
            sink.Disposals.ShouldBe(0);
            Log.Information("previous logger still usable");
            sink.Events.ShouldBe(1);
        }
        finally
        {
            Log.CloseAndFlush();
            Log.Logger = originalLogger;
        }
    }

    [Fact]
    public async Task BootstrapOptions_ConcurrentHostsKeepIndependentSinksAndDisposal()
    {
        var originalLogger = Log.Logger;
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var count = 0;
        try
        {
            var tasks = Enumerable.Range(0, 3).Select(index => Task.Run(async () =>
            {
                var sink = new RecordingSink();
                var builder = Host.CreateApplicationBuilder();
                builder.AddStandardSerilog(null, configuration => configuration.WriteTo.Sink(sink),
                    new SerilogBootstrapOptions { ConsoleEnabled = false });
                using var host = builder.Build();
                if (Interlocked.Increment(ref count) == 3)
                    ready.TrySetResult();
                await ready.Task.WaitAsync(TestContext.Current.CancellationToken);
                var logger = host.Services.GetRequiredService<ILogger<SerilogBootstrapOptionsTests>>();
                if (logger.IsEnabled(LogLevel.Information))
                    logger.LogInformation("Host {Index}", index);
                sink.Events.Count.ShouldBe(1);
                sink.Events[0].Properties["Index"].ShouldBeOfType<ScalarValue>().Value.ShouldBe(index);
            })).ToArray();
            await Task.WhenAll(tasks);
        }
        finally
        {
            Log.CloseAndFlush();
            Log.Logger = originalLogger;
        }
    }

    [Fact]
    public void BootstrapCallbackFailure_PreservesExistingStaticLogger()
    {
        var originalLogger = Log.Logger;
        var builder = Host.CreateApplicationBuilder();
        Should.Throw<InvalidOperationException>(() => builder.AddStandardSerilog(null, null,
            new SerilogBootstrapOptions { ConfigureLogger = _ => throw new InvalidOperationException("failure") }));
        Log.Logger.ShouldBeSameAs(originalLogger);
    }

    [Fact]
    public void LegacyNullCallback_RemainsUnambiguousAndUsesConsole()
    {
        var originalLogger = Log.Logger;
        var originalOut = Console.Out;
        using var output = new StringWriter();
        try
        {
            Console.SetOut(output);
            Host.CreateApplicationBuilder().AddStandardSerilog(null);
            Log.Information("legacy-bootstrap-marker");
            output.ToString().ShouldContain("legacy-bootstrap-marker");
        }
        finally
        {
            Log.CloseAndFlush();
            Log.Logger = originalLogger;
            Console.SetOut(originalOut);
        }
    }

    private sealed class RecordingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];
        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }

    private sealed class DisposableSink : ILogEventSink, IDisposable
    {
        public int Disposals { get; private set; }
        public int Events { get; private set; }
        public void Emit(LogEvent logEvent) => Events++;
        public void Dispose() => Disposals++;
    }
}

[CollectionDefinition("Console bootstrap", DisableParallelization = true)]
public sealed class ConsoleBootstrapTestGroup;
