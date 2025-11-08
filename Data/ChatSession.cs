using AgentBlazor.Components.Chat;
using System.ComponentModel.DataAnnotations;

namespace AgentBlazor.Data;

public class ChatSession
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = "New Chat";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid AgentContextId { get; set; }

    // ++ ADD THIS PROPERTY ++
    // This will store the serialized AgentThread as a JSON string.
    public string? ThreadStateJson { get; set; }

    // Navigation property for messages in this session
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
