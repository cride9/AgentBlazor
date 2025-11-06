using AgentBlazor.Models;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace AgentBlazor.Services.AgentTools;

public class StopLoop : AIFunction
{
    private readonly AgentContext _ctx;
    public StopLoop(AgentContext ctx)
    {
        _ctx = ctx;
    }
    public override string Name => "stop_loop";
    public override string Description => "This function will stop the ReAct loop, when you decide your task is done.";

    public override JsonElement JsonSchema => JsonDocument.Parse(@"
        {
            ""type"": ""object"",
            ""properties"": {},
            ""required"": []
        }").RootElement;

    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {

        _ctx.OnToolCallReceived?.Invoke(new ToolCallModel
        {
            Id = Guid.NewGuid(),
            Name = Name,
            Arguments = "",
            Status = "Done",
        });

        _ctx.CancellationSource.Cancel();
        _ctx.StopLoop = true;
        return true;
    }
}

