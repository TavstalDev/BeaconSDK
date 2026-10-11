using FluentAssertions;
using Tavstal.BeaconSDK.Models.Instance;
using Tavstal.BeaconSDK.Models.Installer;
using Tavstal.BeaconSDK.Models.MojangApi.Meta;
using Tavstal.BeaconSDK.Tests.Fixtures;

namespace Tavstal.BeaconSDK.Tests.Models.Instance;

public class ArgumentBuilderTests
{
    private const string DefaultNativesDir = "/natives";
    private const string DefaultGameDir = "/game";

    private static ArgumentBuilder CreateBuilder(
        string version = "1.20.4",
        VersionMeta? meta = null,
        GameDetails? game = null,
        ClientDetails? client = null,
        Resolution? resolution = null,
        string nativesDir = DefaultNativesDir,
        string gameDir = DefaultGameDir)
    {
        return new ArgumentBuilder(version, version, nativesDir, gameDir, "5", meta ?? ModelFactory.CreateVersionMeta(version),
            ModelFactory.CreateLauncherDetails(), game ?? ModelFactory.CreateGameDetails(minecraftVersion: version),
            client ?? ModelFactory.CreateClientDetails(), ModelFactory.CreatePathDetails(), resolution);
    }

    [Fact(DisplayName = "Custom JVM args are added when non-empty")]
    public void CustomJvmArgs_AreAdded_WhenNonEmpty()
    {
        var builder = CreateBuilder(game: ModelFactory.CreateGameDetails(jvmArgs: "-XX:+UseG1GC"));

        var (jvmArgs, _) = builder.Build();

        jvmArgs.Should().Contain("-XX:+UseG1GC");
    }

    [Fact(DisplayName = "Custom JVM args are not added when empty")]
    public void CustomJvmArgs_AreNotAdded_WhenEmpty()
    {
        var builder = CreateBuilder(game: ModelFactory.CreateGameDetails(jvmArgs: ""));

        var (jvmArgs, _) = builder.Build();

        jvmArgs.Should().NotContain("-XX:+UseG1GC");
    }

    [Fact(DisplayName = "Memory args default to -Xms512M and -Xmx2048M")]
    public void MemoryArgs_DefaultTo512And2048()
    {
        var builder = CreateBuilder(game: ModelFactory.CreateGameDetails(minMemory: 0, maxMemory: 0));

        var (jvmArgs, _) = builder.Build();

        jvmArgs.Should().Contain("-Xms512M").And.Contain("-Xmx2048M");
    }

    [Fact(DisplayName = "Max memory below 2048M is ignored")]
    public void MemoryArgs_MaxBelow2048_IsIgnored()
    {
        var builder = CreateBuilder(game: ModelFactory.CreateGameDetails(maxMemory: 1024));

        var (jvmArgs, _) = builder.Build();

        jvmArgs.Should().Contain("-Xmx2048M").And.NotContain("-Xmx1024M");
    }

    [Fact(DisplayName = "Min memory above 512M is applied")]
    public void MemoryArgs_MinAbove512_IsApplied()
    {
        var builder = CreateBuilder(game: ModelFactory.CreateGameDetails(minMemory: 1024));

        var (jvmArgs, _) = builder.Build();

        jvmArgs.Should().Contain("-Xms1024M");
    }

    [Fact(DisplayName = "Offline 1.16+ locks Microsoft API hosts to nope.invalid")]
    public void Offline116_LocksMicrosoftApiHostsToNopeInvalid()
    {
        const string version = "1.16.5";
        var builder = CreateBuilder(
            version,
            ModelFactory.CreateVersionMeta(version),
            ModelFactory.CreateGameDetails(minecraftVersion: version),
            ModelFactory.CreateClientDetails(isOffline: true));

        var (jvmArgs, _) = builder.Build();

        jvmArgs.Should().Contain("https://nope.invalid");
    }

