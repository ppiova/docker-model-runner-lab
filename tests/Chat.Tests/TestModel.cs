using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using OpenAI;
using OpenAI.Chat;

namespace Chat.Tests;

// Exercise the real OpenAI SDK and its cancellation path without a network or GPU.
internal sealed class TestModel(Func<CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
{
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    public ConcurrentQueue<JsonElement> Requests { get; } = new();

    public ChatClient CreateClient() => new OpenAIClient(new ApiKeyCredential("test"),
        new OpenAIClientOptions
        {
            Endpoint = new Uri("https://model.test/v1"),
            Transport = new HttpClientPipelineTransport(new HttpClient(this, disposeHandler: false)),
            RetryPolicy = new ClientRetryPolicy(0)
        }).GetChatClient("test-model");

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
        Requests.Enqueue(json.RootElement.Clone());
        return await respond(cancellationToken);
    }

    public static string Chunk(string text) => "data: " + JsonSerializer.Serialize(new
    {
        id = "test", @object = "chat.completion.chunk", created = 0, model = "test-model",
        choices = new[] { new { index = 0, delta = new { content = text }, finish_reason = (string?)null } }
    }) + "\n\n";

    public static HttpResponseMessage Streaming(Stream stream)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(stream) };
        response.Content.Headers.ContentType = new("text/event-stream");
        return response;
    }

    public static HttpResponseMessage Completed(string text = "Complete answer") =>
        Streaming(new MemoryStream(Encoding.UTF8.GetBytes(Chunk(text) + "data: [DONE]\n\n")));

    public static string[] Contents(JsonElement request) => request.GetProperty("messages")
        .EnumerateArray().Select(message => message.GetProperty("content").GetString()!).ToArray();
}

internal sealed class PendingModelStream(string partial = "") : Stream
{
    private readonly MemoryStream prefix = new(Encoding.UTF8.GetBytes(
        partial.Length == 0 ? "" : TestModel.Chunk(partial)));
    public TaskCompletionSource Reading { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Canceled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public bool WasDisposed { get; private set; }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        int count = await prefix.ReadAsync(buffer, cancellationToken);
        if (count != 0) { return count; }
        Reading.TrySetResult();
        try { await Task.Delay(System.Threading.Timeout.Infinite, cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Canceled.TrySetResult();
            throw;
        }
        return 0;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    protected override void Dispose(bool disposing)
    {
        WasDisposed = true;
        if (disposing) { prefix.Dispose(); }
        base.Dispose(disposing);
    }
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override void Flush() => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
