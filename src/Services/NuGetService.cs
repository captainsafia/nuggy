using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Spectre.Console;
using System.IO.Compression;

namespace Nuggy.Services;

public class NuGetService(FeedService feedService)
{
    private static string GetGlobalPackagesFolder()
    {
        // Check environment variable first
        var nugetPackagesPath = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        if (!string.IsNullOrEmpty(nugetPackagesPath))
        {
            return nugetPackagesPath;
        }

        // Try to get from NuGet configuration
        try
        {
            var settings = Settings.LoadDefaultSettings(root: null);
            var globalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder(settings);
            if (!string.IsNullOrEmpty(globalPackagesFolder))
            {
                return globalPackagesFolder;
            }
        }
        catch
        {
            // Fall back to default if configuration loading fails
        }

        // Default path
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nuget",
            "packages"
        );
    }

    public async Task DisplayPackageMetadataAsync(string packageName, string? feedName = null, string? version = null)
    {
        await AnsiConsole.Status()
            .StartAsync("Fetching package metadata...", async ctx =>
            {
                var repository = await feedService.GetRepositoryAsync(feedName);
                var metadataResource = await repository.GetResourceAsync<PackageMetadataResource>();
                var cancellationToken = CancellationToken.None;

                var packages = await metadataResource.GetMetadataAsync(
                    packageName,
                    includePrerelease: true,
                    includeUnlisted: false,
                    new SourceCacheContext(),
                    NullLogger.Instance,
                    cancellationToken);

                IPackageSearchMetadata? targetPackage;
                if (!string.IsNullOrEmpty(version))
                {
                    if (NuGetVersion.TryParse(version, out var parsedVersion))
                    {
                        targetPackage = packages.FirstOrDefault(p => p.Identity.Version.Equals(parsedVersion));
                        if (targetPackage == null)
                        {
                            AnsiConsole.MarkupLine($"[red]Version '{version}' of package '{packageName}' not found[/]");
                            return;
                        }
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]Invalid version format: '{version}'[/]");
                        return;
                    }
                }
                else
                {
                    targetPackage = packages.OrderByDescending(p => p.Identity.Version).FirstOrDefault();
                    if (targetPackage == null)
                    {
                        AnsiConsole.MarkupLine($"[red]Package '{packageName}' not found[/]");
                        return;
                    }
                }

                ctx.Status("Formatting metadata...");
                await DisplayPackageDetailsAsync(targetPackage);
            });
    }

    public async Task DisplayPackageVersionsAsync(string packageName, string? feedName = null)
    {
        await AnsiConsole.Status()
            .StartAsync("Fetching package versions...", async ctx =>
            {
                var repository = await feedService.GetRepositoryAsync(feedName);
                var metadataResource = await repository.GetResourceAsync<PackageMetadataResource>();
                var cancellationToken = CancellationToken.None;

                var packages = await metadataResource.GetMetadataAsync(
                    packageName,
                    includePrerelease: true,
                    includeUnlisted: false,
                    new SourceCacheContext(),
                    NullLogger.Instance,
                    cancellationToken);

                var orderedPackages = packages.OrderByDescending(p => p.Identity.Version).ToList();

                if (orderedPackages.Count == 0)
                {
                    AnsiConsole.MarkupLine($"[red]Package '{packageName}' not found[/]");
                    return;
                }

                ctx.Status("Formatting versions table...");
                DisplayVersionsTable(packageName, orderedPackages);
            });
    }

    public async Task DisplayPackageContentsAsync(string packageName, string? feedName = null, string? version = null)
    {
        await AnsiConsole.Status()
            .StartAsync("Downloading and analyzing package...", async ctx =>
            {
                var repository = await feedService.GetRepositoryAsync(feedName);
                var metadataResource = await repository.GetResourceAsync<PackageMetadataResource>();
                var downloadResource = await repository.GetResourceAsync<DownloadResource>();
                var cancellationToken = CancellationToken.None;

                var packages = await metadataResource.GetMetadataAsync(
                    packageName,
                    includePrerelease: true,
                    includeUnlisted: false,
                    new SourceCacheContext(),
                    NullLogger.Instance,
                    cancellationToken);

                IPackageSearchMetadata? targetPackage;
                if (!string.IsNullOrEmpty(version))
                {
                    if (NuGetVersion.TryParse(version, out var parsedVersion))
                    {
                        targetPackage = packages.FirstOrDefault(p => p.Identity.Version.Equals(parsedVersion));
                        if (targetPackage == null)
                        {
                            AnsiConsole.MarkupLine($"[red]Version '{version}' of package '{packageName}' not found[/]");
                            return;
                        }
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]Invalid version format: '{version}'[/]");
                        return;
                    }
                }
                else
                {
                    targetPackage = packages.OrderByDescending(p => p.Identity.Version).FirstOrDefault();
                    if (targetPackage == null)
                    {
                        AnsiConsole.MarkupLine($"[red]Package '{packageName}' not found[/]");
                        return;
                    }
                }

                var globalPackagesFolder = GetGlobalPackagesFolder();
                var packagePath = Path.Combine(globalPackagesFolder, targetPackage.Identity.Id.ToLowerInvariant(), targetPackage.Identity.Version.ToNormalizedString());

                // Check if package already exists in global packages folder
                if (Directory.Exists(packagePath))
                {
                    ctx.Status("Package already exists in global packages folder. Displaying contents...");
                    await DisplayPackageContentsFromDirectoryAsync(packagePath, targetPackage.Identity.Id, targetPackage.Identity.Version.ToString());
                    AnsiConsole.MarkupLine($"[green]Package location: {packagePath}[/]");
                    return;
                }

                ctx.Status($"Downloading {packageName} v{targetPackage.Identity.Version}...");

                var downloadResult = await downloadResource.GetDownloadResourceResultAsync(
                    targetPackage.Identity,
                    new PackageDownloadContext(new SourceCacheContext()),
                    globalPackagesFolder,
                    NullLogger.Instance,
                    cancellationToken);

                if (downloadResult.Status != DownloadResourceResultStatus.Available || downloadResult.PackageStream == null)
                {
                    AnsiConsole.MarkupLine("[red]Failed to download package[/]");
                    return;
                }

                ctx.Status("Saving to global packages folder...");
                await SavePackageToGlobalFolderAsync(downloadResult.PackageStream, packagePath, targetPackage.Identity.Id, targetPackage.Identity.Version.ToString());

                AnsiConsole.MarkupLine($"[green]Package saved to: {packagePath}[/]");
            });
    }

    private static Task DisplayPackageDetailsAsync(IPackageSearchMetadata package)
    {
        var table = new Table()
            .AddColumn("[bold]Property[/]")
            .AddColumn("[bold]Value[/]");

        table.AddRow("Package", package.Identity.Id);
        table.AddRow("Version", package.Identity.Version.ToString());
        table.AddRow("Authors", package.Authors ?? "N/A");
        table.AddRow("Description", package.Description ?? "N/A");
        table.AddRow("License", package.LicenseMetadata?.License ?? package.LicenseUrl?.ToString() ?? "N/A");
        table.AddRow("Project URL", package.ProjectUrl?.ToString() ?? "N/A");
        table.AddRow("Tags", package.Tags ?? "N/A");
        table.AddRow("Download Count", package.DownloadCount?.ToString("N0") ?? "N/A");
        table.AddRow("Published", package.Published?.ToString("yyyy-MM-dd HH:mm:ss UTC") ?? "N/A");

        var panel = new Panel(table)
        {
            Header = new PanelHeader($"[bold green]{package.Identity.Id}[/]"),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(panel);

        if (package.DependencySets.Any())
        {
            var dependenciesTable = new Table()
                .AddColumn("[bold]Target Framework[/]")
                .AddColumn("[bold]Dependencies[/]");

            foreach (var depSet in package.DependencySets)
            {
                var framework = depSet.TargetFramework?.GetShortFolderName() ?? "Any";
                var deps = depSet.Packages.Any()
                    ? string.Join(", ", depSet.Packages.Select(d => $"{d.Id} {d.VersionRange}"))
                    : "None";

                dependenciesTable.AddRow(framework, Markup.Escape(deps));
            }

            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Panel(dependenciesTable)
            {
                Header = new PanelHeader("[bold]Dependencies[/]"),
                Border = BoxBorder.Rounded
            });
        }

        return Task.CompletedTask;
    }

    private static void DisplayVersionsTable(string packageName, List<IPackageSearchMetadata> packages)
    {
        var table = new Table()
            .AddColumn("[bold]Version[/]")
            .AddColumn("[bold]Published[/]")
            .AddColumn("[bold]Downloads[/]")
            .AddColumn("[bold]Prerelease[/]");

        foreach (var package in packages)
        {
            var isPrerelease = package.Identity.Version.IsPrerelease ? "[yellow]Yes[/]" : "[green]No[/]";
            var published = package.Published?.ToString("yyyy-MM-dd") ?? "N/A";
            var downloads = package.DownloadCount?.ToString("N0") ?? "N/A";

            table.AddRow(
                package.Identity.Version.ToString(),
                published,
                downloads,
                isPrerelease
            );
        }

        AnsiConsole.Write(new Panel(table)
        {
            Header = new PanelHeader($"[bold green]{packageName} - All Versions[/]"),
            Border = BoxBorder.Rounded
        });
    }

    private async Task SavePackageToGlobalFolderAsync(Stream packageStream, string packagePath, string packageId, string version)
    {
        // Create the package directory
        Directory.CreateDirectory(packagePath);

        // Extract the package to the global packages folder
        using var archive = new ZipArchive(packageStream, ZipArchiveMode.Read);

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name))
                continue;

            var entryPath = Path.Combine(packagePath, entry.FullName);
            var entryDirectory = Path.GetDirectoryName(entryPath);

            if (!string.IsNullOrEmpty(entryDirectory))
            {
                Directory.CreateDirectory(entryDirectory);
            }

            using var entryStream = entry.Open();
            using var fileStream = File.Create(entryPath);
            await entryStream.CopyToAsync(fileStream);
        }

        // Display the contents from the saved directory
        await DisplayPackageContentsFromDirectoryAsync(packagePath, packageId, version);
    }

    private static Task DisplayPackageContentsFromDirectoryAsync(string packagePath, string packageId, string version)
    {
        var files = Directory.GetFiles(packagePath, "*", SearchOption.AllDirectories)
            .Select(f => new FileInfo(f))
            .OrderBy(f => f.FullName)
            .ToList();

        var table = new Table()
            .AddColumn("[bold]Path[/]")
            .AddColumn("[bold]Size[/]");

        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(packagePath, file.FullName);
            var fileIcon = GetFileIcon(file.Name);
            var sizeText = FormatFileSize(file.Length);
            var escapedPath = Markup.Escape(relativePath);
            table.AddRow($"{fileIcon} {escapedPath}", sizeText);
        }

        AnsiConsole.Write(new Panel(table)
        {
            Header = new PanelHeader($"[bold green]{packageId} v{version} - Package Contents[/]"),
            Border = BoxBorder.Rounded
        });

        // Display summary
        var totalFiles = files.Count;
        var totalSize = files.Sum(f => f.Length);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[dim]Total files: {totalFiles}, Total size: {FormatFileSize(totalSize)}[/]");

        return Task.CompletedTask;
    }

    private static Task DisplayPackageContentsFromStreamAsync(Stream packageStream, string packageId, string version)
    {
        using var archive = new ZipArchive(packageStream, ZipArchiveMode.Read);

        var entries = archive.Entries
            .Where(e => !string.IsNullOrEmpty(e.Name))
            .OrderBy(e => e.FullName)
            .ToList();

        var table = new Table()
            .AddColumn("[bold]Path[/]")
            .AddColumn("[bold]Size[/]");

        foreach (var entry in entries)
        {
            var fileIcon = GetFileIcon(entry.Name);
            var sizeText = FormatFileSize(entry.Length);
            var escapedPath = Markup.Escape(entry.FullName);
            table.AddRow($"{fileIcon} {escapedPath}", sizeText);
        }

        AnsiConsole.Write(new Panel(table)
        {
            Header = new PanelHeader($"[bold green]{packageId} v{version} - Package Contents[/]"),
            Border = BoxBorder.Rounded
        });

        // Display summary
        var totalFiles = entries.Count;
        var totalSize = entries.Sum(e => e.Length);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[dim]Total files: {totalFiles}, Total size: {FormatFileSize(totalSize)}[/]");

        return Task.CompletedTask;
    }

    private static string GetFileIcon(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".dll" => "🔧",
            ".exe" => "⚙️",
            ".xml" => "📄",
            ".json" => "📋",
            ".md" => "📖",
            ".txt" => "📝",
            ".nuspec" => "📦",
            ".targets" => "🎯",
            ".props" => "🏷️",
            _ => "📄"
        };
    }

    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB" };
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        return $"{number:n1} {suffixes[counter]}";
    }

    public async Task ExtractPackageFileAsync(string packageName, string filePath, string? feedName = null, string? version = null)
    {
        var repository = await feedService.GetRepositoryAsync(feedName);
        var metadataResource = await repository.GetResourceAsync<PackageMetadataResource>();
        var downloadResource = await repository.GetResourceAsync<DownloadResource>();
        var cancellationToken = CancellationToken.None;

        var packages = await metadataResource.GetMetadataAsync(
            packageName,
            includePrerelease: true,
            includeUnlisted: false,
            new SourceCacheContext(),
            NullLogger.Instance,
            cancellationToken);

        IPackageSearchMetadata? targetPackage;
        if (!string.IsNullOrEmpty(version))
        {
            if (NuGetVersion.TryParse(version, out var parsedVersion))
            {
                targetPackage = packages.FirstOrDefault(p => p.Identity.Version.Equals(parsedVersion));
                if (targetPackage == null)
                {
                    throw new InvalidOperationException($"Version '{version}' of package '{packageName}' not found");
                }
            }
            else
            {
                throw new ArgumentException($"Invalid version format: '{version}'");
            }
        }
        else
        {
            targetPackage = packages.OrderByDescending(p => p.Identity.Version).FirstOrDefault();
            if (targetPackage == null)
            {
                throw new InvalidOperationException($"Package '{packageName}' not found");
            }
        }

        var globalPackagesFolder = GetGlobalPackagesFolder();
        var packagePath = Path.Combine(globalPackagesFolder, targetPackage.Identity.Id.ToLowerInvariant(), targetPackage.Identity.Version.ToNormalizedString());

        // Check if package already exists in global packages folder
        if (Directory.Exists(packagePath))
        {
            var localFilePath = Path.Combine(packagePath, filePath);
            if (File.Exists(localFilePath))
            {
                var fileContent = await File.ReadAllTextAsync(localFilePath);
                Console.Write(fileContent);
                return;
            }
            else
            {
                throw new FileNotFoundException($"File '{filePath}' not found in package '{packageName}' v{targetPackage.Identity.Version}");
            }
        }

        // Download package if not in global packages folder
        var downloadResult = await downloadResource.GetDownloadResourceResultAsync(
            targetPackage.Identity,
            new PackageDownloadContext(new SourceCacheContext()),
            globalPackagesFolder,
            NullLogger.Instance,
            cancellationToken);

        if (downloadResult.Status != DownloadResourceResultStatus.Available || downloadResult.PackageStream == null)
        {
            throw new InvalidOperationException("Failed to download package");
        }

        // Extract the specific file from the package stream
        using var archive = new ZipArchive(downloadResult.PackageStream, ZipArchiveMode.Read);
        var entry = archive.Entries.FirstOrDefault(e =>
            string.Equals(e.FullName, filePath, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(e.FullName.Replace('/', Path.DirectorySeparatorChar), filePath, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(e.FullName.Replace('\\', Path.DirectorySeparatorChar), filePath, StringComparison.OrdinalIgnoreCase)) ?? throw new FileNotFoundException($"File '{filePath}' not found in package '{packageName}' v{targetPackage.Identity.Version}");
        using var entryStream = entry.Open();
        using var reader = new StreamReader(entryStream);
        var content = await reader.ReadToEndAsync();
        Console.Write(content);
    }
}