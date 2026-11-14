using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Server.TestSupport;
using MediatR;

namespace ClearMeasure.Bootcamp.UI.Server;

public class ChatClientConfigQueryHandler(IConfiguration configuration, ILogger<ChatClientConfigQueryHandler> logger)
    : IRequestHandler<ChatClientConfigQuery, ChatClientConfig>
{
    public Task<ChatClientConfig> Handle(ChatClientConfigQuery request, CancellationToken cancellationToken)
    {
        if (ShouldUseStubChatClient())
        {
            return Task.FromResult(StubConfig());
        }

        return Task.FromResult(AzureOpenAiConfig());
    }

    private bool ShouldUseStubChatClient()
    {
        var testSupport = TestSupportOptions.FromConfiguration(configuration);
        var useStub = configuration.GetValue("AI_UseStubChatClient", false) || testSupport.Enabled;
        logger.LogDebug(
            "AI_UseStubChatClient / TestSupport.Enabled => stub={UseStub} (testSupport={TestSupport})",
            useStub,
            testSupport.Enabled);
        return useStub;
    }

    private static ChatClientConfig StubConfig() =>
        new()
        {
            AiOpenAiApiKey = "",
            AiOpenAiUrl = "",
            AiOpenAiModel = "",
            UseStubChatClient = true
        };

    private ChatClientConfig AzureOpenAiConfig()
    {
        var apiKey = configuration.GetValue<string>("AI_OpenAI_ApiKey");
        logger.LogDebug("AI_OpenAI_ApiKey configured: {Configured}", !string.IsNullOrEmpty(apiKey));
        var openAiUrl = configuration.GetValue<string>("AI_OpenAI_Url");
        logger.LogDebug("AI_OpenAI_Url found as {Url}", openAiUrl);
        var openAiModel = configuration.GetValue<string>("AI_OpenAI_Model");
        logger.LogDebug("AI_OpenAI_Model found as {Model}", openAiModel);
        return new ChatClientConfig
        {
            AiOpenAiApiKey = apiKey,
            AiOpenAiUrl = openAiUrl,
            AiOpenAiModel = openAiModel,
            UseStubChatClient = false
        };
    }
}
