using AgentBlazor.Services.AgentTools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentBlazor.Services;
public delegate void OnChunkReceived(string chunkText);

public class AgentService
{
    readonly private IChatClient _chatClient;
    readonly private AIAgent _agent;
    private AgentContext _context = new();
    public event OnChunkReceived? OnChunkReceived;

    public AgentService(IChatClient chatClient)
    {
        _chatClient = chatClient;
        _agent = new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions
            {
                Name = "MainAgent",
                Instructions = "You're an agent running in a ReAct loop. Do the task with your tools and then end the loop when it's done. DO NOT ASK FOR OTHER TASKS OR HELP. STOP WHEN THE MAIN TASK IS DONE. You have experts to call for specific tasks",
                ChatOptions = new ChatOptions
                {
                    AllowMultipleToolCalls = true,
                    ToolMode = ChatToolMode.Auto,
                    Tools = new List<AITool> {
                        new ListDirectory(_context.WorkingDirectory),
                        new StopLoop(_context)
                    },
                },
            }
        );
        _context.Thread = _agent.GetNewThread();
    }

    public async Task RunAsync(string userInput, CancellationToken cancellationToken = default)
    {
        await foreach (var chunk in _agent.RunStreamingAsync(userInput, _context.Thread))
        {
            OnChunkReceived?.Invoke(chunk.Text);
        }
        while (!_context.StopLoop)
        {
            await foreach (var chunk in _agent.RunStreamingAsync(_context.Thread))
            {
                OnChunkReceived?.Invoke(chunk.Text);
            }
        }
    }
}

public class AgentContext
{
    public string WorkingDirectory { get; }
    public Guid AgentGuid { get; } = Guid.NewGuid();
    public bool StopLoop { get; set; } = false;
    public AgentThread Thread { get; set; }

    public AgentContext()
    {
        WorkingDirectory = Path.Combine("agents", AgentGuid.ToString());
        Directory.CreateDirectory(WorkingDirectory);
    }
}
