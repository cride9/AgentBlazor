using AgentBlazor.Models;
using Microsoft.Extensions.AI;
using System.Text.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace AgentBlazor.Services.AgentTools;

public class SendInput : AIFunction
{
    private readonly AgentContext _ctx;

    public SendInput(AgentContext ctx) => _ctx = ctx;

    public override string Name => "send_input";
    public override string Description => "Sends a line of input to the current interactive terminal session.";

    public override JsonElement JsonSchema => JsonDocument.Parse(@"
    {
        ""type"": ""object"",
        ""properties"": {
            ""input"": { ""type"": ""string"", ""description"": ""The input text to send."" }
        },
        ""required"": [""input""]
    }").RootElement;

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments args, CancellationToken cancellationToken)
    {
        string input = args.GetValueOrDefault("input") is JsonElement elem ? elem.GetString()! : string.Empty;

        var call = new ToolCallModel
        {
            Id = Guid.NewGuid(),
            Name = Name,
            Arguments = $"Sending: {input}",
            Status = "Running..."
        };
        _ctx.OnToolCallReceived?.Invoke(call);

        if (_ctx.Terminal == null)
        {
            call.Status = "Error";
            _ctx.OnToolCallReceived?.Invoke(call);
            return "No active terminal session.";
        }

        await _ctx.Terminal.SendInputAsync(input);

        call.Status = "Done";
        _ctx.OnToolCallReceived?.Invoke(call);

        return $"[Sent input: {input}]";
    }
}