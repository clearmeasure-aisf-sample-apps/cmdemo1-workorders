namespace ClearMeasure.Bootcamp.LlmGateway;

public class ChatClientConfig
{
    public required string? AiOpenAiApiKey { get; init; }
    public required string? AiOpenAiUrl { get; init; }
    public required string? AiOpenAiModel { get; init; }

    /// <summary>
    /// When true, <see cref="ChatClientFactory"/> returns <see cref="StubChatClient"/> instead of Azure OpenAI.
    /// True when <c>AI_UseStubChatClient</c> is set or when <c>TestSupport:Enabled</c> (pool slots).
    /// False on tdd (real model), uat, and prod.
    /// </summary>
    public bool UseStubChatClient { get; init; }
}