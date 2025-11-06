using AgentBlazor.Components;
using AgentBlazor.Services;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton(sp =>
{
    var chatClient = new ChatClient(
        "hf.co/unsloth/aquif-3.5-Max-42B-A3B-GGUF:IQ4_NL",
        new ApiKeyCredential("sk-"),
        new OpenAIClientOptions
        {
            Endpoint = new Uri("http://localhost:11434/v1"),
            NetworkTimeout = TimeSpan.FromMinutes(10)
        }
    ).AsIChatClient();

    return chatClient;
});
builder.Services.AddScoped<AgentContext>();
builder.Services.AddScoped<AgentService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
