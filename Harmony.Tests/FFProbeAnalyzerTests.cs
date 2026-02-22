using FluentAssertions;

namespace Harmony.Tests;

public class FFProbeAnalyzerTests
{
    [Fact]
    public async Task AnalyzeFile_ShouldReturnFalse_WhenFileDoesNotExist()
    {
        // Arrange
        var analyzer = new FFProbeAnalyzer();
        var nonExistentPath = "/non/existent/path/to/file.m4b";

        // Act
        var result = await analyzer.AnalyzeFile(nonExistentPath);

        // Assert
        result.Should().BeFalse("AnalyzeFile should return false for non-existent files");
    }

    [Fact]
    public async Task AnalyzeFile_ShouldReturnFalse_WhenPathIsEmpty()
    {
        // Arrange
        var analyzer = new FFProbeAnalyzer();

        // Act
        var result = await analyzer.AnalyzeFile(string.Empty);

        // Assert
        result.Should().BeFalse("AnalyzeFile should return false for empty path");
    }

    [Fact]
    public async Task AnalyzeFile_ShouldReturnFalse_WhenPathIsNull()
    {
        // Arrange
        var analyzer = new FFProbeAnalyzer();

        // Act
        var result = await analyzer.AnalyzeFile(null!);

        // Assert
        result.Should().BeFalse("AnalyzeFile should return false for null path");
    }

    [Fact]
    public async Task AnalyzeFile_ShouldReturnFalse_WhenPathIsInvalid()
    {
        // Arrange
        var analyzer = new FFProbeAnalyzer();
        var invalidPath = "///invalid<>path|?.m4b";

        // Act
        var result = await analyzer.AnalyzeFile(invalidPath);

        // Assert
        result.Should().BeFalse("AnalyzeFile should return false for invalid path characters");
    }

    [Fact]
    public async Task AnalyzeFile_ShouldReturnFalse_WhenFileIsNotValidMedia()
    {
        // Arrange
        var analyzer = new FFProbeAnalyzer();
        // Create a temp file that's not a valid media file
        var tempPath = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempPath, "This is not a valid media file content");

            // Act
            var result = await analyzer.AnalyzeFile(tempPath);

            // Assert
            result.Should().BeFalse("AnalyzeFile should return false for invalid media files");
        }
        finally
        {
            File.Delete(tempPath);
        }
    }
}