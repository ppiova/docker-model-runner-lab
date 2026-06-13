// Blazor Server chat UI for Docker Model Runner (DMR).
//
// The OpenAI SDK runs server-side (same pattern as modules 03 and 04) and tokens are streamed
// to the browser over the Blazor SignalR connection, so there are no CORS concerns.
//
// Configuration (both have sensible DMR defaults):
//   OPENAI_BASE_URL  default http://localhost:12434/engines/v1
//   MODEL            default ai/gemma3

using System.ClientModel;
using BlazorChat.Components;
using OpenAI;
using OpenAI.Chat;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

string baseUrl = (Environment.GetEnvironmentVariable("OPENAI_BASE_URL")
                  ?? "http://localhost:12434/engines/v1").TrimEnd('/');
string model = Environment.GetEnvironmentVariable("MODEL") ?? "ai/gemma3";

// DMR does not require an API key, but the OpenAI client needs a non-empty credential.
ChatClient chatClient = new OpenAIClient(
        new ApiKeyCredential("docker-model-runner"),
        new OpenAIClientOptions { Endpoint = new Uri(baseUrl) })
    .GetChatClient(model);

builder.Services.AddSingleton(chatClient);

// Expose the resolved configuration to components for display.
builder.Services.AddSingleton(new ModelInfo(model, baseUrl));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Resolved model name and endpoint, shown in the UI header.
public record ModelInfo(string Model, string Endpoint);
