using FluentAssertions;
using Harmony;
using Xunit;

namespace Harmony.Tests;

public class PartMergingTests
{
    #region ParsePartFile

    [Fact]
    public void ParsePartFile_WithAudiblePartName_ReturnsBaseAndPartNumber()
    {
        var result = PartMerging.ParsePartFile("Institutes_of_the_Christian_Religion_Institutes_of_the_Christian_Religion_Part_3-LC_64_22050_stereo.aax");
        result.Should().NotBeNull();
        result!.Value.Base.Should().Be("Institutes_of_the_Christian_Religion_Institutes_of_the_Christian_Religion");
        result.Value.PartNumber.Should().Be(3);
    }

    [Fact]
    public void ParsePartFile_WithMultiDigitPart_ReturnsCorrectNumber()
    {
        var result = PartMerging.ParsePartFile("Galaxy_Outlaws_The_Complete_Black_Ocean_Mobius_Missions_1-16.5_Galaxy_Outlaws_The_Complete_Black_Ocean_Mobius_Missions_1-16.5_Part_12-LC_128_44100_stereo.aax");
        result.Should().NotBeNull();
        result!.Value.PartNumber.Should().Be(12);
    }

    [Fact]
    public void ParsePartFile_WithNonPartFile_ReturnsNull()
    {
        PartMerging.ParsePartFile("Voyagers-AAX_44_128.aax").Should().BeNull();
    }

    [Fact]
    public void ParsePartFile_WithNonPartSingleFileBook_ReturnsNull()
    {
        PartMerging.ParsePartFile("12_Rules_for_Life_An_Antidote_to_Chaos-LC_128_44100_stereo.aax").Should().BeNull();
    }

    #endregion

    #region GroupParts

    [Fact]
    public void GroupParts_SeparatesPartGroupsFromSingles()
    {
        string[] files =
        {
            "Institutes_Part_1-LC_64_22050_stereo.aax",
            "Institutes_Part_2-LC_64_22050_stereo.aax",
            "Voyagers-LC_64_22050_stereo.aax",
            "Dune-LC_64_22050_stereo.aax",
        };
        var result = PartMerging.GroupParts(files);
        result.MergeGroups.Should().HaveCount(1);
        var group = result.MergeGroups[0];
        group.Base.Should().Be("Institutes");
        group.Parts.Should().HaveCount(2);
        group.Parts.Select(p => p.PartNumber).Should().ContainInOrder(1, 2);
        group.Parts.Select(p => p.Path).Should().ContainInOrder(
            files[0], files[1]);
        result.Singles.Should().HaveCount(2);
    }

    [Fact]
    public void GroupParts_OrdersPartsNumericallyMultiDigitCorrect()
    {
        var files = Enumerable.Range(1, 10)
            .Select(n => $"Book_Part_{n}-LC_128_44100_stereo.aax")
            .ToArray();
        var result = PartMerging.GroupParts(files);
        result.MergeGroups.Should().HaveCount(1);
        result.MergeGroups[0].Parts.Select(p => p.PartNumber).Should().ContainInOrder(
            Enumerable.Range(1, 10).ToArray(), "double-digit parts must sort numerically, not lexicographically");
    }

    [Fact]
    public void GroupParts_TwoDifferentPartBooks_MakesTwoGroups()
    {
        string[] files =
        {
            "Alpha_Part_1-LC_64_22050_stereo.aax",
            "Alpha_Part_2-LC_64_22050_stereo.aax",
            "Beta_Part_1-LC_64_22050_stereo.aax",
            "Beta_Part_2-LC_64_22050_stereo.aax",
        };
        var result = PartMerging.GroupParts(files);
        result.MergeGroups.Should().HaveCount(2);
        result.Singles.Should().BeEmpty();
    }

    #endregion

    #region MergeChapters

