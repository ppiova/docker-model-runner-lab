// Streaming chat console app for Docker Model Runner (DMR).
//
// DMR exposes an OpenAI-compatible API, so the official OpenAI NuGet package works
// against it unchanged: only the base URL points at DMR instead of api.openai.com.
//
// Configuration (both have sensible DMR defaults):
//   OPENAI_BASE_URL  default http://localhost:12434/engines/v1
//   MODEL            default ai/gemma3

using System.ClientModel;
using OpenAI;
using OpenAI.Chat;

string baseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL")
                 ?? "http://localhost:12434/engines/v1";
string model = Environment.GetEnvironmentVariable("MODEL") ?? "ai/gemma3";

// DMR does not require an API key, but the OpenAI client needs a non-empty credential.
var credential = new ApiKeyCredential("docker-model-runner");
var options = new OpenAIClientOptions { Endpoint = new Uri(baseUrl) };
ChatClient chat = new OpenAIClient(credential, options).GetChatClient(model);

using var shutdown = new CancellationTokenSource();
ConsoleCancelEventHandler onCancel = (_, e) =>
{
    e.Cancel = true;
    shutdown.Cancel();
};
Console.CancelKeyPress += onCancel;
try
{
    await DmrChat.ConsoleChat.RunAsync(chat, Console.In, Console.Out, model, baseUrl, shutdown.Token);
}
finally
{
    Console.CancelKeyPress -= onCancel;
}