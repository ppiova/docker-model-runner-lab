// Minimal API that forwards a prompt to a model served by Docker Model Runner.
//
// When this service is bound to a model in compose.yaml, Compose injects two env vars:
//   OPENAI_BASE_URL  the model runner endpoint, e.g. http://model-runner.docker.internal/engines/v1/
//   MODEL            the model identifier, e.g. ai/gemma3
// The defaults below let the app also run outside Compose against the host endpoint.

using System.ClientModel;
using OpenAI;
using OpenAI.Chat;

var builder = WebApplication.CreateBuilder(args);

string baseUrl = (Environment.GetEnvironmentVariable("OPENAI_BASE_URL")
                  ?? "http://localhost:12434/engines/v1").TrimEnd('/');
string model = Environment.GetEnvironmentVariable("MODEL") ?? "ai/gemma3";

// DMR does not require an API key, but the OpenAI client needs a non-empty credential.
var chatClient = new OpenAIClient(
        new ApiKeyCredential("docker-model-runner"),
        new OpenAIClientOptions { Endpoint = new Uri(baseUrl) })
    .GetChatClient(model);

builder.Services.AddSingleton(chatClient);

var app = builder.Build();

// Health and info endpoint.
app.MapGet("/", () => Results.Ok(new { status = "ok", model, endpoint = baseUrl }));

// Forward a prompt to the model and return the reply.
app.MapPost("/chat", async (ChatRequest request, ChatClient client, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Prompt))
    {
        return Results.BadRequest(new { error = "Field 'prompt' is required." });
    }

    try
    {
        ChatCompletion completion = await client.CompleteChatAsync(
            new ChatMessage[] { new UserChatMessage(request.Prompt) },
            cancellationToken: cancellationToken);

        string reply = completion.Content.Count > 0 ? completion.Content[0].Text : string.Empty;
        return Results.Ok(new { model, reply });
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
        // RequestAborted is bound to cancellationToken. The caller has disconnected;
        // do not report its cancellation as an upstream failure.
        return Results.StatusCode(499);
    }
    catch (Exception ex) when (ex is ClientResultException or HttpRequestException)
    {
        if (cancellationToken.IsCancellationRequested) { return Results.StatusCode(499); }
        return Results.Problem(
            title: "Model request failed",
            detail: $"{ex.Message} Check that the model endpoint '{baseUrl}' is reachable.",
            statusCode: StatusCodes.Status502BadGateway);
    }
});

app.Run();

// Request body for POST /chat.
record ChatRequest(string Prompt);

// Expose the application entry point for in-memory integration tests.
public partial class Program { }
