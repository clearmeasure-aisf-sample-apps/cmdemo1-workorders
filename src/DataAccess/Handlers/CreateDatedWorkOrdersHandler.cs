using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Validation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Persists multiple dated assigned work orders in one database transaction.
/// In-process validation (#1824): DueDates batch rules via
/// <see cref="CreateDatedWorkOrdersCommand.FindRuleViolations"/>, shape rules via injected
/// <see cref="CreateDatedWorkOrdersCommandShapeRules"/> (concrete FluentValidation stays off
/// MediatR <c>IValidator&lt;T&gt;</c>), both mapped to <see cref="CreateDatedWorkOrdersResult.Failed"/>.
/// </summary>
public class CreateDatedWorkOrdersHandler(
    CreateDatedWorkOrdersCommandShapeRules shapeRules,
    DatedWorkOrderBatchCreator batchCreator,
    ILogger<CreateDatedWorkOrdersHandler> logger)
    : IRequestHandler<CreateDatedWorkOrdersCommand, CreateDatedWorkOrdersResult>
{
    public async Task<CreateDatedWorkOrdersResult> Handle(
        CreateDatedWorkOrdersCommand request,
        CancellationToken cancellationToken)
    {
        if (InProcessFailure(request) is { } failure)
        {
            return failure;
        }

        var result = await batchCreator.CreateAsync(request, logger, cancellationToken);
        if (result.Success)
        {
            logger.LogInformation(
                "Created {Count} dated work orders for assignee {AssigneeUsername}",
                result.WorkOrders.Count,
                request.AssigneeUsername);
        }

        return result;
    }

    private CreateDatedWorkOrdersResult? InProcessFailure(CreateDatedWorkOrdersCommand request)
    {
        var batchViolations = request.FindRuleViolations();
        if (batchViolations.Count > 0)
        {
            return CreateDatedWorkOrdersResult.Failed(string.Join(" ", batchViolations));
        }

        var shape = shapeRules.ErrorMessages(request);
        return shape.Count > 0
            ? CreateDatedWorkOrdersResult.Failed(string.Join(" ", shape))
            : null;
    }
}
