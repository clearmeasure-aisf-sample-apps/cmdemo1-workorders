using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.AcceptanceTests.McpServer;

[TestFixture]
public class McpChatConversationTests : McpAcceptanceTestBase
{
	protected override bool RequiresBrowser => false;

	[SetUp]
	public async Task EnsureChatClientAsync()
	{
		await SkipIfNoChatClient();
	}

	[Test, LlmTest]
	public async Task ShouldCreateAndAssignWorkOrderFromConversationalPrompt()
	{
		var creator = await SeedEmployeeAsync();
		var assignee = await SeedEmployeeAsync();
		var response = await Helper.SendPrompt(BuildLawnPrompt(creator, assignee));
		response.Text.ShouldNotBeNullOrEmpty();

		var workOrderNumber = ExtractWorkOrderNumber(response.Text);
		var lawnWorkOrder = await Bus.Send(new WorkOrderByNumberQuery(workOrderNumber));
		AssertAssignedLawnOrder(lawnWorkOrder, creator, assignee);
	}

	private string BuildLawnPrompt(Employee creator, Employee assignee) =>
		$"I am {creator.GetFullName()} (my username is {creator.UserName}). " +
		$"Create a new work order assigned to {assignee.GetFullName()} (username {assignee.UserName}) " +
		"to cut the grass and make sure that the edging is done and that fertilizer is put down. " +
		"This will be on the outdoor lawn. " +
		"Steps to follow:\n" +
		$"1. Call create-work-order with a suitable title that includes '[{TestTag}]', a description that captures the full scope of work " +
		$"(cutting grass, edging, and fertilizer), creatorUsername='{creator.UserName}', and roomNumber='Outdoor Lawn'.\n" +
		"2. Take the work order Number returned from step 1 and call execute-work-order-command with " +
		$"commandName='DraftToAssignedCommand', executingUsername='{creator.UserName}', assigneeUsername='{assignee.UserName}'.\n" +
		"In your final response, include the work order number on its own line in exactly this format: " +
		"WorkOrderNumber: <number>";

	private static string ExtractWorkOrderNumber(string responseText)
	{
		var match = Regex.Match(responseText, @"WorkOrderNumber:\s*(\S+)");
		match.Success.ShouldBeTrue(
			$"Expected response to contain 'WorkOrderNumber: <number>'. Response was: {responseText}");
		return match.Groups[1].Value;
	}

	private static void AssertAssignedLawnOrder(WorkOrder? lawnWorkOrder, Employee creator, Employee assignee)
	{
		lawnWorkOrder.ShouldNotBeNull("Expected a work order to exist");
		lawnWorkOrder.Status.ShouldBe(WorkOrderStatus.Assigned);
		lawnWorkOrder.Creator!.UserName.ShouldBe(creator.UserName);
		lawnWorkOrder.Assignee!.UserName.ShouldBe(assignee.UserName);
		lawnWorkOrder.Title.ShouldNotBeNullOrEmpty();
		lawnWorkOrder.Description.ShouldNotBeNullOrEmpty();
		AssertLawnDescription(lawnWorkOrder.Description!);
		lawnWorkOrder.RoomNumber.ShouldNotBeNullOrEmpty(
			"Room number should be set to a value representing the outdoor lawn");
	}

	private static void AssertLawnDescription(string description)
	{
		var lower = description.ToLowerInvariant();
		lower.ShouldContain("grass");
		(lower.Contains("edging") || lower.Contains("edge"))
			.ShouldBeTrue($"Expected description to mention edging or edges: {description}");
		lower.ShouldContain("fertilizer");
	}
}
