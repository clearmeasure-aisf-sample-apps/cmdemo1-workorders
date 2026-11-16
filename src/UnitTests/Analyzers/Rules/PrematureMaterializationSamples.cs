namespace ClearMeasure.Bootcamp.UnitTests.Analyzers.Rules;

/// <summary>
/// C# snippets that exercise <c>CM0004</c> (<c>PrematureMaterializationAnalyzer</c>).
/// </summary>
public static class PrematureMaterializationSamples
{
    /// <summary>ToList precedes Where on the same chain.</summary>
    public static string ToListPrecedesWhere => """
        using System.Collections.Generic;
        using System.Linq;
        public class H {
          public void Run(IEnumerable<int> values) {
            var bad = values.ToList().Where(x => x > 0).ToList();
          }
        }
        """;

    /// <summary>Where.ToList without a preceding ToList (outside a loop).</summary>
    public static string WhereToListOutsideLoop => """
        using System.Collections.Generic;
        using System.Linq;
        using System.Threading.Tasks;
        public static class Ext {
          public static Task<List<T>> ToListAsync<T>(this IEnumerable<T> src) => Task.FromResult(src.ToList());
        }
        public class H {
          public async Task Run(IEnumerable<int> values) {
            var rows = await values.ToListAsync();
            var kept = values.Where(x => x > 0).ToList();
          }
        }
        """;
}
