# Harmony

A modern AAX/AAXC to M4B converter for audiobooks. Harmony converts Audible AAX and AAXC files to standard M4B format with embedded metadata, chapters, and cover art—compatible with Audiobookshelf and other audiobook players.

## Overview

Harmony is a .NET 10.0 console application that processes Audible AAX and AAXC audiobook files and converts them to M4B format. It preserves metadata (title, author), embeds chapters, and includes cover art. The output is fully compatible with Audiobookshelf, Plex, and other media servers.

**Note:** AAX files require activation bytes (pass them with `-a`). If a book exists as both AAX and AAXC, the newer file is converted and the older one is skipped.

## Features

- Convert AAX and AAXC audiobooks to M4B format
- Convert whichever source file is newer when a book exists in both formats
- Preserve embedded metadata (title, author, narrator)
- Embed chapters from optional chapters.json
- Embed cover art from voucher files
- Batch processing of multiple files
- Progress tracking with ETA
- Graceful cancellation (Ctrl+C)
- Automated FFmpeg download

## Quick Start

```bash
# Build the project
dotnet build

# Run with input/output directories
dotnet run -- -i /path/to/aaxc/files -o /path/to/output

# Or build and run the published executable
dotnet publish -c Release
./bin/Release/net10.0/Harmony -i ./input -o ./output
```

## Standard Workflow

### Prerequisites

