using AgentBlazor.Models;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace AgentBlazor.Services.AgentTools;

public class WebSearch : AIFunction
{
    private readonly AgentContext _ctx;

    public WebSearch(AgentContext ctx)
    {
        _ctx = ctx;
    }

    public override string Name => "web_search";
    public override string Description => "Searches for a query on google. Returns the top 10 result with snippets and link";

    public override JsonElement JsonSchema => JsonDocument.Parse(@"
        {
            ""type"": ""object"",
            ""properties"": {
                ""query"": {
                    ""type"": ""string"",
                    ""description"": ""The query to search for on the web""
                }
            },
            ""required"": [""query""]
        }").RootElement;

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        string query = arguments.GetValueOrDefault("query") is JsonElement pathElem ? pathElem.GetString()! : null!;
        var call = new ToolCallModel
        {
            Id = Guid.NewGuid(),
            Name = Name,
            Arguments = $"Searching: {query}",
            Status = "Running..."
        };
        _ctx.OnToolCallReceived?.Invoke(call);
        var ret = await WebScraper.Search(query);

        call.Status = "Done";
        _ctx.OnToolCallReceived?.Invoke(call);
        return ret;
    }
}
