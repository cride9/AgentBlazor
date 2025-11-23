using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;

namespace AgentBlazor.Services.AgentTools;

public class LessonAgent
{
    readonly private AIAgent _agent;
    readonly private AgentContext _context;

    public LessonAgent(IChatClient chatClient, AgentContext context)
    {
        _context = context;
        _agent = new ChatClientAgent(
        chatClient,
        new ChatClientAgentOptions
        {
            Name = "LessonPlanGenerator",
            Instructions = Instructions.LessonInstruction,
            Description = "A specialized pedagogical agent designed to generate highly detailed, professional primary school lesson plans (Grades 1-4) in Hungarian. It strictly follows the academic format of the University of Szeged (SZTE JGYPK). Use this agent when the user requests a formal 'óratervezet' (lesson plan)",
            ChatOptions = new ChatOptions
            {
                AllowMultipleToolCalls = true,
                ToolMode = ChatToolMode.Auto,
                Tools = new List<AITool> {
                    new CreatePdfFile(_context),
                    new GetTextFromWebPage(_context),
                    new WebSearch(_context),
                },
            },
        });
    }

    public AIFunction AsAIFunction() => _agent.AsAIFunction();
}