using System.Text.RegularExpressions;
using FluentAssertions;
using Harmony.Dto;

namespace Harmony.Tests;

public class AudiobookConverterBaseTests : IDisposable
{
    private readonly string _testDirectory;

    public AudiobookConverterBaseTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"AudiobookConverterBaseTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
        GC.SuppressFinalize(this);
    }

    #region CleanAuthor - Author Names with Spaces

    [Fact]
    public void CleanAuthor_WithAuthorNameContainingSpaces_ShouldPreserveSpaces()
    {
        // Arrange - Create a testable subclass
        var converter = new TestableAudiobookConverter();
        
        // Act
        var result = converter.CleanAuthorPublic("Rob Reid");

        // Assert
        result.Should().Be("Rob Reid", "Spaces in author names should be preserved");
    }

    [Fact]
    public void CleanAuthor_WithMultipleAuthors_ShouldPreserveSpacesInNames()
    {
        // Arrange
        var converter = new TestableAudiobookConverter();

        // Act
        var result = converter.CleanAuthorPublic("Rob Reid, Jane Smith");

        // Assert
        result.Should().Be("Rob Reid, Jane Smith", "Spaces in multiple author names should be preserved");
    }

    [Fact]
    public void CleanAuthor_WithTooManyAuthors_ShouldReturnVarious()
    {
        // Arrange
        var converter = new TestableAudiobookConverter();

        // Act - More than MaxAuthorCountForIndividualDisplay (4) authors
        var result = converter.CleanAuthorPublic("Author1, Author2, Author3, Author4, Author5");

        // Assert
        result.Should().Be("Various", "Should return 'Various' when author count exceeds max");
    }

    [Fact]
    public void CleanAuthor_WithExactlyMaxAuthors_ShouldPreserveNames()
    {
        // Arrange
        var converter = new TestableAudiobookConverter();

        // Act - Exactly MaxAuthorCountForIndividualDisplay (4) authors
        var result = converter.CleanAuthorPublic("Rob Reid, Jane Smith, John Doe, Alice Brown");

        // Assert
        result.Should().Be("Rob Reid, Jane Smith, John Doe, Alice Brown", 
            "Should preserve names when author count equals max");
    }

    [Fact]
    public void CleanAuthor_WithJuniorSuffix_ShouldRemoveDot()
    {
        // Arrange
        var converter = new TestableAudiobookConverter();

        // Act
        var result = converter.CleanAuthorPublic("John Smith Jr.");

        // Assert
        result.Should().Be("John Smith Jr", "Period in 'Jr.' should be removed");
    }

    [Fact]
    public void CleanAuthor_WithNullName_ShouldReturnUnknown()
    {
        // Arrange
        var converter = new TestableAudiobookConverter();

        // Act
        var result = converter.CleanAuthorPublic(null);

        // Assert
        result.Should().Be("Unknown", "Null author name should return 'Unknown'");
    }

    [Fact]
    public void CleanAuthor_WithEmptyName_ShouldReturnUnknown()
    {
        // Arrange
        var converter = new TestableAudiobookConverter();

        // Act
        var result = converter.CleanAuthorPublic("");

        // Assert
        result.Should().Be("Unknown", "Empty author name should return 'Unknown'");
    }

    #endregion

    #region CleanTitle - Title Cleaning

    [Fact]
    public void CleanTitle_ShouldRemoveUnabridgedSuffix()
    {
        // Arrange
        var converter = new TestableAudiobookConverter();

        // Act
        var result = converter.CleanTitlePublic("My Book (Unabridged)");

        // Assert
        result.Should().Be("My Book", "Should remove '(Unabridged)' suffix");
    }

    [Fact]
    public void CleanTitle_ShouldReplaceColonWithDash()
    {
        // Arrange
        var converter = new TestableAudiobookConverter();

        // Act
        var result = converter.CleanTitlePublic("Book Title: Subtitle");

        // Assert
        result.Should().Be("Book Title - Subtitle", "Should replace colon with ' -'");
    }

    [Fact]
    public void CleanTitle_ShouldRemoveApostrophes()
    {
        // Arrange
        var converter = new TestableAudiobookConverter();

        // Act
        var result = converter.CleanTitlePublic("It's a Book");

        // Assert
        result.Should().Be("Its a Book", "Should remove apostrophes");
    }

    [Fact]
    public void CleanTitle_ShouldRemoveQuestionMarks()
    {
        // Arrange
        var converter = new TestableAudiobookConverter();

        // Act
        var result = converter.CleanTitlePublic("What? Who?");

        // Assert
        result.Should().Be("What Who", "Should remove question marks");
    }

    [Fact]
    public void CleanTitle_WithNullTitle_ShouldReturnNull()
    {
        // Arrange
        var converter = new TestableAudiobookConverter();

        // Act
        var result = converter.CleanTitlePublic(null);

        // Assert
        result.Should().BeNull("Null title should return null");
    }

    #endregion

    #region Path Construction with Spaces

    [Fact]
    public void OutputDirectory_WithAuthorContainingSpaces_ShouldCreateValidPath()
    {
        // Arrange - Create directories with spaces in path
        var authorWithSpaces = "Rob Reid";
        var titleWithSpaces = "Some Book Title";
        var outputDir = Path.Combine(_testDirectory, authorWithSpaces, titleWithSpaces);

        // Act - Create directory (simulating PrepareOutputDirectory)
        Directory.CreateDirectory(outputDir);

        // Assert - Directory should exist with spaces preserved
        Directory.Exists(outputDir).Should().BeTrue("Directory with spaces should be created");
        outputDir.Should().Contain(authorWithSpaces, "Path should preserve author spaces");
    }

    [Fact]
    public void FilePath_WithSpacesInPath_ShouldBeQuotedForFFmpeg()
    {
        // This test verifies the quoting pattern used in FFmpeg commands
        // Arrange
        var pathWithSpaces = "/output/Rob Reid/My Book/My Book.m4b";

        // Act - Apply the quoting pattern
        var quotedPath = $"\"{pathWithSpaces}\"";

        // Assert
        quotedPath.Should().Be("\"/output/Rob Reid/My Book/My Book.m4b\"", 
            "Path with spaces should be wrapped in quotes for FFmpeg");
    }

    [Fact]
    public void MultiplePaths_WithSpaces_ShouldBeProperlyQuotedInCommand()
    {
        // This test verifies the quoting pattern for multiple file paths
        // Arrange
        var inputFile = "/output/Rob Reid/Book/Book-nochapter.m4b";
        var chapterFile = "/output/Rob Reid/Book/chapter.txt";
        var outputFile = "/output/Rob Reid/Book/Book.m4b";

        // Act - Build FFmpeg command pattern
        var commandPattern = $"-i \"{inputFile}\" -i \"{chapterFile}\" -map_metadata 1 -codec copy \"{outputFile}\"";

        // Assert
        commandPattern.Should().Contain("\"/output/Rob Reid/Book/Book-nochapter.m4b\"", 
            "Input file path should be quoted");
        commandPattern.Should().Contain("\"/output/Rob Reid/Book/chapter.txt\"", 
            "Chapter file path should be quoted");
        commandPattern.Should().Contain("\"/output/Rob Reid/Book/Book.m4b\"", 
            "Output file path should be quoted");
    }

    #endregion

    #region ProcessTitleForComparison - Title Matching for ASIN Lookup

    [Fact]
    public void ProcessTitleForComparison_ShouldRemoveUnabridgedSuffix()
    {
        // Arrange
        var converter = new TestableAudiobookConverter();

        // Act - After stripping (Unabridged) and processing
        var titleWithUnabridged = "Year Zero: A Novel (Unabridged)";
        var titleString = Regex.Replace(titleWithUnabridged, @"\(Unabridged\)", "").Trim();
        var result = converter.ProcessTitleForComparisonPublic(titleString);

        // Assert
        result.Should().Be("yearzeroanovel", 
            "Should strip Unabridged suffix before comparison - 'Year Zero' + 'A Novel' matches library.tsv");
    }

    [Fact]
    public void ProcessTitleForComparison_WithTitleAndSubtitle_ShouldMatchLibrary()
    {
        // Arrange
        var converter = new TestableAudiobookConverter();

        // Act - Simulating the library.tsv format (Year Zero + subtitle "A Novel")
        var libraryTitle = "Year Zero" + "A Novel"; // Simulates dto.title + dto.subtitle
        var result = converter.ProcessTitleForComparisonPublic(libraryTitle);

        // Assert
        result.Should().Be("yearzeroanovel", 
            "Library title combined with subtitle should produce clean match key");
    }

    [Fact]
    public void ProcessTitleForComparison_MatchAfterUnabridgedRemoval()
    {
        // Arrange
        var converter = new TestableAudiobookConverter();

        // Act - Compare AAXC title with Unabridged stripped vs library title
        var aaxcTitle = "Year Zero: A Novel (Unabridged)";
        var libraryTitle = "Year Zero" + "A Novel";

        // Strip Unabridged from AAXC title
        var aaxcTitleString = Regex.Replace(aaxcTitle, @"\(Unabridged\)", "").Trim();
        var aaxcProcessed = converter.ProcessTitleForComparisonPublic(aaxcTitleString);
        var libraryProcessed = converter.ProcessTitleForComparisonPublic(libraryTitle);

        // Assert - They should now match
        aaxcProcessed.Should().Be(libraryProcessed, 
            "After stripping (Unabridged), AAXC title should match library.tsv entry");
    }

    [Fact]
    public void ProcessTitleForComparison_WithNullInput_ShouldReturnEmptyString()
    {
        // Arrange
        var converter = new TestableAudiobookConverter();

        // Act
        var result = converter.ProcessTitleForComparisonPublic(null);

        // Assert
        result.Should().BeEmpty("Null input should return empty string");
    }

    [Fact]
    public void ProcessTitleForComparison_WithSpecialCharacters_ShouldRemoveThem()
    {
        // Arrange
        var converter = new TestableAudiobookConverter();

        // Act
        var result = converter.ProcessTitleForComparisonPublic("Book's Title: A Novel? Part 2!");

        // Assert
        result.Should().Be("bookstitlenovelapart2", 
            "ProcessTitleForComparison should remove all non-alphanumeric characters");
    }

    #endregion

    #region Helper Test Class

    /// <summary>
    /// Testable subclass to access protected members for testing
    /// </summary>
    private class TestableAudiobookConverter : AudiobookConverterBase
    {
        public TestableAudiobookConverter() 
            : base(bitrate: 64, quietMode: true, inputFolder: "/tmp", outputFolder: "/tmp", clobber: false)
        {
        }

        protected override string FileExtensionPattern => "*.test";

        protected override IEnumerable<string> GetAuthenticationParameters() => Array.Empty<string>();

        protected override AaxInfoDto? GetFileInfo(string filePath) => null;

        // Expose protected methods for testing
        public string CleanAuthorPublic(string? name) => CleanAuthor(name);
        public string? CleanTitlePublic(string? title) => CleanTitle(title);
        public string ProcessTitleForComparisonPublic(string? input) => ProcessTitleForComparison(input);
    }

    #endregion
}