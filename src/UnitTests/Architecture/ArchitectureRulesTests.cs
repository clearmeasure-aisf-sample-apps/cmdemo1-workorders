using ClearMeasure.Bootcamp.UnitTests.BuildGates;
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
    public void Namespace_WhenMasterIsScanned_MatchesBaselineAndSkipsGeneratedProtos()
    {
        var mismatches = Master.Value.NamespaceMismatches;
        mismatches.ShouldNotContain("src/UI/Server/Generated/Protos/Workorders.cs");
        mismatches.ShouldNotContain("src/UI/Server/Generated/Protos/WorkordersGrpc.cs");
        ShouldMatchBaseline(mismatches, "namespace-folder-mismatches.txt");
    }

    [Test]
    public void WorkOrderBoolExpressionMembers_WhenDeclaredOutsideCore_AreAbsent()
    {
        var violations = WorkOrderBoolExpressionsOutsideCore(RepositoryCheckout.Current);

        violations.ShouldBeEmpty(
            "Expression<Func<WorkOrder, bool>> predicates belong in Core WorkOrderSpecifications. Found: " +
            string.Join(", ", violations));
    }

    [Test]
    public void WorkOrderBoolExpressionMembers_WhenSampleDeclaresPredicate_ReportsTheMember()
    {
        ArchitectureRules.WorkOrderBoolExpressionMembers("""
            using System;
            using System.Linq.Expressions;
            using ClearMeasure.Bootcamp.Core.Model;

            namespace Sample;

            public static class Filters
            {
                public static Expression<Func<WorkOrder, bool>> IsOpen() => wo => true;
            }
            """).ShouldBe(["IsOpen"]);
    }

    [Test]
    public void ReferencesDbContext_WhenSampleUsesDbContext_IsTrue()
    {
        ArchitectureRules.ReferencesDbContext("""
            using Microsoft.EntityFrameworkCore;
            namespace Sample;
            public class Page {
              public void Run(System.IServiceProvider sp) {
                var db = sp.GetService(typeof(DbContext));
              }
            }
            """).ShouldBeTrue();
    }

    [Test]
    public void ReferencesDbContext_WhenSampleHasNoPersistence_IsFalse()
    {
        ArchitectureRules.ReferencesDbContext("""
            namespace Sample;
            public class Page { public string Title { get; set; } = ""; }
            """).ShouldBeFalse();
    }

    [Test]
    public void UiServerDbContextReferences_WhenMasterIsScanned_OnlyAllowListedCompositionFiles()
    {
        var violations = UiServerDbContextOutsideAllowList(RepositoryCheckout.Current);
        violations.ShouldBeEmpty(
            "UI.Server may reference DbContext only in composition files. Found: " +
            string.Join(", ", violations));
    }

    [Test]
    public void ConstructorsCallingCreateScope_WhenSampleConstructorCallsIt_ReportsTheType()
    {
        ArchitectureRules.ConstructorsCallingCreateScope("""
            namespace Sample;
            public class Job {
              public Job(System.IServiceProvider sp) {
                var scope = sp.CreateScope();
              }
            }
            """).ShouldBe(["Job"]);
    }

    [Test]
    public void ConstructorsCallingCreateScope_WhenCreateScopeIsInAMethod_IsEmpty()
    {
        ArchitectureRules.ConstructorsCallingCreateScope("""
            namespace Sample;
            public class Job {
              public Job() { }
              public void Run(System.IServiceProvider sp) {
                using var scope = sp.CreateScope();
              }
            }
            """).ShouldBeEmpty();
    }

    [Test]
    public void ConstructorsCallingCreateScope_WhenMasterProductionSourcesAreScanned_IsEmpty()
    {
        var violations = CreateScopeInConstructorViolations(RepositoryCheckout.Current);
        violations.ShouldBeEmpty(
            "CreateScope() must not run in a constructor. Found: " + string.Join(", ", violations));
    }

    private static void ShouldMatchBaseline(IReadOnlyList<string> actual, string baselineFile)
    {
        var comparison = SolutionArchitectureScan.ReadBaseline(RepositoryCheckout.Current, baselineFile).Compare(actual);
        comparison.IsClean.ShouldBeTrue(comparison.Report(baselineFile));
    }

    private static readonly HashSet<string> UiServerDbContextAllowList = new(StringComparer.OrdinalIgnoreCase)
    {
        "src/UI/Server/UIServiceRegistry.cs",
        "src/UI/Server/TestingDatabaseStartupFilter.cs",
    };

    private static IReadOnlyList<string> WorkOrderBoolExpressionsOutsideCore(RepositoryCheckout checkout)
    {
        var violations = new List<string>();
        foreach (var sourceFile in checkout.SourceFiles(".cs"))
        {
            var relative = checkout.Relative(sourceFile);
            if (IsTestOrCorePath(relative))
            {
                continue;
            }

            foreach (var member in ArchitectureRules.WorkOrderBoolExpressionMembers(File.ReadAllText(sourceFile)))
            {
                violations.Add($"{relative}:{member}");
            }
        }

        return violations;
    }

    private static IReadOnlyList<string> UiServerDbContextOutsideAllowList(RepositoryCheckout checkout)
    {
        var violations = new List<string>();
        foreach (var sourceFile in checkout.SourceFiles(".cs"))
        {
            var relative = checkout.Relative(sourceFile);
            if (!relative.StartsWith("src/UI/Server/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (UiServerDbContextAllowList.Contains(relative))
            {
                continue;
            }

            if (ArchitectureRules.ReferencesDbContext(File.ReadAllText(sourceFile)))
            {
                violations.Add(relative);
            }
        }

        return violations;
    }

    private static IReadOnlyList<string> CreateScopeInConstructorViolations(RepositoryCheckout checkout)
    {
        var violations = new List<string>();
        foreach (var sourceFile in checkout.SourceFiles(".cs"))
        {
            var relative = checkout.Relative(sourceFile);
            if (IsTestPath(relative))
            {
                continue;
            }

            foreach (var ctor in ArchitectureRules.ConstructorsCallingCreateScope(File.ReadAllText(sourceFile)))
            {
                violations.Add($"{relative}:{ctor}");
            }
        }

        return violations;
    }

    private static bool IsTestOrCorePath(string relative) =>
        relative.StartsWith("src/Core/", StringComparison.OrdinalIgnoreCase) ||
        IsTestPath(relative);

    private static bool IsTestPath(string relative) =>
        relative.Contains("/UnitTests/", StringComparison.OrdinalIgnoreCase) ||
        relative.Contains("/IntegrationTests/", StringComparison.OrdinalIgnoreCase) ||
        relative.Contains("/AcceptanceTests/", StringComparison.OrdinalIgnoreCase);

    private static SolutionArchitectureScan.Result ScanMaster()
    {
        return SolutionArchitectureScan.Scan(RepositoryCheckout.Current);
    }
}
