# nuggy

<div align="center">
  <img src="assets/logo.png" alt="Nuggy Logo" width="200"/>
</div>

nuggy is a command-line tool that provides an intuitive interface to explore package metadata, view package contents, and manage NuGet feed sources.

## Installation

### As a .NET Global Tool (Recommended)

```bash
# Install from NuGet
dotnet tool install -g Nuggy.Cli
```

### Install from local source

```bash
git clone https://github.com/captainsafia/nuggy.git
cd nuggy/src
dotnet pack
dotnet tool install -g Nuggy.Cli --add-source ../artifacts/packages
```

### Build from Source

```bash
git clone https://github.com/captainsafia/nuggy.git
cd nuggy/src
dotnet build
dotnet run -- --help
```

## Usage

### Getting Started

```bash
# Show help
nuggy --help

# Show version
nuggy --version
```

### :globe_with_meridians: Feed Management

#### List all configured feeds
```bash
nuggy feeds list
```

#### Add a new feed
```bash
# Add the official NuGet.org feed
nuggy feeds add --name "nuget.org" --source "https://api.nuget.org/v3/index.json"

# Add a feed and set it as default
nuggy feeds add --name "dotnet10" --source "https://www.myget.org/F/aspnetcore-dev/api/v3/index.json" --default
```

#### Set default feed
```bash
nuggy feeds set nuget.org
```

### :package: Package Operations

#### View package metadata
```bash
# Get metadata for the latest version
nuggy package metadata Newtonsoft.Json

# Get metadata for a specific version
nuggy package metadata Newtonsoft.Json --version 13.0.3

# Use a specific feed
nuggy package metadata Newtonsoft.Json --feed nuget.org
```

#### List package versions
```bash
# List all versions of a package
nuggy package versions Newtonsoft.Json

# Use a specific feed
nuggy package versions Newtonsoft.Json --feed nuget.org
```

#### View and download package contents

```bash
# Download and view package contents (saves to global packages folder)
nuggy package show Newtonsoft.Json

# Show a specific version
nuggy package show Newtonsoft.Json --version 13.0.3

# Use a specific feed
nuggy package show Newtonsoft.Json --feed nuget.org
```

## Contributing

### :gear: Development Setup

#### Prerequisites
- .NET 9.0 SDK (see `global.json` for exact version)
- Git

#### Getting Started
```bash
# Clone the repository
git clone https://github.com/YOURORGNAME/nuggy.git
cd nuggy

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run --project src -- --help

# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "ClassName=FeedConfigurationTests"
```
