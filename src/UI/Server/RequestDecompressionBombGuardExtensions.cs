using Microsoft.AspNetCore.RequestDecompression;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>Registers request decompression with <see cref="RequestDecompressionBombGuard"/> in front of it.</summary>
public static class RequestDecompressionBombGuardExtensions
{
    /// <summary>
    /// Calls <c>AddRequestDecompression</c>, binds <see cref="RequestDecompressionBombGuardOptions"/> and wraps the
    /// registered <see cref="IRequestDecompressionProvider"/> so decompressed bodies cannot exceed the configured cap.
    /// </summary>
    public static void AddRequestDecompressionBombGuard(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRequestDecompression();
        services.Configure<RequestDecompressionBombGuardOptions>(
            configuration.GetSection(RequestDecompressionBombGuardOptions.SectionName));
        var framework = services.Last(descriptor => descriptor.ServiceType == typeof(IRequestDecompressionProvider));
        services.Remove(framework);
        services.AddSingleton<IRequestDecompressionProvider>(provider => new RequestDecompressionBombGuard(
            CreateFrameworkProvider(provider, framework),
            provider.GetRequiredService<IOptions<RequestDecompressionBombGuardOptions>>()));
    }

    private static IRequestDecompressionProvider CreateFrameworkProvider(
        IServiceProvider provider, ServiceDescriptor framework) =>
        (IRequestDecompressionProvider)(framework.ImplementationInstance
            ?? framework.ImplementationFactory?.Invoke(provider)
            ?? ActivatorUtilities.CreateInstance(provider, framework.ImplementationType!));
}
