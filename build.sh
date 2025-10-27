#!/bin/bash

# Build script for creating single-file executables locally
# Usage: ./build.sh [runtime-id]
# Example: ./build.sh linux-x64

set -e

PROJECT="src/Nuggy.Cli.csproj"
OUTPUT_DIR="./artifacts/publish"
VERSION="1.0.0"
ASSEMBLY_VERSION="1.0.0.0"
FILE_VERSION="1.0.0.0"

# Default to current platform if no runtime specified
if [ -z "$1" ]; then
    case "$(uname -s)" in
        Linux*)     RUNTIME="linux-x64";;
        Darwin*)    RUNTIME="osx-x64";;
        CYGWIN*|MINGW*|MSYS*) RUNTIME="win-x64";;
        *)          echo "Unknown platform. Please specify runtime manually."; exit 1;;
    esac
else
    RUNTIME="$1"
fi

echo "Building for runtime: $RUNTIME"

# Clean previous builds
rm -rf "$OUTPUT_DIR/$RUNTIME"

# Publish single-file executable
dotnet publish "$PROJECT" \
    --configuration Release \
    --runtime "$RUNTIME" \
    --self-contained true \
    --output "$OUTPUT_DIR/$RUNTIME" \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=true \
    -p:TrimMode=link \
    -p:EnableCompressionInSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:AssemblyVersion="$ASSEMBLY_VERSION" \
    -p:FileVersion="$FILE_VERSION" \
    -p:Version="$VERSION"

echo "Build completed successfully!"
echo "Output location: $OUTPUT_DIR/$RUNTIME"

# List the output files
echo "Generated files:"
ls -la "$OUTPUT_DIR/$RUNTIME"