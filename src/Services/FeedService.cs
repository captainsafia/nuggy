using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using Nuggy.Models;
using Spectre.Console;

namespace Nuggy.Services;

public class FeedService(ConfigurationManager configManager)
{
    public async Task<List<FeedConfiguration>> GetAllFeedsAsync()
    {
        var settings = await configManager.LoadSettingsAsync();
        return settings.Feeds;
    }

    public async Task AddFeedAsync(string name, string source, bool setAsDefault = false)
    {
        await AnsiConsole.Status()
            .StartAsync("Validating feed...", async ctx =>
            {
                // Validate the feed source by attempting to connect
                await ValidateFeedSourceAsync(source);

                var feed = new FeedConfiguration
                {
                    Name = name,
                    Source = source,
                    IsDefault = setAsDefault
                };

                ctx.Status("Adding feed to configuration...");
                await configManager.AddFeedAsync(feed);

                if (setAsDefault)
                {
                    await configManager.SetDefaultFeedAsync(name);
                }
            });

        AnsiConsole.MarkupLine($"[green]✓[/] Successfully added feed '[bold]{name}[/]'");
    }

    public async Task SetDefaultFeedAsync(string feedName)
    {
        await configManager.SetDefaultFeedAsync(feedName);
        AnsiConsole.MarkupLine($"[green]✓[/] Set '[bold]{feedName}[/]' as the default feed");
    }

    public async Task DisplayFeedsTableAsync()
    {
        var feeds = await GetAllFeedsAsync();

        if (feeds.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No feeds configured[/]");
            return;
        }

        var table = new Table()
            .AddColumn("[bold]Name[/]")
            .AddColumn("[bold]Source[/]")
            .AddColumn("[bold]Default[/]");

        foreach (var feed in feeds)
        {
            var defaultMark = feed.IsDefault ? "[green]✓[/]" : "";

            table.AddRow(
                feed.Name,
                feed.Source,
                defaultMark
            );
        }

        AnsiConsole.Write(table);
    }

    private static async Task ValidateFeedSourceAsync(string source, CancellationToken cancellationToken = default)
    {
        try
        {
            var cache = new SourceCacheContext();
            var repository = Repository.Factory.GetCoreV3(source);
            var resource = await repository.GetResourceAsync<ServiceIndexResourceV3>(cancellationToken) ?? throw new InvalidOperationException("Unable to access the feed service index");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to validate feed source '{source}': {ex.Message}", ex);
        }
    }

    public async Task<SourceRepository> GetRepositoryAsync(string? feedName = null)
    {
        FeedConfiguration? feed;

        if (!string.IsNullOrEmpty(feedName))
        {
            feed = await configManager.GetFeedByNameAsync(feedName);
            if (feed == null)
            {
                throw new ArgumentException($"Feed '{feedName}' not found.");
            }
        }
        else
        {
            feed = await configManager.GetDefaultFeedAsync();
            if (feed == null)
            {
                throw new InvalidOperationException("No default feed configured.");
            }
        }

        return Repository.Factory.GetCoreV3(feed.Source);
    }
}