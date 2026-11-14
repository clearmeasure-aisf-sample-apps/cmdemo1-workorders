using ClearMeasure.Bootcamp.AcceptanceTests.Builders;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Api;

/// <summary>
/// API acceptance base: fixtures proven under <see cref="ParallelScope.All"/> with no shared-row seeds.
/// </summary>
public abstract class ApiAcceptanceTestBase : AcceptanceApiTestBase
{
    /// <inheritdoc />
    protected override bool RequiresBrowser => false;
}