    [Fact(DisplayName = "Build returns JVM args sorted by priority descending")]
    public void Build_ReturnsJvmArgsSorted_ByPriorityDescending()
    {
        var builder = CreateBuilder();
        builder.AddJvmArgument(new LaunchArg("low-priority", 1));
        builder.AddJvmArgument(new LaunchArg("high-priority", 10));

        var (jvmArgs, _) = builder.Build();

        jvmArgs.IndexOf("high-priority", StringComparison.Ordinal)
            .Should().BeLessThan(jvmArgs.IndexOf("low-priority", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Build inserts -cp fallback when no classpath arg is present")]
    public void Build_InsertsCpFallback_WhenNoClasspathArgPresent()
    {
        var builder = CreateBuilder();

        var (jvmArgs, _) = builder.Build();

        jvmArgs.Should().Contain("-cp");
    }

    [Fact(DisplayName = "Build does not insert -cp fallback when a classpath arg is present")]
    public void Build_DoesNotInsertCpFallback_WhenClasspathArgPresent()
    {
        var builder = CreateBuilder();
        builder.AddClass("/libs/a.jar");
        builder.AddJvmArgument(new LaunchArg("-cp customPath", 50));

        var (jvmArgs, _) = builder.Build();

        jvmArgs.Should().Contain("-cp customPath").And.NotContain("${classpath}");
    }

    [Fact(DisplayName = "Build quotes paths containing spaces")]
    public void Build_QuotesPathsWithSpaces()
    {
        const string nativesWithSpaces = "/tmp/my natives";
        var builder = CreateBuilder(nativesDir: nativesWithSpaces);

        var (jvmArgs, _) = builder.Build();

        jvmArgs.Should().Contain($"-Djava.library.path=\"{nativesWithSpaces}\"");
    }

    [Fact(DisplayName = "Build substitutes classpath, user type, arch and token placeholders")]
    public void Build_SubstitutesPlaceholders_ClasspathUserTypeArchToken()
    {
        var builder = CreateBuilder(client: ModelFactory.CreateClientDetails(accessToken: "the-token"));
        builder.AddClass("/libs/a.jar");

        var (jvmArgs, gameArgs) = builder.Build();

        jvmArgs.Should().NotContain("${").And.NotContain("${classpath}");
        gameArgs.Should().Contain("msa").And.Contain("the-token");
    }

    [Fact(DisplayName = "Build uses 'none' when the access token is empty")]
    public void Build_UsesNone_ForEmptyAccessToken()
    {
        var builder = CreateBuilder(client: ModelFactory.CreateClientDetails(accessToken: null));

        var (_, gameArgs) = builder.Build();

        gameArgs.Should().Contain("none");
    }

    [Fact(DisplayName = "Build uses offline user type when the client is offline")]
    public void Build_UserTypeOffline_WhenClientIsOffline()
    {
        var builder = CreateBuilder(client: ModelFactory.CreateClientDetails(isOffline: true));

        var (_, gameArgs) = builder.Build();

        gameArgs.Should().Contain("offline");
    }

    [Fact(DisplayName = "AddClass deduplicates identical classpath entries")]
    public void AddClass_Dedupes()
    {
        var builder = CreateBuilder();

        builder.AddClass("/libs/a.jar");
        builder.AddClass("/libs/a.jar");

        builder.ClassPath.Should().ContainSingle().Which.Should().Be("/libs/a.jar");
    }

    [Fact(DisplayName = "AddGameArgument deduplicates by value")]
    public void AddGameArgument_DedupesByValue()
    {
        var builder = CreateBuilder();
        builder.AddGameArgument(new LaunchArg("custom-game-arg", 5));
        builder.AddGameArgument(new LaunchArg("custom-game-arg", 5));

        var (_, gameArgs) = builder.Build();

        gameArgs.Split("custom-game-arg").Length.Should().Be(2);
    }

    [Fact(DisplayName = "AddJvmArgument does not deduplicate")]
    public void AddJvmArgument_DoesNotDedupe()
    {
        var builder = CreateBuilder();
        builder.AddJvmArgument(new LaunchArg("jvm-dup", 5));
        builder.AddJvmArgument(new LaunchArg("jvm-dup", 5));

        var (jvmArgs, _) = builder.Build();

        jvmArgs.Split("jvm-dup").Length.Should().Be(3);
    }

    [Fact(DisplayName = "Build emits --width and --height when a resolution is set")]
    public void Build_WithResolution_EmitsWidthAndHeight()
    {
        var builder = CreateBuilder(resolution: ModelFactory.CreateResolution(1920, 1080));

        var (_, gameArgs) = builder.Build();

        gameArgs.Should().Contain("--width 1920").And.Contain("--height 1080");
    }

    [Fact(DisplayName = "Build omits --width and --height when no resolution is set")]
    public void Build_WithoutResolution_OmitsThem()
    {
        var builder = CreateBuilder();

        var (_, gameArgs) = builder.Build();

        gameArgs.Should().NotContain("--width").And.NotContain("--height");
    }

    [Fact(DisplayName = "Build emits --quickPlayMultiplayer when a server address is set")]
    public void Build_WithServerAddress_EmitsQuickPlayMultiplayer()
    {
        var builder = CreateBuilder(
            game: ModelFactory.CreateGameDetails(serverAddressToJoin: "play.example.com"));

        var (_, gameArgs) = builder.Build();

        gameArgs.Should().Contain("--quickPlayMultiplayer play.example.com");
    }

    [Fact(DisplayName = "Build throws when called twice due to duplicate classpath placeholder")]
    public void Build_CalledTwice_Throws_DuplicateClasspathPlaceholder()
    {
        var builder = CreateBuilder();
        builder.Build();

        var act = () => builder.Build();

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "UseClasspathFile writes an arg file and uses @ syntax")]
    public void UseClasspathFile_WritesArgFile_AndUsesAtSyntax()
    {
        var classpathFile = Path.Combine(Path.GetTempPath(), $"beacon-cp-{Guid.NewGuid():N}.txt");
        try
        {
            var builder = CreateBuilder();
            builder.AddClass("/libs/a.jar");
            builder.UseClasspathFile = true;
            builder.ClasspathFilePath = classpathFile;

            var (jvmArgs, _) = builder.Build();

            File.Exists(classpathFile).Should().BeTrue();
            File.ReadAllText(classpathFile).Should().Contain("a.jar");
            jvmArgs.Should().Contain($"@\"{classpathFile}\"");
        }
        finally
        {
            if (File.Exists(classpathFile))
                File.Delete(classpathFile);
        }
    }
}
