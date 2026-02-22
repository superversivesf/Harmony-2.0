using System.Text;
using FluentAssertions;

namespace Harmony.Tests;

public class LoggerTests : IDisposable
{
    private readonly StringBuilder _stringBuilder;
    private readonly StringWriter _stringWriter;
    private readonly TextWriter _originalOut;

    public LoggerTests()
    {
        _originalOut = Console.Out;
        _stringBuilder = new StringBuilder();
        _stringWriter = new StringWriter(_stringBuilder);
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        _stringWriter.Dispose();
        GC.SuppressFinalize(this);
    }

    private void CaptureConsoleOutput()
    {
        Console.SetOut(_stringWriter);
    }

    private string GetCapturedOutput()
    {
        _stringWriter.Flush();
        return _stringBuilder.ToString();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithQuietModeFalse_ShouldCreateLogger()
    {
        // Arrange & Act
        var logger = new Logger(quietMode: false);

        // Assert
        logger.Should().NotBeNull("Logger should be instantiated with quietMode false");
    }

    [Fact]
    public void Constructor_WithQuietModeTrue_ShouldCreateLogger()
    {
        // Arrange & Act
        var logger = new Logger(quietMode: true);

        // Assert
        logger.Should().NotBeNull("Logger should be instantiated with quietMode true");
    }

    #endregion

    #region WriteLine Tests

    [Fact]
    public void WriteLine_WhenQuietModeIsFalse_ShouldWriteToConsole()
    {
        // Arrange
        CaptureConsoleOutput();
        var logger = new Logger(quietMode: false);
        const string testMessage = "Test message";

        // Act
        logger.WriteLine(testMessage);

        // Assert
        GetCapturedOutput().Should().Be(testMessage + Environment.NewLine,
            "WriteLine should write the message followed by a newline when not in quiet mode");
    }

    [Fact]
    public void WriteLine_WhenQuietModeIsTrue_ShouldNotWriteToConsole()
    {
        // Arrange
        CaptureConsoleOutput();
        var logger = new Logger(quietMode: true);
        const string testMessage = "Test message";

        // Act
        logger.WriteLine(testMessage);

        // Assert
        GetCapturedOutput().Should().BeEmpty(
            "WriteLine should not write anything when in quiet mode");
    }

    [Fact]
    public void WriteLine_WithEmptyString_ShouldWriteNewlineOnly()
    {
        // Arrange
        CaptureConsoleOutput();
        var logger = new Logger(quietMode: false);

        // Act
        logger.WriteLine(string.Empty);

        // Assert
        GetCapturedOutput().Should().Be(Environment.NewLine,
            "WriteLine with empty string should write only a newline");
    }

    [Fact]
    public void WriteLine_MultipleCalls_ShouldAccumulateOutput()
    {
        // Arrange
        CaptureConsoleOutput();
        var logger = new Logger(quietMode: false);

        // Act
        logger.WriteLine("Line 1");
        logger.WriteLine("Line 2");
        logger.WriteLine("Line 3");

        // Assert
        var expected = $"Line 1{Environment.NewLine}Line 2{Environment.NewLine}Line 3{Environment.NewLine}";
        GetCapturedOutput().Should().Be(expected,
            "Multiple WriteLine calls should accumulate output");
    }

    #endregion

    #region Write Tests

    [Fact]
    public void Write_WhenQuietModeIsFalse_ShouldWriteToConsole()
    {
        // Arrange
        CaptureConsoleOutput();
        var logger = new Logger(quietMode: false);
        const string testMessage = "Test message";

        // Act
        logger.Write(testMessage);

        // Assert
        GetCapturedOutput().Should().Be(testMessage,
            "Write should write the message without a newline when not in quiet mode");
    }

    [Fact]
    public void Write_WhenQuietModeIsTrue_ShouldNotWriteToConsole()
    {
        // Arrange
        CaptureConsoleOutput();
        var logger = new Logger(quietMode: true);
        const string testMessage = "Test message";

        // Act
        logger.Write(testMessage);

        // Assert
        GetCapturedOutput().Should().BeEmpty(
            "Write should not write anything when in quiet mode");
    }

    [Fact]
    public void Write_WithEmptyString_ShouldWriteNothing()
    {
        // Arrange
        CaptureConsoleOutput();
        var logger = new Logger(quietMode: false);

        // Act
        logger.Write(string.Empty);

        // Assert
        GetCapturedOutput().Should().BeEmpty(
            "Write with empty string should write nothing");
    }

    [Fact]
    public void Write_MultipleCalls_ShouldConcatenateWithoutNewlines()
    {
        // Arrange
        CaptureConsoleOutput();
        var logger = new Logger(quietMode: false);

        // Act
        logger.Write("Part1");
        logger.Write("Part2");
        logger.Write("Part3");

        // Assert
        GetCapturedOutput().Should().Be("Part1Part2Part3",
            "Multiple Write calls should concatenate without newlines");
    }

    [Fact]
    public void Write_FollowedByWriteLine_ShouldCombineCorrectly()
    {
        // Arrange
        CaptureConsoleOutput();
        var logger = new Logger(quietMode: false);

        // Act
        logger.Write("Hello ");
        logger.WriteLine("World");

        // Assert
        GetCapturedOutput().Should().Be($"Hello World{Environment.NewLine}",
            "Write followed by WriteLine should combine correctly on the same line");
    }

    #endregion

    #region AdvanceSpinner Tests

    [Fact]
    public void AdvanceSpinner_ShouldWriteSpinnerCharacter()
    {
        // Arrange
        CaptureConsoleOutput();
        var logger = new Logger(quietMode: false);

        // Act
        logger.AdvanceSpinner();

        // Assert
        var output = GetCapturedOutput();
        output.Should().NotBeEmpty("AdvanceSpinner should write a spinner character");
    }

    [Fact]
    public void AdvanceSpinner_ShouldWriteBackspaceFirst()
    {
        // Arrange
        CaptureConsoleOutput();
        var logger = new Logger(quietMode: false);

        // Act
        logger.AdvanceSpinner();

        // Assert
        var output = GetCapturedOutput();
        output.Should().StartWith("\b",
            "AdvanceSpinner should start with a backspace character to clear previous spinner");
    }

    [Fact]
    public void AdvanceSpinner_ShouldWriteEvenInQuietMode()
    {
        // Arrange
        CaptureConsoleOutput();
        var logger = new Logger(quietMode: true);

        // Act
        logger.AdvanceSpinner();

        // Assert
        var output = GetCapturedOutput();
        output.Should().NotBeEmpty(
            "AdvanceSpinner should write even in quiet mode (for progress indication)");
    }

    [Fact]
    public void AdvanceSpinner_MultipleCalls_ShouldCycleThroughSpinnerCharacters()
    {
        // Arrange
        CaptureConsoleOutput();
        var logger = new Logger(quietMode: false);
        const string expectedSpinnerString = "←↖↑↗→↘↓↙";

        // Act - Call AdvanceSpinner 8 times (length of spinner string)
        for (var i = 0; i < 8; i++)
        {
            logger.AdvanceSpinner();
        }

        // Assert
        var output = GetCapturedOutput();
        var spinnerChars = new List<char>();
        for (var i = 0; i < output.Length; i += 2) // Each output is "\b" + spinner char
        {
            if (i + 1 < output.Length && output[i] == '\b')
            {
                spinnerChars.Add(output[i + 1]);
            }
        }

        // Should have cycled through all spinner characters
        spinnerChars.Count.Should().Be(8, "Should have 8 spinner characters after 8 calls");

        // Verify each character is from the spinner string
        foreach (var c in spinnerChars)
        {
            expectedSpinnerString.Should().Contain(c.ToString(),
                $"Spinner character '{c}' should be in the spinner string");
        }
    }

    [Fact]
    public void AdvanceSpinner_MoreCallsThanSpinnerLength_ShouldWrapAround()
    {
        // Arrange
        CaptureConsoleOutput();
        var logger = new Logger(quietMode: false);

        // Act - Call more times than the spinner string length
        for (var i = 0; i < 20; i++)
        {
            logger.AdvanceSpinner();
        }

        // Assert - Should not throw and should continue cycling
        var output = GetCapturedOutput();
        output.Should().NotBeEmpty("AdvanceSpinner should work even when called more times than spinner length");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Logger_QuietMode_ShouldSuppressWriteAndWriteLineButNotSpinner()
    {
        // Arrange
        CaptureConsoleOutput();
        var logger = new Logger(quietMode: true);

        // Act
        logger.Write("This should not appear");
        logger.WriteLine("Neither should this");
        logger.AdvanceSpinner();
        logger.AdvanceSpinner();

        // Assert
        var output = GetCapturedOutput();
        output.Should().NotContain("This should not appear",
            "Write should be suppressed in quiet mode");
        output.Should().NotContain("Neither should this",
            "WriteLine should be suppressed in quiet mode");
        output.Should().Contain("\b",
            "AdvanceSpinner should still work in quiet mode");
    }

    [Fact]
    public void Logger_NotQuietMode_AllMethodsShouldWrite()
    {
        // Arrange
        CaptureConsoleOutput();
        var logger = new Logger(quietMode: false);

        // Act
        logger.Write("Write");
        logger.WriteLine(" WriteLine");
        logger.AdvanceSpinner();

        // Assert
        var output = GetCapturedOutput();
        output.Should().Contain("Write",
            "Write output should be present");
        output.Should().Contain("WriteLine",
            "WriteLine output should be present");
        output.Should().Contain("\b",
            "AdvanceSpinner should write backspace");
    }

    #endregion
}