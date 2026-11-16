namespace ClearMeasure.Bootcamp.UnitTests.Analyzers.Rules;

/// <summary>
/// C# snippets that exercise <c>CM0005</c> (<c>DbContextInUiAnalyzer</c>).
/// </summary>
public static class DbContextInUiSamples
{
    /// <summary>UI page declaring DbContext-shaped members.</summary>
    public static string UiDeclaresDbContextMembers => """
        public class DbContext {}
        public class DataContext : DbContext {}
        public class UnitOfWork : DataContext {}
        public class Page {
          public DbContext Database { get; set; } = null!;
          public DataContext Store { get; set; } = null!;
          public UnitOfWork Unit { get; set; } = null!;
          public System.Threading.Tasks.Task<DbContext> Open() => throw null!;
        }
        """;

    /// <summary>Simple DbContext member for assembly-name matching.</summary>
    public static string SimpleDbContextMember => """
        public class DbContext {}
        public class Page {
          public DbContext Database { get; set; } = null!;
        }
        """;

    /// <summary>DataAccess repository with a DataContext member.</summary>
    public static string RepositoryOutsideUi => """
        public class DbContext {}
        public class DataContext : DbContext {}
        public class Repository {
          public DataContext Store { get; set; } = null!;
        }
        """;
}
