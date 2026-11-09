namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// One-class Cobertura reports for the <see cref="CoreCoberturaPresence"/> fixture.
/// </summary>
internal static class CoberturaMother
{
    private const string CorePackage = "ClearMeasure.Bootcamp.Core";
    private const string Employee = "ClearMeasure.Bootcamp.Core.Model.Employee";

    public static string CorePackageWithHits()
    {
        return Report(CorePackage, Employee, "Model/Employee.cs", hits: 3);
    }

    public static string CoreSourcePathWithHits()
    {
        return Report(CorePackage, Employee, "src/Core/Model/Employee.cs", hits: 3);
    }

    public static string UnitTestsCoreFolderOnly()
    {
        return Report(
            "ClearMeasure.Bootcamp.UnitTests",
            "ClearMeasure.Bootcamp.UnitTests.Core.Model.EmployeeTests",
            "src/UnitTests/Core/Model/EmployeeTests.cs",
            hits: 5);
    }

    public static string CorePackageWithoutHits()
    {
        return Report(CorePackage, Employee, "Model/Employee.cs", hits: 0);
    }

    private static string Report(string package, string className, string filename, int hits)
    {
        return $"""
            <?xml version="1.0" encoding="utf-8"?>
            <coverage>
              <packages>
                <package name="{package}">
                  <classes>
                    <class name="{className}" filename="{filename}" line-rate="{(hits > 0 ? 1 : 0)}">
                      <lines>
                        <line number="10" hits="{hits}" />
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;
    }
}
