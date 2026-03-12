using System.Reflection;
using FluentAssertions;

namespace Harmony.Tests;

public class ProgressContextManagerTests : IDisposable
{
    #region Constructor Tests

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(100)]
    public void Constructor_WithValidTotalFiles_ShouldInitializeCorrectly(int totalFiles)
    {
        // Arrange & Act
        var manager = new ProgressContextManager(totalFiles);

        // Assert
        manager.TotalFiles.Should().Be(totalFiles);
        manager.CurrentFileIndex.Should().Be(0);
        manager.CurrentBookTitle.Should().Be("Starting...");
        manager.IsCancelled.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Constructor_WithEdgeCaseTotalFiles_ShouldInitializeCorrectly(int totalFiles)
    {
        // Arrange & Act - constructor doesn't validate, just stores
        var manager = new ProgressContextManager(totalFiles);

        // Assert
        manager.TotalFiles.Should().Be(totalFiles);
        manager.CurrentFileIndex.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithCancellationToken_ShouldStoreToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        var manager = new ProgressContextManager(5, cts.Token);

        // Assert
        manager.IsCancelled.Should().BeFalse();

        // Cancel and verify
        cts.Cancel();
        manager.IsCancelled.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithPreCancelledToken_ShouldReportCancelled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var manager = new ProgressContextManager(5, cts.Token);

        // Assert
        manager.IsCancelled.Should().BeTrue();
    }

    #endregion

    #region FormatBookTitle Tests

    [Theory]
    [InlineData("Book_Title.aax", "Book Title")]
    [InlineData("My_Book_Title.m4b", "My Book Title")]
    [InlineData("SimpleBook.aaxc", "SimpleBook")]
    public void FormatBookTitle_ShouldReplaceUnderscoresWithSpaces(string fileName, string expected)
    {
        // Arrange
        var manager = new ProgressContextManager(1);

        // Act
        var result = InvokeFormatBookTitle(fileName);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Book-AAX_64_128.aax", "Book")]
    [InlineData("Book-AAXC_64_128.aax", "Book")]
    [InlineData("Book-AAX_32_64.aaxc", "Book")]
    [InlineData("Book_AAX_64_128.aax", "Book AAX 64 128")] // Underscore doesn't match regex, remains
    [InlineData("Book_AAXC_64_128.aax", "Book AAXC 64 128")] // Underscore doesn't match regex, remains
    public void FormatBookTitle_ShouldRemoveBitrateSuffix(string fileName, string expected)
    {
        // Arrange
        var manager = new ProgressContextManager(1);

        // Act
        var result = InvokeFormatBookTitle(fileName);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Book_Title-AAX_64_128.aax", "Book Title")]
    [InlineData("My_Book-AAXC_32_64.aaxc", "My Book")]
    [InlineData("The_Great_Book-AAX_128_256.m4b", "The Great Book")]
    [InlineData("My_Book_AAXC_32_64.aaxc", "My Book AAXC 32 64")] // Underscore doesn't match regex, remains
    public void FormatBookTitle_ShouldHandleCombinedPatterns(string fileName, string expected)
    {
        // Arrange
        var manager = new ProgressContextManager(1);

        // Act
        var result = InvokeFormatBookTitle(fileName);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Book.m4b", "Book")]
    [InlineData("Book.mp3", "Book")]
    [InlineData("Book.txt", "Book")]
    public void FormatBookTitle_ShouldRemoveAnyExtension(string fileName, string expected)
    {
        // Arrange
        var manager = new ProgressContextManager(1);

        // Act
        var result = InvokeFormatBookTitle(fileName);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Book (Unabridged).aax", "Book (Unabridged)")]
    [InlineData("Book: The Sequel.m4b", "Book: The Sequel")]
    [InlineData("Book-Part 1.aax", "Book-Part 1")]
    public void FormatBookTitle_ShouldPreserveOtherSpecialCharacters(string fileName, string expected)
    {
        // Arrange
        var manager = new ProgressContextManager(1);

        // Act
        var result = InvokeFormatBookTitle(fileName);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void FormatBookTitle_WithEmptyString_ShouldReturnEmpty()
    {
        // Arrange
        var manager = new ProgressContextManager(1);

        // Act
        var result = InvokeFormatBookTitle("");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void FormatBookTitle_WithOnlyExtension_ShouldReturnEmpty()
    {
        // Arrange
        var manager = new ProgressContextManager(1);

        // Act
        var result = InvokeFormatBookTitle(".aax");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void FormatBookTitle_WithJustUnderscores_ShouldReturnSpaces()
    {
        // Arrange
        var manager = new ProgressContextManager(1);

        // Act
        var result = InvokeFormatBookTitle("___");

        // Assert
        result.Should().Be("   ");
    }

    #endregion

    #region StartNewFile Tests

    [Fact]
    public void StartNewFile_ShouldIncrementCurrentFileIndex()
    {
        // Arrange
        var manager = new ProgressContextManager(5);

        // Act
        manager.StartNewFile("Book_1.aax");

        // Assert
        manager.CurrentFileIndex.Should().Be(1);
    }

    [Fact]
    public void StartNewFile_MultipleCalls_ShouldIncrementEachTime()
    {
        // Arrange
        var manager = new ProgressContextManager(5);

        // Act
        manager.StartNewFile("Book_1.aax");
        manager.StartNewFile("Book_2.aax");
        manager.StartNewFile("Book_3.aax");

        // Assert
        manager.CurrentFileIndex.Should().Be(3);
    }

    [Theory]
    [InlineData("Book_Title.aax", "Book Title")]
    [InlineData("Another_Book.m4b", "Another Book")]
    public void StartNewFile_ShouldUpdateCurrentBookTitle(string fileName, string expectedTitle)
    {
        // Arrange
        var manager = new ProgressContextManager(1);

        // Act
        manager.StartNewFile(fileName);

        // Assert
        manager.CurrentBookTitle.Should().Be(expectedTitle);
    }

    [Fact]
    public void StartNewFile_WithCancelledToken_ShouldThrowOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var manager = new ProgressContextManager(5, cts.Token);

        // Act
        Action act = () => manager.StartNewFile("Book.aax");

        // Assert
        act.Should().Throw<OperationCanceledException>();
    }

    #endregion

    #region CompleteFile Tests

    [Fact]
    public void CompleteFile_WithCancelledToken_ShouldThrowOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var manager = new ProgressContextManager(5, cts.Token);

        // Act
        Action act = () => manager.CompleteFile();

        // Assert
        act.Should().Throw<OperationCanceledException>();
    }

    #endregion

    #region IsCancelled Tests

    [Fact]
    public void IsCancelled_WithDefaultToken_ShouldBeFalse()
    {
        // Arrange
        var manager = new ProgressContextManager(5);

        // Assert
        manager.IsCancelled.Should().BeFalse();
    }

    [Fact]
    public void IsCancelled_WithActiveToken_ShouldBeFalse()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var manager = new ProgressContextManager(5, cts.Token);

        // Assert
        manager.IsCancelled.Should().BeFalse();
    }

    [Fact]
    public void IsCancelled_AfterTokenCancellation_ShouldBeTrue()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var manager = new ProgressContextManager(5, cts.Token);

        // Act
        cts.Cancel();

        // Assert
        manager.IsCancelled.Should().BeTrue();
    }

    #endregion

    #region ThrowIfCancellationRequested Tests

    [Fact]
    public void ThrowIfCancellationRequested_WithDefaultToken_ShouldNotThrow()
    {
        // Arrange
        var manager = new ProgressContextManager(5);

        // Act
        Action act = () => manager.ThrowIfCancellationRequested();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ThrowIfCancellationRequested_WithActiveToken_ShouldNotThrow()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var manager = new ProgressContextManager(5, cts.Token);

        // Act
        Action act = () => manager.ThrowIfCancellationRequested();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ThrowIfCancellationRequested_WithCancelledToken_ShouldThrowOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var manager = new ProgressContextManager(5, cts.Token);

        // Act
        Action act = () => manager.ThrowIfCancellationRequested();

        // Assert
        act.Should().Throw<OperationCanceledException>();
    }

    [Fact]
    public void ThrowIfCancellationRequested_CalledMultipleTimesAfterCancel_ShouldThrowEachTime()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var manager = new ProgressContextManager(5, cts.Token);

        // Act & Assert - First call
        manager.Invoking(m => m.ThrowIfCancellationRequested())
            .Should().Throw<OperationCanceledException>();

        // Act & Assert - Second call
        manager.Invoking(m => m.ThrowIfCancellationRequested())
            .Should().Throw<OperationCanceledException>();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void TotalFiles_ShouldReturnConstructorValue()
    {
        // Arrange
        const int expectedTotal = 42;
        var manager = new ProgressContextManager(expectedTotal);

        // Assert
        manager.TotalFiles.Should().Be(expectedTotal);
    }

    [Fact]
    public void TotalFiles_ShouldBeImmutable()
    {
        // Arrange
        var manager = new ProgressContextManager(5);
        var initialTotal = manager.TotalFiles;

        // Act - start a file
        manager.StartNewFile("book.aax");

        // Assert - TotalFiles shouldn't change
        manager.TotalFiles.Should().Be(initialTotal);
    }

    [Fact]
    public void CurrentBookTitle_Initially_ShouldBeStarting()
    {
        // Arrange
        var manager = new ProgressContextManager(5);

        // Assert
        manager.CurrentBookTitle.Should().Be("Starting...");
    }

    [Fact]
    public void CurrentBookTitle_AfterStartNewFile_ShouldBeFormattedFileName()
    {
        // Arrange
        var manager = new ProgressContextManager(5);

        // Act
        manager.StartNewFile("My_Book_Title.aax");

        // Assert
        manager.CurrentBookTitle.Should().Be("My Book Title");
    }

    #endregion

    #region Dispose Pattern Tests

    [Fact]
    public void Dispose_SingleCall_ShouldNotThrow()
    {
        // Arrange
        var manager = new ProgressContextManager(5);

        // Act
        Action act = () => manager.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_MultipleCalls_ShouldNotThrow()
    {
        // Arrange
        var manager = new ProgressContextManager(5);

        // Act
        manager.Dispose();
        Action act = () => manager.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_ShouldSetIsDisposedFlag()
    {
        // Arrange
        var manager = new ProgressContextManager(5);

        // Get private field via reflection
        var field = typeof(ProgressContextManager).GetField("_isDisposed",
            BindingFlags.NonPublic | BindingFlags.Instance);
        field.Should().NotBeNull();

        // Act
        manager.Dispose();

        // Assert
        var isDisposed = (bool)field!.GetValue(manager)!;
        isDisposed.Should().BeTrue();
    }

    [Fact]
    public void Dispose_AfterStartNewFile_ShouldNotThrow()
    {
        // Arrange
        var manager = new ProgressContextManager(5);
        manager.StartNewFile("book.aax");

        // Act
        Action act = () => manager.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void FullWorkflow_SingleFile_ShouldTrackCorrectly()
    {
        // Arrange
        var manager = new ProgressContextManager(1);

        // Act & Assert
        manager.CurrentFileIndex.Should().Be(0);
        manager.CurrentBookTitle.Should().Be("Starting...");

        manager.StartNewFile("My_Book.aax");
        manager.CurrentFileIndex.Should().Be(1);
        manager.CurrentBookTitle.Should().Be("My Book");

        // Complete doesn't change index, just progress
        manager.CompleteFile();
        manager.CurrentFileIndex.Should().Be(1);
    }

    [Fact]
    public void FullWorkflow_MultipleFiles_ShouldTrackProgress()
    {
        // Arrange
        var manager = new ProgressContextManager(3);

        // File 1
        manager.StartNewFile("Book_One.aax");
        manager.CurrentFileIndex.Should().Be(1);
        manager.CurrentBookTitle.Should().Be("Book One");
        manager.CompleteFile();

        // File 2
        manager.StartNewFile("Book_Two.m4b");
        manager.CurrentFileIndex.Should().Be(2);
        manager.CurrentBookTitle.Should().Be("Book Two");
        manager.CompleteFile();

        // File 3
        manager.StartNewFile("Book_Three.aaxc");
        manager.CurrentFileIndex.Should().Be(3);
        manager.CurrentBookTitle.Should().Be("Book Three");
        manager.CompleteFile();

        // Verify final state
        manager.TotalFiles.Should().Be(3);
        manager.CurrentFileIndex.Should().Be(3);
    }

    [Fact]
    public void Cancellation_Flow_ShouldWorkCorrectly()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var manager = new ProgressContextManager(5, cts.Token);

        // Initially not cancelled
        manager.IsCancelled.Should().BeFalse();

        // Cancel token
        cts.Cancel();

        // Now cancelled
        manager.IsCancelled.Should().BeTrue();
        manager.Invoking(m => m.ThrowIfCancellationRequested()).Should().Throw<OperationCanceledException>();
        manager.Invoking(m => m.StartNewFile("book.aax")).Should().Throw<OperationCanceledException>();
        manager.Invoking(m => m.CompleteFile()).Should().Throw<OperationCanceledException>();
    }

    [Fact]
    public void IDisposable_ImplementsInterface()
    {
        // Arrange
        var manager = new ProgressContextManager(5);

        // Assert
        manager.Should().BeAssignableTo<IDisposable>();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void StartNewFile_WithEmptyFileName_ShouldHandleGracefully()
    {
        // Arrange
        var manager = new ProgressContextManager(1);

        // Act
        manager.StartNewFile("");

        // Assert
        manager.CurrentFileIndex.Should().Be(1);
        manager.CurrentBookTitle.Should().BeEmpty();
    }

    [Fact]
    public void StartNewFile_WithOnlyUnderscores_ShouldReturnSpaces()
    {
        // Arrange
        var manager = new ProgressContextManager(1);

        // Act
        manager.StartNewFile("___");

        // Assert
        manager.CurrentBookTitle.Should().Be("   ");
    }

    [Fact]
    public void StartNewFile_WithLongFileName_ShouldHandleCorrectly()
    {
        // Arrange
        var manager = new ProgressContextManager(1);
        var longName = string.Join("_", Enumerable.Repeat("VeryLongBookTitle", 100)) + ".aax";

        // Act
        manager.StartNewFile(longName);

        // Assert
        manager.CurrentBookTitle.Should().Contain("VeryLongBookTitle");
        manager.CurrentFileIndex.Should().Be(1);
    }

    [Fact]
    public void CompleteFile_CalledBeforeStartNewFile_ShouldWork()
    {
        // Arrange
        var manager = new ProgressContextManager(5);

        // Act - CompleteFile doesn't throw even if no file started
        Action act = () => manager.CompleteFile();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void StartNewFile_ExceedingTotalFiles_ShouldStillIncrement()
    {
        // Arrange
        var manager = new ProgressContextManager(2);

        // Act
        manager.StartNewFile("Book1.aax");
        manager.StartNewFile("Book2.aax");
        manager.StartNewFile("Book3.aax"); // Exceeds TotalFiles

        // Assert - CurrentFileIndex still increments
        manager.CurrentFileIndex.Should().Be(3);
    }

    #endregion

    #region Helper Methods

    private static string InvokeFormatBookTitle(string fileName)
    {
        var method = typeof(ProgressContextManager).GetMethod("FormatBookTitle",
            BindingFlags.NonPublic | BindingFlags.Static);
        return method?.Invoke(null, new object[] { fileName }) as string ?? string.Empty;
    }

    #endregion

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
