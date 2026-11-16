using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Toolbelt.Blazor.SpeechRecognition;
using WorkOrderManagePage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderManage;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderManage;

/// <summary>
/// DOM selectors and shared arrange data for manage-page tests. No assertions.
/// </summary>
public static class WorkOrderManageSteps
{
    /// <summary>CSS selector for a manage-page data-testid.</summary>
    public static string TestId(WorkOrderManagePage.Elements element) => $"[data-testid='{element}']";

    /// <summary>Jeffrey Palermo creator used across manage-page fixtures.</summary>
    public static Employee Creator(string? preferredLanguage = null)
    {
        var employee = EmployeeBuilder.Create("jpalermo")
            .WithFirstName("Jeffrey")
            .WithLastName("Palermo")
            .WithEmail("jp@example.com")
            .WithId(Guid.NewGuid())
            .WithRole("creator", canCreate: true, canFulfill: false)
            .Build();
        if (preferredLanguage != null)
        {
            employee.PreferredLanguage = preferredLanguage;
        }

        return employee;
    }

    /// <summary>Creator without an explicit create role (display-only scenarios).</summary>
    public static Employee Viewer() =>
        EmployeeBuilder.Create("jpalermo")
            .WithFirstName("Jeffrey")
            .WithLastName("Palermo")
            .WithEmail("jp@example.com")
            .WithId(Guid.NewGuid())
            .Build();

    /// <summary>Fulfiller assignee used by shelve and attachment tests.</summary>
    public static Employee Fulfiller(string userName = "shelver") =>
        EmployeeBuilder.Create(userName)
            .WithFirstName("Shel")
            .WithLastName("Ver")
            .WithEmail($"{userName}@example.com")
            .WithId(Guid.NewGuid())
            .WithRole("fulfiller", canCreate: false, canFulfill: true)
            .Build();

    /// <summary>Timothy Lovejoy assignee for attachment RBAC tests.</summary>
    public static Employee Lovejoy() =>
        EmployeeBuilder.Create("tlovejoy")
            .WithFirstName("Timothy")
            .WithLastName("Lovejoy")
            .WithEmail("tl@example.com")
            .WithId(Guid.NewGuid())
            .Build();

    /// <summary>Builds a draft attachment row.</summary>
    public static WorkOrderAttachment Attachment(Guid workOrderId, string fileName, Employee uploadedBy) =>
        new()
        {
            Id = Guid.NewGuid(),
            WorkOrderId = workOrderId,
            FileName = fileName,
            ContentType = "image/jpeg",
            FileSize = 2048,
            UploadedById = uploadedBy.Id,
            UploadedBy = uploadedBy,
            UploadedDate = new DateTime(2025, 3, 1, 10, 0, 0)
        };

    /// <summary>In-progress work order owned and assigned to <paramref name="user"/>.</summary>
    public static WorkOrder InProgressOwnedBy(Employee user, string number = "WO-SHELVE") =>
        WorkOrderBuilder.Create()
            .WithId(Guid.NewGuid())
            .WithNumber(number)
            .WithTitle("Existing")
            .WithDescription("Desc")
            .WithStatus(WorkOrderStatus.InProgress)
            .CreatedBy(user)
            .AssignedTo(user)
            .Build();

    /// <summary>Builds a final speech-recognition event.</summary>
    public static SpeechRecognitionEventArgs FinalTranscript(string transcript) =>
        new()
        {
            ResultIndex = 0,
            Results =
            [
                new SpeechRecognitionResult
                {
                    IsFinal = true,
                    Items = [new SpeechRecognitionAlternative { Transcript = transcript, Confidence = 0.9 }]
                }
            ]
        };

    /// <summary>Builds an interim speech-recognition event.</summary>
    public static SpeechRecognitionEventArgs InterimTranscript(string transcript) =>
        new()
        {
            ResultIndex = 0,
            Results =
            [
                new SpeechRecognitionResult
                {
                    IsFinal = false,
                    Items = [new SpeechRecognitionAlternative { Transcript = transcript, Confidence = 0.5 }]
                }
            ]
        };
}
