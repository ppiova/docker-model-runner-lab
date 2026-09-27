using DmrChat;

namespace Chat.Tests;

public class ConsoleCancellationTests
{
    [Fact]
    public async Task Completed_exchanges_are_preserved_for_the_next_prompt()
    {
        using var model = new TestModel(_ => Task.FromResult(TestModel.Completed()));
        using var input = new StringReader("First prompt\nNext prompt\n/exit\n");
        using var output = new StringWriter();
        await ConsoleChat.RunAsync(model.CreateClient(), input, output, "test", "https://model.test")
            .WaitAsync(TestModel.Timeout);

        Assert.Equal(new[] { "First prompt", "Complete answer", "Next prompt" },
            TestModel.Contents(model.Requests.Last()).Skip(1));
        Assert.Contains("Bye.", output.ToString());
    }

    [Fact]
    public async Task Failed_exchange_is_removed_before_the_next_prompt()
    {
        int calls = 0;
        using var model = new TestModel(_ => Task.FromResult(++calls == 1
            ? new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                { Content = new StringContent("{}") }
            : TestModel.Completed()));
        using var input = new StringReader("Failed prompt\nNext prompt\n/exit\n");
        using var output = new StringWriter();
        await ConsoleChat.RunAsync(model.CreateClient(), input, output, "test", "https://model.test")
            .WaitAsync(TestModel.Timeout);

        Assert.Equal(new[] { "Next prompt" }, TestModel.Contents(model.Requests.Last()).Skip(1));
        Assert.Contains("error>", output.ToString());
        Assert.Contains("Bye.", output.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("Partial answer")]
    public async Task Cancel_stops_the_sdk_stream_and_exits_cleanly(string partial)
    {
        using var stream = new PendingModelStream(partial);
        using var model = new TestModel(_ => Task.FromResult(TestModel.Streaming(stream)));
        using var cancel = new CancellationTokenSource();
        using var output = new StringWriter();
        using var input = new StringReader("First prompt\nMust not be sent\n");
        var run = ConsoleChat.RunAsync(model.CreateClient(), input, output, "test", "https://model.test", cancel.Token);

        await stream.Reading.Task.WaitAsync(TestModel.Timeout);
        cancel.Cancel();
        await run.WaitAsync(TestModel.Timeout);
        await stream.Canceled.Task.WaitAsync(TestModel.Timeout);

        Assert.Contains("Canceled.", output.ToString());
        Assert.Contains("Bye.", output.ToString());
        Assert.DoesNotContain("error>", output.ToString());
        Assert.Single(model.Requests);
        Assert.True(stream.WasDisposed);
    }

    [Fact]
    public async Task Cancel_while_waiting_for_keyboard_input_exits_without_a_model_request()
    {
        using var input = new BlockingReader();
        using var output = new StringWriter();
        using var model = new TestModel(_ => throw new InvalidOperationException("No request expected"));
        using var cancel = new CancellationTokenSource();
        var run = ConsoleChat.RunAsync(model.CreateClient(), input, output, "test", "https://model.test", cancel.Token);
        try
        {
            await input.Reading.Task.WaitAsync(TestModel.Timeout);
            cancel.Cancel();
            await run.WaitAsync(TestModel.Timeout);
            Assert.Contains("Bye.", output.ToString());
            Assert.Empty(model.Requests);
        }
        finally { input.Release.Set(); }
    }

    private sealed class BlockingReader : TextReader
    {
        public TaskCompletionSource Reading { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ManualResetEventSlim Release { get; } = new();
        public override string? ReadLine()
        {
            Reading.TrySetResult();
            Release.Wait(TestModel.Timeout);
            return null;
        }
    }
}
