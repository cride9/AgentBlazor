using AgentBlazor.Models;
using Microsoft.Extensions.AI;
using System.Text;
using System.Text.Json;
using UglyToad.PdfPig;

namespace AgentBlazor.Services.AgentTools;

public class ReadFile : AIFunction
{
    private readonly AgentContext _ctx;

    public ReadFile(AgentContext ctx)
    {
        _ctx = ctx;
    }

    public override string Name => "read_file";
    public override string Description => "Reads the entire content of a specified file. Accepts only relative paths.";

    public override JsonElement JsonSchema => JsonDocument.Parse(@"
        {
            ""type"": ""object"",
            ""properties"": {
                ""path"": {
                    ""type"": ""string"",
                    ""description"": ""The relative path of the file to read (e.g., './data/report.txt').""
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
            string fullPath = Path.GetFullPath(Path.Combine(_ctx.WorkingDirectory, relativePath));

            // SECURITY: Ensure the path is within the agent's working directory.
            if (!fullPath.StartsWith(_ctx.WorkingDirectory, StringComparison.OrdinalIgnoreCase))
            {
                call.Status = "Error";
                _ctx.OnToolCallReceived?.Invoke(call);
                return "Error: Access outside of the agent's working directory is not allowed.";
            }

            if (!File.Exists(fullPath))
            {
                call.Status = "Error";
                _ctx.OnToolCallReceived?.Invoke(call);
                return $"Error: File not found at '{relativePath}'.";
            }

            string content;

            // Detect PDF files
            if (Path.GetExtension(fullPath).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                content = await _ReadTextFromPDF(fullPath, _ctx.WorkingDirectory);
            }
            else
            {
                content = await File.ReadAllTextAsync(fullPath, cancellationToken);
            }

            call.Status = "Done";
            _ctx.OnToolCallReceived?.Invoke(call);

            return content;
        }
        catch (Exception ex)
        {
            call.Status = "Error";
            _ctx.OnToolCallReceived?.Invoke(call);
            return $"Error reading file: {ex.Message}";
        }
    }

    // PDF reading method
    private Task<string> _ReadTextFromPDF(string filename, string cwd)
    {
        var filePath = filename;
        if (!File.Exists(filePath))
            return Task.FromResult($"File \"{filename}\" does not exist.");

        using var document = PdfDocument.Open(filePath);
        StringBuilder sb = new();
        foreach (var page in document.GetPages())
        {
            double? lastY = null;
            foreach (var word in page.GetWords())
            {
                var y = word.BoundingBox.Top;
                if (lastY != null && Math.Abs(lastY.Value - y) > 5)
                    sb.AppendLine();

                sb.Append($"{word.Text} ");
                lastY = y;
            }
        }

        return Task.FromResult($"PDF file content:\n{sb}");
    }
}