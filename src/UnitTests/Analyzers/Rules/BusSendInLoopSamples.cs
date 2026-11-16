namespace ClearMeasure.Bootcamp.UnitTests.Analyzers.Rules;

/// <summary>
/// C# snippets that exercise <c>CM0001</c> (<c>BusSendInLoopAnalyzer</c>).
/// </summary>
public static class BusSendInLoopSamples
{
    /// <summary>Shared IBus and query stubs prepended to most snippets.</summary>
    public const string BusStubs = """
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

    /// <summary>foreach awaits Bus.Send per employee.</summary>
    public static string TeamsForeachAwaitsBusSend => BusStubs + """
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

    /// <summary>Single Send outside any loop.</summary>
    public static string SingleSendOutsideLoop => BusStubs + """
        public class TeamsAndVendors {
          public IBus Bus { get; set; } = null!;
          public async Task OnInitializedAsync() {
            var page = await Bus.Send(new TeamsPageQuery());
          }
        }
        """;

    /// <summary>foreach awaits a field named bus.</summary>
    public static string ForeachAwaitsFieldNamedBus => BusStubs + """
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

    /// <summary>Select lambda awaits Bus.Send.</summary>
    public static string SelectLambdaAwaitsBusSend => BusStubs + """
        public class TeamsAndVendors {
          public IBus Bus { get; set; } = null!;
          public async Task OnInitializedAsync() {
            var assignableEmployees = new List<Employee>();
            var loads = assignableEmployees.Select(async employee =>
              await Bus.Send(new EmployeeWorkLoadQuery(employee.UserName)));
          }
        }
        """;

    /// <summary>Send only enumerates the foreach source.</summary>
    public static string SendOnlyEnumeratesForeach => BusStubs + """
        public class TeamsAndVendors {
          public IBus Bus { get; set; } = null!;
          public async Task OnInitializedAsync() {
            foreach (var employee in await Bus.Send(new EmployeeListQuery())) {
            }
          }
        }
        """;

    /// <summary>for-loop condition awaits Bus.Send.</summary>
    public static string SendInForCondition => BusStubs + """
        public class Poll {
          public IBus Bus { get; set; } = null!;
          public async Task Run() {
            for (var attempt = 0; await Bus.Send(new TeamsPageQuery()) != null && attempt < 1; attempt++) {
            }
          }
        }
        """;

    /// <summary>while condition awaits Bus.Send.</summary>
    public static string SendInWhileCondition => BusStubs + """
        public class Poll {
          public IBus Bus { get; set; } = null!;
          public async Task Run() {
            while (await Bus.Send(new TeamsPageQuery()) != null) {
              break;
            }
          }
        }
        """;

    /// <summary>do-while condition awaits Bus.Send.</summary>
    public static string SendInDoWhileCondition => BusStubs + """
        public class Poll {
          public IBus Bus { get; set; } = null!;
          public async Task Run() {
            do {
            } while (await Bus.Send(new TeamsPageQuery()) != null);
          }
        }
        """;

    /// <summary>for-loop incrementor awaits Bus.Send.</summary>
    public static string SendInForIncrementor => BusStubs + """
        public class Poll {
          public IBus Bus { get; set; } = null!;
          public async Task Run() {
            for (var count = 0; count < 1; count = (await Bus.Send(new EmployeeListQuery())).Count) {
            }
          }
        }
        """;

    /// <summary>Local Send methods inside foreach (not IBus).</summary>
    public static string LocalSendInsideForeach => """
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

    /// <summary>for body awaits Bus.Send.</summary>
    public static string SendInsideForBody => BusStubs + """
        public class Poll {
          public IBus Bus { get; set; } = null!;
          public async Task Run() {
            for (var attempt = 0; attempt < 2; attempt++) {
              await Bus.Send(new TeamsPageQuery());
            }
          }
        }
        """;

    /// <summary>while body awaits Bus.Send.</summary>
    public static string SendInsideWhileBody => BusStubs + """
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

    /// <summary>do-while body awaits Bus.Send.</summary>
    public static string SendInsideDoWhileBody => BusStubs + """
        public class Poll {
          public IBus Bus { get; set; } = null!;
          public async Task Run() {
            do {
              await Bus.Send(new TeamsPageQuery());
            } while (false);
          }
        }
        """;

    /// <summary>ConfigureAwait on Bus.Send inside foreach.</summary>
    public static string ConfigureAwaitSendInsideForeach => BusStubs + """
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

    /// <summary>HttpClient.SendAsync inside foreach (not IBus).</summary>
    public static string HttpClientSendAsyncInsideForeach => """
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

    /// <summary>Unrelated type named Send inside foreach.</summary>
    public static string UnrelatedSendInsideForeach => """
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

    /// <summary>Concrete type implementing IBus used inside foreach.</summary>
    public static string ConcreteBusImplementsIBus => BusStubs + """
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

    /// <summary>ISender receiver not named Bus.</summary>
    public static string SenderReceiverNotNamedBus => """
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
}
