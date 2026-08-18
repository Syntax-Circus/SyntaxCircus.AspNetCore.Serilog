# Consumer integration guide for AI agents

Use this guide when modifying an application that consumes
`SyntaxCircus.AspNetCore.Serilog`. It is a factual integration reference, not a substitute for the
application's existing logging conventions.

## Fast decision table

| Need | Do this | Do not do this |
| --- | --- | --- |
| Standard web or worker logging | Call `builder.AddStandardSerilog()` before `builder.Build()`. | Use `Host.UseSerilog()`; it is not available on every `IHostApplicationBuilder`. |
| Application logging after startup | Inject `ILogger<T>` or obtain it from the host service provider. | Assume `Serilog.Log.Logger` is the host's fully configured logger. |
| Extra properties/enrichers | Use `configureEnrichment`. | Expect that callback to configure the bootstrap logger. |
| Package-managed file logging | Set options in `configureFileLogging`. | Add `SerilogFileLoggingOptions` to `appsettings.json` and expect automatic binding. |
| Same file from independent processes | Set `Shared = true` deliberately. | Expect shared writes to be ordered or coordinated. |
| Standard Serilog configuration | Add a `Serilog` section to the host configuration. | Configure the package File sink and a configuration File sink for the same destination unless duplicate events are desired. |

## Required call order

1. Create `WebApplicationBuilder` or `HostApplicationBuilder`.
2. Call `AddStandardSerilog()` exactly during builder configuration and before `Build()`.
3. Register application services.
4. Build the host.
5. Use injected `ILogger<T>` in endpoints, hosted services, and other DI-created components.

### Web application recipe

```csharp
using Serilog;
using SyntaxCircus.AspNetCore.Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddStandardSerilog(
    configureFileLogging: fileLogging =>
    {
        fileLogging.Enabled = true;
        fileLogging.Path = "logs/web-.log";
        fileLogging.RollingInterval = RollingInterval.Day;
        fileLogging.RetainedFileCountLimit = 14;
    },
    configureEnrichment: loggerConfiguration => loggerConfiguration
        .Enrich.WithProperty("Service", "WebApi"));

var app = builder.Build();
app.UseSerilogRequestLogging();

app.MapGet("/health", (ILogger<Program> logger) =>
{
    logger.LogInformation("Health check requested.");
    return Results.Ok();
});

app.Run();
```

### Worker recipe

```csharp
using Microsoft.Extensions.Hosting;
using SyntaxCircus.AspNetCore.Serilog;

var builder = Host.CreateApplicationBuilder(args);
builder.AddStandardSerilog();
builder.Services.AddHostedService<Worker>();

using var host = builder.Build();
await host.RunAsync();
```

## Facts agents must preserve

- The package extension targets `IHostApplicationBuilder`; this supports both web and worker
  builders.
- The full logger configuration applies `ReadFrom.Configuration`,
  `ReadFrom.Services`, and `Enrich.FromLogContext` before the caller's enrichment callback.
- The file callback runs immediately. The enrichment callback runs later, when the full logger is
  first constructed from DI.
- `Serilog.Log.Logger` is a shared process-global console bootstrap logger. The full per-host
  logger is accessed through Microsoft.Extensions.Logging.
- Several hosts may safely run concurrently in one process when they use the DI logger.
- File logging starts disabled. Its callback options are not configuration-bound.
- An unset output template uses Serilog's standard File-sink format. `Shared` is false by default.

## Safe modification rules

When editing a consumer:

- Prefer the existing application's logging configuration if it already has one. Add this package
  once at the composition root; do not call it from services, endpoints, or each test.
- Do not assign `Log.Logger` to “finish” this package's configuration. That changes global,
  process-wide behavior and defeats its multi-host isolation.
- Use the file callback only for the package's supported options: enabled state, path, rolling
  interval, retained count, output template, and sharing.
- Treat a relative file path as content-root-relative. Use an absolute path only when the
  deployment layout requires it.
- Set retention deliberately for long-running applications. `null` means unlimited rolled-file
  retention.
- Use `Shared = true` only when two independent processes deliberately append to the same file.
  Prefer separate paths when possible.
- Keep `UseSerilogRequestLogging()` limited to ASP.NET Core HTTP pipelines.

## Validation checklist

After a consumer change:

1. Build the application.
2. Start the relevant host or execute its focused integration tests.
3. Confirm `ILogger<T>` emits events through the expected configured sinks.
4. If file logging is enabled, verify the resolved location, rolling behavior, and retention.
5. If tests create multiple hosts in one process, keep test parallelization enabled and validate
   that host startup and first logger resolution succeed.
6. If static `Log.*` calls remain, explicitly decide whether bootstrap-only output is acceptable.

For the complete API, defaults, configuration details, and troubleshooting matrix, read
[usage.md](usage.md).
