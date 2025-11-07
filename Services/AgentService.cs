using AgentBlazor.Models;
using AgentBlazor.Services.AgentTools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;


namespace AgentBlazor.Services;
public delegate void OnChunkReceived(string chunkText);
public delegate void OnToolCallReceived(ToolCallModel toolCall);

public class AgentService
{
    readonly private IChatClient _chatClient;
    readonly private AIAgent _agent;
    private readonly AgentContext _context;
    public event OnChunkReceived? OnChunkReceived;
    public event OnToolCallReceived? OnToolCallReceived;
    public event Action? OnNewLoop;

    public AgentService(IChatClient chatClient, AgentContext context)
    {
        _context = context;
        _context.OnToolCallReceived = (toolCall) =>
        {
            OnToolCallReceived?.Invoke(toolCall);
        };
        _chatClient = chatClient;
        _agent = new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions
            {
                Name = "MainAgent",
                Instructions = Instructions.Instruction,
                ChatOptions = new ChatOptions
                {
                    AllowMultipleToolCalls = false,
                    ToolMode = ChatToolMode.Auto,
                    Tools = new List<AITool> {
                        new CreateDirectory(_context),
                        new CreatePdfFile(_context),
                        new ExecuteCommand(_context),
                        new GetTextFromWebPage(_context),
                        new ListDirectory(_context),
                        new ReadFile(_context),
                        new WebSearch(_context),
                        new WriteFile(_context),
                    },
                },
            }
        );
        _context.Thread = _agent.GetNewThread();
    }

    public async Task RunAsync(string userInput, CancellationToken cancellationToken = default)
    {
        OnNewLoop?.Invoke();
        _context.Initialize();

        var stream = string.IsNullOrEmpty(userInput)
                ? _agent.RunStreamingAsync(_context.Thread, cancellationToken: _context.CancellationSource.Token)
                : _agent.RunStreamingAsync(userInput, _context.Thread, cancellationToken: _context.CancellationSource.Token);

        await foreach (var chunk in stream)
            OnChunkReceived?.Invoke(chunk.Text);

        userInput = string.Empty;

        _context.Reset();
    }
}

public class AgentContext
{
    public string WorkingDirectory { get; set; }
    public Guid AgentGuid { get; } = Guid.NewGuid();
    public bool StopLoop { get; set; } = false;
    public AgentThread Thread { get; set; }
    public Action<ToolCallModel>? OnToolCallReceived { get; set; }
    public CancellationTokenSource CancellationSource { get; private set; }
    private bool initialized = false;

    public void Initialize()
    {
        if (initialized) return;
        CancellationSource = new();
        WorkingDirectory = Path.Combine("agents", AgentGuid.ToString());
        Directory.CreateDirectory(WorkingDirectory);
        initialized = true;
    }

    public void Reset()
    {
        if (CancellationSource != null)
        {
            try { CancellationSource.Cancel(); } catch { }
            CancellationSource.Dispose();
        }

        CancellationSource = new CancellationTokenSource();
        StopLoop = false;
    }
}
