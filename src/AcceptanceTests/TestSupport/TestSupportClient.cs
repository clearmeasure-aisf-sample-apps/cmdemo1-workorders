using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.AcceptanceTests.Builders;
using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.AcceptanceTests.TestSupport;

/// <summary>
/// HTTP client for the pool-slot test-support API. Creates employees and work orders without SQL.
/// </summary>
public sealed class TestSupportClient
{
    private const string PathPrefix = "/api/test-support";
    private const string SecretHeaderName = "X-Test-Support-Secret";

    /// <summary>Must match the Blazor localStorage session key.</summary>
    public const string UserSessionStorageKey = "bootcamp.userSession.username";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _secret;

    /// <summary>Builds a client aimed at <paramref name="applicationBaseUrl"/>.</summary>
    public TestSupportClient(string applicationBaseUrl, string secret)
    {
        _baseUrl = applicationBaseUrl.TrimEnd('/');
        _secret = secret;
        _http = TestHttpClientFactory.CreateInsecureClient();
    }

    /// <summary>Creates an admin employee keyed by <paramref name="testTag"/>.</summary>
    public async Task<TestSupportEmployee> CreateEmployeeAsync(
        string testTag,
        TestSupportEmployeeFields? fields = null)
    {
        fields ??= new TestSupportEmployeeFields();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}{PathPrefix}/employees");
        AddSecret(request);
        request.Content = JsonContent.Create(new
        {
            testTag,
            canCreateWorkOrder = true,
            canFulfillWorkOrder = true,
            firstName = fields.FirstName,
            lastName = fields.LastName,
            middleName = fields.MiddleName,
            additionalCreateRoleNames = fields.AdditionalCreateRoleNames
        });

        using var response = await _http.SendAsync(request);
        await EnsureSuccess(response, "create employee");
        var employee = await response.Content.ReadFromJsonAsync<TestSupportEmployee>(JsonOptions);
        return employee ?? throw new InvalidOperationException("Test-support employee response was empty.");
    }

    /// <summary>Creates a work order from seed fields.</summary>
    public async Task<TestSupportWorkOrder> CreateWorkOrderAsync(
        string testTag,
        string creatorUserName,
        WorkOrderStatus status,
        WorkOrderSeedFields fields)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}{PathPrefix}/work-orders");
        AddSecret(httpRequest);
        httpRequest.Content = JsonContent.Create(WorkOrderJsonBody(testTag, creatorUserName, status, fields));
        using var response = await _http.SendAsync(httpRequest);
        await EnsureSuccess(response, "create work order");
        var workOrder = await response.Content.ReadFromJsonAsync<TestSupportWorkOrder>(JsonOptions);
        return workOrder ?? throw new InvalidOperationException("Test-support work-order response was empty.");
    }

    private static object WorkOrderJsonBody(
        string testTag, string creatorUserName, WorkOrderStatus status, WorkOrderSeedFields fields) =>
        new
        {
            testTag,
            creatorUserName,
            title = fields.Title,
            description = fields.Description,
            roomNumber = fields.RoomNumber,
            instructions = fields.Instructions,
            number = fields.Number,
            statusKey = status.Key,
            priority = fields.Priority.ToString(),
            dueDate = fields.DueDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            assigneeUserName = fields.AssigneeUserName,
            building = fields.Building,
            createdDate = fields.CreatedDate?.ToString("o", System.Globalization.CultureInfo.InvariantCulture),
            completedDate = fields.CompletedDate?.ToString("o", System.Globalization.CultureInfo.InvariantCulture)
        };

    /// <summary>Probes whether the host exposes test-support (pool) or answers 404 (tdd/uat/prod).</summary>
    public async Task<HttpStatusCode> ProbeStatusAsync()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{_baseUrl}{PathPrefix}/sign-in-storage-state?userName=probe");
        AddSecret(request);
        using var response = await _http.SendAsync(request);
        return response.StatusCode;
    }

    private void AddSecret(HttpRequestMessage request) =>
        request.Headers.TryAddWithoutValidation(SecretHeaderName, _secret);

    private static async Task EnsureSuccess(HttpResponseMessage response, string action)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException(
            $"Test-support {action} failed with {(int)response.StatusCode} {response.StatusCode}: {body}");
    }
}
