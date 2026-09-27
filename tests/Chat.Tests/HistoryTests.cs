using OpenAI.Chat;

namespace Chat.Tests;

public class HistoryTests
{
    // Both standalone labs must follow the same retention rules.
    private sealed record History(Func<string, IReadOnlyList<ChatMessage>> Request,
        Func<string, string, bool> Remember, Action Clear);

    private static History Create(bool blazor, int characters = 100, int turns = 2)
    {
        if (blazor)
        {
            var history = new BlazorChat.ConversationHistory("sys", characters, turns);
            return new(history.CreateRequest, history.Remember, history.Clear);
        }
        var console = new DmrChat.ConversationHistory("sys", characters, turns);
        return new(console.CreateRequest, console.Remember, console.Clear);
    }

    private static string[] Text(IReadOnlyList<ChatMessage> messages) =>
        messages.Select(message => string.Concat(message.Content.Select(part => part.Text))).ToArray();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Turn_limit_keeps_system_and_recent_complete_pairs(bool blazor)
    {
        var history = Create(blazor);
        history.Remember("one", "ONE");
        history.Remember("two", "TWO");
        history.Remember("three", "THREE");
        var request = history.Request("next");
        Assert.Equal(new[] { "sys", "two", "TWO", "three", "THREE", "next" }, Text(request));
        Assert.IsType<SystemChatMessage>(request[0]);
        Assert.IsType<UserChatMessage>(request[1]);
        Assert.IsType<AssistantChatMessage>(request[2]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Request_budget_includes_new_prompt_without_mutating_saved_turns(bool blazor)
    {
        var history = Create(blazor, characters: 13);
        history.Remember("aa", "AA");
        history.Remember("bb", "BB");
        Assert.Equal(new[] { "sys", "longprompt" }, Text(history.Request("longprompt")));
        // A failed/canceled long request must not erase older complete exchanges.
        Assert.Equal(new[] { "sys", "aa", "AA", "bb", "BB", "cc" }, Text(history.Request("cc")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Retained_character_budget_evicts_oldest_pairs(bool blazor)
    {
        var history = Create(blazor, characters: 12, turns: 10);
        history.Remember("aa", "AA");
        history.Remember("bb", "BB");
        history.Remember("cc", "CC");
        Assert.Equal(new[] { "sys", "bb", "BB", "cc", "CC", "x" }, Text(history.Request("x")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Exact_prompt_boundary_is_allowed_and_oversized_prompt_preserves_history(bool blazor)
    {
        var history = Create(blazor, characters: 10);
        history.Remember("a", "A");
        Assert.Equal(new[] { "sys", "1234567" }, Text(history.Request("1234567")));
        Assert.Throws<ArgumentException>(() => history.Request("12345678"));
        Assert.Equal(new[] { "sys", "a", "A", "b" }, Text(history.Request("b")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Oversized_response_is_not_retained_and_clear_preserves_system(bool blazor)
    {
        var history = Create(blazor, characters: 10);
        Assert.True(history.Remember("a", "A"));
        Assert.False(history.Remember("b", "1234567"));
        Assert.Equal(new[] { "sys", "a", "A", "c" }, Text(history.Request("c")));
        history.Clear();
        Assert.Equal(new[] { "sys", "c" }, Text(history.Request("c")));
        Assert.True(history.Remember("123", "4567"));
        Assert.Equal(new[] { "sys", "123", "4567", "" }, Text(history.Request("")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Invalid_limits_fail_early(bool blazor)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(blazor, characters: 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(blazor, turns: 0));
    }
}
