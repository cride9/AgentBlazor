using System.ComponentModel.DataAnnotations;

namespace AgentBlazor.Data;

public class ChatMessage
{
    [Key]
    public int Id { get; set; }

    [Required]
    public Guid SessionId { get; set; }

    // Foreign key to ChatSession
    public ChatSession Session { get; set; }

    [Required]
    public bool IsUserMessage { get; set; }

    [Required]
    public string Text { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Store tool calls as a serialized JSON string
    public string? ToolCallsJson { get; set; }
}
