using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.Core.Queries;
using MediatR;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Evaluates every <see cref="FeatureFlagNames.All"/> entry through the runtime <see cref="IFeatureFlagEvaluator"/>.
/// </summary>
public class FeatureFlagsQueryHandler(IFeatureFlagEvaluator evaluator) : IRequestHandler<FeatureFlagsQuery, FeatureFlagsResult>
{
    public async Task<FeatureFlagsResult> Handle(FeatureFlagsQuery request, CancellationToken cancellationToken)
    {
        var flags = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var name in FeatureFlagNames.All)
        {
            flags[name] = await evaluator.IsEnabledAsync(name, cancellationToken);
        }

        return new FeatureFlagsResult(flags);
    }
}
