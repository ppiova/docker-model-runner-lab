using OpenAI.Chat;

namespace BlazorChat;

// Also present in module 03 so each lab can be built from its own directory.
// Both copies are exercised by the same behavioral test cases.
public sealed class ConversationHistory
{
    public const int DefaultMaxCharacters = 16_000;
    public const int DefaultMaxTurns = 10;
    private readonly string systemPrompt;
    private readonly List<(string Prompt, string Reply)> turns = new();
    private int characters;
    public int MaxCharacters { get; }
    public int MaxTurns { get; }
    public int MaxPromptCharacters => MaxCharacters - systemPrompt.Length;

    public ConversationHistory(string systemPrompt, int maxCharacters = DefaultMaxCharacters,
        int maxTurns = DefaultMaxTurns)
    {
        if (maxCharacters <= systemPrompt.Length)
            throw new ArgumentOutOfRangeException(nameof(maxCharacters), "CHAT_MAX_HISTORY_CHARS must leave room after the system message.");
        if (maxTurns <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxTurns), "CHAT_MAX_HISTORY_TURNS must be positive.");
        this.systemPrompt = systemPrompt;
        MaxCharacters = maxCharacters;
        MaxTurns = maxTurns;
    }

    public static ConversationHistory FromEnvironment(string systemPrompt) => new(systemPrompt,
        ReadLimit("CHAT_MAX_HISTORY_CHARS", DefaultMaxCharacters),
        ReadLimit("CHAT_MAX_HISTORY_TURNS", DefaultMaxTurns));

    private static int ReadLimit(string name, int fallback)
    {
        string? value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrEmpty(value)) { return fallback; }
        if (int.TryParse(value, out int limit) && limit > 0) { return limit; }
        throw new ArgumentException($"{name} must be a positive integer.");
    }

    public IReadOnlyList<ChatMessage> CreateRequest(string prompt)
    {
        if (prompt.Length > MaxPromptCharacters)
            throw new ArgumentException($"Message is too long. Use at most {MaxPromptCharacters} characters.", nameof(prompt));

        int remaining = MaxPromptCharacters - prompt.Length;
        int start = turns.Count;
        while (start > 0)
        {
            var turn = turns[start - 1];
            long length = (long)turn.Prompt.Length + turn.Reply.Length;
            if (length > remaining) { break; }
            remaining -= (int)length;
            start--;
        }
        var messages = new List<ChatMessage> { new SystemChatMessage(systemPrompt) };
        foreach (var turn in turns.Skip(start))
        {
            messages.Add(new UserChatMessage(turn.Prompt));
            messages.Add(new AssistantChatMessage(turn.Reply));
        }
        messages.Add(new UserChatMessage(prompt));
        return messages;
    }

    // Commit only after successful generation. Canceled/failed requests do not
    // mutate retained history, including when their request omitted older turns.
    public bool Remember(string prompt, string reply)
    {
        long length = (long)prompt.Length + reply.Length;
        if (length > MaxPromptCharacters) { return false; }
        while (turns.Count > 0 && (turns.Count >= MaxTurns || characters > MaxPromptCharacters - length))
        {
            characters -= turns[0].Prompt.Length + turns[0].Reply.Length;
            turns.RemoveAt(0);
        }
        turns.Add((prompt, reply));
        characters += (int)length;
        return true;
    }

    public void Clear()
    {
        turns.Clear();
        characters = 0;
    }
}
