using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Architecture;

[TestFixture]
public class ArchitectureRulesTests
{
    private const string DataAccessProject = """
        <Project Sdk="Microsoft.NET.Sdk">
          <ItemGroup>
            <ProjectReference Include="..\DataAccess\DataAccess.csproj" />
          </ItemGroup>
        </Project>
        """;

    private const string CorePackageOnly = """
        <Project Sdk="Microsoft.NET.Sdk">
          <ItemGroup>
            <ProjectReference Include="..\Core\Core.csproj" />
            <PackageReference Include="DataAccess" Version="1.0.0" />
          </ItemGroup>
        </Project>
        """;

    private static readonly Lazy<SolutionArchitectureScan.Result> Master = new(ScanMaster);

    [Test]
    public void CoreProjectReferenceViolations_WhenSampleReferencesAnotherProject_ReportsTheInclude()
    {
        var violations = ArchitectureRules.CoreProjectReferenceViolations("src/Core/Core.csproj", DataAccessProject);

        violations.ShouldBe(["../DataAccess/DataAccess.csproj"]);
    }

    [Test]
    public void CoreProjectReferenceViolations_WhenSampleIsNotCore_IsEmpty()
    {
        ArchitectureRules.CoreProjectReferenceViolations("src/DataAccess/DataAccess.csproj", CorePackageOnly)
            .ShouldBeEmpty();
    }

    [Test]
    public void CoreProjectReferenceViolations_WhenMasterIsScanned_IsEmpty()
    {
        Master.Value.CoreProjectReferences.ShouldBeEmpty();
    }

    [Test]
    public void UiProjectsReferencingDataAccess_WhenSharedUiSampleReferencesDataAccess_ReportsThatProject()
    {
        var violations = ArchitectureRules.UiProjectsReferencingDataAccess([
            ("src/UI.Shared/UI.Shared.csproj", DataAccessProject),
        ]);

        violations.ShouldBe(["src/UI.Shared/UI.Shared.csproj"]);
    }

    [Test]
    public void UiProjectsReferencingDataAccess_WhenClientReferencesOnlyCore_IsEmpty()
    {
        var violations = ArchitectureRules.UiProjectsReferencingDataAccess([
            ("src/UI/Client/UI.Client.csproj", CorePackageOnly),
        ]);

        violations.ShouldBeEmpty();
    }

    [Test]
    public void UiProjectsReferencingDataAccess_WhenWorkerReferencesDataAccess_IsEmpty()
    {
        var violations = ArchitectureRules.UiProjectsReferencingDataAccess([
            ("src/Worker/Worker.csproj", DataAccessProject),
        ]);

        violations.ShouldBeEmpty();
    }

    [Test]
    public void UiProjectsReferencingDataAccess_WhenMasterIsScanned_MatchesBaseline()
    {
        ShouldMatchBaseline(Master.Value.UiDataAccessReferences, "ui-dataaccess-references.txt");
    }

    [Test]
    public void AboveBaseline_WhenActualAddsAnEntry_ReturnsThatEntry()
    {
        var baseline = new HashSet<string>(StringComparer.Ordinal) { "src/UI/Server/UI.Server.csproj" };

        var extras = ArchitectureRules.AboveBaseline(
            ["src/UI/Server/UI.Server.csproj", "src/UI/Client/UI.Client.csproj"],
            baseline);

        extras.ShouldBe(["src/UI/Client/UI.Client.csproj"]);
    }

    [Test]
    public void AboveBaseline_WhenActualDropsAnEntry_IsEmpty()
    {
        var baseline = new HashSet<string>(StringComparer.Ordinal)
        {
            "src/UI/Server/UI.Server.csproj",
            "src/UI/Client/UI.Client.csproj",
        };

        ArchitectureRules.AboveBaseline(["src/UI/Server/UI.Server.csproj"], baseline).ShouldBeEmpty();
    }

