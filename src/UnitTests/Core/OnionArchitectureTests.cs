using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core;

/// <summary>
/// Guards the onion rule: Core depends on no other project, domain types live in Core and persistence
/// types live in DataAccess.
/// </summary>
[TestFixture]
public class OnionArchitectureTests
{
    [Test]
    public void CoreShouldNotReferenceAnyBootcampAssembly()
    {
        var references = typeof(Team).Assembly.GetReferencedAssemblies()
            .Select(name => name.Name ?? "")
            .Where(name => name.StartsWith("ClearMeasure.Bootcamp.", StringComparison.Ordinal))
            .ToArray();

        references.ShouldBeEmpty();
    }

    [Test]
    public void CoreProjectShouldHaveNoProjectReference()
    {
        var project = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Core", "Core.csproj"));

        project.ShouldNotContain("<ProjectReference");
    }

    [TestCase(typeof(Team))]
    [TestCase(typeof(Vendor))]
    [TestCase(typeof(TeamsQuery))]
    [TestCase(typeof(VendorsQuery))]
    [TestCase(typeof(CreateTeamCommand))]
    [TestCase(typeof(CreateVendorCommand))]
    public void DomainTypesShouldLiveInCore(Type type)
    {
        type.Assembly.ShouldBe(typeof(IRemotableRequest).Assembly);
    }

    [TestCase(typeof(TeamMap))]
    [TestCase(typeof(VendorMap))]
    [TestCase(typeof(TeamQueryHandler))]
    [TestCase(typeof(VendorQueryHandler))]
    public void PersistenceTypesShouldLiveInDataAccess(Type type)
    {
        type.Assembly.ShouldBe(typeof(DataContext).Assembly);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "src", "ChurchBulletin.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root from test directory.");
    }
}
