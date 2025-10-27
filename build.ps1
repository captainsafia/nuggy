# Build script for creating single-file executables locally on Windows
# Usage: .\build.ps1 [runtime-id]
# Example: .\build.ps1 win-x64

param(
    [string]$Runtime = "win-x64"
)

$PROJECT = "src\Nuggy.Cli.csproj"
$OUTPUT_DIR = ".\artifacts\publish"
$VERSION = "1.0.0"
$ASSEMBLY_VERSION = "1.0.0.0"
$FILE_VERSION = "1.0.0.0"

Write-Host "Building for runtime: $Runtime" -ForegroundColor Green

# Clean previous builds
$runtimePath = Join-Path $OUTPUT_DIR $Runtime
if (Test-Path $runtimePath) {
    Remove-Item -Recurse -Force $runtimePath
}

# Publish single-file executable
dotnet publish $PROJECT `
    --configuration Release `
    --runtime $Runtime `
    --self-contained true `
    --output $runtimePath `
    -p:PublishSingleFile=true `
    -p:PublishTrimmed=true `
    -p:TrimMode=link `
    -p:EnableCompressionInSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:AssemblyVersion=$ASSEMBLY_VERSION `
    -p:FileVersion=$FILE_VERSION `
    -p:Version=$VERSION

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build completed successfully!" -ForegroundColor Green
    Write-Host "Output location: $runtimePath" -ForegroundColor Yellow
    
    # List the output files
    Write-Host "Generated files:" -ForegroundColor Yellow
    Get-ChildItem $runtimePath | Format-Table Name, Length, LastWriteTime
} else {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}