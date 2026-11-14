using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class ChatClientConfigQueryHandlerTests
{
    [Test]
    public async Task Handle_ShouldReturnConfigurationValues_WithoutConditionalLoggerAccess()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI_OpenAI_ApiKey"] = "test-key",
                ["AI_OpenAI_Url"] = "https://example.test",
                ["AI_OpenAI_Model"] = "test-model"
            })
            .Build();
        var handler = new ChatClientConfigQueryHandler(
            configuration,
            NullLogger<ChatClientConfigQueryHandler>.Instance);

        var result = await handler.Handle(new ChatClientConfigQuery(), CancellationToken.None);

        result.AiOpenAiApiKey.ShouldBe("test-key");
        result.AiOpenAiUrl.ShouldBe("https://example.test");
        result.AiOpenAiModel.ShouldBe("test-model");
        result.UseStubChatClient.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_ShouldReturnUseStubChatClient_WhenConfiguredExplicitly()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI_UseStubChatClient"] = "true",
                ["AI_OpenAI_ApiKey"] = "should-be-cleared"
            })
            .Build();
        var handler = new ChatClientConfigQueryHandler(
            configuration,
            NullLogger<ChatClientConfigQueryHandler>.Instance);

        var result = await handler.Handle(new ChatClientConfigQuery(), CancellationToken.None);

        result.UseStubChatClient.ShouldBeTrue();
        result.AiOpenAiApiKey.ShouldBe("");
    }

    [Test]
    public async Task Handle_ShouldForceStub_WhenTestSupportEnabled_EvenIfAzureKeysPresent()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TestSupport:Enabled"] = "true",
                ["AI_OpenAI_ApiKey"] = "real-key",
                ["AI_OpenAI_Url"] = "https://real.example",
                ["AI_OpenAI_Model"] = "gpt-real",
                ["AI_UseStubChatClient"] = "false"
            })
            .Build();
        var handler = new ChatClientConfigQueryHandler(
            configuration,
            NullLogger<ChatClientConfigQueryHandler>.Instance);

        var result = await handler.Handle(new ChatClientConfigQuery(), CancellationToken.None);

        result.UseStubChatClient.ShouldBeTrue();
        result.AiOpenAiApiKey.ShouldBeEmpty();
        result.AiOpenAiUrl.ShouldBeEmpty();
        result.AiOpenAiModel.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_ShouldKeepRealModel_WhenTestSupportOffAndStubFlagOff()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TestSupport:Enabled"] = "false",
                ["AI_UseStubChatClient"] = "false",
                ["AI_OpenAI_ApiKey"] = "tdd-key",
                ["AI_OpenAI_Url"] = "https://tdd.example",
                ["AI_OpenAI_Model"] = "gpt-tdd"
            })
            .Build();
        var handler = new ChatClientConfigQueryHandler(
            configuration,
            NullLogger<ChatClientConfigQueryHandler>.Instance);

        var result = await handler.Handle(new ChatClientConfigQuery(), CancellationToken.None);

        result.UseStubChatClient.ShouldBeFalse();
        result.AiOpenAiApiKey.ShouldBe("tdd-key");
        result.AiOpenAiModel.ShouldBe("gpt-tdd");
    }
}
