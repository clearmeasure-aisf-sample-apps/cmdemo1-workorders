using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Messaging;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Validation;
using ClearMeasure.Bootcamp.UI.Server.Middleware;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.WorkOrders.FieldRules;

/// <summary>
/// Pins the single-API 400 body so SaveDraft title-length failures keep the Core message.
/// </summary>
[TestFixture]
public class WorkOrderFieldRulesSingleApiCharacterizationTests
{
    [Test]
    public async Task SingleApi_SaveDraftTitleTooLong_ReturnsCoreMessageInProblemBody()
    {
        var services = new ServiceCollection()
            .AddSingleton<IValidator<SaveDraftCommand>>(new SaveDraftCommandValidator(TimeProvider.System))
            .BuildServiceProvider();
        var command = new SaveDraftCommand(
            new WorkOrder { Title = new string('T', WorkOrder.TitleMaxLength + 1), Description = "d" },
            new Employee());
        var validation = await WebServiceMessagePayloadValidator.ValidateAsync(
            new WebServiceMessage(command), services, CancellationToken.None);
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        validation.IsValid.ShouldBeFalse();
        await ProblemDetailsWriter.WriteValidationProblemAsync(context, validation.Errors);

        using var problem = JsonDocument.Parse(((MemoryStream)context.Response.Body).ToArray());
        context.Response.StatusCode.ShouldBe(400);
        problem.RootElement.GetProperty("errors").GetProperty("WorkOrder.Title")[0].GetString()
            .ShouldBe(WorkOrderFieldRules.TitleCannotExceed);
    }
}
