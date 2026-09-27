using BlazorChat.Components.Pages;
using Bunit;
using DmrChat;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Chat.Tests;

public class HistoryIntegrationTests
{
    [Fact]
    public async Task Console_rejects_oversized_prompt_and_sends_bounded_history()
    {
        using var model = new TestModel(_ => Task.FromResult(TestModel.Completed("OK")));
        using var input = new StringReader(new string('x', 30) + "\na\nb\nc\n/exit\n");
        using var output = new StringWriter();
        await ConsoleChat.RunAsync(model.CreateClient(), input, output, "test", "https://model.test",
            history: new ConversationHistory("sys", 20, 1)).WaitAsync(TestModel.Timeout);
        Assert.Equal(3, model.Requests.Count);
        Assert.Contains("Message is too long", output.ToString());
        Assert.Equal(new[] { "sys", "b", "OK", "c" }, TestModel.Contents(model.Requests.Last()));
    }

    [Fact]
    public async Task Blazor_rejects_oversized_prompt_bounds_transcript_and_clears_context()
    {
        using var model = new TestModel(_ => Task.FromResult(TestModel.Completed("OK")));
        await using var context = new BunitContext();
        context.Services.AddSingleton(model.CreateClient());
        context.Services.AddSingleton(new ModelInfo("test", "https://model.test"));
        context.Services.AddTransient(_ => new BlazorChat.ConversationHistory("sys", 20, 1));
        context.JSInterop.SetupVoid("scrollChatToBottom").SetVoidResult();
        var component = context.Render<Home>();

        await Send(new string('x', 30));
        Assert.Empty(model.Requests);
        Assert.Contains("Message is too long", component.Find("[role=status]").TextContent);
        Assert.False(component.Find("textarea").HasAttribute("disabled"));
        await Send("a");
        await Send("b");
        await Send("c");
        Assert.Equal(new[] { "sys", "b", "OK", "c" }, TestModel.Contents(model.Requests.Last()));
        Assert.Equal(2, component.FindAll(".row").Count);
        await component.Find("button.reset").ClickAsync(new MouseEventArgs());
        await Send("d");
        Assert.Equal(new[] { "sys", "d" }, TestModel.Contents(model.Requests.Last()));

        async Task Send(string text)
        {
            await component.Find("textarea").InputAsync(text);
            await component.Find("button.send").ClickAsync(new MouseEventArgs()).WaitAsync(TestModel.Timeout);
        }
    }
}
