using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class CoreCoberturaPresenceTests
{
    [Test]
    public void HasProductionCoreHits_WhenCorePackageHasHits_ReturnsTrue()
    {
        CoreCoberturaPresence.HasProductionCoreHits(CoberturaMother.CorePackageWithHits()).ShouldBeTrue();
    }

    [Test]
    public void HasProductionCoreHits_WhenCoreFileHasHits_ReturnsTrue()
    {
        CoreCoberturaPresence.HasProductionCoreHits(CoberturaMother.CoreSourcePathWithHits()).ShouldBeTrue();
    }

    [Test]
    public void HasProductionCoreHits_WhenOnlyUnitTestsCorePaths_ReturnsFalse()
    {
        CoreCoberturaPresence.HasProductionCoreHits(CoberturaMother.UnitTestsCoreFolderOnly()).ShouldBeFalse();
    }

    [Test]
    public void HasProductionCoreHits_WhenCorePresentButZeroHits_ReturnsFalse()
    {
        CoreCoberturaPresence.HasProductionCoreHits(CoberturaMother.CorePackageWithoutHits()).ShouldBeFalse();
    }

    [Test]
    public void IsProductionCoreFilename_WhenWindowsSrcCorePath_ReturnsTrue()
    {
        CoreCoberturaPresence.IsProductionCoreFilename(
                @"D:\repo\src\Core\Model\Employee.cs")
            .ShouldBeTrue();
    }

    [Test]
    public void IsProductionCoreFilename_WhenCoverletRelativeCorePath_ReturnsTrue()
    {
        CoreCoberturaPresence.IsProductionCoreFilename(@"Core\Model\Employee.cs")
            .ShouldBeTrue();
    }

    [Test]
    public void IsProductionCoreFilename_WhenUnitTestsCorePath_ReturnsFalse()
    {
        CoreCoberturaPresence.IsProductionCoreFilename(
                @"D:\repo\src\UnitTests\Core\Model\EmployeeTests.cs")
            .ShouldBeFalse();
    }

    [Test]
    public void CoverletRunSettings_WhenRead_IncludesBootcampAndExcludesTests()
    {
        var xml = RepositoryCheckout.Current.ReadText("coverlet.runsettings");

        xml.ShouldContain("<Include>[ClearMeasure.Bootcamp.*]*,[Worker]*,[ChurchBulletin.ServiceDefaults]*</Include>");
        xml.ShouldContain("UnitTests");
        xml.ShouldContain("IntegrationTests");
        xml.ShouldContain("AcceptanceTests");
        xml.ShouldContain("cobertura");
    }

    [Test]
    public void BuildScript_WhenRead_PassesCoverletRunSettingsToUnitAndIntegration()
    {
        var source = RepositoryCheckout.Current.ReadText("build.ps1");

        source.ShouldContain("coverlet.runsettings");
        source.ShouldContain("--settings:");
        source.ShouldContain("coverletRunSettings");
    }

    [Test]
    public void AssertCoreCoberturaScript_WhenRead_FailsWhenCoreMissing()
    {
        var source = RepositoryCheckout.Current.ReadText("scripts/crap/assert-core-cobertura.ps1");

        source.ShouldContain("ClearMeasure.Bootcamp.Core");
        source.ShouldContain("exit 1");
    }

    [Test]
    public void CoreProject_WhenRead_EnablesCopyLocalLockFileAssemblies()
    {
        RepositoryCheckout.Current.ReadText("src/Core/Core.csproj").ShouldContain("CopyLocalLockFileAssemblies");
    }

    [Test]
    public void UnitTestsProject_WhenRead_CopiesCoreCoverletResolutionAssemblies()
    {
        var source = RepositoryCheckout.Current.ReadText("src/UnitTests/UnitTests.csproj");
        source.ShouldContain("CopyCoreCoverletResolutionAssemblies");
        source.ShouldContain("HealthChecks.Abstractions");
    }
}
