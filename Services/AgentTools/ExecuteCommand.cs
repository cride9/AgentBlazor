using AgentBlazor.Models;
using Microsoft.Extensions.AI;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace AgentBlazor.Services.AgentTools;

public class ExecuteCommand : AIFunction
{
    private readonly AgentContext _ctx;

    public ExecuteCommand(AgentContext ctx)
    {
        _ctx = ctx;
    }

    public override string Name => "execute_command";
    public override string Description => "Executes a WINDOWS CMD command line process and returns its standard output and error streams. USE WITH CAUTION. The command runs inside the agent's sandboxed working directory.";

    public override JsonElement JsonSchema => JsonDocument.Parse(@"
        {
            ""type"": ""object"",
            ""properties"": {
                ""command"": {
                    ""type"": ""string"",
                    ""description"": ""The command to execute (e.g., 'dotnet --version' or 'ls -l').""
                 }
            },
            ""required"": [""command""]
        }").RootElement;

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        string command = arguments.GetValueOrDefault("command") is JsonElement cmdElem ? cmdElem.GetString()! : null!;
        var call = new ToolCallModel
        {
            Id = Guid.NewGuid(),
            Name = Name,
            Arguments = command ?? "command not provided",
            Status = "Running..."
        };
        _ctx.OnToolCallReceived?.Invoke(call);

        if (string.IsNullOrEmpty(command))
        {
            call.Status = "Error";
            _ctx.OnToolCallReceived?.Invoke(call);
            return "Error: 'command' argument is missing or empty.";
        }

        try
        {
            _ctx.Terminal ??= new TerminalSession(_ctx.WorkingDirectory);

            string result = await _ctx.Terminal.ExecuteCommandAsync(command, cancellationToken);

            call.Status = "Done";
            _ctx.OnToolCallReceived?.Invoke(call);
            return result;
        }
        catch (Exception ex)
        {
            call.Status = "Error";
            _ctx.OnToolCallReceived?.Invoke(call);
            return $"Error executing command: {ex.Message}";
        }
    }
}
