# SyntaxCircus.AspNetCore.Serilog

[![Build](https://github.com/Syntax-Circus/SyntaxCircus.AspNetCore.Serilog/actions/workflows/build.yml/badge.svg)](https://github.com/Syntax-Circus/SyntaxCircus.AspNetCore.Serilog/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/SyntaxCircus.AspNetCore.Serilog.svg)](https://www.nuget.org/packages/SyntaxCircus.AspNetCore.Serilog)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.txt)

A standard Serilog bootstrap for both ASP.NET Core hosts and worker services, with an optional file-sink path resolver. Standalone on purpose — pulled out of [SyntaxCircus.AspNetCore.Common](https://github.com/Syntax-Circus/SyntaxCircus.AspNetCore.Common) so that package stays free of the Serilog dependency for consumers who don't want it.

> **No support guaranteed.** Published as-is and maintained on a best-effort basis. Issues and PRs are welcome, but there's no SLA — fork it or vendor what you need if that's not enough.

## Usage

```csharp
var builder = WebApplication.CreateBuilder(args); // or Host.CreateApplicationBuilder(args) for a worker service
builder.AddStandardSerilog();

var app = builder.Build();
app.UseSerilogRequestLogging(); // ASP.NET Core hosts only
```

Sets up a console-only bootstrap logger active until the host finishes building, then reconfigures from `appsettings`/DI services (`ReadFrom.Configuration` + `ReadFrom.Services` + `Enrich.FromLogContext`). Uses `Services.AddSerilog(...)` rather than `Host.UseSerilog(...)` so it works on both `WebApplicationBuilder` and a plain worker-service `HostApplicationBuilder` — `AddStandardSerilog` targets the shared `IHostApplicationBuilder` interface both implement.

## Optional file logging

```csharp
builder.AddStandardSerilog(fileLogging =>
{
    fileLogging.Enabled = true;
    fileLogging.Path = "logs/log-.txt"; // optional — relative to content root; omit for the LocalAppData default
    fileLogging.RollingInterval = RollingInterval.Day;
});
```

When `Path` is omitted, logs go to `%LocalAppData%/{ApplicationName}/logs/log-.txt`.

## Contributing

Issues and pull requests are welcome:
- Keep changes focused, with a clear description of the behavior change.
- Match the existing code style (see `.editorconfig`).
- Call out any breaking changes to the public API in your PR description.

## License

MIT — see [LICENSE.txt](LICENSE.txt).
