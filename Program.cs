using AgentBlazor.Components;
using AgentBlazor.Services;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<IChatClient>(sp =>
{
    var chatClient = new ChatClient(
        "deepseek-chat",
        new ApiKeyCredential("sk-"),
        new OpenAIClientOptions
        {
            Endpoint = new Uri("https://api.deepseek.com"),
            NetworkTimeout = TimeSpan.FromMinutes(10)
        }
    ).AsIChatClient();

    return chatClient;
});
builder.Services.AddSingleton<AgentContext>();
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
