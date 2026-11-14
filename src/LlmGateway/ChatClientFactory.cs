using Azure;
using Azure.AI.OpenAI;
using ClearMeasure.Bootcamp.Core;
using Microsoft.Extensions.AI;
using OpenAI.Chat;

namespace ClearMeasure.Bootcamp.LlmGateway;

public class ChatClientFactory(IBus bus)
{
    /// <summary>
    /// Returns whether chat client configuration is present and usable.
    /// Virtual for unit-test stubs of dependent health checks.
    /// </summary>
    public virtual async Task<ChatClientAvailabilityResult> IsChatClientAvailable()
    {
        var config = await bus.Send(new ChatClientConfigQuery());
        return ChatClientConfigValidator.Validate(config);
    }

    /// <summary>
    /// Builds a tracing-wrapped chat client: stub when configured for pool slots, otherwise Azure OpenAI.
    /// Virtual for unit-test stubs of dependent health checks.
    /// </summary>
    public virtual async Task<IChatClient> GetChatClient()
    {
        var config = await bus.Send(new ChatClientConfigQuery());
        var availability = ChatClientConfigValidator.Validate(config);
        if (!availability.IsAvailable)
        {
            throw new InvalidOperationException(availability.Message);
        }

        IChatClient innerClient = config.UseStubChatClient
            ? BuildStubChatClient()
            : BuildAzureOpenAiChatClient(config);

        return new TracingChatClient(innerClient);
    }

    private static IChatClient BuildStubChatClient() =>
        new StubChatClient()
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();

    private static IChatClient BuildAzureOpenAiChatClient(ChatClientConfig config)
    {
        var apiKey = config.AiOpenAiApiKey
            ?? throw new InvalidOperationException("AI_OpenAI_ApiKey is not configured.");
        var openAiUrl = config.AiOpenAiUrl
            ?? throw new InvalidOperationException("AI_OpenAI_Url is not configured.");
        var openAiModel = config.AiOpenAiModel
            ?? throw new InvalidOperationException("AI_OpenAI_Model is not configured.");

        var credential = new AzureKeyCredential(apiKey);
        var openAiClient = new AzureOpenAIClient(new Uri(openAiUrl), credential);
        ChatClient chatClient = openAiClient.GetChatClient(openAiModel);
        return chatClient.AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();
    }
}
