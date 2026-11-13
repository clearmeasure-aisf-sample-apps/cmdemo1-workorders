using ClearMeasure.Bootcamp.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ClearMeasure.Bootcamp.UnitTests.Architecture;

/// <summary>
/// Conventions that already hold across the compiled solution, enforced so they keep holding.
/// </summary>
[TestFixture]
public class CodeConventionTests
{
    private const string InfrastructureNamespaces =
        @"^(Microsoft\.EntityFrameworkCore|Microsoft\.AspNetCore|Microsoft\.Data\.SqlClient|System\.Data\.SqlClient|Microsoft\.Data\.Sqlite|NServiceBus|Azure|Lamar|OpenAI|Microsoft\.Extensions\.AI)(\..+)?$";

    [Test]
    public void ShouldStartWithI_WhenTypeIsASolutionInterface()
    {
        var rule = Interfaces().That().ResideInNamespaceMatching(SolutionArchitecture.SolutionNamespacePattern)
            .Should().HaveNameStartingWith("I");

        ArchitectureViolations.Of(rule).ShouldBeEmpty(rule.Description);
    }

    [Test]
    public void ShouldNotDependOnInfrastructureFrameworks_WhenTypeIsInCore()
    {
        var core = SolutionArchitecture.Layers.Single(layer => layer.Name == "Core");

        var rule = Types().That().ResideInNamespaceMatching(core.NamespacePattern)
            .Should().NotDependOnAny(Types(true).That().ResideInNamespaceMatching(InfrastructureNamespaces))
            .Because("persistence, web, messaging and AI SDKs belong to the outer layers");

        ArchitectureViolations.Of(rule).ShouldBeEmpty(rule.Description);
    }

    [Test]
    public void ShouldLiveInStateCommandsAndEndWithCommand_WhenClassIsAConcreteStateCommand()
    {
        var rule = Classes().That().ImplementInterface(typeof(IStateCommand)).And().AreNotAbstract()
            .Should().ResideInNamespace("ClearMeasure.Bootcamp.Core.Model.StateCommands")
            .AndShould().HaveNameEndingWith("Command");

        ArchitectureViolations.Of(rule).ShouldBeEmpty(rule.Description);
    }

    [Test]
    public void ShouldLiveInControllersAndEndWithController_WhenClassIsAConcreteController()
    {
        var rule = Classes().That().AreAssignableTo(typeof(ControllerBase)).And().AreNotAbstract()
            .Should().ResideInNamespaceMatching(@"\.Controllers$")
            .AndShould().HaveNameEndingWith("Controller");

        ArchitectureViolations.Of(rule).ShouldBeEmpty(rule.Description);
    }
}
