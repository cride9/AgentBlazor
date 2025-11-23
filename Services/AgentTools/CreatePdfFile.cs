using AgentBlazor.Models;
using Microsoft.Extensions.AI;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Markdown;
using System.IO;
using System.Text.Json;

namespace AgentBlazor.Services.AgentTools;

public class CreatePdfFile : AIFunction
{
    private readonly AgentContext _ctx;

    public CreatePdfFile(AgentContext ctx)
    {
        _ctx = ctx;
    }

    public override string Name => "create_pdf";
    public override string Description => "Creates a PDF file from markdown string. Markdown file is not needed to generate PDF. Accepts only relative paths.";

    public override JsonElement JsonSchema => JsonDocument.Parse(@"
        {
            ""type"": ""object"",
            ""properties"": {
                ""path"": {
                    ""type"": ""string"",
                    ""description"": ""The relative path of the pdf file to create (e.g., './new_folder/example.pdf').""
                 },
                ""content"": {
                    ""type"": ""string"",
                    ""description"": ""Markdown formated content. Markdown will be converted into a PDF format""
                 }
            },
            ""required"": [""path"", ""content""]
        }").RootElement;

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
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

        content ??= "";
        try
        {
            string fullPath = Path.GetFullPath(Path.Combine(_ctx.WorkingDirectory, relativePath));
            if (!fullPath.Contains(_ctx.WorkingDirectory))
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

            var markdowntext = content.Replace("\\n", "\n").Replace("\\t", "\t");
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.PageColor(Colors.White);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11));
                    page.Content().Markdown(markdowntext);
                });
            });

            document.GeneratePdf(fullPath);

            call.Status = "Done";
            _ctx.OnToolCallReceived?.Invoke(call);

            return $"Successfully wrote {content.Length} characters to '{relativePath}'.";

        }
        catch (Exception ex)
        {
            if (File.Exists(Path.GetFullPath(Path.Combine(_ctx.WorkingDirectory, relativePath))))
            {
                try { File.Delete(Path.GetFullPath(Path.Combine(_ctx.WorkingDirectory, relativePath))); } catch { }
            }
            call.Status = "Error";
            _ctx.OnToolCallReceived?.Invoke(call);
            return $"Error writing to file: {ex.Message} \nStack: {ex.StackTrace}";
        }
    }
}
