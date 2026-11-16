namespace ClearMeasure.Bootcamp.UnitTests.Analyzers.Rules;

/// <summary>
/// C# snippets that exercise <c>CM0002</c> (<c>EfMaterializerInLoopAnalyzer</c>).
/// </summary>
public static class EfMaterializerInLoopSamples
{
    /// <summary>IQueryable materializers inside foreach.</summary>
    public static string QueryableMaterializerInsideForeach => """
        using System.Collections.Generic;
        using System.Linq;
        using System.Threading.Tasks;
        public static class Ext {
          public static Task<List<T>> ToListAsync<T>(this IQueryable<T> src) => Task.FromResult(new List<T>());
        }
        public class H {
          public async Task Run(IQueryable<int> rows, int[] ids) {
            foreach (var id in ids) {
              var list = await rows.ToListAsync();
              var first = rows.FirstOrDefault();
              var copy = rows.ToArray();
              var maybe = rows?.ToArray();
            }
          }
        }
        """;

    /// <summary>Concrete type implementing IQueryable materialized inside foreach.</summary>
    public static string ReceiverImplementsIQueryable => """
        using System;
        using System.Collections;
        using System.Collections.Generic;
        using System.Linq;
        using System.Linq.Expressions;
        using System.Threading.Tasks;
        public sealed class Query<T> : IQueryable<T> {
          public Type ElementType => typeof(T);
          public Expression Expression => System.Linq.Expressions.Expression.Constant(this);
          public IQueryProvider Provider => null!;
          public IEnumerator<T> GetEnumerator() => new List<T>().GetEnumerator();
          IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        public static class Ext {
          public static Task<List<T>> ToListAsync<T>(this IQueryable<T> src) => Task.FromResult(new List<T>());
        }
        public class H {
          public async Task Run(int[] ids) {
            var rows = new Query<int>();
            foreach (var id in ids) {
              var list = await rows.ToListAsync();
            }
          }
        }
        """;

    /// <summary>In-memory IEnumerable materializers inside foreach.</summary>
    public static string InMemoryMaterializerInsideForeach => """
        using System.Collections.Generic;
        using System.Linq;
        using System.Threading.Tasks;
        public static class Ext {
          public static Task<List<T>> ToListAsync<T>(this IEnumerable<T> src) => Task.FromResult(src.ToList());
        }
        public class H {
          public async Task Run(List<int> rows, int[] ids) {
            foreach (var id in ids) {
              var list = await rows.ToListAsync();
              var first = rows.FirstOrDefault();
              var copy = ids.ToArray();
              var maybe = rows?.ToArray();
              ToList();
            }
          }
          private static void ToList() {}
        }
        """;

    /// <summary>Select projecting in-memory arrays (no IQueryable loop materializer).</summary>
    public static string SelectProjectsInMemoryArrays => """
        using System.Collections.Generic;
        using System.Linq;
        public class H {
          public int[][] Build(IReadOnlyList<int> rows) =>
            new[] { 1, 2 }.Select(status => rows.Where(row => row == status).ToArray()).ToArray();
        }
        """;

    /// <summary>Materializer and Where.ToList run outside any loop.</summary>
    public static string MaterializerOutsideLoop => """
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
