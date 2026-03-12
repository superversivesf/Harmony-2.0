# AGENTS.md - Harmony 2.0

Guidelines for AI coding agents working in this repository.

## Project Overview

Harmony is a .NET 10.0 console application that converts Audible AAX/AAXC audiobook files to M4B format. It uses FFmpeg for audio processing and TagLibSharp for metadata management.

## Build Commands

```bash
# Build the project
dotnet build

# Build in Release mode
dotnet build -c Release

# Run the application
dotnet run -- [options]

# Publish as single-file executable
dotnet publish -r win-x64 -c Release -p:PublishSingleFile=true -p:PublishTrimmed=true
dotnet publish -r osx-x64 -c Release -p:PublishSingleFile=true -p:PublishTrimmed=true
dotnet publish -r linux-x64 -c Release -p:PublishSingleFile=true -p:PublishTrimmed=true
dotnet publish -r linux-arm64 -c Release -p:PublishSingleFile=true -p:PublishTrimmed=true
```

## Lint/Format Commands

```bash
# Format code (whitespace, style)
dotnet format

# Format with verification only (CI)
dotnet format --verify-no-changes

# Build with warnings as errors
dotnet build /warnaserror
```

## Test Commands

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~ClassName"

# Run specific test method
dotnet test --filter "FullyQualifiedName~ClassName.MethodName"
```

## Dependencies

- **CommandLineParser** (2.9.1) - CLI argument parsing
- **CsvHelper** (33.0.1) - TSV library file parsing
- **Instances** (3.0.0) - Process invocation
- **Newtonsoft.Json** (13.0.3) - JSON serialization (legacy DTOs)
- **Spectre.Console** (0.49.1) - Terminal UI components
- **TagLibSharp** (2.3.0) - Audio metadata handling
- **Xabe.FFmpeg.Downloader** (5.2.6) - FFmpeg wrapper

## Code Style Guidelines

### Imports and Usings

- Implicit usings are enabled (`<ImplicitUsings>enable</ImplicitUsings>`)
- Place `using` statements at the top, alphabetically sorted
- Group imports: System namespaces first, then third-party, then project namespaces
- Use fully qualified names for disambiguation (e.g., `System.IO.File` vs `TagLib.File`)

```csharp
using System.Diagnostics;
using System.Text.Json;
using Harmony.Dto;
using TagLib;
using File = System.IO.File;
```

### Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Classes | PascalCase | `AaxToM4BConvertor` |
| Methods | PascalCase | `ProcessAaxFile` |
| Properties | PascalCase | `public string Title { get; set; }` |
| Private fields | Underscore + camelCase | `_activationBytes`, `_inputFolder` |
| Local variables | camelCase | `outputDirectory`, `logger` |
| Constants | PascalCase | `MaxAuthorCountForIndividualDisplay` |
| Parameters | camelCase | `quietMode`, `inputFolder` |

### Type System

- Nullable reference types are enabled (`<Nullable>enable</Nullable>`)
- Use `?` for nullable reference types: `string?`, `List<T>?`
- Use `int`, `bool` for non-nullable value types
- Prefer `var` for local variables when type is obvious

### Formatting

- Indent with 4 spaces
- Opening braces on same line for control structures
- Blank line between method definitions
- Single statement per line

### Class Structure

- Internal classes preferred unless public access required
- Constructor parameters should initialize private readonly fields
- One class per file (DTOs may have multiple related classes)

```csharp
internal class AaxToM4BConvertor
{
    private readonly string _activationBytes;
    private readonly int _bitrate;

    public AaxToM4BConvertor(string activationBytes, int bitrate)
    {
        _activationBytes = activationBytes;
        _bitrate = bitrate;
    }
}
```

### Error Handling

- Throw exceptions for unrecoverable errors with descriptive messages
- Catch exceptions at appropriate boundaries
- Prefer early returns for validation failures

```csharp
if (!Directory.Exists(_inputFolder))
    throw new Exception("Input folder does not exist: " + _inputFolder);
```

### Null Handling

- Check for null before accessing nullable types
- Use null-conditional (`?.`) and null-coalescing (`??`) operators
- Never suppress null warnings

### Async/Await

- Use `async`/`await` for I/O-bound operations
- Prefer `ConfigureAwait(false)` for library code
- Avoid `async void` except for event handlers

### Comments

- Code should be self-documenting; prefer clear naming
- Use `// ReSharper disable` comments to suppress warnings when justified
- Avoid commented-out code in production

### JSON Handling

- Prefer `System.Text.Json` for new code
- Use `Newtonsoft.Json` for legacy DTOs with `[JsonProperty]` attributes
- Use `PropertyNameCaseInsensitive = true` when deserializing

```csharp
JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
});
```

## Testing Guidelines

- Use **xUnit** with **FluentAssertions**
- Tests are in `Harmony.Tests/` project
- Use `[Fact]` for single test cases, `[Theory]` for parameterized tests
- Test both public API and internal methods via reflection if needed

## External Dependencies

- **FFmpeg/FFprobe** must be available in PATH or downloaded via `-f` flag
- Application downloads FFmpeg automatically if missing
- Requires .NET 10.0 SDK for development
