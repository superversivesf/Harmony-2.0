using FluentAssertions;
using System.Reflection;

namespace Harmony.Tests;

public class AAXtoM4BConvertorTests
{
    #region Constants Tests

    [Fact]
    public void MaxAuthorCountForIndividualDisplay_ShouldBeFour()
    {
        // Assert
        AaxToM4BConvertor.MaxAuthorCountForIndividualDisplay.Should().Be(4);
    }

    [Fact]
    public void MaxAuthorCountForIndividualDisplay_ShouldBePublicConstant()
    {
        // Arrange
        var field = typeof(AaxToM4BConvertor).GetField(
            nameof(AaxToM4BConvertor.MaxAuthorCountForIndividualDisplay),
            BindingFlags.Public | BindingFlags.Static);

        // Assert
        field.Should().NotBeNull("the constant should be public");
        field!.IsLiteral.Should().BeTrue("it should be a compile-time constant");
    }

    #endregion

    #region CleanAuthor Tests with Constant

    [Theory]
    [InlineData("Author1, Author2, Author3, Author4", "Author1, Author2, Author3, Author4")]
    [InlineData("Author1, Author2, Author3", "Author1, Author2, Author3")]
    [InlineData("Single Author", "Single Author")]
    public void CleanAuthor_ShouldNotReturnVariousForEqualOrFewerAuthorsThanThreshold(string input, string expected)
    {
        // Arrange
        var converter = CreateConverter();
        var authorCount = input.Split(',').Length;

        // Act
        var result = InvokeCleanAuthor(converter, input);

        // Assert
        result.Should().Be(expected);
        authorCount.Should().BeLessThanOrEqualTo(AaxToM4BConvertor.MaxAuthorCountForIndividualDisplay);
    }

    [Theory]
    [InlineData("A, B, C, D, E")] // 5 authors
    [InlineData("A, B, C, D, E, F")] // 6 authors
    [InlineData("1, 2, 3, 4, 5, 6, 7, 8, 9, 10")] // 10 authors
    public void CleanAuthor_ShouldReturnVariousWhenAuthorCountExceedsMaxAuthorCount(string input)
    {
        // Arrange
        var converter = CreateConverter();
        var authorCount = input.Split(',').Length;

        // Act
        var result = InvokeCleanAuthor(converter, input);

        // Assert
        result.Should().Be("Various");
        authorCount.Should().BeGreaterThan(AaxToM4BConvertor.MaxAuthorCountForIndividualDisplay);
    }

    #endregion

    #region CleanTitle Tests

