# Repository guide for agents

## Purpose and public surface

This repository publishes `SyntaxCircus.AspNetCore.Serilog`, a .NET 10 package that standardizes
Serilog setup for both `WebApplicationBuilder` and worker-service `HostApplicationBuilder`.

The public surface is intentionally small:

| Symbol | Location | Contract |
| --- | --- | --- |
| `AddStandardSerilog()` | `src/SyntaxCircus.AspNetCore.Serilog/SerilogBootstrapExtensions.cs` | Adds Serilog to an `IHostApplicationBuilder`, creates a console bootstrap logger, and registers a full DI-aware logger. |
| `SerilogFileLoggingOptions` | `src/SyntaxCircus.AspNetCore.Serilog/SerilogFileLoggingOptions.cs` | Configures the optional file sink through the `AddStandardSerilog()` callback. |
| `SerilogFilePathResolver.Resolve()` | `src/SyntaxCircus.AspNetCore.Serilog/SerilogFilePathResolver.cs` | Resolves explicit paths against content root or uses the LocalAppData fallback. |

## Behavior that must be preserved

- `AddStandardSerilog()` accepts `IHostApplicationBuilder`; do not narrow it to web-only APIs.
- Callbacks run at different times: `configureFileLogging` runs immediately; `configureEnrichment`
  runs when the DI logger is first resolved.
- The full logger uses `ReadFrom.Configuration`, `ReadFrom.Services`, and
  `Enrich.FromLogContext`, in that order before the caller's enrichment callback and optional file
  sink.
- `Log.Logger` is a process-global, console-only bootstrap logger. The DI-provided logger is an
  independent full logger. `preserveStaticLogger: true` is required to prevent multiple hosts in
  one process from sharing Serilog's reload/freeze lifecycle.
- `SerilogFileLoggingOptions` is callback-only; it is not bound automatically from
  `appsettings.json`.
- File options default to disabled, daily rolling, unlimited retention, Serilog's standard output
  template, and `Shared = false`.
- `Shared = true` permits concurrent file access but does not establish ordering between writers.

Read `docs/usage.md` before changing package behavior. Read `docs/agent-guide.md` when adapting a
consumer application.

## Repository map

```text
src/SyntaxCircus.AspNetCore.Serilog/     Package code and package metadata
tests/SyntaxCircus.AspNetCore.Serilog.Tests/  xUnit v3 coverage
docs/usage.md                            Human-facing complete reference
docs/agent-guide.md                      Consumer integration guide for agents
docs/enhancements/                       Historical behavior proposals
README.md                                NuGet-packaged landing page
.github/workflows/build.yml              Authoritative CI workflow
```

## Editing rules

- Keep public API additions additive unless a breaking change is explicitly requested.
- Preserve file-sink defaults when adding options. Do not pass a null output template directly to
  Serilog's `File` sink; it rejects null.
- Do not claim support for options not exposed by `SerilogFileLoggingOptions`.
- Do not replace the static bootstrap behavior with a per-host static logger; `Log.Logger` cannot
  be per-host.
- Keep XML documentation, `README.md`, `docs/usage.md`, `docs/agent-guide.md`, and tests aligned
  with behavioral changes.
- Treat existing uncommitted changes as user work unless you made them. Do not revert unrelated
  files.

## Validation

Use the existing CI-equivalent workflow:

```powershell
dotnet restore SyntaxCircus.AspNetCore.Serilog.slnx
dotnet build SyntaxCircus.AspNetCore.Serilog.slnx --no-restore --configuration Release
dotnet test --solution SyntaxCircus.AspNetCore.Serilog.slnx --no-build --configuration Release
```

The xUnit v3 runner uses runner-native selectors after `--`, not the legacy `--filter` switch:

```powershell
dotnet test --project tests\SyntaxCircus.AspNetCore.Serilog.Tests\SyntaxCircus.AspNetCore.Serilog.Tests.csproj --configuration Release -- --filter-method "*FileLogging*"
```

Use a focused test first when changing a single behavior, then run the full Release workflow.
