using Nuggy.Services;
using Spectre.Console;
using System.CommandLine;

namespace Nuggy.Commands;

public static class PackageCommand
{
    public static Command Create(NuGetService nugetService)
    {
        var packageCommand = new Command("package", "Interact with NuGet packages");

        // package metadata
        var metadataCommand = new Command("metadata", "Display metadata for a package");

        var packageNameArgument = new Argument<string>("package", "The name of the package");
        var feedOption = new Option<string>("--feed", "The name of the feed to search (uses default if not specified)")
        {
            IsRequired = false
        };
        feedOption.AddAlias("-f");

        var versionOption = new Option<string>("--version", "The version of the package (uses latest if not specified)")
        {
            IsRequired = false
        };
        versionOption.AddAlias("-v");

        metadataCommand.AddArgument(packageNameArgument);
        metadataCommand.AddOption(feedOption);
        metadataCommand.AddOption(versionOption);

        metadataCommand.SetHandler(async (string packageName, string? feedName, string? version) =>
        {
            try
            {
                await nugetService.DisplayPackageMetadataAsync(packageName, feedName, version);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching package metadata: {ex.Message}[/]");
            }
        }, packageNameArgument, feedOption, versionOption);

        // package versions
        var versionsCommand = new Command("versions", "List all versions of a package");

        var versionsPackageArgument = new Argument<string>("package", "The name of the package");
        var versionsFeedOption = new Option<string>("--feed", "The name of the feed to search (uses default if not specified)")
        {
            IsRequired = false
        };
        versionsFeedOption.AddAlias("-f");

        versionsCommand.AddArgument(versionsPackageArgument);
        versionsCommand.AddOption(versionsFeedOption);

        versionsCommand.SetHandler(async (string packageName, string? feedName) =>
        {
            try
            {
                await nugetService.DisplayPackageVersionsAsync(packageName, feedName);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching package versions: {ex.Message}[/]");
            }
        }, versionsPackageArgument, versionsFeedOption);

        // package show
        var showCommand = new Command("show", "Display the contents of a package");

        var showPackageArgument = new Argument<string>("package", "The name of the package");
        var showFeedOption = new Option<string>("--feed", "The name of the feed to search (uses default if not specified)")
        {
            IsRequired = false
        };
        showFeedOption.AddAlias("-f");

        var showVersionOption = new Option<string>("--version", "The version of the package (uses latest if not specified)")
        {
            IsRequired = false
        };
        showVersionOption.AddAlias("-v");

        showCommand.AddArgument(showPackageArgument);
        showCommand.AddOption(showFeedOption);
        showCommand.AddOption(showVersionOption);

        showCommand.SetHandler(async (string packageName, string? feedName, string? version) =>
        {
            try
            {
                await nugetService.DisplayPackageContentsAsync(packageName, feedName, version);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching package contents: {ex.Message}[/]");
            }
        }, showPackageArgument, showFeedOption, showVersionOption);

        // package file
        var fileCommand = new Command("file", "Extract a specific file from a package");

        var filePackageArgument = new Argument<string>("package", "The name of the package");
        var filePathArgument = new Argument<string>("file-path", "The path to the file within the package");
        var fileFeedOption = new Option<string>("--feed", "The name of the feed to search (uses default if not specified)")
        {
            IsRequired = false
        };
        fileFeedOption.AddAlias("-f");

        var fileVersionOption = new Option<string>("--version", "The version of the package (uses latest if not specified)")
        {
            IsRequired = false
        };
        fileVersionOption.AddAlias("-v");

        fileCommand.AddArgument(filePackageArgument);
        fileCommand.AddArgument(filePathArgument);
        fileCommand.AddOption(fileFeedOption);
        fileCommand.AddOption(fileVersionOption);

        fileCommand.SetHandler(async (string packageName, string filePath, string? feedName, string? version) =>
        {
            try
            {
                await nugetService.ExtractPackageFileAsync(packageName, filePath, feedName, version);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error extracting package file: {ex.Message}[/]");
                Environment.Exit(1);
            }
        }, filePackageArgument, filePathArgument, fileFeedOption, fileVersionOption);

        packageCommand.AddCommand(metadataCommand);
        packageCommand.AddCommand(versionsCommand);
        packageCommand.AddCommand(showCommand);
        packageCommand.AddCommand(fileCommand);

        return packageCommand;
    }
}