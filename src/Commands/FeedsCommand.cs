using Nuggy.Services;
using Spectre.Console;
using System.CommandLine;

namespace Nuggy.Commands;

public static class FeedsCommand
{
    public static Command Create(FeedService feedService)
    {
        var feedsCommand = new Command("feeds", "Manage NuGet feed sources");

        // feeds list
        var listCommand = new Command("list", "List all configured feeds");
        listCommand.SetHandler(async () =>
        {
            try
            {
                await feedService.DisplayFeedsTableAsync();
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error listing feeds: {ex.Message}[/]");
            }
        });

        // feeds add
        var addCommand = new Command("add", "Add a new feed source");

        var sourceOption = new Option<string>("--source", "The URL of the NuGet feed")
        {
            IsRequired = true
        };
        sourceOption.AddAlias("-s");

        var nameOption = new Option<string>("--name", "The name for this feed")
        {
            IsRequired = true
        };
        nameOption.AddAlias("-n");

        var defaultOption = new Option<bool>("--default", "Set this feed as the default")
        {
            IsRequired = false
        };
        defaultOption.AddAlias("-d");

        addCommand.AddOption(sourceOption);
        addCommand.AddOption(nameOption);
        addCommand.AddOption(defaultOption);

        addCommand.SetHandler(async (string source, string name, bool setDefault) =>
        {
            try
            {
                await feedService.AddFeedAsync(name, source, setDefault);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error adding feed: {ex.Message}[/]");
            }
        }, sourceOption, nameOption, defaultOption);

        // feeds set
        var setCommand = new Command("set", "Set the default feed for searches");

        var feedNameArgument = new Argument<string>("name", "The name of the feed to set as default");
        setCommand.AddArgument(feedNameArgument);

        setCommand.SetHandler(async (string feedName) =>
        {
            try
            {
                await feedService.SetDefaultFeedAsync(feedName);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error setting default feed: {ex.Message}[/]");
            }
        }, feedNameArgument);

        feedsCommand.AddCommand(listCommand);
        feedsCommand.AddCommand(addCommand);
        feedsCommand.AddCommand(setCommand);

        return feedsCommand;
    }
}