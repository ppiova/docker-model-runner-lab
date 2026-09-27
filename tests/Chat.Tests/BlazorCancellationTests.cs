using BlazorChat.Components.Pages;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Chat.Tests;

public class BlazorCancellationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("Partial answer")]
    public async Task Stop_cancels_stream_restores_controls_and_excludes_the_turn_from_next_request(string partial)
    {
        using var stream = new PendingModelStream(partial);
        int calls = 0;
        using var model = new TestModel(_ => Task.FromResult(++calls == 2
            ? TestModel.Streaming(stream) : TestModel.Completed()));
        await using var context = CreateContext(model);
        var component = context.Render<Home>();

        // Preserve a successful exchange before the canceled turn.
        await component.Find("textarea").InputAsync("Earlier prompt");
        await component.Find("button.send").ClickAsync(new MouseEventArgs()).WaitAsync(TestModel.Timeout);
        await component.Find("textarea").InputAsync("Canceled prompt");
        var send = component.Find("button.send").ClickAsync(new MouseEventArgs());
        await stream.Reading.Task.WaitAsync(TestModel.Timeout);
        await component.WaitForAssertionAsync(() => Assert.Equal("Stop", component.Find("button.send").TextContent), TestModel.Timeout);
        await component.Find("button.send").ClickAsync(new MouseEventArgs());
        await send.WaitAsync(TestModel.Timeout);
        await stream.Canceled.Task.WaitAsync(TestModel.Timeout);

        Assert.False(component.Find("textarea").HasAttribute("disabled"));
        Assert.Equal("Canceled prompt", component.Find("textarea").GetAttribute("value"));
        Assert.False(component.Find("button.reset").HasAttribute("disabled"));
        Assert.Equal("Send", component.Find("button.send").TextContent);
        Assert.Contains("Response stopped", component.Markup);
        if (partial.Length > 0) { Assert.Contains(partial, component.Markup); }
        Assert.DoesNotContain("Could not reach", component.Markup);
        Assert.True(stream.WasDisposed);

        await component.Find("textarea").InputAsync("Next prompt");
        await component.Find("button.send").ClickAsync(new MouseEventArgs()).WaitAsync(TestModel.Timeout);
        var messages = TestModel.Contents(model.Requests.Last());
        Assert.Equal(4, messages.Length); // system + prior complete exchange + new user prompt
        Assert.Equal(new[] { "Earlier prompt", "Complete answer", "Next prompt" }, messages.Skip(1));
    }

    [Fact]
    public async Task Disposing_component_cancels_an_active_stream()
    {
        using var stream = new PendingModelStream("Partial");
        using var model = new TestModel(_ => Task.FromResult(TestModel.Streaming(stream)));
        await using var context = CreateContext(model);
        var component = context.Render<Home>();
        await component.Find("textarea").InputAsync("Hello");
        var send = component.Find("button.send").ClickAsync(new MouseEventArgs());
        await stream.Reading.Task.WaitAsync(TestModel.Timeout);

        await context.DisposeComponentsAsync();
        await stream.Canceled.Task.WaitAsync(TestModel.Timeout);
        await send.WaitAsync(TestModel.Timeout);
        Assert.True(stream.WasDisposed);
    }

    private static BunitContext CreateContext(TestModel model)
    {
        var context = new BunitContext();
        context.Services.AddSingleton(model.CreateClient());
        context.Services.AddSingleton(new ModelInfo("test-model", "https://model.test"));
        context.JSInterop.SetupVoid("scrollChatToBottom").SetVoidResult();
        return context;
    }
}
