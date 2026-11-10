using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

[TestFixture]
public class OwnTypeExtensionRuleTests
{
    private const string WorkOrder = "namespace ClearMeasure.Bootcamp.Core.Model; public class WorkOrder { }";

    [TestCase("public static bool IsLate(this WorkOrder order) => true;")]
    [TestCase("public static bool IsLate(this Core.Model.WorkOrder? order) => true;")]
    [TestCase("extension(WorkOrder order) { public bool IsLate => true; }")]
    public void ShouldFlagExtensionClass_WhenItExtendsOurOwnType(string member)
    {
        var sources = new SourceSetBuilder()
            .WithFile("src/Core/Model/WorkOrder.cs", WorkOrder)
            .WithFile("src/UI.Shared/WorkOrderDisplay.cs", $"public static class WorkOrderDisplay {{ {member} }}")
            .Build();

        var violations = SourceSetBuilder.KeysOf(new OwnTypeExtensionRule(), sources);

        violations.ShouldBe(["src/UI.Shared/WorkOrderDisplay.cs :: WorkOrderDisplay"]);
    }

    [TestCase("public static IServiceCollection AddWorkOrders(this IServiceCollection services) => services;")]
    [TestCase("public static IEnumerable<WorkOrder> Open(this IEnumerable<WorkOrder> orders) => orders;")]
    [TestCase("public static T Echo<T>(this T value) => value;")]
    public void ShouldPassExtensionClass_WhenItExtendsAFrameworkType(string member)
    {
        var sources = new SourceSetBuilder()
            .WithFile("src/Core/Model/WorkOrder.cs", WorkOrder)
            .WithFile("src/UI/Server/WorkOrderRegistration.cs", $"public static class WorkOrderRegistration {{ {member} }}")
            .Build();

        var violations = SourceSetBuilder.KeysOf(new OwnTypeExtensionRule(), sources);

        violations.ShouldBeEmpty();
    }
}
