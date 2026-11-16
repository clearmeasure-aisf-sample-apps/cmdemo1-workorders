using System.Collections;
using System.Diagnostics;
using System.Reflection;
using ClearMeasure.Bootcamp.Core;

namespace ClearMeasure.Bootcamp.UI.Shared;

internal static class BusActivityTagger
{
    /// <summary>
    /// Tags the activity with the scalar properties of a bus message.
    /// </summary>
    /// <remarks>
    /// Only <see cref="IBusMessage"/> types are tagged. Their public properties are kept by the
    /// trimmer, so the reflection below is safe under Native AOT. Other objects get no property tags.
    /// </remarks>
    public static void AddScalarPropertyTags(object message, Activity activity)
    {
        if (message is IBusMessage busMessage)
        {
            AddBusMessageTags(busMessage, activity);
        }
    }

    private static void AddBusMessageTags(IBusMessage message, Activity activity)
    {
        foreach (var property in message.GetType().GetProperties())
        {
            if (IsScalarProperty(property.PropertyType)
                && property.GetCustomAttribute<ExcludeFromBusActivityAttribute>() == null)
            {
                var propertyValue = property.GetValue(message);
                activity.SetTag($"bus.message.{property.Name}", propertyValue?.ToString() ?? string.Empty);
            }
        }
    }

    private static bool IsScalarProperty(Type propertyType) =>
        !typeof(IEnumerable).IsAssignableFrom(propertyType) || propertyType == typeof(string);
}
