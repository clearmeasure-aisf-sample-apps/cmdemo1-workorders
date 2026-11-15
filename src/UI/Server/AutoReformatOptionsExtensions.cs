using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Registers <see cref="AutoReformatOptions"/> from host configuration.
/// </summary>
public static class AutoReformatOptionsExtensions
{
    /// <summary>
    /// Binds options from the flat disable key and the optional <see cref="AutoReformatOptions.SectionName"/> section.
    /// </summary>
    public static IServiceCollection AddAutoReformatOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IOptions<AutoReformatOptions>>(_ =>
            Options.Create(new AutoReformatOptions(configuration)));
        return services;
    }
}
