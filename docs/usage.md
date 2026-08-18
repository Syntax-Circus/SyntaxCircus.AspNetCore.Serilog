# Usage guide

`SyntaxCircus.AspNetCore.Serilog` provides one consistent Serilog registration path for ASP.NET
Core applications and .NET worker services. Its `AddStandardSerilog()` extension targets
`IHostApplicationBuilder`, which both builder types implement.

## Contents

- [Install](#install)
- [Quick starts](#quick-starts)
- [What registration does](#what-registration-does)
- [Configuration and DI](#configuration-and-di)
- [API reference](#api-reference)
- [File logging](#file-logging)
- [Multiple hosts and static logging](#multiple-hosts-and-static-logging)
- [Troubleshooting](#troubleshooting)
- [Scope and non-goals](#scope-and-non-goals)

## Install

Add the package to an application targeting .NET 10:

```powershell
dotnet add package SyntaxCircus.AspNetCore.Serilog
```

The package brings the Serilog ASP.NET Core integration plus console and file sinks. It registers
Serilog as the Microsoft.Extensions.Logging provider, so application code should normally request
`ILogger<T>` through dependency injection.

## Quick starts

### ASP.NET Core

Call `AddStandardSerilog()` before building the application:

```csharp
using SyntaxCircus.AspNetCore.Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.AddStandardSerilog();

var app = builder.Build();
app.UseSerilogRequestLogging();

app.MapGet("/", (ILogger<Program> logger) =>
{
    logger.LogInformation("Handling the home page.");
    return Results.Ok();
});

app.Run();
```

`UseSerilogRequestLogging()` is an ASP.NET Core middleware extension. It is not applicable to a
worker service.

### Worker service

```csharp
using Microsoft.Extensions.Hosting;
using SyntaxCircus.AspNetCore.Serilog;

var builder = Host.CreateApplicationBuilder(args);
builder.AddStandardSerilog();
builder.Services.AddHostedService<Worker>();

using var host = builder.Build();
await host.RunAsync();
```

Consume logging normally in the hosted service:

```csharp
public sealed class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Worker started.");
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
```

## What registration does

`AddStandardSerilog()` has two stages:

1. It immediately installs a console-only Serilog bootstrap logger in the process-global
   `Serilog.Log.Logger`. This permits direct static Serilog calls during the pre-DI startup window.
2. It registers an independent, full Serilog logger with the service collection. That logger is
   constructed when the host first resolves its logging infrastructure and is the logger obtained
   by normal `ILogger<T>` injection.

The full logger is configured in this order:

1. `ReadFrom.Configuration(builder.Configuration)`
2. `ReadFrom.Services(services)`
3. `Enrich.FromLogContext()`
4. The optional `configureEnrichment` callback
5. The optional file sink

The file-options callback executes immediately, while the enrichment callback executes later when
the full DI logger is constructed. Do not expect an enrichment callback to have run merely because
`AddStandardSerilog()` returned.

## Configuration and DI

The package reads standard Serilog configuration from the host's configuration through
`ReadFrom.Configuration`. For example, application configuration can set minimum levels and
additional Serilog sinks:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning"
      }
    }
  }
}
```

It also uses `ReadFrom.Services`, allowing Serilog components registered in the application's
service provider to participate in configuration.

`SerilogFileLoggingOptions` is intentionally separate from this configuration path. The package
does **not** bind it from `appsettings.json`; configure it in the callback shown below. If a File
sink is also configured through the `Serilog` configuration section, both sinks are active and can
write duplicate events.

### Additional enrichers

Use `configureEnrichment` for enrichers not supplied by the package:

```csharp
builder.AddStandardSerilog(
    configureEnrichment: loggerConfiguration => loggerConfiguration
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .Enrich.WithProperty("Service", "Orders"));
```

The callback receives the same `LoggerConfiguration` used by the full DI logger. It is not a hook
for the bootstrap logger.

## API reference

### `AddStandardSerilog`

```csharp
public static IHostApplicationBuilder AddStandardSerilog(
    this IHostApplicationBuilder builder,
    Action<SerilogFileLoggingOptions>? configureFileLogging = null,
    Action<LoggerConfiguration>? configureEnrichment = null)
```

| Parameter | Behavior |
| --- | --- |
| `builder` | Required. `null` throws `ArgumentNullException`. The same builder instance is returned. |
| `configureFileLogging` | Optional. Runs synchronously during registration. Configure the optional File sink here. |
| `configureEnrichment` | Optional. Runs when the full DI logger is created, after `Enrich.FromLogContext()` and before the package-managed File sink. |

### `SerilogFileLoggingOptions`

File logging is disabled unless `Enabled` is set to `true`.

| Property | Type | Default | Behavior |
| --- | --- | --- | --- |
| `Enabled` | `bool` | `false` | Enables the package-managed File sink. |
| `Path` | `string?` | `null` | Explicit log path. Relative paths are resolved against the host content root. |
| `RollingInterval` | `RollingInterval` | `Day` | Rolling period passed to Serilog's File sink. |
| `RetainedFileCountLimit` | `int?` | `null` | Maximum retained rolled files. `null` retains all files. |
| `OutputTemplate` | `string?` | `null` | Text template passed to the File sink. `null` uses Serilog's standard file template. |
| `Shared` | `bool` | `false` | Enables shared file access for concurrent writers. |

## File logging

### Basic rolling file

```csharp
using Serilog;

builder.AddStandardSerilog(fileLogging =>
{
    fileLogging.Enabled = true;
    fileLogging.Path = "logs/log-.txt";
    fileLogging.RollingInterval = RollingInterval.Day;
    fileLogging.RetainedFileCountLimit = 14;
});
```

With a relative `Path`, the resolved location is:

```text
{ContentRootPath}\logs\log-.txt
```

When `Path` is null, empty, or whitespace, the package uses the platform's LocalAppData location:

```text
{LocalAppData}/{ApplicationName}/logs/log-.txt
```

The exact separator is platform dependent. The File sink handles the rolling suffix for a path
such as `log-.txt`.

### Output format

Set `OutputTemplate` when downstream tooling expects a specific text shape:

```csharp
builder.AddStandardSerilog(fileLogging =>
{
    fileLogging.Enabled = true;
    fileLogging.Path = "logs/application-.log";
    fileLogging.OutputTemplate =
        "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}";
});
```

Leaving `OutputTemplate` unset preserves Serilog's standard File-sink template:

```text
{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}
```

### Shared files

Set `Shared = true` only if independent processes must append to the same physical file:

```csharp
builder.AddStandardSerilog(fileLogging =>
{
    fileLogging.Enabled = true;
    fileLogging.Path = @"C:\Logs\shared.log";
    fileLogging.Shared = true;
});
```

Shared access permits file handles from other writers. It does not serialize events, guarantee
event order, coordinate rolling, or replace a centralized log collector. Prefer separate files or
a centralized sink when ordering and attribution matter.

## Multiple hosts and static logging

Multiple `IHostApplicationBuilder` instances may run concurrently in the same process, including
parallel `WebApplicationFactory` tests. Each host receives an independent full logger through DI;
the package deliberately preserves the static bootstrap logger to avoid Serilog's global
reload/freeze lifecycle.

Consequently, after the host has been built:

- Use `ILogger<T>` or `ILoggerFactory` from the host service provider for application logging.
- Treat `Serilog.Log.Logger` and `Log.Information(...)` as process-wide bootstrap logging, not as
  a host-specific full logger.
- If an application deliberately requires static Serilog calls after startup, it owns assigning
  and managing one intended process-wide static logger.

Do not rely on the package to provide a distinct static logger for every host; a static property
cannot express that isolation.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| No expected full logger behavior | Confirm `AddStandardSerilog()` runs before `Build()` and that logging is resolved or used through DI. |
| Enricher callback has not run | It runs when the full DI logger is constructed, not during registration. Resolve or inject an `ILogger<T>`. |
| No package-managed file is produced | Set `Enabled = true`, use a writable path, emit an event through `ILogger<T>`, and inspect the resolved content-root or LocalAppData location. |
| Log appears twice | Check whether both the callback-managed File sink and a File sink in standard `Serilog` configuration target the same destination. |
| Another process cannot open the log file | Set `Shared = true` only when the processes intentionally write the same file. |
| Parallel hosts previously caused “logger is already frozen” | Update to the version containing the multi-host fix and keep normal host logging on `ILogger<T>` rather than the static logger. |
| Static `Log.Information()` misses configured enrichers/sinks | This is expected: the static logger remains the bootstrap logger. Use DI-provided logging or explicitly manage a process-wide static logger. |

## Scope and non-goals

This package standardizes a small bootstrap and optional File-sink path/options callback. It does
not:

- bind `SerilogFileLoggingOptions` from configuration;
- expose every Serilog File-sink parameter;
- configure request logging for non-web hosts;
- provide per-host instances of `Serilog.Log.Logger`;
- coordinate log ordering across shared writers; or
- replace application-specific sink, level, and enrichment choices.

Use standard Serilog configuration and extensions for those requirements.
