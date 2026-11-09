using ClearMeasure.Bootcamp.McpServer.Resources;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer;

[TestFixture]
public class McpReferenceResourceTests
{
    [Test]
    public void ShouldReturnAllWorkOrderStatuses()
    {
        string result = ReferenceResources.GetWorkOrderStatuses();

        result.ShouldContain("Draft");
        result.ShouldContain("Assigned");
        result.ShouldContain("InProgress");
        result.ShouldContain("Complete");
        result.ShouldContain("DRT");
        result.ShouldContain("ASD");
        result.ShouldContain("IPG");
        result.ShouldContain("CMP");
    }

    [Test]
    public void ShouldReturnAllRoles()
    {
        string result = ReferenceResources.GetRoles();

        result.ShouldContain("Manager");
        result.ShouldContain("Worker");
        result.ShouldContain("Admin");
        result.ShouldContain("CanCreateWorkOrder");
        result.ShouldContain("CanFulfillWorkOrder");
    }

    [Test]
    public void ShouldReturnStatusTransitions()
    {
        string result = ReferenceResources.GetStatusTransitions();

        result.ShouldContain("DraftToAssignedCommand");
        result.ShouldContain("AssignedToInProgressCommand");
        result.ShouldContain("InProgressToCompleteCommand");
    }

    [Test]
    public void ShouldReturnAllWorkOrderPriorities()
    {
        string result = ReferenceResources.GetWorkOrderPriorities();

        result.ShouldContain("Low");
        result.ShouldContain("Medium");
        result.ShouldContain("High");
    }
}
