using AgentBlazor.Models;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace AgentBlazor.Services.AgentTools;

public class ListDirectory : AIFunction
{
    private readonly AgentContext _ctx;
    public ListDirectory(AgentContext ctx)
    {
        _ctx = ctx;
    }

    public override string Name => "list_directory";
    public override string Description => "List out a directory content. Accepts only relative paths";
    public override JsonElement JsonSchema => JsonDocument.Parse(@"
        {
            ""type"": ""object"",
            ""properties"": {
                ""path"": {
                    ""type"": ""string"",
                    ""description"": ""The relative path of the directory to list. (./ current dir, ./example example dir)""
                 }
            },
            ""required"": [""path""]
        }").RootElement;
    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {

        string relativePath = arguments.GetValueOrDefault("path") is JsonElement nameElem ? nameElem.GetString()! : null!;

        var call = new ToolCallModel
        {
            Id = Guid.NewGuid(),
            Name = Name,
            Arguments = relativePath,
            Status = "Running..."
        };
        _ctx.OnToolCallReceived?.Invoke(call);

        string fullPath = Path.GetFullPath(Path.Combine(_ctx.WorkingDirectory, relativePath));

        if (!fullPath.Contains(_ctx.WorkingDirectory))
        {
            call.Status = "Error";
            _ctx.OnToolCallReceived?.Invoke(call);
            return ("Access outside of agent folder is not allowed.");

        }

        var entries = Directory.EnumerateFileSystemEntries(fullPath)
                               .Select(Path.GetFileName)
                               .ToList();

        call.Status = "Done";
        _ctx.OnToolCallReceived?.Invoke(call);

        return entries;
    }
}

