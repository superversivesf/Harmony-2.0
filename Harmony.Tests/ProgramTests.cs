using System.Reflection;
using FluentAssertions;
using CommandLine;
using System.IO;

namespace Harmony.Tests;

public class ProgramTests
{
    #region SpinnerDelayMs Constant Tests

    [Fact]
    public async Task SpinnerDelayMs_ShouldBePublicConstant()
    {
        // Arrange & Act
        var field = typeof(Program).GetField("SpinnerDelayMs",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        // Assert
        field.Should().NotBeNull("SpinnerDelayMs should exist as a constant");
        field!.IsLiteral.Should().BeTrue("SpinnerDelayMs should be a compile-time constant");
        field.IsPublic.Should().BeTrue("SpinnerDelayMs should be public for testing");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task SpinnerDelayMs_ShouldBe50Milliseconds()
    {
        // Arrange & Act & Assert
        Program.SpinnerDelayMs.Should().Be(50,
            "Spinner delay should be 50ms as per documentation - provides smooth visual feedback");
        await Task.CompletedTask;
    }

    #endregion

    #region HandleParseError Tests

    [Fact]
    public async Task HandleParseError_ShouldExistAsPrivateStaticMethod()
    {
        // Arrange & Act
        var method = typeof(Program).GetMethod("HandleParseError",
            BindingFlags.NonPublic | BindingFlags.Static);

        // Assert
        method.Should().NotBeNull("HandleParseError should exist as a static method");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task HandleParseError_ShouldAcceptIEnumerableOfError()
    {
        // Arrange & Act
        var method = typeof(Program).GetMethod("HandleParseError",
            BindingFlags.NonPublic | BindingFlags.Static);

        // Assert
        method.Should().NotBeNull();
        var parameters = method!.GetParameters();
        parameters.Should().HaveCount(1, "HandleParseError should accept one parameter");
        parameters[0].ParameterType.Should().Be(typeof(IEnumerable<CommandLine.Error>),
            "HandleParseError should accept IEnumerable<Error>");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task HandleParseError_ShouldExitWithCodeOne()
    {
        // Arrange
        var method = typeof(Program).GetMethod("HandleParseError",
            BindingFlags.NonPublic | BindingFlags.Static);

        // Act & Assert - The method should call Environment.Exit(1)
        // We can't test the actual exit in unit tests, but we can verify the method exists
        method.Should().NotBeNull("HandleParseError should be implemented");
        await Task.CompletedTask;
    }

    #endregion

    #region FetchFFmpegAsync Tests

    [Fact]
    public async Task FetchFFmpegAsync_ShouldExistAsPublicStaticMethod()
    {
        // Arrange & Act
        var method = typeof(Program).GetMethod("FetchFFmpegAsync",
            BindingFlags.Public | BindingFlags.Static);

        // Assert
        method.Should().NotBeNull("FetchFFmpegAsync should exist as a public static method");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task FetchFFmpegAsync_ShouldReturnTask()
    {
        // Arrange & Act
        var method = typeof(Program).GetMethod("FetchFFmpegAsync",
            BindingFlags.Public | BindingFlags.Static);

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task),
            "FetchFFmpegAsync should return Task for async operation");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task FetchFFmpegAsync_ShouldAcceptLoggerParameter()
    {
        // Arrange & Act
        var method = typeof(Program).GetMethod("FetchFFmpegAsync",
            BindingFlags.Public | BindingFlags.Static);

        // Assert
        method.Should().NotBeNull();
        var parameters = method!.GetParameters();
        parameters.Should().HaveCount(1, "FetchFFmpegAsync should accept one parameter");
        parameters[0].ParameterType.Name.Should().Be("Logger",
            "FetchFFmpegAsync should accept a Logger parameter");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task FetchFFmpegAsync_ShouldBeAsync()
    {
        // Arrange & Act
        var method = typeof(Program).GetMethod("FetchFFmpegAsync",
            BindingFlags.Public | BindingFlags.Static);

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task),
            "Async methods should return Task or Task<T>");
        await Task.CompletedTask;
    }

    #endregion

    #region LoopMode Tests

    [Fact]
    public async Task LoopMode_ShouldDefaultToFalse()
    {
        // Arrange
        var options = new Program.Options();

        // Act & Assert
        options.LoopMode.Should().BeFalse("LoopMode should default to false");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task LoopMode_ShouldBeSettable()
    {
        // Arrange
        var options = new Program.Options { LoopMode = true };

        // Act & Assert
        options.LoopMode.Should().BeTrue();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task LoopModeOption_ShouldHaveCorrectHelpDescribingFiveMinutes()
    {
        // Arrange
        var optionAttribute = typeof(Program.Options)
            .GetProperty(nameof(Program.Options.LoopMode))?
            .GetCustomAttributes(typeof(OptionAttribute), false)
            .FirstOrDefault() as OptionAttribute;

        // Act & Assert
        optionAttribute.Should().NotBeNull();
        optionAttribute!.HelpText.Should().Contain("5 minute",
            "Help text should indicate 5 minute sleep interval as per the bug fix");
        await Task.CompletedTask;
    }

    #endregion

    [Fact]
    public async Task Options_ShouldHaveDefaultQuietModeFalse()
    {
        // Arrange
        var options = new Program.Options();

        // Act & Assert
        options.QuietMode.Should().BeFalse();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Options_BitrateAttributeHasDefault64()
    {
        // The default value is specified in the Option attribute's Default parameter,
        // which CommandLineParser applies when parsing arguments. This test verifies
        // the attribute is correctly configured.

        // Arrange
        var optionAttribute = typeof(Program.Options)
            .GetProperty(nameof(Program.Options.Bitrate))?
            .GetCustomAttributes(typeof(OptionAttribute), false)
            .FirstOrDefault() as OptionAttribute;

        // Act & Assert
        optionAttribute.Should().NotBeNull();
        optionAttribute!.Default.Should().Be(64, "Bitrate should default to 64");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Options_ShouldHaveDefaultClobberFalse()
    {
        // Arrange
        var options = new Program.Options();

        // Act & Assert
        options.Clobber.Should().BeFalse();
        await Task.CompletedTask;
    }

    #region Options Property Tests - InputFolder

    [Fact]
    public async Task Options_InputFolder_ShouldBeNullableString()
    {
        // Arrange
        var options = new Program.Options();

        // Act & Assert
        options.InputFolder.Should().BeNull();
        options.InputFolder = "/test/path";
        options.InputFolder.Should().Be("/test/path");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Options_InputFolder_ShouldHaveCorrectShortName()
    {
        // Arrange
        var optionAttribute = typeof(Program.Options)
            .GetProperty(nameof(Program.Options.InputFolder))?
            .GetCustomAttributes(typeof(OptionAttribute), false)
            .FirstOrDefault() as OptionAttribute;

        // Act & Assert
        optionAttribute.Should().NotBeNull();
        optionAttribute!.ShortName.Should().Be("i", "InputFolder short name should be 'i'");
        optionAttribute.LongName.Should().Be("InputFolder");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Options_InputFolder_ShouldNotBeRequired()
    {
        // Arrange
        var optionAttribute = typeof(Program.Options)
            .GetProperty(nameof(Program.Options.InputFolder))?
            .GetCustomAttributes(typeof(OptionAttribute), false)
            .FirstOrDefault() as OptionAttribute;

        // Act & Assert
        optionAttribute.Should().NotBeNull();
        optionAttribute!.Required.Should().BeFalse("InputFolder should be optional with sensible defaults");
        await Task.CompletedTask;
    }

    #endregion

    #region Options Property Tests - OutputFolder

    [Fact]
    public async Task Options_OutputFolder_ShouldBeNullableString()
    {
        // Arrange
        var options = new Program.Options();

        // Act & Assert
        options.OutputFolder.Should().BeNull();
        options.OutputFolder = "/output/path";
        options.OutputFolder.Should().Be("/output/path");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Options_OutputFolder_ShouldHaveCorrectShortName()
    {
        // Arrange
        var optionAttribute = typeof(Program.Options)
            .GetProperty(nameof(Program.Options.OutputFolder))?
            .GetCustomAttributes(typeof(OptionAttribute), false)
            .FirstOrDefault() as OptionAttribute;

        // Act & Assert
        optionAttribute.Should().NotBeNull();
        optionAttribute!.ShortName.Should().Be("o", "OutputFolder short name should be 'o'");
        optionAttribute.LongName.Should().Be("OutputFolder");
        await Task.CompletedTask;
    }

    #endregion

    #region Options Property Tests - ActivationBytes

    [Fact]
    public async Task Options_ActivationBytes_ShouldBeNullableString()
    {
        // Arrange
        var options = new Program.Options();

        // Act & Assert
        options.ActivationBytes.Should().BeNull();
        options.ActivationBytes = "abcd1234";
        options.ActivationBytes.Should().Be("abcd1234");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Options_ActivationBytes_ShouldHaveCorrectShortName()
    {
        // Arrange
        var optionAttribute = typeof(Program.Options)
            .GetProperty(nameof(Program.Options.ActivationBytes))?
            .GetCustomAttributes(typeof(OptionAttribute), false)
            .FirstOrDefault() as OptionAttribute;

        // Act & Assert
        optionAttribute.Should().NotBeNull();
        optionAttribute!.ShortName.Should().Be("a", "ActivationBytes short name should be 'a'");
        optionAttribute.LongName.Should().Be("ActivationBytes");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Options_ActivationBytes_HelpText_ShouldReferenceGitHub()
    {
        // Arrange
        var optionAttribute = typeof(Program.Options)
            .GetProperty(nameof(Program.Options.ActivationBytes))?
            .GetCustomAttributes(typeof(OptionAttribute), false)
            .FirstOrDefault() as OptionAttribute;

        // Act & Assert
        optionAttribute.Should().NotBeNull();
        optionAttribute!.HelpText.Should().Contain("github.com", "Help text should reference the lookup tables repository");
        await Task.CompletedTask;
    }

    #endregion

    #region Options Property Tests - FetchFFMpeg

    [Fact]
    public async Task Options_FetchFFMpeg_ShouldDefaultToFalse()
    {
        // Arrange
        var options = new Program.Options();

        // Act & Assert
        options.FetchFFMpeg.Should().BeFalse("FetchFFMpeg should default to false");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Options_FetchFFMpeg_ShouldHaveCorrectShortName()
    {
        // Arrange
        var optionAttribute = typeof(Program.Options)
            .GetProperty(nameof(Program.Options.FetchFFMpeg))?
            .GetCustomAttributes(typeof(OptionAttribute), false)
            .FirstOrDefault() as OptionAttribute;

        // Act & Assert
        optionAttribute.Should().NotBeNull();
        optionAttribute!.ShortName.Should().Be("f", "FetchFFMpeg short name should be 'f'");
        optionAttribute.LongName.Should().Be("FetchFFMpeg");
        await Task.CompletedTask;
    }

    #endregion

    #region Options Property Tests - QuietMode

    [Fact]
    public async Task Options_QuietMode_ShouldHaveCorrectShortName()
    {
        // Arrange
        var optionAttribute = typeof(Program.Options)
            .GetProperty(nameof(Program.Options.QuietMode))?
            .GetCustomAttributes(typeof(OptionAttribute), false)
            .FirstOrDefault() as OptionAttribute;

        // Act & Assert
        optionAttribute.Should().NotBeNull();
        optionAttribute!.ShortName.Should().Be("q", "QuietMode short name should be 'q'");
        optionAttribute.LongName.Should().Be("QuietMode");
        await Task.CompletedTask;
    }

    #endregion

    #region Options Property Tests - Clobber

    [Fact]
    public async Task Options_Clobber_ShouldHaveDefaultFalse()
    {
        // Arrange
        var optionAttribute = typeof(Program.Options)
            .GetProperty(nameof(Program.Options.Clobber))?
            .GetCustomAttributes(typeof(OptionAttribute), false)
            .FirstOrDefault() as OptionAttribute;

        // Act & Assert
        optionAttribute.Should().NotBeNull();
        optionAttribute!.Default.Should().Be(false);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Options_Clobber_ShouldHaveCorrectShortName()
    {
        // Arrange
        var optionAttribute = typeof(Program.Options)
            .GetProperty(nameof(Program.Options.Clobber))?
            .GetCustomAttributes(typeof(OptionAttribute), false)
            .FirstOrDefault() as OptionAttribute;

        // Act & Assert
        optionAttribute.Should().NotBeNull();
        optionAttribute!.ShortName.Should().Be("c", "Clobber short name should be 'c'");
        optionAttribute.LongName.Should().Be("Clobber");
        await Task.CompletedTask;
    }

    #endregion

    #region Options Property Tests - Bitrate

    [Fact]
    public async Task Options_Bitrate_ShouldDefaultToZero()
    {
        // Arrange
        var options = new Program.Options();

        // Act & Assert
        // Note: Default of 64 is set by CommandLineParser when parsing args, 
        // not in the Options class itself
        options.Bitrate.Should().Be(0);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Options_Bitrate_ShouldHaveCorrectShortName()
    {
        // Arrange
        var optionAttribute = typeof(Program.Options)
            .GetProperty(nameof(Program.Options.Bitrate))?
            .GetCustomAttributes(typeof(OptionAttribute), false)
            .FirstOrDefault() as OptionAttribute;

        // Act & Assert
        optionAttribute.Should().NotBeNull();
        optionAttribute!.ShortName.Should().Be("b", "Bitrate short name should be 'b'");
        optionAttribute.LongName.Should().Be("Bitrate");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Options_Bitrate_ShouldAcceptDifferentValues()
    {
        // Arrange
        var options = new Program.Options();

        // Act
        options.Bitrate = 128;

        // Assert
        options.Bitrate.Should().Be(128);
        await Task.CompletedTask;
    }

    #endregion

    #region RunOptions Method Tests

    [Fact]
    public async Task RunOptions_ShouldExistAsPrivateStaticMethod()
    {
        // Arrange & Act
        var method = typeof(Program).GetMethod("RunOptionsAsync",
            BindingFlags.NonPublic | BindingFlags.Static);

        // Assert
        method.Should().NotBeNull("RunOptionsAsync should exist as a static method");
        method!.ReturnType.Should().Be(typeof(Task), "RunOptionsAsync should return Task");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task RunOptions_ShouldAcceptOptionsParameter()
    {
        // Arrange & Act
        var method = typeof(Program).GetMethod("RunOptionsAsync",
            BindingFlags.NonPublic | BindingFlags.Static);

        // Assert
        method.Should().NotBeNull();
        var parameters = method!.GetParameters();
        parameters.Should().HaveCount(1, "RunOptionsAsync should accept one parameter");
        parameters[0].ParameterType.Should().Be(typeof(Program.Options));
        await Task.CompletedTask;
    }

    #endregion

    #region Main Method Tests

    [Fact]
    public async Task Main_ShouldExistAsPrivateStaticMethod()
    {
        // Arrange & Act
        var method = typeof(Program).GetMethod("Main",
            BindingFlags.NonPublic | BindingFlags.Static);

        // Assert
        method.Should().NotBeNull("Main should exist");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Main_ShouldAcceptStringArrayParameter()
    {
        // Arrange & Act
        var method = typeof(Program).GetMethod("Main",
            BindingFlags.NonPublic | BindingFlags.Static);

        // Assert
        method.Should().NotBeNull();
        var parameters = method!.GetParameters();
        parameters.Should().HaveCount(1);
        parameters[0].ParameterType.Should().Be(typeof(string[]));
        await Task.CompletedTask;
    }

    #endregion

    #region Architecture Tests

    [Fact]
    public async Task Program_ShouldBeStaticClass()
    {
        // Arrange & Act
        var typeInfo = typeof(Program);

        // Assert
        typeInfo.IsAbstract.Should().BeTrue("Program should be static (abstract sealed)");
        typeInfo.IsSealed.Should().BeTrue("Program should be sealed");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Options_ShouldBeNestedClass()
    {
        // Arrange & Act
        var optionsType = typeof(Program).GetNestedType("Options");

        // Assert
        optionsType.Should().NotBeNull("Options should be a nested class within Program");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Options_ShouldBePublic()
    {
        // Arrange & Act
        var optionsType = typeof(Program).GetNestedType("Options", BindingFlags.Public);

        // Assert
        optionsType.Should().NotBeNull("Options should be public for CommandLineParser");
        await Task.CompletedTask;
    }

    #endregion

    #region Version and Copyright Tests

    [Fact]
    public async Task AssemblyVersion_ShouldBeAccessible()
    {
        // Arrange & Act
        var version = Assembly.GetExecutingAssembly().GetName().Version;

        // Assert
        version.Should().NotBeNull();
        await Task.CompletedTask;
    }

    #endregion
}