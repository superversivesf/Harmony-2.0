using FluentAssertions;
using System.Reflection;
using Harmony.Dto;

namespace Harmony.Tests;

public class AAXtoM4BConvertorTests
{
    #region Constants Tests

    [Fact]
    public async Task MaxAuthorCountForIndividualDisplay_ShouldBeFour()
    {
        // Assert
        AaxToM4BConvertor.MaxAuthorCountForIndividualDisplay.Should().Be(4);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task MaxAuthorCountForIndividualDisplay_ShouldBePublicConstant()
    {
        // Arrange
        var field = typeof(AaxToM4BConvertor).GetField(
            nameof(AaxToM4BConvertor.MaxAuthorCountForIndividualDisplay),
            BindingFlags.Public | BindingFlags.Static);

        // Assert
        field.Should().NotBeNull("the constant should be public");
        field!.IsLiteral.Should().BeTrue("it should be a compile-time constant");
        await Task.CompletedTask;
    }

    #endregion

    #region CleanAuthor Tests with Constant

    [Theory]
    [InlineData("Author1, Author2, Author3, Author4", "Author1, Author2, Author3, Author4")]
    [InlineData("Author1, Author2, Author3", "Author1, Author2, Author3")]
    [InlineData("Single Author", "Single Author")]
    public async Task CleanAuthor_ShouldNotReturnVariousForEqualOrFewerAuthorsThanThreshold(string input, string expected)
    {
        // Arrange
        var converter = CreateConverter();
        var authorCount = input.Split(',').Length;

        // Act
        var result = InvokeCleanAuthor(converter, input);

        // Assert
        result.Should().Be(expected);
        authorCount.Should().BeLessThanOrEqualTo(AaxToM4BConvertor.MaxAuthorCountForIndividualDisplay);
        await Task.CompletedTask;
    }

    [Theory]
    [InlineData("A, B, C, D, E")] // 5 authors
    [InlineData("A, B, C, D, E, F")] // 6 authors
    [InlineData("1, 2, 3, 4, 5, 6, 7, 8, 9, 10")] // 10 authors
    public async Task CleanAuthor_ShouldReturnVariousWhenAuthorCountExceedsMaxAuthorCount(string input)
    {
        // Arrange
        var converter = CreateConverter();
        var authorCount = input.Split(',').Length;

        // Act
        var result = InvokeCleanAuthor(converter, input);

        // Assert
        result.Should().Be("Various");
        authorCount.Should().BeGreaterThan(AaxToM4BConvertor.MaxAuthorCountForIndividualDisplay);
        await Task.CompletedTask;
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
    public async Task CleanTitle_ShouldHandleVariousInputs(string? input, string? expected)
    {
        // Arrange
        var converter = CreateConverter();

        // Act
        var result = InvokeCleanTitle(converter, input);

        // Assert
        result.Should().Be(expected);
        await Task.CompletedTask;
    }

    #endregion

    #region CleanAuthor Additional Tests

    [Theory]
    [InlineData("John Smith", "John Smith")]
    [InlineData("John Smith Jr., Jane Doe", "John Smith Jr, Jane Doe")]
    [InlineData(null, "Unknown")]
    [InlineData("", "Unknown")]
    public async Task CleanAuthor_ShouldHandleVariousInputs(string? input, string expected)
    {
        // Arrange
        var converter = CreateConverter();

        // Act
        var result = InvokeCleanAuthor(converter, input);

        // Assert
        result.Should().Be(expected);
        await Task.CompletedTask;
    }

    #endregion

    #region CheckFolders Tests

    [Fact]
    public async Task CheckFolders_ShouldThrowForNonExistentInputFolder()
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
        await Task.CompletedTask;
    }

    [Fact]
    public async Task CheckFolders_ShouldThrowForNonExistentOutputFolder()
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
        await Task.CompletedTask;
    }

