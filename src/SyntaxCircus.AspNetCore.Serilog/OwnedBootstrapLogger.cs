using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using System.Diagnostics.CodeAnalysis;

namespace SyntaxCircus.AspNetCore.Serilog;

internal sealed class OwnedBootstrapLogger(ILogger inner) : ILogger, IDisposable
{
    private int disposed;
    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 0)
            (inner as IDisposable)?.Dispose();
    }
    public void Write(LogEvent logEvent) => inner.Write(logEvent);
    public bool IsEnabled(LogEventLevel level) => inner.IsEnabled(level);
    public ILogger ForContext(ILogEventEnricher enricher) => inner.ForContext(enricher);
    public ILogger ForContext(IEnumerable<ILogEventEnricher> enrichers) => inner.ForContext(enrichers);
    public ILogger ForContext(string propertyName, object? value, bool destructureObjects = false) => inner.ForContext(propertyName, value, destructureObjects);
    public ILogger ForContext<TSource>() => inner.ForContext<TSource>();
    public ILogger ForContext(Type source) => inner.ForContext(source);
    public bool BindMessageTemplate(string messageTemplate, object?[]? propertyValues,
        [NotNullWhen(true)] out MessageTemplate? parsedTemplate,
        [NotNullWhen(true)] out IEnumerable<LogEventProperty>? boundProperties)
        => inner.BindMessageTemplate(messageTemplate, propertyValues, out parsedTemplate, out boundProperties);
    public bool BindProperty(string? propertyName, object? value, bool destructureObjects,
        [NotNullWhen(true)] out LogEventProperty? property)
        => inner.BindProperty(propertyName, value, destructureObjects, out property);
}
