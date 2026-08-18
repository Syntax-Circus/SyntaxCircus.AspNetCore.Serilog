namespace SyntaxCircus.AspNetCore.Serilog.Tests;

public class SerilogFileLoggingOptionsTests
{
    [Fact]
    public void Defaults_AreExpected()
    {
        var options = new SerilogFileLoggingOptions();

        options.Enabled.ShouldBeFalse();
        options.Path.ShouldBeNull();
        options.RollingInterval.ShouldBe(global::Serilog.RollingInterval.Day);
        options.RetainedFileCountLimit.ShouldBeNull();
    }

    [Fact]
    public void Properties_AreSettable()
    {
        var options = new SerilogFileLoggingOptions
        {
            Enabled = true,
            Path = "/var/log/app.txt",
            RollingInterval = global::Serilog.RollingInterval.Hour,
            RetainedFileCountLimit = 14,
        };

        options.Enabled.ShouldBeTrue();
        options.Path.ShouldBe("/var/log/app.txt");
        options.RollingInterval.ShouldBe(global::Serilog.RollingInterval.Hour);
        options.RetainedFileCountLimit.ShouldBe(14);
    }
}