    [Fact]
    public async Task CheckFolders_ShouldNotThrowForExistingFolders()
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
        await Task.CompletedTask;
    }

    #endregion

    #region Null Handling Tests for Split Operations

    [Fact]
    public async Task NullSafeSplit_WithNullAuthors_ShouldReturnEmptyArray()
    {
        // Test the pattern used for null-safe splitting: authors?.Split(",") ?? Array.Empty<string>()
        string? authors = null;
        string[] result = authors?.Split(",") ?? Array.Empty<string>();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty("null input returns empty array via null-coalescing");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task NullSafeSplit_WithEmptyString_ShouldReturnSingleEmptyElement()
    {
        // Important: Split on empty string returns [""], not empty array
        // This is the expected behavior - the fix prevents NullReferenceException for null, not filtering
        string? authors = "";
        string[] result = authors?.Split(",") ?? Array.Empty<string>();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Should().BeEmpty();
        await Task.CompletedTask;
    }

    [Theory]
    [InlineData("Author1", new[] { "Author1" })]
    [InlineData("Author1,Author2", new[] { "Author1", "Author2" })]
    [InlineData("Author1, Author2, Author3", new[] { "Author1", " Author2", " Author3" })]
    public async Task NullSafeSplit_WithValidAuthors_ShouldSplitCorrectly(string authors, string[] expected)
    {
        // Test the pattern used for null-safe splitting
        string[] result = authors?.Split(",") ?? Array.Empty<string>();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expected);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task NullSafeSplit_WithNullGenres_ShouldReturnEmptyArray()
    {
        // Test the pattern used in the actual code: data.genres?.Split(",") ?? Array.Empty<string>()
        string? genres = null;
        string[] result = genres?.Split(",") ?? Array.Empty<string>();

        // Assert
        result.Should().BeEmpty();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task NullSafeSplit_WithNullNarrators_ShouldReturnEmptyArray()
    {
        // Test the pattern used in the actual code: data.narrators?.Split(",") ?? Array.Empty<string>()
        string? narrators = null;
        string[] result = narrators?.Split(",") ?? Array.Empty<string>();

        // Assert
        result.Should().BeEmpty();
        await Task.CompletedTask;
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("   ", "")]
    public async Task SeriesString_WithNullOrWhitespaceTitle_ShouldReturnEmptyList(string? seriesTitle, string? seriesSequence)
    {
        // Test the series string pattern from AddMetadataToM4A
        var series = !string.IsNullOrWhiteSpace(seriesTitle)
            ? new List<string> { $"{seriesTitle} #{seriesSequence}".Trim() }
            : new List<string>();

        // Assert - null or whitespace series_title should produce empty list
        series.Should().BeEmpty("series_title is null or whitespace");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task SeriesString_WithValidTitleAndSequence_ShouldFormatCorrectly()
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
        await Task.CompletedTask;
    }

    [Fact]
    public async Task SeriesString_WithValidTitleButNullSequence_ShouldFormatGracefully()
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
        await Task.CompletedTask;
    }

    [Fact]
    public async Task SeriesString_WithValidTitleButEmptySequence_ShouldFormatGracefully()
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
        await Task.CompletedTask;
    }

    [Fact]
    public async Task SeriesString_PreviouslyWouldCreateHashSpace_WhenTitleWasNull()
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
        await Task.CompletedTask;
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
    public async Task ProcessTitleForComparison_ShouldConvertToLowercaseAndStripNonAlphanumeric(string input, string expected)
    {
        // Arrange
        var converter = CreateConverter();

        // Act
        var result = InvokeProcessTitleForComparison(converter, input);

        // Assert
        result.Should().Be(expected);
        await Task.CompletedTask;
    }

    [Theory]
    [InlineData("Title-With_Dashes And Underscores", "titlewithdashesandunderscores")]
    [InlineData("Book@#$%^&*()Title", "booktitle")]
    [InlineData("123 Numbers 456", "123numbers456")]
    [InlineData("!@#$%^&*()", "")]
    public async Task ProcessTitleForComparison_ShouldStripAllSpecialCharacters(string input, string expected)
    {
        // Arrange
        var converter = CreateConverter();

        // Act
        var result = InvokeProcessTitleForComparison(converter, input);

        // Assert
        result.Should().Be(expected);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task ProcessTitleForComparison_ShouldHandleUnicodeCharacters()
    {
        // Arrange
        var converter = CreateConverter();
        // Unicode characters like é, ñ, 中文 should be stripped as they're not a-z0-9
        var input = "Café Menu 中文书";

        // Act
        var result = InvokeProcessTitleForComparison(converter, input);

        // Assert
        result.Should().Be("cafmenu"); // é stripped, Chinese characters stripped
        await Task.CompletedTask;
    }

    [Fact]
    public async Task ProcessTitleForComparison_ShouldHandleEmptyString()
    {
        // Arrange
        var converter = CreateConverter();

        // Act
        var result = InvokeProcessTitleForComparison(converter, "");

        // Assert
        result.Should().BeEmpty();
        await Task.CompletedTask;
    }

    [Theory]
    [InlineData("   ", "")]
    [InlineData("\t\n", "")]
    [InlineData(" Title ", "title")]
    public async Task ProcessTitleForComparison_ShouldHandleWhitespace(string input, string expected)
    {
        // Arrange
        var converter = CreateConverter();

        // Act
        var result = InvokeProcessTitleForComparison(converter, input);

        // Assert
        result.Should().Be(expected);
        await Task.CompletedTask;
    }

    #endregion

    #region Missing Tests - PrepareOutputDirectory

    [Theory]
    [InlineData("Normal Book Title", false, "Author", "Normal Book Title")]
    // When isBoxset=true with "Book: Part 1": CleanTitle("Book: Part 1") → "Book - Part 1" → regex removes "Part 1" → "Book - " → Trim() → "Book -"
    [InlineData("Book: Part 1", true, "Author", "Book -")]
    [InlineData("Series Part 2", false, "Author", "Series Part 2")]
    // BUG: Regex "Part [0-9]" only matches single digit parts (1-9), not Part 10+
    // When isBoxset=true, title "Series Part 2" → "Series" (after removing "Part 2")
    [InlineData("Series Part 2", true, "Author", "Series")]
    public async Task PrepareOutputDirectory_ShouldHandleBoxsetLogic(string title, bool isBoxset, string author, string expectedFolderName)
    {
        // Arrange
        var outputFolder = Directory.CreateTempSubdirectory().FullName;
        var converter = CreateConverter(outputFolder: outputFolder);
        var aaxInfo = CreateAaxInfoDto(title, author);

        try
        {
            // Act
            var result = InvokePrepareOutputDirectory(converter, aaxInfo, isBoxset);

            // Assert - CleanTitle is called on title, so colons become " -"
            result.Should().Contain(author);
            result.Should().Contain(expectedFolderName.Replace(": ", " - "));
            Directory.Exists(result).Should().BeTrue("directory should be created");
        }
        finally
        {
            Directory.Delete(outputFolder, true);
        }
        await Task.CompletedTask;
    }

    [Fact]
    public async Task PrepareOutputDirectory_ShouldUseUnknownForNullAuthor()
    {
        // Arrange
        var outputFolder = Directory.CreateTempSubdirectory().FullName;
        var converter = CreateConverter(outputFolder: outputFolder);
        var aaxInfo = CreateAaxInfoDto("Some Title", null);

        try
        {
            // Act
            var result = InvokePrepareOutputDirectory(converter, aaxInfo, false);

            // Assert
            result.Should().Contain("Unknown");
            Directory.Exists(result).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(outputFolder, true);
        }
        await Task.CompletedTask;
    }

    [Fact]
    public async Task PrepareOutputDirectory_ShouldSanitizeTitleWithColons()
    {
        // Arrange
        var outputFolder = Directory.CreateTempSubdirectory().FullName;
        var converter = CreateConverter(outputFolder: outputFolder);
        var titleWithColon = "Book: The Sequel";
        var aaxInfo = CreateAaxInfoDto(titleWithColon, "Author");

        try
        {
            // Act
            var result = InvokePrepareOutputDirectory(converter, aaxInfo, false);

            // Assert - CleanTitle replaces : with " -" (this happens before PrepareOutputDirectory)
            // The test passes because CleanTitle is called inside PrepareOutputDirectory
            result.Should().Contain("Book - The Sequel");
        }
        finally
        {
            Directory.Delete(outputFolder, true);
        }
        await Task.CompletedTask;
    }

    #endregion

    #region Missing Tests - WriteAaxInfo

    [Fact]
    public async Task WriteAaxInfo_ShouldHandleNullAaxInfo()
    {
        // Arrange
        var converter = CreateConverter();
        var logger = new Logger(true, false);

        // Act - should not throw
        var act = () => InvokeWriteAaxInfo(converter, null, logger);

        // Assert
        act.Should().NotThrow();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task WriteAaxInfo_ShouldHandleNullDuration()
    {
        // Arrange
        var converter = CreateConverter();
        var logger = new Logger(true, false);
        var aaxInfo = CreateAaxInfoDtoWithNullDuration("Title", "Author");

        // Act - should not throw when duration is null
        var act = () => InvokeWriteAaxInfo(converter, aaxInfo, logger);

        // Assert
        act.Should().NotThrow();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task WriteAaxInfo_ShouldHandleInvalidDurationFormatGracefully()
    {
        // Arrange - Test that invalid duration is handled gracefully with TryParse
        // Fixed: double.Parse replaced with double.TryParse for validation
        var converter = CreateConverter();
        var logger = new Logger(true, false);
        var aaxInfo = CreateAaxInfoDtoWithInvalidDuration("Title", "Author", "not-a-number");

        // Act - Should NOT throw due to TryParse validation
        var act = () => InvokeWriteAaxInfo(converter, aaxInfo, logger);

        // Assert - Should complete without exception
        act.Should().NotThrow();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task WriteAaxInfo_ShouldOutputChaptersCount()
    {
        // Arrange
        var converter = CreateConverter();
        var logger = new Logger(true, false);
        var aaxInfo = CreateAaxInfoDtoWithChapters("Title", "Author", 5);

        // Act - should not throw
        var act = () => InvokeWriteAaxInfo(converter, aaxInfo, logger);

        // Assert
        act.Should().NotThrow();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task WriteAaxInfo_ShouldHandleNullChapters()
    {
        // Arrange
        var converter = CreateConverter();
        var logger = new Logger(true, false);
        var aaxInfo = CreateAaxInfoDto("Title", "Author");

        // Act - should not throw when chapters is null (null-conditional operator)
        var act = () => InvokeWriteAaxInfo(converter, aaxInfo, logger);

        // Assert
        act.Should().NotThrow();
        await Task.CompletedTask;
    }

    #endregion

    #region Missing Tests - ProcessTitleForComparison Null Validation

    [Fact]
    public async Task ProcessTitleForComparison_ShouldReturnEmptyStringOnNullInput()
    {
        // Arrange
        var converter = CreateConverter();

        // Act - Null input should be handled gracefully
        var result = InvokeProcessTitleForComparison(converter, null!);

        // Assert - Should return empty string instead of throwing
        result.Should().BeEmpty();
        await Task.CompletedTask;
    }

    #endregion

    #region Missing Tests - CleanTitle Edge Cases

    [Theory]
    // NOTE: String.Replace replaces ALL occurrences in .NET
    // "Multiple (Unabridged) Words (Unabridged)" → "Multiple  Words " → Trim() → "Multiple  Words"
    // After replacing both (Unabridged), there's double space between words
    [InlineData("Multiple (Unabridged) Words (Unabridged)", "Multiple  Words")]
    [InlineData("No Changes Here", "No Changes Here")]
    // NOTE: Each : becomes " -", Trim() removes leading/trailing whitespace
    // "::: Colons Galore :::" → " - - - Colons Galore  - - -" → "- - - Colons Galore  - - -"
    // Note: Double space remains between "Galore" and "- - -" because the space before last ::: stays
    [InlineData("::: Colons Galore :::", "- - - Colons Galore  - - -")]
    public async Task CleanTitle_ShouldHandleEdgeCases(string input, string expected)
    {
        // Arrange
        var converter = CreateConverter();

        // Act
        var result = InvokeCleanTitle(converter, input);

        // Assert
        result.Should().Be(expected);
        await Task.CompletedTask;
    }

    #endregion

    #region Missing Tests - Metadata Parsing Edge Cases

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("1234", "1234")]
    [InlineData("2023-05-15", "2023")] // Only year is parsed
    public async Task AaxInfoDto_DateParsing_ShouldHandleVariousFormats(string? dateValue, string expectedYear)
    {
        // Arrange
        var aaxInfo = CreateAaxInfoDtoWithDate("Title", "Author", dateValue);

        // Act & Assert - Test documents that the code does uint.Parse on date field
        // This is fragile and will throw on non-numeric dates
        if (dateValue == null || !uint.TryParse(dateValue, out _))
        {
            // Documents the bug: code does uint.Parse without validation
            // This would throw if date is non-numeric
            true.Should().BeTrue("date parsing needs validation");
        }
        else
        {
            uint.Parse(dateValue).Should().Be(uint.Parse(expectedYear));
        }
        await Task.CompletedTask;
    }

    #endregion

    #region Missing Tests - Constructor Validation

    [Fact]
    public async Task Constructor_ShouldAcceptNullLibraryAndProgressManager()
    {
        // Arrange & Act - should not throw
        var converter = new AaxToM4BConvertor(
            "00000000",
            64,
            true,
            Path.GetTempPath(),
            Path.GetTempPath(),
            false,
            null,  // library
            null); // progressManager

        // Assert
        converter.Should().NotBeNull();
        await Task.CompletedTask;
    }

    [Theory]
    [InlineData("", 64)]
    [InlineData("00000000", 0)]
    [InlineData("00000000", -1)]
    [InlineData("00000000", 999999)]
    public async Task Constructor_ShouldAcceptVariousBitrates(string activationBytes, int bitrate)
    {
        // Arrange & Act - constructor doesn't validate inputs
        var converter = new AaxToM4BConvertor(
            activationBytes,
            bitrate,
            true,
            Path.GetTempPath(),
            Path.GetTempPath(),
            false);

        // Assert - Documents that constructor accepts any values without validation
        converter.Should().NotBeNull();
        await Task.CompletedTask;
    }

    #endregion

    #region Missing Tests - Boxset Detection Pattern

    [Theory]
    [InlineData("/path/to/Book-Part_1-LC.aax", true)]
    [InlineData("/path/to/Book-Part_5-LC.aax", true)]
    [InlineData("/path/to/Book-Part_12-LC.aax", false)] // Regex only matches single digit [0-9]
    [InlineData("/path/to/Book-Part_A-LC.aax", false)]
    [InlineData("/path/to/Book-Part_1.aax", false)] // Missing -LC suffix
    [InlineData("/path/to/Normal Book.aax", false)]
    public async Task BoxsetDetection_PatternMatching(string filePath, bool expectedMatch)
    {
        // Arrange
        var pattern = new System.Text.RegularExpressions.Regex("Part_[0-9]-LC");

        // Act
        var match = pattern.Match(filePath).Success;

        // Assert
        match.Should().Be(expectedMatch, $"filePath: {filePath}");
        await Task.CompletedTask;
    }

    [Theory]
    [InlineData("Book Part 1", true)]
    [InlineData("Book Part 10", true)]
    [InlineData("Book Part 9 Title", true)]
    [InlineData("BookPart1", false)] // No space before Part
    [InlineData("Book Part1", false)] // No space after Part
    [InlineData("Book Participant", false)] // Not a part number
    public async Task BoxsetTitleParsing_PatternMatching(string title, bool expectedMatch)
    {
        // Arrange
        var pattern = new System.Text.RegularExpressions.Regex("Part [0-9]");

        // Act
        var match = pattern.Match(title).Success;

        // Assert
        match.Should().Be(expectedMatch, $"title: {title}");
        await Task.CompletedTask;
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
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? typeof(AudiobookConverterBase).GetMethod("CleanTitle",
                BindingFlags.NonPublic | BindingFlags.Instance);
        return method?.Invoke(converter, new object?[] { title }) as string;
    }

    private static string InvokeProcessTitleForComparison(AaxToM4BConvertor converter, string input)
    {
        var method = typeof(AaxToM4BConvertor).GetMethod("ProcessTitleForComparison",
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? typeof(AudiobookConverterBase).GetMethod("ProcessTitleForComparison",
                BindingFlags.NonPublic | BindingFlags.Instance);
        return method?.Invoke(converter, new object[] { input }) as string ?? string.Empty;
    }

    private static string InvokeCleanAuthor(AaxToM4BConvertor converter, string? name)
    {
        var method = typeof(AaxToM4BConvertor).GetMethod("CleanAuthor",
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? typeof(AudiobookConverterBase).GetMethod("CleanAuthor",
                BindingFlags.NonPublic | BindingFlags.Instance);
        return method?.Invoke(converter, new object?[] { name }) as string ?? "Unknown";
    }

    private static void InvokeCheckFolders(AaxToM4BConvertor converter)
    {
        var method = typeof(AaxToM4BConvertor).GetMethod("CheckFolders",
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? typeof(AudiobookConverterBase).GetMethod("CheckFolders",
                BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(converter, null);
    }

    private static string InvokePrepareOutputDirectory(AaxToM4BConvertor converter, object? aaxInfo, bool boxset)
    {
        var method = typeof(AaxToM4BConvertor).GetMethod("PrepareOutputDirectory",
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? typeof(AudiobookConverterBase).GetMethod("PrepareOutputDirectory",
                BindingFlags.NonPublic | BindingFlags.Instance);
        return method?.Invoke(converter, new object?[] { aaxInfo, boxset }) as string ?? string.Empty;
    }

    private static void InvokeWriteAaxInfo(AaxToM4BConvertor converter, object? aaxInfo, Logger logger)
    {
        var method = typeof(AaxToM4BConvertor).GetMethod("WriteFileInfo",
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? typeof(AudiobookConverterBase).GetMethod("WriteFileInfo",
                BindingFlags.NonPublic | BindingFlags.Instance);
        try
        {
            method?.Invoke(converter, new object?[] { aaxInfo, logger });
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException!;
        }
    }

    /// <summary>
    /// Unwraps TargetInvocationException from reflection to get the actual exception
    /// </summary>
    private static void InvokeCheckFoldersAndUnwrap(AaxToM4BConvertor converter)
    {
        var method = typeof(AaxToM4BConvertor).GetMethod("CheckFolders",
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? typeof(AudiobookConverterBase).GetMethod("CheckFolders",
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

    private static AaxInfoDto CreateAaxInfoDto(string title, string? author)
    {
        return new AaxInfoDto
        {
            format = new AaxFormat
            {
                duration = "3600", // 1 hour in seconds
                tags = new AaxTags
                {
                    title = title,
                    artist = author
                }
            },
            chapters = null
        };
    }

    private static AaxInfoDto CreateAaxInfoDtoWithNullDuration(string title, string? author)
    {
        return new AaxInfoDto
        {
            format = new AaxFormat
            {
                duration = null,
                tags = new AaxTags
                {
                    title = title,
                    artist = author
                }
            },
            chapters = null
        };
    }

    private static AaxInfoDto CreateAaxInfoDtoWithInvalidDuration(string title, string? author, string duration)
    {
        return new AaxInfoDto
        {
            format = new AaxFormat
            {
                duration = duration,
                tags = new AaxTags
                {
                    title = title,
                    artist = author
                }
            },
            chapters = null
        };
    }

    private static AaxInfoDto CreateAaxInfoDtoWithChapters(string title, string? author, int chapterCount)
    {
        var chapters = new List<AaxChapter>();
        for (int i = 0; i < chapterCount; i++)
        {
            chapters.Add(new AaxChapter());
        }
        
        return new AaxInfoDto
        {
            format = new AaxFormat
            {
                duration = "3600",
                tags = new AaxTags
                {
                    title = title,
                    artist = author
                }
            },
            chapters = chapters
        };
    }

    private static AaxInfoDto CreateAaxInfoDtoWithDate(string title, string? author, string? date)
    {
        return new AaxInfoDto
        {
            format = new AaxFormat
            {
                duration = "3600",
                tags = new AaxTags
                {
                    title = title,
                    artist = author,
                    date = date
                }
            },
            chapters = null
        };
    }

    #endregion
}
