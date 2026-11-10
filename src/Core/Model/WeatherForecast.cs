// ReSharper disable PropertyCanBeMadeInitOnly.Global -- Qodana P5 (#9440): NHibernate proxy / System.Text.Json set-by-convention requires mutable setters

namespace ClearMeasure.Bootcamp.Core.Model;

/// <summary>
///     Template class from sample app - moved from UI.Shared
/// </summary>
public record WeatherForecast
{
    public DateTime Date { get; set; }

    public int TemperatureC { get; set; }

    public string? Summary { get; set; }

    /// <summary>
    ///     Non-null label for display: <c>"No summary"</c> when <see cref="Summary"/> is null or whitespace; otherwise <see cref="Summary"/>.
    /// </summary>
    public string SummaryDisplay => string.IsNullOrWhiteSpace(Summary) ? "No summary" : Summary;

    // ReSharper disable once UnusedMember.Global -- JSON field of the weather forecast API; the removed template page was its only C# reader
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

}