using System.Reflection;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.UI.Shared.Components;

/// <summary>
/// Site footer: copyright year, assembly version, git SHA and environment name.
/// </summary>
public partial class AppFooter
{
    [Inject]
    private TimeProvider Clock { get; set; } = null!;

    private DeploymentIdentity _deployment = DeploymentIdentity.Unreported;

    /// <summary>
    /// Calendar year shown in the site copyright line (UTC, matches acceptance tests).
    /// </summary>
    private int CopyrightYear => Clock.GetUtcNow().Year;

    /// <summary>
    /// Informational version of the running entry assembly, e.g. "1.2.3+abc1234".
    /// </summary>
    private string AppVersion =>
        Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _deployment = await Bus.Send(new DeploymentIdentityQuery());
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
        {
            Logger.LogWarning(ex, "Deployment identity could not be loaded; footer shows unreported values.");
            _deployment = DeploymentIdentity.Unreported;
        }
    }
}
