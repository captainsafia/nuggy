using Nuggy.Commands;
using Nuggy.Services;
using Spectre.Console;
using System.CommandLine;

var configManager = new ConfigurationManager();
var feedService = new FeedService(configManager);
var nugetService = new NuGetService(feedService);

var rootCommand = new RootCommand("nuggy - A command line tool for browsing and searching NuGet packages");

rootCommand.AddCommand(FeedsCommand.Create(feedService));
rootCommand.AddCommand(PackageCommand.Create(nugetService));

try
{
    return await rootCommand.InvokeAsync(args);
}
catch (Exception ex)
{
    AnsiConsole.WriteException(ex);
    return 1;
}