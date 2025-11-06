using AgentBlazor.Models;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace AgentBlazor.Services.AgentTools;

public class WriteFile : AIFunction
{
    private readonly string _basePath;
    private readonly AgentContext _ctx;

    public WriteFile(AgentContext ctx)
    {
        _ctx = ctx;
        _basePath = _ctx.WorkingDirectory;
    }

    public override string Name => "write_file";
    public override string Description => "Writes or overwrites content to a specified file. Accepts only relative paths. Will create the file if it does not exist, but not the directory.";

    public override JsonElement JsonSchema => JsonDocument.Parse(@"
        {
            ""type"": ""object"",
            ""properties"": {
                ""path"": {
                    ""type"": ""string"",
                    ""description"": ""The relative path of the file to write to (e.g., './output/result.json').""
                },
                ""content"": {
                    ""type"": ""string"",
                    ""description"": ""The content to write into the file.""
                }
            },
            ""required"": [""path"", ""content""]
        }").RootElement;

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        string relativePath = arguments.GetValueOrDefault("path") is JsonElement pathElem ? pathElem.GetString()! : null!;
        string content = arguments.GetValueOrDefault("content") is JsonElement contentElem ? contentElem.GetString()! : null; // Content can be null/empty

        var call = new ToolCallModel
        {
            Id = Guid.NewGuid(),
            Name = Name,
            Arguments = $"Path: {relativePath ?? "not provided"}",
            Status = "Running..."
        };
        _ctx.OnToolCallReceived?.Invoke(call);

        if (string.IsNullOrEmpty(relativePath))
        {
            call.Status = "Error";
            _ctx.OnToolCallReceived?.Invoke(call);
            return "Error: 'path' argument is missing or empty.";
        }

        // The LLM might pass null or an empty string for content, both are valid.
        // We will default null to an empty string for the write operation.
        content ??= "";

        try
        {
            string fullPath = Path.GetFullPath(Path.Combine(_basePath, relativePath));

            if (!fullPath.Contains(_basePath))
            {
                call.Status = "Error";
                _ctx.OnToolCallReceived?.Invoke(call);
                return "Error: Access outside of the agent's working directory is not allowed.";
            }

            string? directoryName = Path.GetDirectoryName(fullPath);
            if (directoryName == null || !Directory.Exists(directoryName))
            {
                call.Status = "Error";
                _ctx.OnToolCallReceived?.Invoke(call);
                return $"Error: The directory for path '{relativePath}' does not exist. Please create it first using the 'create_directory' tool.";
            }

            await File.WriteAllTextAsync(fullPath, content, cancellationToken);

            call.Status = "Done";
            _ctx.OnToolCallReceived?.Invoke(call);

            return $"Successfully wrote {content.Length} characters to '{relativePath}'.";
        }
        catch (Exception ex)
        {
            call.Status = "Error";
            _ctx.OnToolCallReceived?.Invoke(call);
            return $"Error writing to file: {ex.Message}";
        }
    }
}
