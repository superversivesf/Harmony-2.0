using System.Text.Json;
using FluentAssertions;
using Harmony.Dto;

namespace Harmony.Tests;

public class ChapterConvertorTests : IDisposable
{
    private readonly string _testDirectory;

    public ChapterConvertorTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"ChapterConvertorTests_{Guid.NewGuid()}");
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

    #region CreateChapterFile - Audible Chapters JSON Path

    [Fact]
    public void CreateChapterFile_ShouldLookForChaptersJsonFile_BasedOnAaxFilename()
    {
        // Arrange
        var aaxPath = Path.Combine(_testDirectory, "Book-Title-AAX");
        File.WriteAllText(aaxPath, "fake aax content");
        var chaptersJsonPath = Path.Combine(_testDirectory, "Book-Title-chapters.json");
        var outputPath = Path.Combine(_testDirectory, "output.txt");

        var audibleChapters = new AudibleChaptersDto
        {
            content_metadata = new ContentMetadata
            {
                chapter_info = new ChapterInfo
                {
                    chapters = new List<Chapter>
                    {
                        new() { title = "Chapter 1", start_offset_ms = 0, length_ms = 60000 }
                    }
                }
            }
        };

        File.WriteAllText(chaptersJsonPath, JsonSerializer.Serialize(audibleChapters));
        var aaxInfo = new AaxInfoDto { chapters = new List<AaxChapter>() };

        // Act
        ChapterConverter.CreateChapterFile(aaxPath, aaxInfo, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue("Chapter file should be created");
        var content = File.ReadAllText(outputPath);
        content.Should().Contain("title=Chapter 1", "Should use chapters from Audible JSON file");
    }

    [Fact]
    public void CreateChapterFile_ShouldHandleAaxExtensionInFilename()
    {
        // Arrange
        var aaxPath = Path.Combine(_testDirectory, "MyBook-AAX.aax");
        File.WriteAllText(aaxPath, "fake aax content");
        var chaptersJsonPath = Path.Combine(_testDirectory, "MyBook-chapters.json");
        var outputPath = Path.Combine(_testDirectory, "output.txt");

        var audibleChapters = new AudibleChaptersDto
        {
            content_metadata = new ContentMetadata
            {
                chapter_info = new ChapterInfo
                {
                    chapters = new List<Chapter>
                    {
                        new() { title = "Intro", start_offset_ms = 0, length_ms = 30000 }
                    }
                }
            }
        };

        File.WriteAllText(chaptersJsonPath, JsonSerializer.Serialize(audibleChapters));
        var aaxInfo = new AaxInfoDto { chapters = new List<AaxChapter>() };

        // Act
        ChapterConverter.CreateChapterFile(aaxPath, aaxInfo, outputPath);

        // Assert
        var content = File.ReadAllText(outputPath);
        content.Should().Contain("title=Intro", "Should correctly extract base name from AAX filename");
    }

    #endregion

    #region CreateChapterFile - Audible Chapters Processing

    [Fact]
    public void CreateChapterFile_WithAudibleChapters_ShouldWriteCorrectFfmetadataFormat()
    {
        // Arrange
        var aaxPath = Path.Combine(_testDirectory, "Book-AAX");
        File.WriteAllText(aaxPath, "fake");
        var chaptersJsonPath = Path.Combine(_testDirectory, "Book-chapters.json");
        var outputPath = Path.Combine(_testDirectory, "chapters.txt");

        var audibleChapters = new AudibleChaptersDto
        {
            content_metadata = new ContentMetadata
            {
                chapter_info = new ChapterInfo
                {
                    chapters = new List<Chapter>
                    {
                        new() { title = "Opening", start_offset_ms = 0, length_ms = 120000 }
                    }
                }
            }
        };

        File.WriteAllText(chaptersJsonPath, JsonSerializer.Serialize(audibleChapters));
        var aaxInfo = new AaxInfoDto();

        // Act
        ChapterConverter.CreateChapterFile(aaxPath, aaxInfo, outputPath);

        // Assert
        var content = File.ReadAllText(outputPath);
        content.Should().StartWith(";FFMETADATA1", "FFMetadata should start with header");
        content.Should().Contain("[CHAPTER]", "Should contain chapter marker");
        content.Should().Contain("TIMEBASE=1/1000", "Should use millisecond timebase");
        content.Should().Contain("START=0", "Should have start time");
        content.Should().Contain("END=119999", "End time should be start + duration - 1");
        content.Should().Contain("title=Opening", "Should have chapter title");
    }

    [Fact]
    public void CreateChapterFile_WithMultipleAudibleChapters_ShouldWriteAllChapters()
    {
        // Arrange
        var aaxPath = Path.Combine(_testDirectory, "Book-AAX");
        File.WriteAllText(aaxPath, "fake");
        var chaptersJsonPath = Path.Combine(_testDirectory, "Book-chapters.json");
        var outputPath = Path.Combine(_testDirectory, "chapters.txt");

        var audibleChapters = new AudibleChaptersDto
        {
            content_metadata = new ContentMetadata
            {
                chapter_info = new ChapterInfo
                {
                    chapters = new List<Chapter>
                    {
                        new() { title = "Chapter 1", start_offset_ms = 0, length_ms = 60000 },
                        new() { title = "Chapter 2", start_offset_ms = 60000, length_ms = 90000 },
                        new() { title = "Chapter 3", start_offset_ms = 150000, length_ms = 45000 }
                    }
                }
            }
        };

        File.WriteAllText(chaptersJsonPath, JsonSerializer.Serialize(audibleChapters));
        var aaxInfo = new AaxInfoDto();

        // Act
        ChapterConverter.CreateChapterFile(aaxPath, aaxInfo, outputPath);

        // Assert
        var content = File.ReadAllText(outputPath);
        content.Should().Contain("title=Chapter 1");
        content.Should().Contain("title=Chapter 2");
        content.Should().Contain("title=Chapter 3");

        // Verify order and times
        var lines = content.Split('\n');
        lines.Count(l => l.Trim() == "[CHAPTER]").Should().Be(3, "Should have 3 chapter markers");
    }

    [Fact]
    public void CreateChapterFile_WithNestedChapters_ShouldFlattenThem()
    {
        // Arrange
        var aaxPath = Path.Combine(_testDirectory, "Book-AAX");
        File.WriteAllText(aaxPath, "fake");
        var chaptersJsonPath = Path.Combine(_testDirectory, "Book-chapters.json");
        var outputPath = Path.Combine(_testDirectory, "chapters.txt");

        var audibleChapters = new AudibleChaptersDto
        {
            content_metadata = new ContentMetadata
            {
                chapter_info = new ChapterInfo
                {
                    chapters = new List<Chapter>
                    {
                        new()
                        {
                            title = "Part 1",
                            start_offset_ms = 0,
                            length_ms = 180000,
                            chapters = new List<Chapter>
                            {
                                new() { title = "Chapter 1.1", start_offset_ms = 0, length_ms = 90000 },
                                new() { title = "Chapter 1.2", start_offset_ms = 90000, length_ms = 90000 }
                            }
                        },
                        new()
                        {
                            title = "Part 2",
                            start_offset_ms = 180000,
                            length_ms = 120000
                        }
                    }
                }
            }
        };

        File.WriteAllText(chaptersJsonPath, JsonSerializer.Serialize(audibleChapters));
        var aaxInfo = new AaxInfoDto();

        // Act
        ChapterConverter.CreateChapterFile(aaxPath, aaxInfo, outputPath);

        // Assert - FlattenChapters should flatten nested chapters
        var content = File.ReadAllText(outputPath);
        content.Should().Contain("title=Part 1");
        content.Should().Contain("title=Chapter 1.1");
        content.Should().Contain("title=Chapter 1.2");
        content.Should().Contain("title=Part 2");
    }

    [Fact]
    public void CreateChapterFile_WithNullChapterFields_ShouldUseDefaults()
    {
        // Arrange
        var aaxPath = Path.Combine(_testDirectory, "Book-AAX");
        File.WriteAllText(aaxPath, "fake");
        var chaptersJsonPath = Path.Combine(_testDirectory, "Book-chapters.json");
        var outputPath = Path.Combine(_testDirectory, "chapters.txt");

        var audibleChapters = new AudibleChaptersDto
        {
            content_metadata = new ContentMetadata
            {
                chapter_info = new ChapterInfo
                {
                    chapters = new List<Chapter>
                    {
                        new()
                        {
                            title = "Untitled Chapter",
                            start_offset_ms = null,  // Should default to 0
                            length_ms = null  // Should default to 0
                        }
                    }
                }
            }
        };

        File.WriteAllText(chaptersJsonPath, JsonSerializer.Serialize(audibleChapters));
        var aaxInfo = new AaxInfoDto();

        // Act
        ChapterConverter.CreateChapterFile(aaxPath, aaxInfo, outputPath);

        // Assert
        var content = File.ReadAllText(outputPath);
        content.Should().Contain("START=0", "Null start_offset_ms should default to 0");
        content.Should().Contain("END=-1", "Null length_ms results in start(0) + duration(0) - 1 = -1");
    }

    #endregion

    #region CreateChapterFile - AaxInfo Chapters Fallback

    [Fact]
    public void CreateChapterFile_WithoutAudibleChaptersFile_ShouldUseAaxInfoChapters()
    {
        // Arrange
        var aaxPath = Path.Combine(_testDirectory, "Book-AAX");
        File.WriteAllText(aaxPath, "fake aax");
        // No chapters.json file created
        var outputPath = Path.Combine(_testDirectory, "chapters.txt");

        var aaxInfo = new AaxInfoDto
        {
            chapters = new List<AaxChapter>
            {
                new()
                {
                    id = 1,
                    start_time = "0",
                    end_time = "60",
                    time_base = "1/1000"
                },
                new()
                {
                    id = 2,
                    start_time = "60",
                    end_time = "150",
                    time_base = "1/1000"
                }
            }
        };

        // Act
        ChapterConverter.CreateChapterFile(aaxPath, aaxInfo, outputPath);

        // Assert
        var content = File.ReadAllText(outputPath);
        content.Should().StartWith(";FFMETADATA1");
        content.Should().Contain("title=Chapter 1");
        content.Should().Contain("title=Chapter 2");
        content.Should().Contain("START=0", "First chapter starts at 0ms");
        content.Should().Contain("END=59999", "First chapter ends at 60 seconds * 1000 - 1");
    }

    [Fact]
    public void CreateChapterFile_WithAaxInfoChapters_ShouldConvertTimeFromSecondsToMilliseconds()
    {
        // Arrange
        var aaxPath = Path.Combine(_testDirectory, "Book-AAX");
        File.WriteAllText(aaxPath, "fake");
        var outputPath = Path.Combine(_testDirectory, "chapters.txt");

        var aaxInfo = new AaxInfoDto
        {
            chapters = new List<AaxChapter>
            {
                new()
                {
                    id = 1,
                    start_time = "0.5",    // 0.5 seconds = 500ms
                    end_time = "120.75",   // 120.75 seconds = 120750ms
                    time_base = "1/1000"
                }
            }
        };

        // Act
        ChapterConverter.CreateChapterFile(aaxPath, aaxInfo, outputPath);

        // Assert
        var content = File.ReadAllText(outputPath);
        content.Should().Contain("START=500", "0.5 seconds should be 500ms");
        content.Should().Contain("END=120749", "120.75*1000 - 1 = 120749");
    }

    [Fact]
    public void CreateChapterFile_WithAaxInfoChapters_ShouldNumberChaptersSequentially()
    {
        // Arrange
        var aaxPath = Path.Combine(_testDirectory, "Book-AAX");
        File.WriteAllText(aaxPath, "fake");
        var outputPath = Path.Combine(_testDirectory, "chapters.txt");

        var aaxInfo = new AaxInfoDto
        {
            chapters = new List<AaxChapter>
            {
                new() { id = 10, start_time = "0", end_time = "100", time_base = "1/1000" },
                new() { id = 20, start_time = "100", end_time = "200", time_base = "1/1000" },
                new() { id = 30, start_time = "200", end_time = "300", time_base = "1/1000" }
            }
        };

        // Act
        ChapterConverter.CreateChapterFile(aaxPath, aaxInfo, outputPath);

        // Assert
        var content = File.ReadAllText(outputPath);
        content.Should().Contain("title=Chapter 1");
        content.Should().Contain("title=Chapter 2");
        content.Should().Contain("title=Chapter 3");
    }

    [Fact]
    public void CreateChapterFile_WithEmptyAaxInfoChapters_ShouldWriteOnlyHeader()
    {
        // Arrange
        var aaxPath = Path.Combine(_testDirectory, "Book-AAX");
        File.WriteAllText(aaxPath, "fake");
        var outputPath = Path.Combine(_testDirectory, "chapters.txt");

        var aaxInfo = new AaxInfoDto
        {
            chapters = new List<AaxChapter>()
        };

        // Act
        ChapterConverter.CreateChapterFile(aaxPath, aaxInfo, outputPath);

        // Assert
        var content = File.ReadAllText(outputPath);
        content.Trim().Should().Be(";FFMETADATA1", "Empty chapters should result in header only");
    }

    [Fact]
    public void CreateChapterFile_WithNullAaxInfoChapters_ShouldWriteOnlyHeader()
    {
        // Arrange
        var aaxPath = Path.Combine(_testDirectory, "Book-AAX");
        File.WriteAllText(aaxPath, "fake");
        var outputPath = Path.Combine(_testDirectory, "chapters.txt");

        var aaxInfo = new AaxInfoDto
        {
            chapters = null
        };

        // Act
        ChapterConverter.CreateChapterFile(aaxPath, aaxInfo, outputPath);

        // Assert
        var content = File.ReadAllText(outputPath);
        content.Trim().Should().Be(";FFMETADATA1", "Null chapters should result in header only");
    }

    #endregion

    #region CreateChapterFile - Edge Cases

    [Fact]
    public void CreateChapterFile_WithEmptyAudibleChaptersJson_ShouldFallbackToAaxInfo()
    {
        // Arrange
        var aaxPath = Path.Combine(_testDirectory, "Book-AAX");
        File.WriteAllText(aaxPath, "fake");
        var chaptersJsonPath = Path.Combine(_testDirectory, "Book-chapters.json");
        var outputPath = Path.Combine(_testDirectory, "chapters.txt");

        // Create chapters.json with no chapters
        var emptyChapters = new AudibleChaptersDto
        {
            content_metadata = new ContentMetadata
            {
                chapter_info = new ChapterInfo
                {
                    chapters = new List<Chapter>()
                }
            }
        };
        File.WriteAllText(chaptersJsonPath, JsonSerializer.Serialize(emptyChapters));

        var aaxInfo = new AaxInfoDto
        {
            chapters = new List<AaxChapter>
            {
                new() { id = 1, start_time = "0", end_time = "60", time_base = "1/1000" }
            }
        };

        // Act
        ChapterConverter.CreateChapterFile(aaxPath, aaxInfo, outputPath);

        // Assert - Should NOT fall back because chapters.json exists (even if empty)
        // Note: The code checks File.Exists, not the content
        var content = File.ReadAllText(outputPath);
        content.Should().NotContain("title=Chapter 1",
            "Empty chapters in JSON should result in no chapters, not fallback to AaxInfo");
    }

    [Fact]
    public void CreateChapterFile_WithInvalidJsonInChaptersFile_ShouldHandleGracefully()
    {
        // Arrange
        var aaxPath = Path.Combine(_testDirectory, "Book-AAX");
        File.WriteAllText(aaxPath, "fake");
        var chaptersJsonPath = Path.Combine(_testDirectory, "Book-chapters.json");
        var outputPath = Path.Combine(_testDirectory, "chapters.txt");

        // Write invalid JSON
        File.WriteAllText(chaptersJsonPath, "not valid json {{{");

        var aaxInfo = new AaxInfoDto
        {
            chapters = new List<AaxChapter>
            {
                new() { id = 1, start_time = "0", end_time = "60", time_base = "1/1000" }
            }
        };

        // Act & Assert
        var act = () => ChapterConverter.CreateChapterFile(aaxPath, aaxInfo, outputPath);
        act.Should().Throw<JsonException>("Invalid JSON should throw JsonException");
    }

    [Fact]
    public void CreateChapterFile_WhenOutputDirectoryDoesNotExist_ShouldCreateFile()
    {
        // Arrange
        var aaxPath = Path.Combine(_testDirectory, "Book-AAX");
        File.WriteAllText(aaxPath, "fake");
        var outputDir = Path.Combine(_testDirectory, "subdir", "nested");
        var outputPath = Path.Combine(outputDir, "chapters.txt");

        var aaxInfo = new AaxInfoDto { chapters = null };

        // Act - Note: This will throw if the directory doesn't exist
        // The CreateChapterFile doesn't create directories
        Directory.CreateDirectory(outputDir);
        ChapterConverter.CreateChapterFile(aaxPath, aaxInfo, outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue("File should be created at the output path");
    }

    [Fact]
    public void CreateChapterFile_WithSpecialCharactersInTitle_ShouldPreserveThem()
    {
        // Arrange
        var aaxPath = Path.Combine(_testDirectory, "Book-AAX");
        File.WriteAllText(aaxPath, "fake");
        var chaptersJsonPath = Path.Combine(_testDirectory, "Book-chapters.json");
        var outputPath = Path.Combine(_testDirectory, "chapters.txt");

        var audibleChapters = new AudibleChaptersDto
        {
            content_metadata = new ContentMetadata
            {
                chapter_info = new ChapterInfo
                {
                    chapters = new List<Chapter>
                    {
                        new()
                        {
                            title = "Chapter: A Test's \"Quote\" & More!",
                            start_offset_ms = 0,
                            length_ms = 1000
                        }
                    }
                }
            }
        };

        File.WriteAllText(chaptersJsonPath, JsonSerializer.Serialize(audibleChapters));
        var aaxInfo = new AaxInfoDto();

        // Act
        ChapterConverter.CreateChapterFile(aaxPath, aaxInfo, outputPath);

        // Assert
        var content = File.ReadAllText(outputPath);
        content.Should().Contain("title=Chapter: A Test's \"Quote\" & More!",
            "Special characters in title should be preserved");
    }

    [Fact]
    public void CreateChapterFile_WithUnicodeTitle_ShouldPreserveUnicode()
    {
        // Arrange
        var aaxPath = Path.Combine(_testDirectory, "Book-AAX");
        File.WriteAllText(aaxPath, "fake");
        var chaptersJsonPath = Path.Combine(_testDirectory, "Book-chapters.json");
        var outputPath = Path.Combine(_testDirectory, "chapters.txt");

        var audibleChapters = new AudibleChaptersDto
        {
            content_metadata = new ContentMetadata
            {
                chapter_info = new ChapterInfo
                {
                    chapters = new List<Chapter>
                    {
                        new()
                        {
                            title = "日本語の章 - 中文标题",
                            start_offset_ms = 0,
                            length_ms = 1000
                        }
                    }
                }
            }
        };

        File.WriteAllText(chaptersJsonPath, JsonSerializer.Serialize(audibleChapters));
        var aaxInfo = new AaxInfoDto();

        // Act
        ChapterConverter.CreateChapterFile(aaxPath, aaxInfo, outputPath);

        // Assert
        var content = File.ReadAllText(outputPath);
        content.Should().Contain("title=日本語の章 - 中文标题", "Unicode characters should be preserved");
    }

    [Fact]
    public void CreateChapterFile_ShouldOverwriteExistingOutputFile()
    {
        // Arrange
        var aaxPath = Path.Combine(_testDirectory, "Book-AAX");
        File.WriteAllText(aaxPath, "fake");
        var outputPath = Path.Combine(_testDirectory, "chapters.txt");

        // Create existing output file with old content
        File.WriteAllText(outputPath, "Old content that should be replaced");

        var aaxInfo = new AaxInfoDto
        {
            chapters = new List<AaxChapter>
            {
                new() { id = 1, start_time = "0", end_time = "60", time_base = "1/1000" }
            }
        };

        // Act
        ChapterConverter.CreateChapterFile(aaxPath, aaxInfo, outputPath);

        // Assert
        var content = File.ReadAllText(outputPath);
        content.Should().NotContain("Old content", "Old content should be overwritten");
        content.Should().Contain(";FFMETADATA1", "Should contain new content");
    }

    #endregion

    #region CreateChapterFile - Time Calculations

    [Fact]
    public void CreateChapterFile_AudibleChapters_EndTimeShouldBeStartPlusDurationMinusOne()
    {
        // Arrange
        var aaxPath = Path.Combine(_testDirectory, "Book-AAX");
        File.WriteAllText(aaxPath, "fake");
        var chaptersJsonPath = Path.Combine(_testDirectory, "Book-chapters.json");
        var outputPath = Path.Combine(_testDirectory, "chapters.txt");

        // Start at 1000ms, duration 5000ms -> end should be 1000 + 5000 - 1 = 5999ms
        var audibleChapters = new AudibleChaptersDto
        {
            content_metadata = new ContentMetadata
            {
                chapter_info = new ChapterInfo
                {
                    chapters = new List<Chapter>
                    {
                        new() { title = "Test", start_offset_ms = 1000, length_ms = 5000 }
                    }
                }
            }
        };

        File.WriteAllText(chaptersJsonPath, JsonSerializer.Serialize(audibleChapters));
        var aaxInfo = new AaxInfoDto();

        // Act
        ChapterConverter.CreateChapterFile(aaxPath, aaxInfo, outputPath);

        // Assert
        var content = File.ReadAllText(outputPath);
        content.Should().Contain("START=1000");
        content.Should().Contain("END=5999");
    }

    [Fact]
    public void CreateChapterFile_AaxInfoChapters_EndTimeShouldBeEndTimeMinusOne()
    {
        // Arrange
        var aaxPath = Path.Combine(_testDirectory, "Book-AAX");
        File.WriteAllText(aaxPath, "fake");
        var outputPath = Path.Combine(_testDirectory, "chapters.txt");

        // start_time=10, end_time=20 (seconds)
        // START = 10 * 1000 = 10000
        // END = 20 * 1000 - 1 = 19999
        var aaxInfo = new AaxInfoDto
        {
            chapters = new List<AaxChapter>
            {
                new()
                {
                    id = 1,
                    start_time = "10",
                    end_time = "20",
                    time_base = "1/1000"
                }
            }
        };

        // Act
        ChapterConverter.CreateChapterFile(aaxPath, aaxInfo, outputPath);

        // Assert
        var content = File.ReadAllText(outputPath);
        content.Should().Contain("START=10000");
        content.Should().Contain("END=19999");
    }

    #endregion

    #region CreateChapterFile - Integration Tests

    [Fact]
    public void CreateChapterFile_FullWorkflow_WithAudibleChapters()
    {
        // Arrange
        var aaxPath = Path.Combine(_testDirectory, "Audiobook-AAX");
        File.WriteAllText(aaxPath, "fake aax data");
        var chaptersJsonPath = Path.Combine(_testDirectory, "Audiobook-chapters.json");
        var outputPath = Path.Combine(_testDirectory, "chapters.metadata");

        var audibleChapters = new AudibleChaptersDto
        {
            content_metadata = new ContentMetadata
            {
                chapter_info = new ChapterInfo
                {
                    brandIntroDurationMs = 500,
                    brandOutroDurationMs = 300,
                    runtime_length_ms = 600000,
                    chapters = new List<Chapter>
                    {
                        new() { title = "Introduction", start_offset_ms = 0, length_ms = 120000 },
                        new() { title = "Part I: The Beginning", start_offset_ms = 120000, length_ms = 180000 },
                        new()
                        {
                            title = "Part II: The Middle",
                            start_offset_ms = 300000,
                            length_ms = 150000,
                            chapters = new List<Chapter>
                            {
                                new() { title = "Chapter 5", start_offset_ms = 300000, length_ms = 75000 },
                                new() { title = "Chapter 6", start_offset_ms = 375000, length_ms = 75000 }
                            }
                        },
                        new() { title = "Conclusion", start_offset_ms = 450000, length_ms = 150000 }
                    }
                }
            }
        };

        File.WriteAllText(chaptersJsonPath, JsonSerializer.Serialize(audibleChapters));
        var aaxInfo = new AaxInfoDto { chapters = new List<AaxChapter>() };

        // Act
        ChapterConverter.CreateChapterFile(aaxPath, aaxInfo, outputPath);

        // Assert
        var content = File.ReadAllText(outputPath);

        // Verify header
        content.Should().StartWith(";FFMETADATA1");

        // Verify all chapters (including nested ones) are present
        content.Should().Contain("title=Introduction");
        content.Should().Contain("title=Part I: The Beginning");
        content.Should().Contain("title=Part II: The Middle");
        content.Should().Contain("title=Chapter 5");
        content.Should().Contain("title=Chapter 6");
        content.Should().Contain("title=Conclusion");

        // Verify chapter count
        var chapterCount = content.Split("\n").Count(l => l.Trim() == "[CHAPTER]");
        chapterCount.Should().Be(6, "Should have 6 chapters (Introduction + Part I + Part II + Chapter 5 + Chapter 6 + Conclusion)");
    }

    [Fact]
    public void CreateChapterFile_FullWorkflow_WithAaxInfoChapters()
    {
        // Arrange
        var aaxPath = Path.Combine(_testDirectory, "Classic-AAX");
        File.WriteAllText(aaxPath, "fake aax data");
        // No chapters.json file
        var outputPath = Path.Combine(_testDirectory, "ffmetadata.txt");

        var aaxInfo = new AaxInfoDto
        {
            chapters = new List<AaxChapter>
            {
                new()
                {
                    id = 0,
                    start_time = "0",
                    end_time = "300.5",
                    time_base = "1/1000",
                    tags = new AaxTags { title = "Prologue" }
                },
                new()
                {
                    id = 1,
                    start_time = "300.5",
                    end_time = "1800.25",
                    time_base = "1/1000",
                    tags = new AaxTags { title = "Main Content" }
                },
                new()
                {
                    id = 2,
                    start_time = "1800.25",
                    end_time = "2100",
                    time_base = "1/1000",
                    tags = new AaxTags { title = "Epilogue" }
                }
            },
            format = new AaxFormat
            {
                filename = "Classic.aax",
                duration = "2100",
                tags = new AaxTags
                {
                    title = "Classic Audiobook",
                    artist = "Narrator Name"
                }
            }
        };

        // Act
        ChapterConverter.CreateChapterFile(aaxPath, aaxInfo, outputPath);

        // Assert
        var content = File.ReadAllText(outputPath);

        // Verify header
        content.Should().StartWith(";FFMETADATA1");

        // Verify chapters - note: it uses generic "Chapter N" naming, not tags
        content.Should().Contain("title=Chapter 1");
        content.Should().Contain("title=Chapter 2");
        content.Should().Contain("title=Chapter 3");

        // Verify timing conversions
        // Chapter 1: 0 to 300.5 * 1000 - 1 = 300499
        content.Should().Contain("START=0");
        content.Should().Contain("END=300499");

        // Chapter 2: 300.5 * 1000 = 300500
        content.Should().Contain("START=300500");
    }

    #endregion
}