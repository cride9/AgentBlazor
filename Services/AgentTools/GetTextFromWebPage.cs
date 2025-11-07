using AgentBlazor.Models;
using AngleSharp.Dom;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace AgentBlazor.Services.AgentTools;

public class GetTextFromWebPage : AIFunction
{
    private readonly AgentContext _ctx;

    public GetTextFromWebPage(AgentContext ctx)
    {
        _ctx = ctx;
    }

    public override string Name => "web_scrape";
    public override string Description => "Scrapes a webpage from URL. Supports PDF and Text scraping. To get websites use web_search before scraping. Don't assume links";

    public override JsonElement JsonSchema => JsonDocument.Parse(@"
        {
            ""type"": ""object"",
            ""properties"": {
                ""url"": {
                    ""type"": ""string"",
                    ""description"": ""The url of the webpage you wan't to scrape text from""
                }
            },
            ""required"": [""url""]
        }").RootElement;

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        string url = arguments.GetValueOrDefault("url") is JsonElement pathElem ? pathElem.GetString()! : null!;
        var call = new ToolCallModel
        {
            Id = Guid.NewGuid(),
            Name = Name,
            Arguments = $"Scraping: {url}",
            Status = "Running..."
        };
        _ctx.OnToolCallReceived?.Invoke(call);
        var ret = await WebScraper.ScrapeTextFromUrlAsync(url);

        call.Status = "Done";
        _ctx.OnToolCallReceived?.Invoke(call);
        return ret;
    }
}