    [Test]
    public void ParseBaseline_WhenCommentsAndBlankLines_KeepsPathsOnly()
    {
        var baseline = ArchitectureRules.ParseBaseline("""
            # UI.Server is the composition host.

            src/UI/Server/UI.Server.csproj
            """);

        baseline.Count.ShouldBe(1);
        baseline.Contains("src/UI/Server/UI.Server.csproj").ShouldBeTrue();
    }

    [Test]
    public void MisnamedHandlerTypeNames_WhenSampleDoesNotEndWithHandler_ReportsTheType()
    {
        ArchitectureRules.MisnamedHandlerTypeNames("""
            namespace Sample;

            public sealed class WorkOrderQuery : IRequestHandler<WorkOrderByNumberQuery, WorkOrder>
            {
            }
            """).ShouldBe(["WorkOrderQuery"]);
    }

    [Test]
    public void MisnamedHandlerTypeNames_WhenSampleEndsWithHandler_IsEmpty()
    {
        ArchitectureRules.MisnamedHandlerTypeNames("""
            namespace Sample;

            public sealed class WorkOrderQueryHandler : MediatR.IRequestHandler<WorkOrderByNumberQuery, WorkOrder>
            {
            }
            """).ShouldBeEmpty();
    }

    [Test]
    public void MisnamedHandlerTypeNames_WhenNotificationHandlerIsMisnamed_ReportsTheType()
    {
        ArchitectureRules.MisnamedHandlerTypeNames("""
            namespace Sample;

            file class Audit : INotificationHandler<UserLoggedInEvent>
            {
            }
            """).ShouldBe(["Audit"]);
    }

    [Test]
    public void MisnamedHandlerTypeNames_WhenNestedHandlerIsMisnamed_ReportsTheNestedType()
    {
        ArchitectureRules.MisnamedHandlerTypeNames("""
            namespace Sample;

            public sealed class Outer
            {
                private sealed class Inner : global::MediatR.IRequestHandler<Q, R>
                {
                }
            }
            """).ShouldBe(["Inner"]);
    }

    [Test]
    public void MisnamedHandlerTypeNames_WhenTypeOnlyMentionsTheInterface_IsEmpty()
    {
        ArchitectureRules.MisnamedHandlerTypeNames("""
            namespace Sample;

            public sealed class Notes
            {
                public string Explain() => "IRequestHandler";
            }
            """).ShouldBeEmpty();
    }

    [Test]
    public void MisnamedHandlerTypeNames_WhenMasterIsScanned_IsEmpty()
    {
        Master.Value.HandlerNamingViolations.ShouldBeEmpty();
    }

    [Test]
    public void HasMultiplePublicTypes_WhenSampleDeclaresTwo_IsTrue()
    {
        ArchitectureRules.HasMultiplePublicTypes("""
            namespace Sample.Console;

            public static class DatabaseRebuildSteps
            {
            }

            public readonly record struct DatabaseResult(bool Successful, string? ErrorMessage);
            """).ShouldBeTrue();
    }

    [Test]
    public void HasMultiplePublicTypes_WhenSampleDeclaresOnePlusInternal_IsFalse()
    {
        ArchitectureRules.HasMultiplePublicTypes("""
            namespace Sample.Models;

            public class WorkOrder
            {
            }

            internal class WorkOrderMapper
            {
            }
            """).ShouldBeFalse();
    }

    [Test]
    public void HasMultiplePublicTypes_WhenNestedPublicType_IsFalse()
    {
        ArchitectureRules.HasMultiplePublicTypes("""
            namespace Sample;

            public class Outer
            {
                public class Inner
                {
                }
            }
            """).ShouldBeFalse();
    }

    [Test]
    public void HasMultiplePublicTypes_WhenPartialsShareAName_IsFalse()
    {
        ArchitectureRules.HasMultiplePublicTypes("""
            namespace Sample;

            public partial class WorkOrder
            {
            }

            public partial class WorkOrder
            {
            }
            """).ShouldBeFalse();
    }

    [Test]
    public void PublicTypes_WhenMasterIsScanned_MatchesBaseline()
    {
        ShouldMatchBaseline(Master.Value.MultiplePublicTypeFiles, "multiple-public-types.txt");
    }

