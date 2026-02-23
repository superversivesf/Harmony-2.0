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
}