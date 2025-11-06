using Microsoft.Extensions.AI;
using System.Text.Json;

namespace AgentBlazor.Services.AgentTools;

public class ListDirectory : AIFunction
{
    private readonly string _basePath;
    public ListDirectory(string path)
    {
        _basePath = path;
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

        string fullPath = Path.GetFullPath(Path.Combine(_basePath, relativePath));

        if (!fullPath.Contains(_basePath))
            return ("Access outside of agent folder is not allowed.");

        var entries = Directory.EnumerateFileSystemEntries(fullPath)
                               .Select(Path.GetFileName)
                               .ToList();

        return entries;
    }
}

