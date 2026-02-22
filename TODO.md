# Harmony 2.0 - Fix TODO List

Generated from .NET 10 Modernization Audit

---

## Critical (Fix Immediately)

### 1. Loop Mode Sleep Bug
- **File:** `Program.cs:120`
- **Issue:** `Thread.Sleep(600)` is 0.6 seconds, not 5 minutes as documented
- **Fix:**
  ```csharp
  Thread.Sleep(TimeSpan.FromMinutes(5));
  ```

### 2. FFProbeAnalyzer Malformed Command
- **File:** `FFProbeAnalyzer.cs:19`
- **Issue:** Malformed command string, result discarded, always returns `true`
- **Fix:**
  ```csharp
  public async Task<bool> AnalyzeFile(string filePath)
  {
      try
      {
          IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(filePath).ConfigureAwait(false);
          return mediaInfo != null;
      }
      catch (Exception)
      {
          return false;
      }
  }
  ```

### 3. NullReferenceException in GenerateCover
- **File:** `AAXCtoM4BConvertor.cs:289`
- **Issue:** `Path.GetDirectoryName()` can return null
- **Fix:**
  ```csharp
  var directory = Path.GetDirectoryName(filePath);
  if (directory is null)
  {
      logger.WriteLine("Done (no directory)");
      return Path.Combine(outputDirectory, "cover.jpg");
  }
  ```

---

## High Priority

### 4. Duplicate FFmpeg Fetch Logic
- **File:** `Program.cs:52-66` and `Program.cs:68-79`
- **Issue:** Identical FFmpeg download logic appears twice
- **Fix:** Extract to method:
  ```csharp
  private static async Task FetchFFmpegAsync(Logger logger)
  {
      logger.Write("Fetching Latest FFMpeg ...  ");
      await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);
      logger.WriteLine("\bDone");
  }
  ```

### 5. Empty HandleParseError Method
- **File:** `Program.cs:34-36`
- **Issue:** CLI parse errors are silently ignored
- **Fix:**
  ```csharp
  private static void HandleParseError(IEnumerable<Error> errors)
  {
      foreach (var error in errors)
      {
          Console.Error.WriteLine($"Argument error: {error.Tag}");
      }
      Environment.Exit(1);
  }
  ```

### 6. Null Dereference in AAXtoM4BConvertor Metadata
- **File:** `AAXtoM4BConvertor.cs:330-331`
- **Issue:** Null-conditional followed by direct access can throw
- **Fix:**
  ```csharp
  var title = aaxInfo?.format?.tags?.title;
  if (title is null) return;
  var boxset = regex.Match(title).Success;
  ```

### 7. Null Dereference in AAXCtoM4BConvertor Metadata
- **File:** `AAXCtoM4BConvertor.cs:330-331`
- **Issue:** Same as #6, null-handling issue in AddMetadataToM4A
- **Fix:** Same pattern as #6

### 8. Async Methods Synchronously Waited
- **Files:** `AAXtoM4BConvertor.cs`, `AAXCtoM4BConvertor.cs`
- **Issue:** Multiple `.Wait()` calls on async methods
- **Note:** Acceptable for console apps, but consider migrating to `await` for .NET 10
- **Locations:**
  - `AnalyzeFile().Wait()` (lines 81, 91)
  - `conversion.Start().Wait()` (throughout)

---

## Medium Priority

### 9. Redundant `this.` Qualifiers
- **File:** `AAXCtoM4BConvertor.cs:65-66, 242`
- **Fix:** Remove `this.` prefix from field assignments

### 10. Inconsistent String Empty Handling
- **Files:** Multiple
- **Issue:** Mix of `""` and `string.Empty`
- **Fix:** Standardize on one style across the project

### 11. Remove Dead Code - ProgressMeter
- **File:** `AAXtoM4BConvertor.cs:263`
- **Issue:** `ProgressMeter` method defined but event subscription commented out
- **Fix:** Remove unused method

### 12. Inconsistent Null Check Patterns
- **Files:** Multiple
- **Issue:** Mix of `!= null`, `is not null`, and `?.`
- **Fix:** Standardize on pattern matching (`is null`/`is not null`)

---

## Low Priority

### 13. Magic Numbers
- **Files:** Multiple
- **Issue:** Magic numbers without named constants
- **Fix:** Define constants:
  ```csharp
  private const int LoopSleepMinutes = 5;
  private const int MaxAuthorsBeforeVarious = 4;
  private const int SpinnerDelayMs = 50;
  ```

### 14. Document Newtonsoft.Json Usage
- **File:** `AudibleVoucherDto.cs`
- **Issue:** Newtonsoft.Json retained for API compatibility
- **Fix:** Add comment explaining why Newtonsoft is kept

---

## Completed (Auto-fixed in Audit)

- [x] DTO classes migrated to System.Text.Json (except AudibleVoucherDto)
- [x] Added namespace Harmony.Dto to AaxInfoDto.cs
- [x] Added nullable annotations to DTO properties
- [x] Renamed colliding DTO classes (Chapter, Tags, Format prefixed)
- [x] Logger.cs converted to primary constructor syntax
- [x] ChapterConvertor.cs string interpolation and null-conditional operators
- [x] FFProbeAnalyzer.cs renamed to correct PascalCase
- [x] Program.cs property naming standardized
- [x] AAXtoM4BConvertor.cs pattern matching and string interpolation
- [x] AAXCtoM4BConvertor.cs modernized same as AAXtoM4BConvertor

---

## Future Considerations

- [ ] Add unit tests (xUnit + FluentAssertions)
- [ ] Add GitHub Actions CI for all target platforms
- [ ] Implement structured logging (replace Console.WriteLine)
- [ ] Add appsettings.json configuration support
- [ ] Complete ConfigureAwait(false) throughout async methods
- [ ] Evaluate full async/await migration