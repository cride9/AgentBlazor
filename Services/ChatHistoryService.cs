using AgentBlazor.Data;
using AgentBlazor.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AgentBlazor.Services;

public class ChatHistoryService
{
    private readonly AppDbContext _context;

    public ChatHistoryService(AppDbContext context)
    {
        _context = context;
    }

    // 1. Create a new session
    public async Task<ChatSession> CreateNewSessionAsync(Guid agentContextId)
    {
        var session = new ChatSession
        {
            AgentContextId = agentContextId
        };
        _context.ChatSessions.Add(session);
        await _context.SaveChangesAsync();
        return session;
    }
    public async Task<ChatSession?> GetSessionAsync(Guid sessionId)
    {
        return await _context.ChatSessions.FindAsync(sessionId);
    }
    // 2. Get all sessions for the sidebar
    public async Task<List<ChatSession>> GetSessionsAsync()
    {
        return await _context.ChatSessions
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    // 3. Get messages for a specific session
    public async Task<List<ChatMessageModel>> GetMessagesForSessionAsync(Guid sessionId)
    {
        var dbMessages = await _context.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

        // Map database models to Blazor UI models (ChatMessageModel)
        return dbMessages.Select(m => new ChatMessageModel
        {
            MessageDbId = m.Id,
            IsUserMessage = m.IsUserMessage,
            Text = m.Text,
            Timestamp = m.Timestamp.ToShortTimeString(),
            AvatarUrl = m.IsUserMessage ? null : "evaLogoTransparent.png",
            IsStreaming = false,
            ToolCalls = m.ToolCallsJson != null
                ? JsonSerializer.Deserialize<List<ToolCallModel>>(m.ToolCallsJson) ?? new()
                : new()
        }).ToList();
    }

    // 4. Save a new message
    public async Task SaveMessageAsync(Guid sessionId, ChatMessageModel message)
    {
        var dbMessage = new ChatMessage
        {
            SessionId = sessionId,
            IsUserMessage = message.IsUserMessage,
            Text = message.Text,
            Timestamp = DateTime.UtcNow,
            ToolCallsJson = message.ToolCalls.Any()
                ? JsonSerializer.Serialize(message.ToolCalls)
                : null
        };

        _context.ChatMessages.Add(dbMessage);
        await _context.SaveChangesAsync();
    }

    // 5. Update session title (e.g., after the first message)
    public async Task UpdateSessionTitleAsync(Guid sessionId, string newTitle)
    {
        var session = await _context.ChatSessions.FindAsync(sessionId);
        if (session != null)
        {
            session.Title = newTitle;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<string?> GetSessionThreadStateAsync(Guid sessionId)
    {
        var session = await _context.ChatSessions.FindAsync(sessionId);
        return session?.ThreadStateJson;
    }

    public async Task UpdateSessionThreadStateAsync(Guid sessionId, JsonElement threadState)
    {
        var session = await _context.ChatSessions.FindAsync(sessionId);
        if (session != null)
        {
            session.ThreadStateJson = JsonSerializer.Serialize(threadState);
            await _context.SaveChangesAsync();
        }
    }
}
