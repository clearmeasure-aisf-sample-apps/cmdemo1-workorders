using ClearMeasure.NpoAnalyzers;
using Microsoft.CodeAnalysis;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Analyzers;

[TestFixture]
public class NpoAnalyzerRulesTests
{
    private const string BusStubs = """
        using System.Collections.Generic;
        using System.Linq;
        using System.Threading.Tasks;

        public interface IRequest<T> {}
        public interface IBus {
          Task<T> Send<T>(IRequest<T> request);
          Task<object?> Send(object request);
          Task Publish(object notification);
        }
        public record EmployeeWorkLoadQuery(string UserName) : IRequest<object>;
        public record TeamsPageQuery() : IRequest<object>;
        public record EmployeeListQuery() : IRequest<List<Employee>>;
        public record WorkOrderByNumberQuery(string Number) : IRequest<object>;
        public class Employee { public string UserName { get; set; } = ""; }
        """;

    [Test]
    public async Task ShouldReportCm0001AsError_When_TeamsForeachAwaitsBusSend()
    {
        var source = BusStubs + """
            public class TeamsAndVendors {
              public IBus Bus { get; set; } = null!;
              public async Task OnInitializedAsync() {
                var assignableEmployees = new List<Employee>();
                foreach (var employee in assignableEmployees) {
                  var load = await Bus.Send(new EmployeeWorkLoadQuery(employee.UserName));
                }
              }
            }
            """;

        var hits = await Cm0001Async(source);
        hits.Length.ShouldBe(1);
        hits[0].Id.ShouldBe("CM0001");
        hits[0].Severity.ShouldBe(DiagnosticSeverity.Error);
    }

