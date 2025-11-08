namespace AgentBlazor.Models;

public class ChatMessageModel
{
    public int MessageDbId { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool IsUserMessage { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Timestamp { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsStreaming { get; set; }
    public List<ToolCallModel> ToolCalls { get; set; } = new();
}

