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

Console.WriteLine($"Docker Model Runner chat. Model: {model}");
Console.WriteLine($"Endpoint: {baseUrl}");
Console.WriteLine("Type a message and press Enter. Type /exit to quit.");
Console.WriteLine();

// Conversation history kept across turns so the model has context.
var history = new List<ChatMessage>
{
    new SystemChatMessage("You are a helpful assistant running locally via Docker Model Runner.")
};

while (true)
{
    Console.Write("you> ");
    string? input = Console.ReadLine();

    if (input is null || input.Trim() is "/exit" or "/quit")
    {
        break;
    }

    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }

    history.Add(new UserChatMessage(input));

    // Stream the response token by token and accumulate it for the history.
    Console.Write("ai>  ");
    var reply = new System.Text.StringBuilder();

    await foreach (StreamingChatCompletionUpdate update in chat.CompleteChatStreamingAsync(history))
    {
        foreach (ChatMessageContentPart part in update.ContentUpdate)
        {
            Console.Write(part.Text);
            reply.Append(part.Text);
        }
    }

    Console.WriteLine();
    Console.WriteLine();
    history.Add(new AssistantChatMessage(reply.ToString()));
}

Console.WriteLine("Bye.");
