using System.Text;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// The services the host registered, captured from its <see cref="IServiceCollection"/> so operators can see the
/// container's wiring at <c>/_diagnostics/services</c>.
/// </summary>
public sealed class RegisteredServices(IEnumerable<ServiceDescriptor> descriptors)
{
    /// <summary>One line per registration: lifetime, service type and what provides it, ordered by service type.</summary>
    public string Describe()
    {
        var text = new StringBuilder();
        foreach (var line in descriptors.Select(Describe).Order(StringComparer.Ordinal))
        {
            text.AppendLine(line);
        }

        return text.ToString();
    }

    private static string Describe(ServiceDescriptor descriptor) =>
        $"{descriptor.Lifetime,-9} {Name(descriptor.ServiceType)}{Key(descriptor)} -> {Provider(descriptor)}";

    private static string Key(ServiceDescriptor descriptor) =>
        descriptor.IsKeyedService ? $" [{descriptor.ServiceKey}]" : "";

    private static string Provider(ServiceDescriptor descriptor)
    {
        if (descriptor.IsKeyedService)
        {
            return descriptor.KeyedImplementationType is { } keyedType ? Name(keyedType) : "(factory or instance)";
        }

        if (descriptor.ImplementationType is { } type)
        {
            return Name(type);
        }

        return descriptor.ImplementationInstance is { } instance ? $"instance of {Name(instance.GetType())}" : "(factory)";
    }

    private static string Name(Type type) => type.FullName ?? type.Name;
}
