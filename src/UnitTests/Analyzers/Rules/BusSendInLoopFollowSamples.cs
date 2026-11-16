namespace ClearMeasure.Bootcamp.UnitTests.Analyzers.Rules;

/// <summary>
/// Snippets for same-type helper follow and non-reaching follow cases under <c>CM0001</c>.
/// </summary>
public static class BusSendInLoopFollowSamples
{
    private static string BusStubs => BusSendInLoopSamples.BusStubs;

    /// <summary>Loop awaits a block-bodied same-type helper that awaits Bus.Send.</summary>
    public static string BlockHelperAwaitsBus => BusStubs + """
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

    /// <summary>Loop awaits an expression-bodied same-type helper that awaits Bus.Send.</summary>
    public static string ExpressionHelperAwaitsBus => BusStubs + """
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

    /// <summary>Loop awaits a local function that awaits Bus.Send.</summary>
    public static string LocalFunctionAwaitsBus => BusStubs + """
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

    /// <summary>Select lambda awaits a same-type helper that awaits Bus.Send.</summary>
    public static string SelectHelperAwaitsBus => BusStubs + """
        public class H {
          public IBus Bus { get; set; } = null!;
          public async Task Run(int[] ids) {
            var loads = ids.Select(async id => await Load(id));
          }
          private async Task Load(int id) => await Bus.Send(new TeamsPageQuery());
        }
        """;

    /// <summary>Helper does not call Bus.Send.</summary>
    public static string HelperWithNoBus => BusStubs + """
        public class H {
          public async Task Run(int[] ids) {
            foreach (var id in ids) {
              await Load(id);
            }
          }
          private async Task Load(int id) => await System.Threading.Tasks.Task.Delay(id);
        }
        """;

    /// <summary>Helper lives on another type that uses Bus.</summary>
    public static string HelperOnOtherType => BusStubs + """
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

    /// <summary>Two levels of same-type helpers before Bus.Send (follow depth exceeded).</summary>
    public static string TwoLevelsBeforeBus => BusStubs + """
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

    /// <summary>Nested local function inside helper holds the Bus.Send.</summary>
    public static string NestedLocalHoldsBus => BusStubs + """
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

    /// <summary>Helper returns a Bus.Send Task without awaiting.</summary>
    public static string HelperReturnsSend => BusStubs + """
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

    /// <summary>Helper that awaits Bus.Send is called outside a loop.</summary>
    public static string HelperOutsideLoop => BusStubs + """
        public class H {
          public IBus Bus { get; set; } = null!;
          public async Task Run() {
            await Load();
          }
          private async Task Load() => await Bus.Send(new TeamsPageQuery());
        }
        """;

    /// <summary>Bus.Send is reached only through a delegate.</summary>
    public static string ThroughDelegate => BusStubs + """
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
}