    [Test]
    public async Task ShouldReportNothing_When_SingleSendIsOutsideALoop()
    {
        var source = BusStubs + """
            public class TeamsAndVendors {
              public IBus Bus { get; set; } = null!;
              public async Task OnInitializedAsync() {
                var page = await Bus.Send(new TeamsPageQuery());
              }
            }
            """;

        var hits = await Cm0001Async(source);
        hits.ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldReportCm0001_When_ForeachAwaitsFieldNamedBus()
    {
        var source = BusStubs + """
            public class WorkOrderBulkActionService {
              private readonly IBus bus;
              public WorkOrderBulkActionService(IBus bus) => this.bus = bus;
              public async Task ApplyAsync(IEnumerable<string> numbers) {
                foreach (var number in numbers) {
                  var order = await bus.Send(new WorkOrderByNumberQuery(number));
                }
              }
            }
            """;

        var hits = await Cm0001Async(source);
        hits.ShouldHaveSingleItem().Severity.ShouldBe(DiagnosticSeverity.Error);
    }

    [Test]
    public async Task ShouldReportCm0001_When_SelectLambdaAwaitsBusSend()
    {
        var source = BusStubs + """
            public class TeamsAndVendors {
              public IBus Bus { get; set; } = null!;
              public async Task OnInitializedAsync() {
                var assignableEmployees = new List<Employee>();
                var loads = assignableEmployees.Select(async employee =>
                  await Bus.Send(new EmployeeWorkLoadQuery(employee.UserName)));
              }
            }
            """;

        var hits = await Cm0001Async(source);
        hits.ShouldHaveSingleItem().Id.ShouldBe("CM0001");
    }

    [Test]
    public async Task ShouldReportNothing_When_SendOnlyEnumeratesTheForeach()
    {
        var source = BusStubs + """
            public class TeamsAndVendors {
              public IBus Bus { get; set; } = null!;
              public async Task OnInitializedAsync() {
                foreach (var employee in await Bus.Send(new EmployeeListQuery())) {
                }
              }
            }
            """;

        var hits = await Cm0001Async(source);
        hits.ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldReportCm0001_When_SendIsInTheLoopCondition()
    {
        var forCondition = BusStubs + """
            public class Poll {
              public IBus Bus { get; set; } = null!;
              public async Task Run() {
                for (var attempt = 0; await Bus.Send(new TeamsPageQuery()) != null && attempt < 1; attempt++) {
                }
              }
            }
            """;
        var whileCondition = BusStubs + """
            public class Poll {
              public IBus Bus { get; set; } = null!;
              public async Task Run() {
                while (await Bus.Send(new TeamsPageQuery()) != null) {
                  break;
                }
              }
            }
            """;
        var doCondition = BusStubs + """
            public class Poll {
              public IBus Bus { get; set; } = null!;
              public async Task Run() {
                do {
                } while (await Bus.Send(new TeamsPageQuery()) != null);
              }
            }
            """;

        (await Cm0001Async(forCondition)).ShouldHaveSingleItem();
        (await Cm0001Async(whileCondition)).ShouldHaveSingleItem();
        (await Cm0001Async(doCondition)).ShouldHaveSingleItem();
    }

    [Test]
    public async Task ShouldReportCm0001_When_SendIsInTheForIncrementor()
    {
        var source = BusStubs + """
            public class Poll {
              public IBus Bus { get; set; } = null!;
              public async Task Run() {
                for (var count = 0; count < 1; count = (await Bus.Send(new EmployeeListQuery())).Count) {
                }
              }
            }
            """;

        (await Cm0001Async(source)).ShouldHaveSingleItem();
    }

    [Test]
    public async Task ShouldReportNothing_When_LocalSendIsInsideForeach()
    {
        var source = """
            using System.Threading.Tasks;
            public class H {
              public async Task Run(int[] ids) {
                foreach (var id in ids) {
                  await Send(id);
                  await Send<int>(id);
                }
              }
              private static Task Send(int id) => Task.CompletedTask;
              private static Task Send<T>(T id) => Task.CompletedTask;
            }
            """;

        (await Cm0001Async(source)).ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldReportCm0001_When_SendIsInsideForWhileOrDo()
    {
        var forLoop = BusStubs + """
            public class Poll {
              public IBus Bus { get; set; } = null!;
              public async Task Run() {
                for (var attempt = 0; attempt < 2; attempt++) {
                  await Bus.Send(new TeamsPageQuery());
                }
              }
            }
            """;
        var whileLoop = BusStubs + """
            public class Poll {
              public IBus Bus { get; set; } = null!;
              public async Task Run() {
                var pending = true;
                while (pending) {
                  await Bus.Send(new TeamsPageQuery());
                  pending = false;
                }
              }
            }
            """;
        var doLoop = BusStubs + """
            public class Poll {
              public IBus Bus { get; set; } = null!;
              public async Task Run() {
                do {
                  await Bus.Send(new TeamsPageQuery());
                } while (false);
              }
            }
            """;

        (await Cm0001Async(forLoop)).ShouldHaveSingleItem();
        (await Cm0001Async(whileLoop)).ShouldHaveSingleItem();
        (await Cm0001Async(doLoop)).ShouldHaveSingleItem();
    }

    [Test]
    public async Task ShouldReportCm0001_When_ConfigureAwaitSendIsInsideForeach()
    {
        var source = BusStubs + """
            public class TeamsAndVendors {
              public IBus Bus { get; set; } = null!;
              public async Task OnInitializedAsync() {
                var employees = new List<Employee>();
                foreach (var employee in employees) {
                  await Bus.Send(new EmployeeWorkLoadQuery(employee.UserName)).ConfigureAwait(false);
                }
              }
            }
            """;

        (await Cm0001Async(source)).ShouldHaveSingleItem();
    }

    [Test]
    public async Task ShouldReportNothing_When_HttpClientSendAsyncIsInsideForeach()
    {
        var source = """
            using System.Net.Http;
            using System.Threading.Tasks;
            public class H {
              private readonly HttpClient _http = new HttpClient();
              public async Task Run(string[] urls) {
                foreach (var url in urls) {
                  await _http.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
                }
              }
            }
            """;

        (await Cm0001Async(source)).ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldReportNothing_When_UnrelatedSendIsInsideForeach()
    {
        var source = """
            using System.Threading.Tasks;
            public class Mailer {
              public Task Send(object message) => Task.CompletedTask;
            }
            public class H {
              private readonly Mailer mailer = new Mailer();
              public async Task Run(int[] ids) {
                foreach (var id in ids) {
                  await mailer.Send(id);
                }
              }
            }
            """;

        (await Cm0001Async(source)).ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldReportCm0001_When_ConcreteBusImplementsIBus()
    {
        var source = BusStubs + """
            public class DirectBus : IBus {
              public Task<T> Send<T>(IRequest<T> request) => Task.FromResult(default(T)!);
              public Task<object?> Send(object request) => Task.FromResult<object?>(null);
              public Task Publish(object notification) => Task.CompletedTask;
            }
            public class H {
              private readonly DirectBus gateway = new DirectBus();
              public async Task Run(List<Employee> employees) {
                foreach (var employee in employees) {
                  await gateway.Send(new EmployeeWorkLoadQuery(employee.UserName));
                }
              }
            }
            """;

        (await Cm0001Async(source)).ShouldHaveSingleItem();
    }

    [Test]
    public async Task ShouldReportCm0001_When_ISenderReceiverIsNotNamedBus()
    {
        var source = """
            using System.Threading.Tasks;
            public interface ISender { Task Send(object request); }
            public class H {
              private readonly ISender gateway;
              public H(ISender gateway) => this.gateway = gateway;
              public async Task Run(int[] ids) {
                foreach (var id in ids) {
                  await gateway.Send(id);
                }
              }
            }
            """;

        (await Cm0001Async(source)).ShouldHaveSingleItem().Severity.ShouldBe(DiagnosticSeverity.Error);
    }

    [Test]
    public async Task ShouldReportCm0002_When_ToListAsyncIsInsideForeach()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;
            public static class Ext {
              public static Task<List<T>> ToListAsync<T>(this IEnumerable<T> src) => Task.FromResult(src.ToList());
            }
            public class H {
              public async Task Run(IEnumerable<int> ids, IEnumerable<int> db) {
                foreach (var id in ids) {
                  var rows = await db.Where(x => x == id).ToListAsync();
                }
              }
            }
            """;

        var hits = await AnalyzerTestHelper.GetDiagnosticsAsync(new EfMaterializerInLoopAnalyzer(), source);
        hits.ShouldContain(diagnostic => diagnostic.Id == "CM0002");
    }

    [Test]
    public async Task ShouldReportCm0004_When_ToListPrecedesWhere()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            public class H {
              public void Run(IEnumerable<int> values) {
                var bad = values.ToList().Where(x => x > 0).ToList();
              }
            }
            """;

        var hits = await AnalyzerTestHelper.GetDiagnosticsAsync(new PrematureMaterializationAnalyzer(), source);
        hits.ShouldContain(diagnostic => diagnostic.Id == "CM0004");
    }

    [Test]
    public async Task ShouldReportCm0003_When_HttpGetIsInsideForeach()
    {
        var source = """
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

        var hits = await AnalyzerTestHelper.GetDiagnosticsAsync(new HttpInLoopAnalyzer(), source);
        hits.ShouldContain(diagnostic => diagnostic.Id == "CM0003");
    }

    [Test]
    public async Task ShouldReportNothing_When_OptionalRulesRunOutsideALoop()
    {
        var materializer = """
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
        var http = """
            using System.Net.Http;
            using System.Threading.Tasks;
            public class H {
              private readonly HttpClient _http = new HttpClient();
              public async Task Run() {
                var body = await _http.GetStringAsync("https://example.test");
              }
            }
            """;

        var cm0002 = await AnalyzerTestHelper.GetDiagnosticsAsync(new EfMaterializerInLoopAnalyzer(), materializer);
        cm0002.Where(diagnostic => diagnostic.Id == "CM0002").ShouldBeEmpty();
        var cm0004 = await AnalyzerTestHelper.GetDiagnosticsAsync(new PrematureMaterializationAnalyzer(), materializer);
        cm0004.Where(diagnostic => diagnostic.Id == "CM0004").ShouldBeEmpty();
        var cm0003 = await AnalyzerTestHelper.GetDiagnosticsAsync(new HttpInLoopAnalyzer(), http);
        cm0003.Where(diagnostic => diagnostic.Id == "CM0003").ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldReportCm0005AsError_When_UiAssemblyDeclaresDbContextMember()
    {
        const string source = """
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

        var hits = await AnalyzerTestHelper.GetDiagnosticsAsync(
            new DbContextInUiAnalyzer(),
            source,
            "ClearMeasure.Bootcamp.UI.Server");
        hits.ShouldContain(diagnostic => diagnostic.Id == "CM0005" && diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Test]
    public async Task ShouldReportCm0005_When_AssemblyNameEndsWithUi()
    {
        const string source = """
            public class DbContext {}
            public class Page {
              public DbContext Database { get; set; } = null!;
            }
            """;

        var hits = await AnalyzerTestHelper.GetDiagnosticsAsync(
            new DbContextInUiAnalyzer(),
            source,
            "Contoso.UI");
        hits.ShouldContain(diagnostic => diagnostic.Id == "CM0005");
    }

    [Test]
    public async Task ShouldReportNothing_When_DbContextIsOutsideUi()
    {
        const string source = """
            public class DbContext {}
            public class DataContext : DbContext {}
            public class Repository {
              public DataContext Store { get; set; } = null!;
            }
            """;

        var hits = await AnalyzerTestHelper.GetDiagnosticsAsync(
            new DbContextInUiAnalyzer(),
            source,
            "ClearMeasure.Bootcamp.DataAccess");
        hits.Where(diagnostic => diagnostic.Id == "CM0005").ShouldBeEmpty();
    }

    [Test]
    public void EditorConfig_ShouldTreatCm0001AndCm0005AsErrors_And_DisableTheRest()
    {
        var text = File.ReadAllText(EditorConfigPath());
        text.ShouldContain("dotnet_diagnostic.CM0001.severity = error");
        text.ShouldContain("dotnet_diagnostic.CM0005.severity = error");
        text.ShouldContain("dotnet_diagnostic.CM0002.severity = none");
        text.ShouldContain("dotnet_diagnostic.CM0003.severity = none");
        text.ShouldContain("dotnet_diagnostic.CM0004.severity = none");
    }

    private static async Task<Diagnostic[]> Cm0001Async(string source)
    {
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new BusSendInLoopAnalyzer(), source);
        return diagnostics.Where(diagnostic => diagnostic.Id == "CM0001").ToArray();
    }

    private static string EditorConfigPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var solution = Path.Combine(dir.FullName, "src", "ChurchBulletin.sln");
            if (File.Exists(solution))
            {
                return Path.Combine(dir.FullName, "src", ".editorconfig");
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate src/.editorconfig");
    }
}
