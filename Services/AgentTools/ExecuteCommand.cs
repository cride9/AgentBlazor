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
    public override string Description => "Executes a command line process and returns its standard output and error streams. USE WITH CAUTION. The command runs inside the agent's sandboxed working directory.";

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
            var process = new Process();
            var processStartInfo = new ProcessStartInfo
            {
                WorkingDirectory = _ctx.WorkingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                processStartInfo.FileName = "cmd.exe";
                processStartInfo.Arguments = $"/C \"{command}\"";
            }
            else
            {
                processStartInfo.FileName = "/bin/bash";
                processStartInfo.Arguments = $"-c \"{command}\"";
            }

            process.StartInfo = processStartInfo;
            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            string error = await process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            call.Status = "Done";
            _ctx.OnToolCallReceived?.Invoke(call);

            var result = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(output))
            {
                result.AppendLine("--- STDOUT ---");
                result.AppendLine(output.Trim());
            }
            if (!string.IsNullOrWhiteSpace(error))
            {
                result.AppendLine("--- STDERR ---");
                result.AppendLine(error.Trim());
            }

            return result.Length > 0 ? result.ToString() : "[No output]";
        }
        catch (Exception ex)
        {
            call.Status = "Error";
            _ctx.OnToolCallReceived?.Invoke(call);
            return $"Error executing command: {ex.Message}";
        }
    }
}
