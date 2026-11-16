namespace ClearMeasure.Bootcamp.UnitTests.Analyzers.Rules;

/// <summary>
/// C# snippets that exercise <c>CM0003</c> (<c>HttpInLoopAnalyzer</c>).
/// </summary>
public static class HttpInLoopSamples
{
    /// <summary>HttpClient.GetStringAsync inside foreach.</summary>
    public static string HttpGetInsideForeach => """
        using System.Collections.Generic;
        using System.Net.Http;
        using System.Threading.Tasks;
        public class H {
          private readonly HttpClient _http = new HttpClient();
          public async Task Run(IEnumerable<string> urls) {
            foreach (var url in urls) {
              var body = await _http.GetStringAsync(url);
            }
          }
        }
        """;

    /// <summary>HttpClient.GetStringAsync outside any loop.</summary>
    public static string HttpGetOutsideLoop => """
        using System.Net.Http;
        using System.Threading.Tasks;
        public class H {
          private readonly HttpClient _http = new HttpClient();
          public async Task Run() {
            var body = await _http.GetStringAsync("https://example.test");
          }
        }
        """;
}
