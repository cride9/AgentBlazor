using AgentBlazor.Components;
using AgentBlazor.Data;
using AgentBlazor.Services;
using Microsoft.EntityFrameworkCore;
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
        "qwen3:30b-a3b-instruct-2507-q4_K_M",
        new ApiKeyCredential(Environment.GetEnvironmentVariable("DEEPSEEK")),
        new OpenAIClientOptions
        {
            Endpoint = new Uri("http://localhost:11434/v1"),
            NetworkTimeout = TimeSpan.FromMinutes(60),
        }
    ).AsIChatClient();

    return chatClient;
});
builder.Services.AddScoped<AgentContext>();
builder.Services.AddScoped<AgentService>();
builder.Services.AddScoped<ChatHistoryService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

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
