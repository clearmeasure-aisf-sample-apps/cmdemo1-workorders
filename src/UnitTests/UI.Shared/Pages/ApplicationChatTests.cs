using System.Text.RegularExpressions;
using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using MediatR;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class ApplicationChatTests
{
    [Test]
    public async Task ShouldRenderChatShellAndInputElements()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<ApplicationChat>();

        component.Find($"[data-testid='{ApplicationChat.Elements.ChatShell}']").ShouldNotBeNull();
        component.Find($"[data-testid='{ApplicationChat.Elements.ChatContainer}']").ShouldNotBeNull();
        component.Find($"[data-testid='{ApplicationChat.Elements.ChatInputContainer}']").ShouldNotBeNull();
        component.Find($"[data-testid='{ApplicationChat.Elements.ChatInput}']").ShouldNotBeNull();
        component.Find($"[data-testid='{ApplicationChat.Elements.SendButton}']").ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldRenderUsageHintUnderHeading()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<ApplicationChat>();

        component.Find($"[data-testid='{ApplicationChat.Elements.ChatHint}']")
            .TextContent.ShouldBe("Ask a question about the application.");
    }

    [Test]
    public async Task ShouldRenderTooltipWithExactTitleOnChatHeading()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<ApplicationChat>();

        var heading = component.Find($"[data-testid='{ApplicationChat.Elements.ChatHeadingTooltip}']");
        heading.GetAttribute("title").ShouldBe("Chat with the application assistant");
    }

    [Test]
    public async Task ShouldRenderChatHistoryViewportAfterSendingMessage()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<ApplicationChat>();
        await component.Find($"[data-testid='{ApplicationChat.Elements.ChatInput}']").ChangeAsync(new() { Value = "first prompt" });
        await component.Find($"[data-testid='{ApplicationChat.Elements.SendButton}']").ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
        {
            component.Find($"[data-testid='{ApplicationChat.Elements.ChatHistory}']").ShouldNotBeNull();
            component.Find($"[data-testid='{ApplicationChat.Elements.ChatHistoryViewport}']").ShouldNotBeNull();
            component.FindAll(".chat-message").Count.ShouldBeGreaterThanOrEqualTo(2);
        });
    }

    [Test]
    public async Task ShouldKeepPromptInputAvailableAfterManyMessages()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<ApplicationChat>();

        for (var i = 0; i < 12; i++)
        {
            var prompt = $"Prompt {i}";
            await component.Find($"[data-testid='{ApplicationChat.Elements.ChatInput}']").ChangeAsync(new() { Value = prompt });
            await component.Find($"[data-testid='{ApplicationChat.Elements.SendButton}']").ClickAsync(new());
            await component.WaitForAssertionAsync(() =>
            {
                component.Markup.ShouldContain(prompt);
            });
        }

        component.Find($"[data-testid='{ApplicationChat.Elements.ChatInput}']").ShouldNotBeNull();
        component.Find($"[data-testid='{ApplicationChat.Elements.SendButton}']").ShouldNotBeNull();
        component.FindAll(".chat-message").Count.ShouldBeGreaterThanOrEqualTo(24);
    }

    [Test]
    public async Task ShouldSendMessageWhenEnterKeyPressed()
    {
        await using var ctx = CreateContext();
        var component = ctx.Render<ApplicationChat>();

        var chatInput = component.Find($"[data-testid='{ApplicationChat.Elements.ChatInput}']");
        await chatInput.ChangeAsync(new() { Value = "test prompt via enter" });
        await chatInput.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        await component.WaitForAssertionAsync(() =>
        {
            component.Find($"[data-testid='{ApplicationChat.Elements.ChatHistory}']").ShouldNotBeNull();
            component.FindAll(".chat-message").Count.ShouldBeGreaterThanOrEqualTo(1);
            component.Markup.ShouldContain("test prompt via enter");
        });
    }

    [Test]
    public async Task ShouldRenderPageHeaderTokenCardAndAppStyleSendButton()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<ApplicationChat>();

        component.FindComponent<PageHeader>().ShouldNotBeNull();
        var heading = component.Find($"[data-testid='{ApplicationChat.Elements.ChatHeadingTooltip}']");
        heading.TagName.ShouldBe("H1", StringCompareShould.IgnoreCase);
        heading.GetAttribute("title").ShouldBe("Chat with the application assistant");
        heading.TextContent.ShouldBe("AI Agent");
        component.Find($"[data-testid='{ApplicationChat.Elements.ChatHint}']")
            .TextContent.ShouldBe("Ask a question about the application.");
        component.Markup.ShouldNotContain("🤖");
        component.Markup.ShouldContain("Ask questions about work orders, employees, or general assistance");

        component.Find($"[data-testid='{ApplicationChat.Elements.SendButton}']").GetAttribute("class")
            .ShouldBe("btn-app btn-app--primary");
        var cardClass = component.Find($"[data-testid='{ApplicationChat.Elements.ChatContainer}']").GetAttribute("class") ?? "";
        cardClass.ShouldContain("token-card");
    }

    [Test]
    public void ShouldKeepAssistantStylesheetFreeOfHexColours()
    {
        var path = Path.GetFullPath(Path.Combine(
            NUnit.Framework.TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "UI.Shared", "Pages", "ApplicationChat.razor.css"));
        File.Exists(path).ShouldBeTrue($"Expected scoped stylesheet at {path}");
        var css = File.ReadAllText(path);

        Regex.IsMatch(css, "#[0-9a-fA-F]{3,8}").ShouldBeFalse();
        css.ShouldNotContain("rgba(");
        css.ShouldNotContain("hsl(");
        css.ShouldContain(".token-card");
        css.ShouldContain(".btn-app--primary");
    }

    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.SetupVoid("scrollToBottom", _ => true).SetVoidResult();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new ApplicationChatStubBus());

        var provider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        provider.Login("hsimpson").GetAwaiter().GetResult();
        ctx.Services.AddSingleton(provider);

        return ctx;
    }

    private sealed class ApplicationChatStubBus() : Bus(null!)
    {
        public override Task Publish(INotification notification)
        {
            return Task.CompletedTask;
        }

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is ApplicationChatQuery)
            {
                throw new InvalidOperationException("Simulated AI service failure");
            }

            throw new NotImplementedException();
        }
    }
}
