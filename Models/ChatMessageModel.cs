namespace AgentBlazor.Models;

public class ChatMessageModel
{
    public Guid Id { get; } = Guid.NewGuid();
    public bool IsUserMessage { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Timestamp { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsStreaming { get; set; }
}