1. **Install audible-cli** - Use [audible-cli](https://github.com/mkb79/audible-cli) to download your Audible library
2. **Download required files**:
   - `library.tsv` - Your Audible library metadata
   - `.aaxc` files - Encrypted audiobook files
   - `.voucher` files - DRM keys for each audiobook
   - Optional: `-chapters.json` files for chapter metadata

### Example Setup

```bash
# Using audible-cli to download your library
audible library export -t library.tsv

# Download your audiobooks (produces .aaxc and .voucher files)
audible download --all

# Convert with Harmony
Harmony -i ./Downloads -o ./Audiobooks
```

### File Requirements

For each audiobook, you need:

| File Type | Required | Description |
|-----------|----------|-------------|
| `.aaxc` | With matching voucher, yes | The encrypted audiobook file from Audible |
| `.voucher` | For AAXC, yes | DRM license file (must match audiobook name) |
| `.aax` | With activation bytes, yes | Legacy encrypted audiobook file — needs `-a` |
| `library.tsv` | Optional | Library metadata for better title/author info |
| `-chapters.json` | Optional | Chapter metadata (e.g., `Title-chapters.json`) |

**Example directory structure:**
```
input/
├── library.tsv
├── Book_1.aaxc
├── Book_1.voucher
├── Book_1-chapters.json
├── Book_2.aaxc
└── Book_2.voucher
```

## CLI Usage

### Basic Options

```bash
Harmony [options]
```

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--input` | `-i` | Input folder containing AAX/AAXC files | |
| `--output` | `-o` | Output folder for M4B files | |
| `--bitrate` | `-b` | Output bitrate in kilobits | 64 |
| `--activation-bytes` | `-a` | Activation bytes for decoding AAX files | |
| `--clobber` | `-c` | Overwrite existing output files | false |
| `--quiet` | `-q` | Disable all console output | false |
| `--fetch-ffmpeg` | `-f` | Download FFmpeg and exit | |

### Examples

```bash
# Basic conversion
Harmony -i ./input -o ./output

# High quality output (128 kbps)
Harmony -i ./input -o ./output -b 128

# Overwrite existing files
Harmony -i ./input -o ./output --clobber

# Quiet mode (no output)
Harmony -i ./input -o ./output --quiet

# Convert legacy AAX files (activation bytes required)
Harmony -i ./input -o ./output -a your_activation_bytes

# Just download FFmpeg
Harmony --fetch-ffmpeg
```

### AAX and AAXC Together

When your input folder contains both formats:

- Books present in both formats convert **only the newer file**. Files pair by filename stem (`Book.aax` pairs with `Book.aaxc`); modification time decides the winner, and a tie favors AAXC.
- When the AAX file wins but no activation bytes were supplied, the paired AAXC is converted instead so the book is never lost.
- AAX files without activation bytes and without an AAXC copy are skipped with a warning; the run continues.

## Building

### Prerequisites

- .NET 10.0 SDK
- FFmpeg (automatically downloaded on first run)

### Build Commands

```bash
# Debug build
dotnet build

# Release build
dotnet build -c Release

# Publish as single-file executable (Windows)
dotnet publish -r win-x64 -c Release -p:PublishSingleFile=true -p:PublishTrimmed=true

# Publish as single-file executable (macOS Intel)
dotnet publish -r osx-x64 -c Release -p:PublishSingleFile=true -p:PublishTrimmed=true

# Publish as single-file executable (macOS ARM)
dotnet publish -r osx-arm64 -c Release -p:PublishSingleFile=true -p:PublishTrimmed=true

# Publish as single-file executable (Linux x64)
dotnet publish -r linux-x64 -c Release -p:PublishSingleFile=true -p:PublishTrimmed=true

# Publish as single-file executable (Linux ARM64)
dotnet publish -r linux-arm64 -c Release -p:PublishSingleFile=true -p:PublishTrimmed=true
```

## Output

### M4B Features

Each converted M4B file includes:

- **Audio**: AAC encoded at specified bitrate (default 64 kbps)
- **Metadata**: Title, author, narrator from library.tsv or voucher
- **Chapters**: Embedded chapters from `-chapters.json` (if provided)
- **Cover Art**: Embedded cover image from voucher files

### Filename Conventions

Output files preserve the original audiobook name:
```
Book_Title.aaxc → Book_Title.m4b
```

## Audiobookshelf Compatibility

Harmony generates M4B files fully compatible with [Audiobookshelf](https://www.audiobookshelf.org/):

- ✅ Standard M4B container format
- ✅ Embedded metadata (title, author, narrator)
- ✅ Chapter markers for navigation
- ✅ Embedded cover artwork
- ✅ Properly formatted for scanning

Simply point Audiobookshelf to your output directory and scan for audiobooks.

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| CommandLineParser | 2.9.1 | CLI argument parsing |
| CsvHelper | 33.0.1 | TSV library file parsing |
| Instances | 3.0.0 | Process invocation |
| Newtonsoft.Json | 13.0.3 | JSON serialization |
| Spectre.Console | 0.49.1 | Terminal UI components |
| TagLibSharp | 2.3.0 | Audio metadata handling |
| Xabe.FFmpeg.Downloader | 5.2.6 | FFmpeg wrapper |

## Sample Files

The `samples/` directory contains example files for testing:

```
samples/
├── library.tsv                         # Library metadata
├── Year_Zero_A_Novel-AAX_22_64.aaxc   # Sample audiobook
├── Year_Zero_A_Novel-AAX_22_64.voucher # DRM license
└── Year_Zero_A_Novel-chapters.json    # Chapter metadata
```

## Development

### Testing

```bash
# Run all tests
dotnet test

# Run specific test
dotnet test --filter "FullyQualifiedName~TestClassName"
```

### Code Style

- .NET 10.0 with C# 12
- Nullable reference types enabled
- 4-space indentation
- PascalCase for public members
- Underscore prefix for private fields

## License

Copyright © 2023 Jason Rennie

## AAX Support

AAX files use the classic activation-bytes DRM and are supported again in Harmony 2.x. To obtain your activation bytes, see [inAudible-NG/tables](https://github.com/inAudible-NG/tables) or run `audible activation-bytes` with [audible-cli](https://github.com/mkb79/audible-cli), then pass them with `-a`/`--activation-bytes`.

## Troubleshooting

### Common Issues

| Issue | Solution |
|-------|----------|
| "No AAX or AAXC files found" | Ensure files have `.aax` or `.aaxc` extension |
| "Missing voucher file" | Each `.aaxc` requires a matching `.voucher` file |
| "AAX files... will be skipped" | Pass your activation bytes with `-a` (see AAX Support) |
| FFmpeg not found | Run with `--fetch-ffmpeg` or install FFmpeg in PATH |

### Verbose Output

Run without `--quiet` to see detailed progress including:
- File conversion status
- FFmpeg command output
- Metadata extraction details
- Progress percentage with ETA

## Contributing

Contributions are welcome! Please read AGENTS.md for development guidelines and coding standards.

## Links

- [Repository](https://github.com/superversivesf/Harmony-2.0)
- [audible-cli](https://github.com/mkb79/audible-cli) - Tool for downloading AAXC files
- [Audiobookshelf](https://www.audiobookshelf.org/) - Compatible media server