namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Named delays and disable switch for <see cref="AutoReformatAgentService"/>.
/// Built from configuration so the flat <see cref="DisableConfigurationKey"/> keeps working.
/// </summary>
public sealed class AutoReformatOptions
{
    /// <summary>Configuration key that disables the background loop when set to <c>true</c>.</summary>
    public const string DisableConfigurationKey = "DISABLE_AUTO_REFORMAT_AGENT";

    /// <summary>Optional section for poll and retry delays.</summary>
    public const string SectionName = "AutoReformat";

    /// <summary>When true, <see cref="AutoReformatAgentService"/> exits without polling.</summary>
    public bool Disabled { get; }

    /// <summary>Delay after a successful cycle (default 5 seconds).</summary>
    public TimeSpan PollInterval { get; }

    /// <summary>Delay after a cycle-level exception (default 30 seconds).</summary>
    public TimeSpan ErrorRetryDelay { get; }

    /// <summary>Creates options from the host configuration.</summary>
    public AutoReformatOptions(IConfiguration configuration)
    {
        Disabled = AutoReformatAgentService.IsDisabled(configuration);
        var section = configuration.GetSection(SectionName);
        PollInterval = TimeSpan.FromSeconds(section.GetValue("PollIntervalSeconds", 5));
        ErrorRetryDelay = TimeSpan.FromSeconds(section.GetValue("ErrorRetryDelaySeconds", 30));
    }

    /// <summary>Test and explicit construction.</summary>
    public AutoReformatOptions(bool disabled, TimeSpan pollInterval, TimeSpan errorRetryDelay)
    {
        Disabled = disabled;
        PollInterval = pollInterval;
        ErrorRetryDelay = errorRetryDelay;
    }

    /// <summary>Defaults matching the prior hard-coded delays.</summary>
    public AutoReformatOptions()
        : this(disabled: false, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30))
    {
    }
}