    [Test]
    public void NamespaceMatchesFolder_WhenSampleFolderDiffers_IsFalse()
    {
        ArchitectureRules.NamespaceMatchesFolder(
            """
            namespace Sample.Wrong;

            public class WorkOrder
            {
            }
            """,
            "Sample",
            ["Models"]).ShouldBeFalse();
    }

    [Test]
    public void NamespaceMatchesFolder_WhenSampleFolderMatches_IsTrue()
    {
        ArchitectureRules.NamespaceMatchesFolder(
            """
            namespace Sample.Models;

            public class WorkOrder
            {
            }
            """,
            "Sample",
            ["Models"]).ShouldBeTrue();
    }

    [Test]
    public void NamespaceMatchesFolder_WhenFileHasNoNamespace_IsTrue()
    {
        ArchitectureRules.NamespaceMatchesFolder("global using System;", "Sample", []).ShouldBeTrue();
    }

    [Test]
    public void RootNamespace_WhenSdkToken_ExpandsProjectName()
    {
        var xml = """
            <Project>
              <PropertyGroup>
                <RootNamespace>ClearMeasure.Bootcamp.$(MSBuildProjectName.Replace(" ", "_"))</RootNamespace>
              </PropertyGroup>
            </Project>
            """;

        ArchitectureRules.RootNamespace(xml, "UI.Shared").ShouldBe("ClearMeasure.Bootcamp.UI.Shared");
    }

    [Test]
    public void RootNamespace_WhenMissing_UsesProjectName()
    {
        ArchitectureRules.RootNamespace("<Project></Project>", "Worker").ShouldBe("Worker");
    }

    [Test]
    public void ExpectedNamespace_WhenFoldersArePresent_AppendsThem()
    {
        ArchitectureRules.ExpectedNamespace("ClearMeasure.Bootcamp.Core", ["Model", "StateCommands"])
            .ShouldBe("ClearMeasure.Bootcamp.Core.Model.StateCommands");
    }

    [Test]
    public void IsExcludedSource_WhenGeneratedOrBuildOutput_IsTrue()
    {
        SolutionArchitectureScan.IsExcludedSource("/repo/src/UI/Server/Generated/Protos/Workorders.cs").ShouldBeTrue();
        SolutionArchitectureScan.IsExcludedSource("/repo/src/UnitTests/bin/Debug/Foo.cs").ShouldBeTrue();
        SolutionArchitectureScan.IsExcludedSource("/repo/src/Core/obj/Debug/Foo.cs").ShouldBeTrue();
        SolutionArchitectureScan.IsExcludedSource("/repo/src/UI/Server/Foo.g.cs").ShouldBeTrue();
        SolutionArchitectureScan.IsExcludedSource("/repo/src/UI/Server/Foo.Designer.cs").ShouldBeTrue();
        SolutionArchitectureScan.IsExcludedSource("/repo/src/Core/Model/WorkOrder.cs").ShouldBeFalse();
    }

    [Test]
    public void Namespace_WhenMasterIsScanned_MatchesBaselineAndSkipsGeneratedProtos()
    {
        var mismatches = Master.Value.NamespaceMismatches;
        mismatches.ShouldNotContain("src/UI/Server/Generated/Protos/Workorders.cs");
        mismatches.ShouldNotContain("src/UI/Server/Generated/Protos/WorkordersGrpc.cs");
        ShouldMatchBaseline(mismatches, "namespace-folder-mismatches.txt");
    }

    private static void ShouldMatchBaseline(IReadOnlyList<string> actual, string baselineFile)
    {
        var baseline = SolutionArchitectureScan.ReadBaseline(RepoRoot(), baselineFile)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        actual.ToArray().ShouldBe(baseline);
    }

    private static SolutionArchitectureScan.Result ScanMaster()
    {
        return SolutionArchitectureScan.Scan(RepoRoot());
    }

    private static string RepoRoot()
    {
        return SolutionArchitectureScan.FindRepoRoot();
    }
}
