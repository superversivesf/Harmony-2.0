# Harmony 2.0 - Fix TODO List

Generated from .NET 10 Modernization Audit

---

## All Previous Items Resolved

All 14 items from the original audit have been addressed:

1. ~~Loop Mode Sleep Bug~~ — Removed loop mode entirely
2. ~~FFProbeAnalyzer Malformed Command~~ — Already fixed (uses `FFmpeg.GetMediaInfo`)
3. ~~NullReferenceException in GenerateCover~~ — Already fixed (`is not null` guard)
4. ~~Duplicate FFmpeg Fetch Logic~~ — Already extracted to `FetchFFmpegAsync`
5. ~~Empty HandleParseError Method~~ — Already fixed (logs + exit)
6. ~~Null Dereference in AAXtoM4BConvertor Metadata~~ — Already fixed (`title is not null` guard)
7. ~~Null Dereference in AAXCtoM4BConvertor Metadata~~ — Moved to base class with guard
8. ~~Async Methods Synchronously Waited~~ — Already migrated to `await`
9. ~~Redundant `this.` Qualifiers~~ — Already removed
10. ~~Inconsistent String Empty Handling~~ — Acceptable (different contexts)
11. ~~Remove Dead Code - ProgressMeter~~ — Already removed
12. ~~Inconsistent Null Check Patterns~~ — Fixed (all `!= null` → `is not null`)
13. ~~Magic Numbers~~ — Fixed (hardcoded `"64k"` → `DefaultAacBitrate` constant)
14. ~~Document Newtonsoft.Json Usage~~ — Already documented in `AudibleVoucherDto.cs`

---

## Pre-existing Issues

- `Harmony.Tests/AudiobookConverterBaseTests.cs:350` — `ProcessTitleForComparison` not found (test references non-public method without reflection)

---

## Future Considerations

- [ ] Add GitHub Actions CI for all target platforms
- [ ] Implement structured logging (replace Console.WriteLine)
- [ ] Add appsettings.json configuration support