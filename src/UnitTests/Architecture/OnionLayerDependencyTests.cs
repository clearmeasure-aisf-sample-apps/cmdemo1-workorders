using ArchUnitNET.Fluent;
using ArchUnitNET.Fluent.Slices;
using Shouldly;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ClearMeasure.Bootcamp.UnitTests.Architecture;

/// <summary>
/// Onion layer rules over the compiled assemblies (ArchUnitNET reads the IL, so method bodies count).
/// Each layer depends only on the layers allowed for it, so the layer graph is acyclic and nothing depends on
/// the composition root. Within a layer, the namespaces have no dependency cycles, except the layers on the
/// known-cycle list, which must keep failing that rule until they are fixed and taken off the list.
/// </summary>
[TestFixture]
public class OnionLayerDependencyTests
{
    private static IEnumerable<OnionLayer> Layers => SolutionArchitecture.Layers;

    private static IEnumerable<OnionLayer> AcyclicLayers => AllLayers.Where(layer => !HasKnownCycles(layer));

    private static IEnumerable<OnionLayer> CyclicLayers => AllLayers.Where(HasKnownCycles);

    private static IEnumerable<OnionLayer> AllLayers => SolutionArchitecture.Layers.Append(SolutionArchitecture.CompositionRoot);

    [TestCaseSource(nameof(Layers))]
    public void ShouldDependOnlyOnInnerLayers_WhenLayerIsCompiled(OnionLayer layer)
    {
        var forbidden = SolutionArchitecture.ForbiddenFor(layer).ToList();
        var forbiddenPattern = string.Join("|", forbidden.Select(other => other.NamespacePattern));

        var rule = Types().That().ResideInNamespaceMatching(layer.NamespacePattern)
            .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(forbiddenPattern))
            .Because($"{layer} may depend only on [{string.Join(", ", layer.MayDependOn)}]");

        ArchitectureViolations.Of(rule).ShouldBeEmpty(rule.Description);
    }

    [TestCaseSource(nameof(AcyclicLayers))]
    public void ShouldBeFreeOfCycles_WhenLayerNamespacesAreSliced(OnionLayer layer)
    {
        var rule = NamespaceCycleRule(layer);

        ArchitectureViolations.Of(rule).ShouldBeEmpty(rule.Description);
    }

    [TestCaseSource(nameof(CyclicLayers))]
    public void ShouldStillHaveACycle_WhenLayerIsOnTheKnownCycleList(OnionLayer layer)
    {
        var rule = NamespaceCycleRule(layer);

        ArchitectureViolations.Of(rule).ShouldNotBeEmpty(
            $"{layer} is now free of namespace cycles. Remove it from SolutionArchitecture.LayersWithKnownNamespaceCycles so the cycle rule guards it.");
    }

    private static IArchRule NamespaceCycleRule(OnionLayer layer)
    {
        return SliceRuleDefinition.Slices()
            .Matching($"{layer.RootNamespace}.(**)")
            .Should().BeFreeOfCycles();
    }

    private static bool HasKnownCycles(OnionLayer layer)
    {
        return SolutionArchitecture.LayersWithKnownNamespaceCycles.Contains(layer.Name);
    }
}
