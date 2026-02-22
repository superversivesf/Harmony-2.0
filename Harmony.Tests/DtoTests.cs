using FluentAssertions;
using Harmony.Dto;
using Newtonsoft.Json;
using STJ = System.Text.Json;

namespace Harmony.Tests;

public class DtoTests
{
    #region AaxInfoDto Tests (System.Text.Json)

    [Fact]
    public void AaxInfoDto_Deserialize_WithFullJson_ShouldMapAllProperties()
    {
        // Arrange
        var json = """
        {
            "chapters": [
                {
                    "id": 1,
                    "time_base": "1/44100",
                    "start": 0,
                    "start_time": "0.000000",
                    "end": 4410000,
                    "end_time": "100.000000",
                    "tags": {
                        "title": "Chapter 1"
                    }
                }
            ],
            "format": {
                "filename": "test.aax",
                "nb_streams": 1,
                "nb_programs": 0,
                "format_name": "aax",
                "format_long_name": "Audible AAX",
                "start_time": "0.000000",
                "duration": "3600.000000",
                "size": "288000000",
                "bit_rate": "64000",
                "probe_score": 100,
                "tags": {
                    "title": "Test Book",
                    "artist": "Test Author",
                    "album": "Test Album",
                    "genre": "Audiobook"
                }
            }
        }
        """;

        // Act
        var dto = STJ.JsonSerializer.Deserialize<AaxInfoDto>(json, new STJ.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        dto.Should().NotBeNull();
        dto!.chapters.Should().HaveCount(1);
        dto.chapters![0].id.Should().Be(1);
        dto.chapters[0].time_base.Should().Be("1/44100");
        dto.chapters[0].start_time.Should().Be("0.000000");
        dto.chapters[0].end_time.Should().Be("100.000000");
        dto.chapters[0].tags!.title.Should().Be("Chapter 1");

        dto.format.Should().NotBeNull();
        dto.format!.filename.Should().Be("test.aax");
        dto.format.nb_streams.Should().Be(1);
        dto.format.format_name.Should().Be("aax");
        dto.format.duration.Should().Be("3600.000000");
        dto.format.tags!.title.Should().Be("Test Book");
        dto.format.tags.artist.Should().Be("Test Author");
    }

    [Fact]
    public void AaxInfoDto_Deserialize_WithNullCollections_ShouldHandleNulls()
    {
        // Arrange
        var json = """
        {
            "chapters": null,
            "format": null
        }
        """;

        // Act
        var dto = STJ.JsonSerializer.Deserialize<AaxInfoDto>(json, new STJ.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        dto.Should().NotBeNull();
        dto!.chapters.Should().BeNull();
        dto.format.Should().BeNull();
    }

    [Fact]
    public void AaxInfoDto_Serialize_ShouldProduceCorrectJson()
    {
        // Arrange
        var dto = new AaxInfoDto
        {
            chapters = new List<AaxChapter>
            {
                new AaxChapter
                {
                    id = 1,
                    time_base = "1/44100",
                    start = 0,
                    start_time = "0.000000",
                    end = 4410000,
                    end_time = "100.000000",
                    tags = new AaxTags { title = "Chapter 1" }
                }
            },
            format = new AaxFormat
            {
                filename = "test.aax",
                nb_streams = 1,
                nb_programs = 0,
                format_name = "aax",
                format_long_name = "Audible AAX",
                start_time = "0.000000"
            }
        };

        // Act
        var json = STJ.JsonSerializer.Serialize(dto);

        // Assert
        json.Should().Contain("\"chapters\"");
        json.Should().Contain("\"format\"");
        json.Should().Contain("\"id\":1");
        json.Should().Contain("\"filename\":\"test.aax\"");
    }

    [Fact]
    public void AaxTags_Deserialize_WithAllProperties_ShouldMapCorrectly()
    {
        // Arrange
        var json = """
        {
            "title": "Book Title",
            "major_brand": "M4A",
            "minor_version": "1",
            "compatible_brands": "M4A",
            "creation_time": "2024-01-15T10:30:00",
            "comment": "Test comment",
            "artist": "Author Name",
            "album_artist": "Author Name",
            "album": "Book Title",
            "genre": "Audiobook",
            "copyright": "2024",
            "date": "2024"
        }
        """;

        // Act
        var tags = STJ.JsonSerializer.Deserialize<AaxTags>(json, new STJ.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        tags.Should().NotBeNull();
        tags!.title.Should().Be("Book Title");
        tags.major_brand.Should().Be("M4A");
        tags.artist.Should().Be("Author Name");
        tags.album.Should().Be("Book Title");
        tags.genre.Should().Be("Audiobook");
        tags.creation_time.Should().NotBeNull();
    }

    [Fact]
    public void AaxTags_DefaultValues_ShouldBeNull()
    {
        // Arrange & Act
        var tags = new AaxTags();

        // Assert
        tags.title.Should().BeNull();
        tags.artist.Should().BeNull();
        tags.album.Should().BeNull();
        tags.creation_time.Should().BeNull();
    }

    #endregion

    #region AbsMetadata Tests (System.Text.Json)

    [Fact]
    public void AbsMetadata_Deserialize_WithFullJson_ShouldMapAllProperties()
    {
        // Arrange
        var json = """
        {
            "tags": ["audiobook", "fiction"],
            "title": "Test Book",
            "subtitle": "A Story",
            "authors": ["Author One", "Author Two"],
            "narrators": ["Narrator Name"],
            "series": ["Test Series"],
            "genres": ["Fiction", "Sci-Fi"],
            "publishedYear": "2024",
            "publishedDate": "2024-01-15",
            "publisher": "Test Publisher",
            "description": "A test book description",
            "isbn": "1234567890",
            "asin": "B00TEST123",
            "language": "en",
            "explicit": true,
            "abridged": false
        }
        """;

        // Act
        var metadata = STJ.JsonSerializer.Deserialize<AbsMetadata>(json, new STJ.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        metadata.Should().NotBeNull();
        metadata!.tags.Should().Contain("audiobook", "fiction");
        metadata.title.Should().Be("Test Book");
        metadata.subtitle.Should().Be("A Story");
        metadata.authors.Should().Contain("Author One", "Author Two");
        metadata.narrators.Should().Contain("Narrator Name");
        metadata.publishedYear.Should().Be("2024");
        metadata.asin.Should().Be("B00TEST123");
        metadata.@explicit.Should().BeTrue();
        metadata.abridged.Should().BeFalse();
    }

    [Fact]
    public void AbsMetadata_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var metadata = new AbsMetadata();

        // Assert
        metadata.tags.Should().BeNull();
        metadata.title.Should().BeNull();
        metadata.authors.Should().BeNull();
        metadata.@explicit.Should().BeFalse();
        metadata.abridged.Should().BeFalse();
    }

    [Fact]
    public void AbsMetadata_Serialize_ShouldProduceCorrectJson()
    {
        // Arrange
        var metadata = new AbsMetadata
        {
            title = "Test Book",
            authors = new List<string> { "Author" },
            @explicit = true
        };

        // Act
        var json = STJ.JsonSerializer.Serialize(metadata);

        // Assert
        json.Should().Contain("\"title\":\"Test Book\"");
        json.Should().Contain("\"explicit\":true");
        json.Should().Contain("\"authors\":[\"Author\"]");
    }

    [Fact]
    public void AbsMetadata_Deserialize_WithMinimalJson_ShouldHandleMissingProperties()
    {
        // Arrange
        var json = """
        {
            "title": "Minimal Book"
        }
        """;

        // Act
        var metadata = STJ.JsonSerializer.Deserialize<AbsMetadata>(json, new STJ.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        metadata.Should().NotBeNull();
        metadata!.title.Should().Be("Minimal Book");
        metadata.authors.Should().BeNull();
        metadata.@explicit.Should().BeFalse();
    }

    #endregion

    #region AudibleLibraryDto Tests

    [Fact]
    public void AudibleLibraryDto_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var dto = new AudibleLibraryDto();

        // Assert
        dto.asin.Should().BeNull();
        dto.title.Should().BeNull();
        dto.runtime_length_min.Should().Be(0);
        dto.is_finished.Should().BeFalse();
        dto.percent_complete.Should().BeNull();
    }

    [Fact]
    public void AudibleLibraryDto_JsonRoundTrip_ShouldPreserveProperties()
    {
        // Arrange
        var original = new AudibleLibraryDto
        {
            asin = "B00TEST123",
            title = "Test Book",
            authors = "Author Name",
            narrators = "Narrator Name",
            runtime_length_min = 360,
            is_finished = true,
            percent_complete = 100.0,
            rating = 4.5,
            num_ratings = 1000,
            date_added = new DateTime(2024, 1, 15),
            release_date = new DateTime(2023, 6, 1)
        };

        // Act
        var json = STJ.JsonSerializer.Serialize(original);
        var deserialized = STJ.JsonSerializer.Deserialize<AudibleLibraryDto>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.asin.Should().Be("B00TEST123");
        deserialized.title.Should().Be("Test Book");
        deserialized.authors.Should().Be("Author Name");
        deserialized.runtime_length_min.Should().Be(360);
        deserialized.is_finished.Should().BeTrue();
        deserialized.percent_complete.Should().Be(100.0);
        deserialized.rating.Should().Be(4.5);
    }

    [Fact]
    public void AudibleLibraryDto_WithNullableProperties_ShouldHandleNulls()
    {
        // Arrange
        var dto = new AudibleLibraryDto
        {
            asin = "B00TEST",
            title = "Test",
            runtime_length_min = 0,
            date_added = DateTime.Now,
            release_date = DateTime.Now,
            purchase_date = DateTime.Now,
            percent_complete = null,
            rating = null,
            num_ratings = null
        };

        // Assert
        dto.percent_complete.Should().BeNull();
        dto.rating.Should().BeNull();
        dto.num_ratings.Should().BeNull();
    }

    #endregion

    #region AudibleChaptersDto Tests (System.Text.Json)

    [Fact]
    public void AudibleChaptersDto_Deserialize_WithFullJson_ShouldMapAllProperties()
    {
        // Arrange
        var json = """
        {
            "content_metadata": {
                "chapter_info": {
                    "brandIntroDurationMs": 5000,
                    "brandOutroDurationMs": 3000,
                    "chapters": [
                        {
                            "length_ms": 1800000,
                            "start_offset_ms": 0,
                            "start_offset_sec": 0,
                            "title": "Chapter 1"
                        }
                    ],
                    "is_accurate": true,
                    "runtime_length_ms": 3600000,
                    "runtime_length_sec": 3600
                },
                "content_reference": {
                    "acr": "ACR123",
                    "asin": "B00TEST123",
                    "codec": "mp3",
                    "content_format": "MPEG",
                    "content_size_in_bytes": 288000000,
                    "file_version": "1.0"
                },
                "last_position_heard": {
                    "status": "IN_PROGRESS"
                }
            },
            "response_groups": ["chapter_info", "content_reference"]
        }
        """;

        // Act
        var dto = STJ.JsonSerializer.Deserialize<AudibleChaptersDto>(json, new STJ.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        dto.Should().NotBeNull();
        dto!.content_metadata.Should().NotBeNull();
        dto.content_metadata!.chapter_info.Should().NotBeNull();
        dto.content_metadata.chapter_info!.runtime_length_ms.Should().Be(3600000);
        dto.content_metadata.chapter_info.chapters.Should().HaveCount(1);
        dto.content_metadata.chapter_info.chapters![0].title.Should().Be("Chapter 1");
        dto.content_metadata.content_reference!.asin.Should().Be("B00TEST123");
        dto.response_groups.Should().Contain("chapter_info");
    }

    [Fact]
    public void Chapter_Deserialize_WithNestedChapters_ShouldMapCorrectly()
    {
        // Arrange
        var json = """
        {
            "length_ms": 3600000,
            "start_offset_ms": 0,
            "start_offset_sec": 0,
            "title": "Part 1",
            "chapters": [
                {
                    "length_ms": 1800000,
                    "start_offset_ms": 0,
                    "start_offset_sec": 0,
                    "title": "Chapter 1"
                },
                {
                    "length_ms": 1800000,
                    "start_offset_ms": 1800000,
                    "start_offset_sec": 1800,
                    "title": "Chapter 2"
                }
            ]
        }
        """;

        // Act
        var chapter = STJ.JsonSerializer.Deserialize<Chapter>(json, new STJ.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        chapter.Should().NotBeNull();
        chapter!.title.Should().Be("Part 1");
        chapter.length_ms.Should().Be(3600000);
        chapter.chapters.Should().HaveCount(2);
        chapter.chapters![0].title.Should().Be("Chapter 1");
        chapter.chapters[1].title.Should().Be("Chapter 2");
    }

    [Fact]
    public void AudibleChaptersDto_FlattenChapters_ShouldFlattenNestedStructure()
    {
        // Arrange
        var rootChapters = new List<Chapter>
        {
            new Chapter
            {
                title = "Part 1",
                chapters = new List<Chapter>
                {
                    new Chapter { title = "Chapter 1" },
                    new Chapter { title = "Chapter 2" }
                }
            },
            new Chapter
            {
                title = "Part 2",
                chapters = new List<Chapter>
                {
                    new Chapter { title = "Chapter 3" }
                }
            }
        };

        // Act
        var flattened = AudibleChaptersDto.FlattenChapters(rootChapters);

        // Assert
        flattened.Should().HaveCount(5);
        flattened[0].title.Should().Be("Part 1");
        flattened[1].title.Should().Be("Chapter 1");
        flattened[2].title.Should().Be("Chapter 2");
        flattened[3].title.Should().Be("Part 2");
        flattened[4].title.Should().Be("Chapter 3");
    }

    [Fact]
    public void AudibleChaptersDto_FlattenChapters_WithNullInput_ShouldReturnEmptyList()
    {
        // Act
        var result = AudibleChaptersDto.FlattenChapters(null);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Chapter_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var chapter = new Chapter();

        // Assert
        chapter.title.Should().BeEmpty();
        chapter.length_ms.Should().BeNull();
        chapter.start_offset_ms.Should().BeNull();
        chapter.chapters.Should().BeNull();
    }

    #endregion

    #region AudibleVoucherDto Tests (Newtonsoft.Json)

    [Fact]
    public void AudibleVoucherDto_Deserialize_WithFullJson_ShouldMapAllProperties()
    {
        // Arrange
        var json = """
        {
            "content_license": {
                "access_expiry_date": "2025-12-31T00:00:00",
                "acr": "ACR123",
                "allowed_users": ["user1", "user2"],
                "asin": "B00TEST123",
                "drm_type": "Adrm",
                "granted_right": "Download",
                "license_id": "license-123",
                "license_response": {
                    "key": "encryption-key",
                    "iv": "initialization-vector",
                    "refreshDate": "2024-06-01T00:00:00",
                    "removalOnExpirationDate": "2025-12-31T00:00:00",
                    "rules": [
                        {
                            "name": "playback-rule",
                            "parameters": [
                                {
                                    "expireDate": "2025-12-31T00:00:00",
                                    "type": "playback"
                                }
                            ]
                        }
                    ]
                },
                "license_response_type": "LicenseResponse",
                "message": "Success",
                "playback_info": {
                    "last_position_heard": {
                        "status": "IN_PROGRESS"
                    }
                },
                "preview": false,
                "refresh_date": "2024-06-01T00:00:00",
                "removal_date": "2025-12-31T00:00:00",
                "request_id": "request-123",
                "requires_ad_supported_playback": false,
                "status_code": "ACTIVE",
                "voucher_id": "voucher-123"
            },
            "response_groups": ["content_license"]
        }
        """;

        // Act
        var dto = JsonConvert.DeserializeObject<AudibleVoucherDto>(json);

        // Assert
        dto.Should().NotBeNull();
        dto!.content_license.Should().NotBeNull();
        dto.content_license!.asin.Should().Be("B00TEST123");
        dto.content_license.acr.Should().Be("ACR123");
        dto.content_license.drm_type.Should().Be("Adrm");
        dto.content_license.license_response.Should().NotBeNull();
        dto.content_license.license_response!.key.Should().Be("encryption-key");
        dto.content_license.license_response.iv.Should().Be("initialization-vector");
        dto.content_license.license_response.rules.Should().HaveCount(1);
        dto.content_license.preview.Should().BeFalse();
        dto.response_groups.Should().Contain("content_license");
    }

    [Fact]
    public void ContentLicense_Deserialize_WithMinimalJson_ShouldHandleNulls()
    {
        // Arrange
        var json = """
        {
            "asin": "B00TEST123"
        }
        """;

        // Act
        var license = JsonConvert.DeserializeObject<ContentLicense>(json);

        // Assert
        license.Should().NotBeNull();
        license!.asin.Should().Be("B00TEST123");
        license.acr.Should().BeNull();
        license.allowed_users.Should().BeNull();
        license.license_response.Should().BeNull();
    }

    [Fact]
    public void LicenseResponse_Deserialize_ShouldMapPropertiesCorrectly()
    {
        // Arrange
        var json = """
        {
            "key": "test-key",
            "iv": "test-iv",
            "refreshDate": "2024-01-15T10:30:00",
            "removalOnExpirationDate": "2025-01-15T10:30:00"
        }
        """;

        // Act
        var response = JsonConvert.DeserializeObject<LicenseResponse>(json);

        // Assert
        response.Should().NotBeNull();
        response!.key.Should().Be("test-key");
        response.iv.Should().Be("test-iv");
        response.refreshDate.Should().NotBeNull();
        response.removalOnExpirationDate.Should().NotBeNull();
    }

    [Fact]
    public void AudibleVoucherDto_Serialize_ShouldProduceCorrectJson()
    {
        // Arrange
        var dto = new AudibleVoucherDto
        {
            content_license = new ContentLicense
            {
                asin = "B00TEST123",
                drm_type = "Adrm",
                license_response = new LicenseResponse
                {
                    key = "test-key",
                    iv = "test-iv"
                }
            },
            response_groups = new List<string> { "content_license" }
        };

        // Act
        var json = JsonConvert.SerializeObject(dto);

        // Assert
        json.Should().Contain("\"asin\":\"B00TEST123\"");
        json.Should().Contain("\"drm_type\":\"Adrm\"");
        json.Should().Contain("\"key\":\"test-key\"");
        json.Should().Contain("\"response_groups\"");
    }

    [Fact]
    public void ContentLicense_DefaultValues_ShouldBeNull()
    {
        // Arrange & Act
        var license = new ContentLicense();

        // Assert
        license.asin.Should().BeNull();
        license.acr.Should().BeNull();
        license.drm_type.Should().BeNull();
        license.license_response.Should().BeNull();
        license.allowed_users.Should().BeNull();
        license.access_expiry_date.Should().BeNull();
    }

    [Fact]
    public void Rule_Deserialize_WithParameters_ShouldMapCorrectly()
    {
        // Arrange
        var json = """
        {
            "name": "test-rule",
            "parameters": [
                {
                    "type": "playback",
                    "expireDate": "2025-12-31T00:00:00",
                    "directedIds": ["id1", "id2"]
                }
            ]
        }
        """;

        // Act
        var rule = JsonConvert.DeserializeObject<Rule>(json);

        // Assert
        rule.Should().NotBeNull();
        rule!.name.Should().Be("test-rule");
        rule.parameters.Should().HaveCount(1);
        rule.parameters![0].type.Should().Be("playback");
        rule.parameters[0].directedIds.Should().Contain("id1", "id2");
    }

    #endregion

    #region Cross-DTO Integration Tests

    [Fact]
    public void AaxInfoDto_WithChaptersAndFormat_RoundTrip_ShouldPreserveData()
    {
        // Arrange
        var original = new AaxInfoDto
        {
            chapters = new List<AaxChapter>
            {
                new AaxChapter
                {
                    id = 1,
                    time_base = "1/44100",
                    start_time = "0.000000",
                    end_time = "1800.000000",
                    tags = new AaxTags { title = "Chapter 1" }
                }
            },
            format = new AaxFormat
            {
                filename = "test.aax",
                nb_streams = 2,
                format_name = "aax",
                tags = new AaxTags { title = "Test Book", artist = "Test Author" }
            }
        };

        // Act
        var json = STJ.JsonSerializer.Serialize(original);
        var deserialized = STJ.JsonSerializer.Deserialize<AaxInfoDto>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.chapters.Should().HaveCount(1);
        deserialized.chapters![0].tags!.title.Should().Be("Chapter 1");
        deserialized.format!.tags!.title.Should().Be("Test Book");
    }

    [Fact]
    public void AudibleChaptersDto_EmptyChapters_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var dto = new AudibleChaptersDto
        {
            content_metadata = new ContentMetadata
            {
                chapter_info = new ChapterInfo
                {
                    chapters = new List<Chapter>()
                }
            }
        };

        // Act
        var json = STJ.JsonSerializer.Serialize(dto);
        var deserialized = STJ.JsonSerializer.Deserialize<AudibleChaptersDto>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.content_metadata!.chapter_info!.chapters.Should().BeEmpty();
    }

    #endregion
}