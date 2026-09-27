using System.ClientModel;
using System.Text;
using OpenAI.Chat;

namespace DmrChat;

public static class ConsoleChat
{
    public static async Task RunAsync(ChatClient chat, TextReader input, TextWriter output,
        string model, string baseUrl, CancellationToken cancellationToken = default)
    {
        output.WriteLine($"Docker Model Runner chat. Model: {model}");
        output.WriteLine($"Endpoint: {baseUrl}");
        output.WriteLine("Type a message and press Enter. Type /exit to quit. Ctrl+C cancels and exits.");
        output.WriteLine();

        var history = new List<ChatMessage>
        {
            new SystemChatMessage("You are a helpful assistant running locally via Docker Model Runner.")
        };

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                output.Write("you> ");
                // Console input can block synchronously even through ReadLineAsync.
                // A background read lets Ctrl+C exit while waiting for keyboard input.
                string? prompt = await Task.Run(input.ReadLine, CancellationToken.None)
                    .WaitAsync(cancellationToken);
                if (prompt is null || prompt.Trim() is "/exit" or "/quit") { break; }
                if (string.IsNullOrWhiteSpace(prompt)) { continue; }

                int turnStart = history.Count;
                history.Add(new UserChatMessage(prompt));
                output.Write("ai>  ");
                var reply = new StringBuilder();
                bool completed = false;

                try
                {
                    await foreach (var update in chat.CompleteChatStreamingAsync(history,
                        cancellationToken: cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        foreach (var part in update.ContentUpdate)
                        {
                            output.Write(part.Text);
                            reply.Append(part.Text);
                        }
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                    history.Add(new AssistantChatMessage(reply.ToString()));
                    completed = true;
                    output.WriteLine();
                    output.WriteLine();
                }
                catch (Exception ex) when (ex is ClientResultException or HttpRequestException)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    output.WriteLine();
                    output.WriteLine($"error> Could not reach the model at {baseUrl}: {ex.Message}");
                    output.WriteLine("error> Is Docker Model Runner enabled? Check with: docker model status");
                }
                finally
                {
                    // Only complete exchanges are kept as context for the next prompt.
                    if (!completed) { history.RemoveRange(turnStart, history.Count - turnStart); }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            output.WriteLine();
            output.WriteLine("Canceled.");
        }

        output.WriteLine("Bye.");
    }
}