    [Fact]
    public void MergeChapters_StacksSecondPartChaptersAfterFirstPartDuration()
    {
        var chapters1 = new List<(int Start, int End, string Title)>
        {
            (0, 1000, "Intro"),
            (1000, 60000, "Chapter 1"),
        };
        var chapters2 = new List<(int Start, int End, string Title)>
        {
            (0, 2000, "Chapter 2"),
            (2000, 40000, "Chapter 3"),
        };
        var merged = PartMerging.MergeChapters(
            new[] { (chapters1, 61_000.0), (chapters2, 45_000.0) });
        merged.Should().HaveCount(4);
        merged[0].Start.Should().Be(0);
        merged[0].End.Should().Be(1000);
        merged[0].Title.Should().Be("Intro");
        merged[1].Start.Should().Be(1000);
        merged[1].End.Should().Be(60000);
        merged[1].Title.Should().Be("Chapter 1");
        merged[2].Start.Should().Be(61_000_000, "offset = 61 s x 1000 ms/s");
        merged[2].End.Should().Be(61_002_000);
        merged[2].Title.Should().Be("Chapter 2");
        merged[3].Start.Should().Be(61_002_000);
        merged[3].End.Should().Be(61_040_000);
        merged[3].Title.Should().Be("Chapter 3");
    }

    [Fact]
    public void MergeChapters_WithPartWithoutChapters_StillOffsetsCorrectly()
    {
        var chapters1 = new List<(int Start, int End, string Title)> { (0, 5000, "Only") };
        var merged = PartMerging.MergeChapters(new[]
        {
            (chapters1, 30_000.0),
            (new List<(int Start, int End, string Title)>(), 20_000.0),
        });
        merged.Should().HaveCount(1, "a part with no chapters contributes nothing but its duration is consumed");
    }

    #endregion

    #region ParseFfmetadata

    [Fact]
    public void ParseFfmetadata_ReadsStartEndTitleBlocks()
    {
        const string text = ";FFMETADATA1\ntitle=X\n\n[CHAPTER]\nTIMEBASE=1/1000\nSTART=0\nEND=1000\ntitle=Intro\n\n[CHAPTER]\nTIMEBASE=1/1000\nSTART=1000\nEND=5000\ntitle=Chapter 1\n";
        var chapters = PartMerging.ParseFfmetadata(text);
        chapters.Should().HaveCount(2);
        chapters[0].Start.Should().Be(0);
        chapters[0].End.Should().Be(1000);
        chapters[0].Title.Should().Be("Intro");
        chapters[1].Start.Should().Be(1000);
        chapters[1].End.Should().Be(5000);
        chapters[1].Title.Should().Be("Chapter 1");
    }

    #endregion

    #region BuildConcatList

    [Fact]
    public void BuildConcatList_WritesSafeQuotedFileEntries()
    {
        var list = PartMerging.BuildConcatList(new[] { "/work/part1.m4a", "/mnt/media/Audible-AAX/book's part2.m4a" });
        var lines = list.Split('\n');
        lines[0].Should().Be("file '/work/part1.m4a'");
        lines[1].Should().Contain("'book''s part2.m4a'".Trim('\''), "single quotes in paths must be escaped for the concat demuxer");
    }

    #endregion
}
public class PartMergingFfmetadataWriterTests
{
    [Fact]
    public void BuildFfmetadataText_WritesHeaderAndChapterBlocks()
    {
        var chapters = new List<(int Start, int End, string Title)>
        {
            (0, 1000, "Intro"),
            (1000, 61002000, "Chapter 2"),
        };
        var text = PartMerging.BuildFfmetadataText(chapters);
        var lines = text.Split('\n');
        lines[0].Should().Be(";FFMETADATA1");
        text.Should().Contain("[CHAPTER]");
        text.Should().Contain("TIMEBASE=1/1000");
        text.Should().Contain("START=0");
        text.Should().Contain("END=1000");
        text.Should().Contain("title=Intro");
        text.Should().Contain("START=1000");
        text.Should().Contain("END=61002000");
        text.Should().Contain("title=Chapter 2");
    }

    [Fact]
    public void BuildFfmetadataText_RoundTripsThroughParser()
    {
        var chapters = new List<(int Start, int End, string Title)>
        {
            (0, 4500000, "Part 1: Chapter 3"),
            (4500000, 91000000, "Part 2: Chapter 14"),
        };
        PartMerging.ParseFfmetadata(PartMerging.BuildFfmetadataText(chapters))
            .Should().BeEquivalentTo(chapters, "writer output must parse back to identical chapters");
    }
}
