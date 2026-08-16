namespace SyntaxCircus.AspNetCore.Serilog.Tests;

public class SerilogFilePathResolverTests
{
    private static IHostEnvironment FakeEnvironment(string contentRootPath = "/app", string applicationName = "MyApp")
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.ContentRootPath.Returns(contentRootPath);
        environment.ApplicationName.Returns(applicationName);
        return environment;
    }

    [Fact]
    public void Resolve_NullEnvironment_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SerilogFilePathResolver.Resolve(null!, new SerilogFileLoggingOptions()));
    }

    [Fact]
    public void Resolve_NullOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => SerilogFilePathResolver.Resolve(FakeEnvironment(), null!));
    }

    [Fact]
    public void Resolve_NoPathConfigured_DefaultsToLocalAppDataUnderApplicationName()
    {
        var environment = FakeEnvironment(applicationName: "MyApp");

        var resolved = SerilogFilePathResolver.Resolve(environment, new SerilogFileLoggingOptions());

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        resolved.ShouldBe(Path.Combine(localAppData, "MyApp", "logs", "log-.txt"));
    }

    [Fact]
    public void Resolve_RootedPath_ReturnedAsIs()
    {
        var environment = FakeEnvironment();
        var rooted = OperatingSystem.IsWindows() ? @"C:\logs\app.txt" : "/var/log/app.txt";

        var resolved = SerilogFilePathResolver.Resolve(environment, new SerilogFileLoggingOptions { Path = rooted });

        resolved.ShouldBe(rooted);
    }

    [Fact]
    public void Resolve_RelativePath_CombinedAgainstContentRoot()
    {
        var environment = FakeEnvironment(contentRootPath: Path.GetTempPath());

        var resolved = SerilogFilePathResolver.Resolve(environment, new SerilogFileLoggingOptions { Path = "logs/app.txt" });

        resolved.ShouldBe(Path.GetFullPath(Path.Combine(Path.GetTempPath(), "logs/app.txt")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_BlankPath_FallsBackToDefault(string? path)
    {
        var environment = FakeEnvironment(applicationName: "MyApp");

        var resolved = SerilogFilePathResolver.Resolve(environment, new SerilogFileLoggingOptions { Path = path });

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        resolved.ShouldBe(Path.Combine(localAppData, "MyApp", "logs", "log-.txt"));
    }
}
