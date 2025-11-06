using AgentBlazor.Models;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace AgentBlazor.Services.AgentTools;

public class CreateDirectory : AIFunction
{
    private readonly string _basePath;
    private readonly AgentContext _ctx;

    public CreateDirectory(AgentContext ctx)
    {
        _ctx = ctx;
        _basePath = _ctx.WorkingDirectory;
    }

    public override string Name => "create_directory";
    public override string Description => "Creates a new directory. Accepts only relative paths.";

    public override JsonElement JsonSchema => JsonDocument.Parse(@"
        {
            ""type"": ""object"",
            ""properties"": {
                ""path"": {
                    ""type"": ""string"",
                    ""description"": ""The relative path of the directory to create (e.g., './new_folder/data').""
                 }
            },
            ""required"": [""path""]
        }").RootElement;

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        string relativePath = arguments.GetValueOrDefault("path") is JsonElement pathElem ? pathElem.GetString()! : null!;

        var call = new ToolCallModel
        {
            Id = Guid.NewGuid(),
            Name = Name,
            Arguments = relativePath ?? "path not provided",
            Status = "Running..."
        };
        _ctx.OnToolCallReceived?.Invoke(call);

        if (string.IsNullOrEmpty(relativePath))
        {
            call.Status = "Error";
            _ctx.OnToolCallReceived?.Invoke(call);
            return "Error: 'path' argument is missing or empty.";
        }

        try
        {
            string fullPath = Path.GetFullPath(Path.Combine(_basePath, relativePath));

            if (!fullPath.Contains(_basePath))
            {
                call.Status = "Error";
                _ctx.OnToolCallReceived?.Invoke(call);
                return "Error: Access outside of the agent's working directory is not allowed.";
            }

            Directory.CreateDirectory(fullPath);

            call.Status = "Done";
            _ctx.OnToolCallReceived?.Invoke(call);

            // Using ValueTask.FromResult for a non-async path
            return await ValueTask.FromResult($"Successfully created directory at '{relativePath}'.");
        }
        catch (Exception ex)
        {
            call.Status = "Error";
            _ctx.OnToolCallReceived?.Invoke(call);
            return $"Error creating directory: {ex.Message}";
        }
    }
}
