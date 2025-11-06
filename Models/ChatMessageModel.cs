namespace AgentBlazor.Models;

public class ChatMessageModel
{
    public Guid Id { get; } = Guid.NewGuid();
    public bool IsUserMessage { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Timestamp { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsStreaming { get; set; }
    public List<ToolCallModel> ToolCalls { get; set; } = new();
}

public class ToolCallModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // e.g., "Calling...", "Success", "Error"
    public string? Output { get; set; }
}
