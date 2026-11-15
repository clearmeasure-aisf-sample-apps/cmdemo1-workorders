using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class ApplicationChatAutocompleteTests
{
    [Test]
    public void ShouldTurnOffAutofillOnTheChatInput()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddLogging();
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(userName: "hsimpson"));

        var component = ctx.Render<ApplicationChat>();

        var chatInput = component.Find($"[data-testid='{ApplicationChat.Elements.ChatInput}']");
        chatInput.GetAttribute("autocomplete").ShouldBe("off");
    }
}
