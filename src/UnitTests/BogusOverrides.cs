using AutoBogus;
using Bogus.Extensions;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services.Impl;

namespace ClearMeasure.Bootcamp.UnitTests;

internal class BogusOverrides : AutoGeneratorOverride
{
    public override bool CanOverride(AutoGenerateContext context)
    {
        return true;
    }

    public override void Generate(AutoGenerateOverrideContext context)
    {
        switch (context.Instance)
        {
            case WorkOrder order:
                order.Number = new WorkOrderNumberGenerator().GenerateNumber();
                order.Title = order.Title.ClampLength(1, WorkOrder.TitleMaxLength); // HasMaxLength(300)
                order.Description = order.Description.ClampLength(1, 4000); // HasMaxLength(4000)
                order.RoomNumber = order.RoomNumber.ClampLength(1, WorkOrder.RoomNumberMaxLength);
                order.Building = order.Building.ClampLength(1, WorkOrder.BuildingMaxLength);
                order.Floor = order.Floor.ClampLength(1, WorkOrder.FloorMaxLength);
                order.Priority = context.Faker.PickRandom(WorkOrderPriority.Low, WorkOrderPriority.Medium, WorkOrderPriority.High);
                // SaveDraft refuses a newly set past due date, so generated dates stay null or in the future.
                order.DueDate = context.Faker.Random.Bool()
                    ? DateOnly.FromDateTime(context.Faker.Date.Future())
                    : null;
                break;
            case WorkOrderStatus:
                context.Instance = context.Faker.PickRandom(WorkOrderStatus.GetAllItems());
                break;
            case WorkOrderSpecificationQuery query:
                query.StatusKey = context.Faker.PickRandom(WorkOrderStatus.GetAllItems()).Key;
                break;
            case Employee employee:
                employee.MiddleName = null;
                employee.PreferredLanguage = context.Faker.PickRandom("en-US", "es-ES", "fr-FR", "de-DE", "pt-BR");
                break;
        }
    }
}