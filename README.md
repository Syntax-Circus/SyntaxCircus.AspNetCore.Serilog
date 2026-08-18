# SyntaxCircus.AspNetCore.Serilog

[![Build](https://github.com/Syntax-Circus/SyntaxCircus.AspNetCore.Serilog/actions/workflows/build.yml/badge.svg)](https://github.com/Syntax-Circus/SyntaxCircus.AspNetCore.Serilog/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/SyntaxCircus.AspNetCore.Serilog.svg)](https://www.nuget.org/packages/SyntaxCircus.AspNetCore.Serilog)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.txt)

A standard Serilog bootstrap for ASP.NET Core hosts and worker services, with optional File-sink
configuration. It targets the shared `IHostApplicationBuilder` interface, so one setup works for
both `WebApplicationBuilder` and worker-service `HostApplicationBuilder`.

> **No support guaranteed.** Published as-is and maintained on a best-effort basis. Issues and PRs are welcome, but there's no SLA — fork it or vendor what you need if that's not enough.

## Install

```powershell
dotnet add package SyntaxCircus.AspNetCore.Serilog
```

## Quick start

### ASP.NET Core

```csharp
using SyntaxCircus.AspNetCore.Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.AddStandardSerilog();

var app = builder.Build();
app.UseSerilogRequestLogging(); // ASP.NET Core hosts only
```

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

`AddStandardSerilog()` installs a console bootstrap logger for pre-DI startup and an independent
full logger for each host. The full logger reads standard Serilog configuration and DI services,
adds `LogContext` enrichment, and is used through normal `ILogger<T>` injection.

> **Static logger boundary:** `Serilog.Log.Logger` remains a process-global bootstrap logger. Use
> DI-provided `ILogger<T>` for a host's fully configured logger. This keeps multiple concurrent
> hosts, including parallel `WebApplicationFactory` tests, isolated from Serilog's global
> reload/freeze lifecycle.

## File logging

```csharp
using Serilog;

builder.AddStandardSerilog(fileLogging =>
{
    fileLogging.Enabled = true;
    fileLogging.Path = "logs/log-.txt";
    fileLogging.RollingInterval = RollingInterval.Day;
    fileLogging.RetainedFileCountLimit = 14;
    fileLogging.OutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";
});
```

File logging is disabled by default. Relative paths are content-root-relative; omitting `Path`
uses the platform's LocalAppData location under `{ApplicationName}/logs/log-.txt`.
`RetainedFileCountLimit = null` retains all rolled files, `OutputTemplate = null` preserves
Serilog's standard File-sink format, and `Shared = false` preserves exclusive file access.

## Documentation

- **[Usage guide](docs/usage.md)** — complete API reference, configuration behavior, file logging,
  lifecycle details, multiple-host guidance, and troubleshooting.
- **[Agent integration guide](docs/agent-guide.md)** — decision rules and safe recipes for AI
  agents modifying consuming applications.
- **[Repository agent guide](AGENTS.md)** — package contracts and contribution/validation guidance
  for agents working in this repository.

## Contributing

Issues and pull requests are welcome:
- Keep changes focused, with a clear description of the behavior change.
- Match the existing code style (see `.editorconfig`).
- Update the relevant documentation and tests when behavior changes.
- Call out any breaking changes to the public API in your PR description.

## License

MIT — see [LICENSE.txt](LICENSE.txt).