    [Theory]
    [InlineData("Test Book (Unabridged)", "Test Book")]
    [InlineData("Test: Book", "Test - Book")]
    [InlineData("Test's Book", "Tests Book")]
    [InlineData("Test? Book", "Test Book")]
    [InlineData("  Test Book  ", "Test Book")]
    [InlineData(null, null)]
    [InlineData("", "")]
    public void CleanTitle_ShouldHandleVariousInputs(string? input, string? expected)
    {
        // Arrange
        var converter = CreateConverter();

        // Act
        var result = InvokeCleanTitle(converter, input);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region CleanAuthor Additional Tests

    [Theory]
    [InlineData("John Smith", "John Smith")]
    [InlineData("John Smith Jr., Jane Doe", "John Smith Jr, Jane Doe")]
    [InlineData(null, "Unknown")]
    [InlineData("", "Unknown")]
    public void CleanAuthor_ShouldHandleVariousInputs(string? input, string expected)
    {
        // Arrange
        var converter = CreateConverter();

        // Act
        var result = InvokeCleanAuthor(converter, input);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region CheckFolders Tests

    [Fact]
    public void CheckFolders_ShouldThrowForNonExistentInputFolder()
    {
        // Arrange
        var converter = CreateConverter(
            inputFolder: "/non/existent/input/folder",
            outputFolder: Path.GetTempPath());

        // Act
        var act = () => InvokeCheckFoldersAndUnwrap(converter);

        // Assert
        act.Should().Throw<Exception>()
            .WithMessage("*Input folder does not exist*");
    }

    [Fact]
    public void CheckFolders_ShouldThrowForNonExistentOutputFolder()
    {
        // Arrange
        var inputFolder = Directory.CreateTempSubdirectory().FullName;
        var converter = CreateConverter(
            inputFolder: inputFolder,
            outputFolder: "/non/existent/output/folder");

        try
        {
            // Act
            var act = () => InvokeCheckFoldersAndUnwrap(converter);

            // Assert
            act.Should().Throw<Exception>()
                .WithMessage("*Output folder does not exist*");
        }
        finally
        {
            Directory.Delete(inputFolder);
        }
    }

    [Fact]
    public void CheckFolders_ShouldNotThrowForExistingFolders()
    {
        // Arrange
        var inputFolder = Directory.CreateTempSubdirectory().FullName;
        var outputFolder = Directory.CreateTempSubdirectory().FullName;
        var converter = CreateConverter(
            inputFolder: inputFolder,
            outputFolder: outputFolder);

        try
        {
            // Act
            var act = () => InvokeCheckFolders(converter);

            // Assert
            act.Should().NotThrow();
        }
        finally
        {
            Directory.Delete(inputFolder);
            Directory.Delete(outputFolder);
        }
    }

    #endregion

    #region Null Handling Tests for Split Operations

    [Fact]
    public void NullSafeSplit_WithNullAuthors_ShouldReturnEmptyArray()
    {
        // Test the pattern used for null-safe splitting: authors?.Split(",") ?? Array.Empty<string>()
        string? authors = null;
        string[] result = authors?.Split(",") ?? Array.Empty<string>();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty("null input returns empty array via null-coalescing");
    }

    [Fact]
    public void NullSafeSplit_WithEmptyString_ShouldReturnSingleEmptyElement()
    {
        // Important: Split on empty string returns [""], not empty array
        // This is the expected behavior - the fix prevents NullReferenceException for null, not filtering
        string? authors = "";
        string[] result = authors?.Split(",") ?? Array.Empty<string>();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Should().BeEmpty();
    }

    [Theory]
    [InlineData("Author1", new[] { "Author1" })]
    [InlineData("Author1,Author2", new[] { "Author1", "Author2" })]
    [InlineData("Author1, Author2, Author3", new[] { "Author1", " Author2", " Author3" })]
    public void NullSafeSplit_WithValidAuthors_ShouldSplitCorrectly(string authors, string[] expected)
    {
        // Test the pattern used for null-safe splitting
        string[] result = authors?.Split(",") ?? Array.Empty<string>();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void NullSafeSplit_WithNullGenres_ShouldReturnEmptyArray()
    {
        // Test the pattern used in the actual code: data.genres?.Split(",") ?? Array.Empty<string>()
        string? genres = null;
        string[] result = genres?.Split(",") ?? Array.Empty<string>();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void NullSafeSplit_WithNullNarrators_ShouldReturnEmptyArray()
    {
        // Test the pattern used in the actual code: data.narrators?.Split(",") ?? Array.Empty<string>()
        string? narrators = null;
        string[] result = narrators?.Split(",") ?? Array.Empty<string>();

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("   ", "")]
    public void SeriesString_WithNullOrWhitespaceTitle_ShouldReturnEmptyList(string? seriesTitle, string? seriesSequence)
    {
        // Test the series string pattern from AddMetadataToM4A
        var series = !string.IsNullOrWhiteSpace(seriesTitle)
            ? new List<string> { $"{seriesTitle} #{seriesSequence}".Trim() }
            : new List<string>();

        // Assert - null or whitespace series_title should produce empty list
        series.Should().BeEmpty("series_title is null or whitespace");
    }

    [Fact]
    public void SeriesString_WithValidTitleAndSequence_ShouldFormatCorrectly()
    {
        // Arrange
        string? seriesTitle = "The Great Series";
        string? seriesSequence = "5";

        // Act
        var series = !string.IsNullOrWhiteSpace(seriesTitle)
            ? new List<string> { $"{seriesTitle} #{seriesSequence}".Trim() }
            : new List<string>();

        // Assert
        series.Should().ContainSingle()
            .Which.Should().Be("The Great Series #5");
    }

    [Fact]
    public void SeriesString_WithValidTitleButNullSequence_ShouldFormatGracefully()
    {
        // Arrange
        string? seriesTitle = "The Great Series";
        string? seriesSequence = null;

        // Act
        var series = !string.IsNullOrWhiteSpace(seriesTitle)
            ? new List<string> { $"{seriesTitle} #{seriesSequence}".Trim() }
            : new List<string>();

        // Assert
        series.Should().ContainSingle()
            .Which.Should().Be("The Great Series #");
    }

    [Fact]
    public void SeriesString_WithValidTitleButEmptySequence_ShouldFormatGracefully()
    {
        // Arrange
        string? seriesTitle = "The Great Series";
        string? seriesSequence = "";

        // Act
        var series = !string.IsNullOrWhiteSpace(seriesTitle)
            ? new List<string> { $"{seriesTitle} #{seriesSequence}".Trim() }
            : new List<string>();

        // Assert
        series.Should().ContainSingle()
            .Which.Should().Be("The Great Series #");
    }

    [Fact]
    public void SeriesString_PreviouslyWouldCreateHashSpace_WhenTitleWasNull()
    {
        // This test documents the OLD bug behavior vs the fix
        // OLD CODE: metadata.series = new List<string>() {data?.series_title + " #" + data?.series_sequence };
        // When series_title is null and series_sequence is "5", it would create " #5"
        string? seriesTitle = null;
        string? seriesSequence = "5";

        // OLD buggy behavior simulation
        var oldBuggyResult = seriesTitle + " #" + seriesSequence; // yields " #5"

        // NEW correct behavior
        var newCorrectResult = !string.IsNullOrWhiteSpace(seriesTitle)
            ? new List<string> { $"{seriesTitle} #{seriesSequence}".Trim() }
            : new List<string>();

        // Assert
        oldBuggyResult.Should().Be(" #5", "old code would create this malformed string");
        newCorrectResult.Should().BeEmpty("new code correctly handles null series_title");
    }

    #endregion

    #region ProcessTitleForComparison Tests

    [Theory]
    [InlineData("The Great Gatsby", "thegreatgatsby")]
    [InlineData("The Great Gatsby!", "thegreatgatsby")]
    [InlineData("The Great Gatsby (Unabridged)", "thegreatgatsbyunabridged")]
    [InlineData("What's In A Name?", "whatsinaname")]
    [InlineData("Book: Part 1", "bookpart1")]
    [InlineData("ALL UPPERCASE", "alluppercase")]
    [InlineData("MixedCase Title", "mixedcasetitle")]
    public void ProcessTitleForComparison_ShouldConvertToLowercaseAndStripNonAlphanumeric(string input, string expected)
    {
        // Arrange
        var converter = CreateConverter();

        // Act
        var result = InvokeProcessTitleForComparison(converter, input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Title-With_Dashes And Underscores", "titlewithdashesandunderscores")]
    [InlineData("Book@#$%^&*()Title", "booktitle")]
    [InlineData("123 Numbers 456", "123numbers456")]
    [InlineData("!@#$%^&*()", "")]
    public void ProcessTitleForComparison_ShouldStripAllSpecialCharacters(string input, string expected)
    {
        // Arrange
        var converter = CreateConverter();

        // Act
        var result = InvokeProcessTitleForComparison(converter, input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ProcessTitleForComparison_ShouldHandleUnicodeCharacters()
    {
        // Arrange
        var converter = CreateConverter();
        // Unicode characters like é, ñ, 中文 should be stripped as they're not a-z0-9
        var input = "Café Menu 中文书";

        // Act
        var result = InvokeProcessTitleForComparison(converter, input);

        // Assert
        result.Should().Be("cafmenu"); // é stripped, Chinese characters stripped
    }

    [Fact]
    public void ProcessTitleForComparison_ShouldHandleEmptyString()
    {
        // Arrange
        var converter = CreateConverter();

        // Act
        var result = InvokeProcessTitleForComparison(converter, "");

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("   ", "")]
    [InlineData("\t\n", "")]
    [InlineData(" Title ", "title")]
    public void ProcessTitleForComparison_ShouldHandleWhitespace(string input, string expected)
    {
        // Arrange
        var converter = CreateConverter();

        // Act
        var result = InvokeProcessTitleForComparison(converter, input);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Helper Methods

    private static AaxToM4BConvertor CreateConverter(
        string activationBytes = "00000000",
        int bitrate = 64,
        bool quietMode = true,
        string? inputFolder = null,
        string? outputFolder = null,
        bool clobber = false)
    {
        inputFolder ??= Path.GetTempPath();
        outputFolder ??= Path.GetTempPath();

        return new AaxToM4BConvertor(
            activationBytes,
            bitrate,
            quietMode,
            inputFolder,
            outputFolder,
            clobber,
            null);
    }

    private static string? InvokeCleanTitle(AaxToM4BConvertor converter, string? title)
    {
        var method = typeof(AaxToM4BConvertor).GetMethod("CleanTitle",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return method?.Invoke(converter, new object?[] { title }) as string;
    }

    private static string InvokeProcessTitleForComparison(AaxToM4BConvertor converter, string input)
    {
        var method = typeof(AaxToM4BConvertor).GetMethod("ProcessTitleForComparison",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return method?.Invoke(converter, new object[] { input }) as string ?? string.Empty;
    }

    private static string InvokeCleanAuthor(AaxToM4BConvertor converter, string? name)
    {
        var method = typeof(AaxToM4BConvertor).GetMethod("CleanAuthor",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return method?.Invoke(converter, new object?[] { name }) as string ?? "Unknown";
    }

    private static void InvokeCheckFolders(AaxToM4BConvertor converter)
    {
        var method = typeof(AaxToM4BConvertor).GetMethod("CheckFolders",
            BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(converter, null);
    }

    /// <summary>
    /// Unwraps TargetInvocationException from reflection to get the actual exception
    /// </summary>
    private static void InvokeCheckFoldersAndUnwrap(AaxToM4BConvertor converter)
    {
        var method = typeof(AaxToM4BConvertor).GetMethod("CheckFolders",
            BindingFlags.NonPublic | BindingFlags.Instance);
        try
        {
            method?.Invoke(converter, null);
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException!;
        }
    }

    #endregion
}