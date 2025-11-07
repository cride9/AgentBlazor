namespace AgentBlazor.Models;

public class ToolCallModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // e.g., "Calling...", "Success", "Error"
    public string? Output { get; set; }
}
