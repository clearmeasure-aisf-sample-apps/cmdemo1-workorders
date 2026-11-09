using System.Reflection;
using ClearMeasure.Bootcamp.Core.FeatureFlags;
using MediatR;

namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// The one place product feature flags are enforced: rejects a request whose type carries
/// <see cref="RequiresFeatureAttribute"/> for a flag that <see cref="IFeatureFlagEvaluator"/> reports disabled,
/// before the handler (or any later behavior) runs.
/// </summary>
public sealed class FeatureFlagBehavior<TRequest, TResponse>(IFeatureFlagEvaluator evaluator)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    // ReSharper disable once StaticMemberInGenericType -- one attribute lookup per closed request type
    private static readonly string[] RequiredFeatures = typeof(TRequest)
        .GetCustomAttributes<RequiresFeatureAttribute>(inherit: true)
        .Select(attribute => attribute.FeatureName)
        .Distinct(StringComparer.Ordinal)
        .ToArray();

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        foreach (string feature in RequiredFeatures)
        {
            if (!await evaluator.IsEnabledAsync(feature, cancellationToken))
            {
                throw new FeatureDisabledException(feature, typeof(TRequest).Name);
            }
        }

        return await next();
    }
}
