# Deferred web-neutral modularization

The monitoring foundation uses this package's existing `IHostApplicationBuilder` integration
and additive bootstrap options. No package extraction or dependency restructuring is implemented.
The current package still references `Serilog.AspNetCore`; worker compatibility does not make its
dependency graph web-neutral.

A later coordinated design across `SyntaxCircus.AspNetCore.Serilog`,
`SyntaxCircus.AspNetCore.Common` and Observability should separate hosting-neutral registration,
file options and bootstrap mechanics from ASP.NET Core request logging and web-specific setup.
Common's ASP.NET dependencies and Observability's remote-export configuration require their own
owner-reviewed extraction plans. Preserve existing APIs/defaults, independent DI logger ownership
and concurrent host lifetimes; add migration and dependency-graph tests before moving contracts.
Remote export remains deferred for the monitoring CLI. This document authorizes no implementation,
publication or package migration.
