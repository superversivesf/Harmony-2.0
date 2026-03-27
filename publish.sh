#!/bin/bash
# Cross-platform publish script for Harmony
# Builds single-file executables for macOS (x64+arm64), Windows x64, and Linux (x64+arm64)

set -e

PROJECT="Harmony.csproj"
CONFIG="Release"
OUTPUT_DIR="publish"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Runtime identifiers for cross-platform builds
RUNTIMES=("osx-x64" "osx-arm64" "win-x64" "linux-x64" "linux-arm64")

# Clean output directory
clean() {
    info "Cleaning output directory..."
    rm -rf "$OUTPUT_DIR"
    mkdir -p "$OUTPUT_DIR"
}

# Publish for a specific runtime
publish() {
    local runtime=$1
    info "Publishing for $runtime..."
    
    dotnet publish "$PROJECT" \
        -r "$runtime" \
        -c "$CONFIG" \
        -p:PublishSingleFile=true \
        -p:PublishTrimmed=true \
        -o "$OUTPUT_DIR/$runtime"
    
    if [ $? -eq 0 ]; then
        info "Successfully published $runtime to $OUTPUT_DIR/$runtime/"
    else
        error "Failed to publish $runtime"
        return 1
    fi
}

# Main
main() {
    info "Starting cross-platform publish for Harmony..."
    
    # Clean previous build artifacts
    clean
    
    # Build for each platform
    for runtime in "${RUNTIMES[@]}"; do
        publish "$runtime"
    done
    
    info "All platforms published successfully!"
    info "Output directory: $OUTPUT_DIR/"
    ls -la "$OUTPUT_DIR/"
}

# Run main function
main "$@"