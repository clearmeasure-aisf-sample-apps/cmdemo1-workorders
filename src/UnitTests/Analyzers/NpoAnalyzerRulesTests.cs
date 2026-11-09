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
        string source = BusStubs + """
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
        string source = BusStubs + """
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
        string source = BusStubs + """
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
        string source = BusStubs + """
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
        string source = BusStubs + """
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
        string forCondition = BusStubs + """
            public class Poll {
              public IBus Bus { get; set; } = null!;
              public async Task Run() {
                for (var attempt = 0; await Bus.Send(new TeamsPageQuery()) != null && attempt < 1; attempt++) {
                }
              }
            }
            """;
        string whileCondition = BusStubs + """
            public class Poll {
              public IBus Bus { get; set; } = null!;
              public async Task Run() {
                while (await Bus.Send(new TeamsPageQuery()) != null) {
                  break;
                }
              }
            }
            """;
        string doCondition = BusStubs + """
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
        string source = BusStubs + """
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
        string source = """
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
        string forLoop = BusStubs + """
            public class Poll {
              public IBus Bus { get; set; } = null!;
              public async Task Run() {
                for (var attempt = 0; attempt < 2; attempt++) {
                  await Bus.Send(new TeamsPageQuery());
                }
              }
            }
            """;
        string whileLoop = BusStubs + """
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
        string doLoop = BusStubs + """
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
        string source = BusStubs + """
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
        string source = """
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
        string source = """
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
        string source = BusStubs + """
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
        string source = """
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
    public async Task ShouldReportCm0002AsWarning_When_QueryableMaterializerIsInsideForeach()
    {
        string source = """
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

        var hits = await Cm0002Async(source);
        hits.Length.ShouldBe(4);
        hits.ShouldAllBe(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning);
    }

    [Test]
    public async Task ShouldReportCm0002_When_ReceiverImplementsIQueryable()
    {
        string source = """
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

        var hits = await Cm0002Async(source);
        hits.ShouldHaveSingleItem().Severity.ShouldBe(DiagnosticSeverity.Warning);
    }

    [Test]
    public async Task ShouldReportNothing_When_InMemoryMaterializerIsInsideForeach()
    {
        string[] sources = new[]
        {
            """
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
            """,
            """
            using System.Collections.Generic;
            using System.Linq;
            public class H {
              public int[][] Build(IReadOnlyList<int> rows) =>
                new[] { 1, 2 }.Select(status => rows.Where(row => row == status).ToArray()).ToArray();
            }
            """
        };

        foreach (string? source in sources)
        {
            (await Cm0002Async(source)).ShouldBeEmpty();
        }
    }

    [Test]
    public async Task ShouldReportCm0001_When_LoopAwaitsSameTypeHelperThatAwaitsBus()
    {
        string blockHelper = BusStubs + """
            public class H {
              public IBus Bus { get; set; } = null!;
              public async Task Run(int[] ids) {
                foreach (var id in ids) {
                  await this.Load(id);
                }
              }
              private async Task Load(int id) {
                await Bus.Send(new TeamsPageQuery());
                await Bus.Send(new EmployeeListQuery());
              }
            }
            """;
        string expressionHelper = BusStubs + """
            public class H {
              public IBus Bus { get; set; } = null!;
              public async Task Run(int[] ids) {
                foreach (var id in ids) {
                  await Load(id);
                }
              }
              private async Task Load(int id) => await Bus.Send(new TeamsPageQuery());
            }
            """;
        string localFunction = BusStubs + """
            public class H {
              public IBus Bus { get; set; } = null!;
              public async Task Run(int[] ids) {
                async Task Load(int id) {
                  await Bus.Send(new TeamsPageQuery()).ConfigureAwait(false);
                }
                foreach (var id in ids) {
                  await Load(id);
                }
              }
            }
            """;
        string selectHelper = BusStubs + """
            public class H {
              public IBus Bus { get; set; } = null!;
              public async Task Run(int[] ids) {
                var loads = ids.Select(async id => await Load(id));
              }
              private async Task Load(int id) => await Bus.Send(new TeamsPageQuery());
            }
            """;

        (await Cm0001Async(blockHelper)).ShouldHaveSingleItem().Severity.ShouldBe(DiagnosticSeverity.Error);
        (await Cm0001Async(expressionHelper)).ShouldHaveSingleItem();
        (await Cm0001Async(localFunction)).ShouldHaveSingleItem();
        (await Cm0001Async(selectHelper)).ShouldHaveSingleItem();
    }

    [Test]
    public async Task ShouldReportNothing_When_HelperFollowDoesNotReachABusSend()
    {
        string noBus = BusStubs + """
            public class H {
              public async Task Run(int[] ids) {
                foreach (var id in ids) {
                  await Load(id);
                }
              }
              private async Task Load(int id) => await System.Threading.Tasks.Task.Delay(id);
            }
            """;
        string otherType = BusStubs + """
            public class Other {
              public IBus Bus { get; set; } = null!;
              public async Task Load(int id) => await Bus.Send(new TeamsPageQuery());
            }
            public class H {
              private readonly Other other = new Other();
              public async Task Run(int[] ids) {
                foreach (var id in ids) {
                  await other.Load(id);
                }
              }
            }
            """;
        string twoLevels = BusStubs + """
            public class H {
              public IBus Bus { get; set; } = null!;
              public async Task Run(int[] ids) {
                foreach (var id in ids) {
                  await Outer(id);
                }
              }
              private async Task Outer(int id) => await Inner(id);
              private async Task Inner(int id) => await Bus.Send(new TeamsPageQuery());
            }
            """;
        string nestedLocal = BusStubs + """
            public class H {
              public IBus Bus { get; set; } = null!;
              public async Task Run(int[] ids) {
                foreach (var id in ids) {
                  await Load(id);
                }
              }
              private async Task Load(int id) {
                async Task Inner() {
                  await Bus.Send(new TeamsPageQuery());
                }
                await Inner();
              }
            }
            """;
        string returnsSend = BusStubs + """
            public class H {
              public IBus Bus { get; set; } = null!;
              public async Task Run(int[] ids) {
                foreach (var id in ids) {
                  await Load(id);
                }
              }
              private Task Load(int id) => Bus.Send(new TeamsPageQuery());
            }
            """;
        string outsideLoop = BusStubs + """
            public class H {
              public IBus Bus { get; set; } = null!;
              public async Task Run() {
                await Load();
              }
              private async Task Load() => await Bus.Send(new TeamsPageQuery());
            }
            """;
        string throughDelegate = BusStubs + """
            public class H {
              public IBus Bus { get; set; } = null!;
              public async Task Run(int[] ids) {
                System.Func<int, System.Threading.Tasks.Task> call = Load;
                foreach (var id in ids) {
                  await call(id);
                }
              }
              private async Task Load(int id) => await Bus.Send(new TeamsPageQuery());
            }
            """;

        (await Cm0001Async(noBus)).ShouldBeEmpty();
        (await Cm0001Async(otherType)).ShouldBeEmpty();
        (await Cm0001Async(twoLevels)).ShouldBeEmpty();
        (await Cm0001Async(nestedLocal)).ShouldBeEmpty();
        (await Cm0001Async(returnsSend)).ShouldBeEmpty();
        (await Cm0001Async(outsideLoop)).ShouldBeEmpty();
        (await Cm0001Async(throughDelegate)).ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldReportCm0004_When_ToListPrecedesWhere()
    {
        string source = """
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
        string source = """
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
        string materializer = """
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
        string http = """
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
    public void ShouldTreatCm0001AndCm0005AsErrors_And_Cm0002AsWarning()
    {
        string text = File.ReadAllText(EditorConfigPath());
        text.ShouldContain("dotnet_diagnostic.CM0001.severity = error");
        text.ShouldContain("dotnet_diagnostic.CM0005.severity = error");
        text.ShouldContain("dotnet_diagnostic.CM0002.severity = warning");
        text.ShouldContain("dotnet_diagnostic.CM0003.severity = none");
        text.ShouldContain("dotnet_diagnostic.CM0004.severity = none");
    }

    private static async Task<Diagnostic[]> Cm0001Async(string source)
    {
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new BusSendInLoopAnalyzer(), source);
        return diagnostics.Where(diagnostic => diagnostic.Id == "CM0001").ToArray();
    }

    private static async Task<Diagnostic[]> Cm0002Async(string source)
    {
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new EfMaterializerInLoopAnalyzer(), source);
        return diagnostics.Where(diagnostic => diagnostic.Id == "CM0002").ToArray();
    }

    private static string EditorConfigPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            string solution = Path.Combine(dir.FullName, "src", "ChurchBulletin.sln");
            if (File.Exists(solution))
            {
                return Path.Combine(dir.FullName, "src", ".editorconfig");
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate src/.editorconfig");
    }
}
