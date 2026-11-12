using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class AcceptanceShardBalanceTests
{
    [Test]
    public void Evaluate_WhenTheCommittedLedgerMatchesSource_HasNoProblems()
    {
        var checkout = RepositoryCheckout.Current;
        var assignment = AcceptanceShardAssignment.Load(checkout);

        var problems = AcceptanceShardBalance.Evaluate(assignment, AcceptanceTestCatalog.Read(checkout));

        problems.ShouldBeEmpty();
        assignment.ShardCount.ShouldBe(6);
        assignment.Workers.ShouldBe(AcceptanceShardBalance.Workers);
    }

    [Test]
    public void Evaluate_WhenAClassIsUnassigned_NamesThatClass()
    {
        var problems = AcceptanceShardBalance.Evaluate(Pair(), [new DiscoveredAcceptanceClass("Alpha", 1), new DiscoveredAcceptanceClass("Beta", 1), new DiscoveredAcceptanceClass("Gamma", 1)]);

        string.Join('\n', problems).ShouldContain("Gamma");
        string.Join('\n', problems).ShouldContain("not assigned to a shard");
    }

    [Test]
    public void Evaluate_WhenTestCountPilesOntoOneShard_Fails()
    {
        var discovered = new[]
        {
            new DiscoveredAcceptanceClass("Alpha", 10),
            new DiscoveredAcceptanceClass("Beta", 1),
        };

        var problems = AcceptanceShardBalance.Evaluate(Pair(), discovered);

        string.Join('\n', problems).ShouldContain("test count shard 1");
    }

    [Test]
    public void Evaluate_WhenDurationPilesOntoOneShard_Fails()
    {
        var problems = AcceptanceShardBalance.Evaluate(HeavyShard(), PairDiscovered());

        string.Join('\n', problems).ShouldContain("x64 duration shard 1");
    }

    [Test]
    public void Evaluate_WhenTheDurationCeilingIsRaised_Fails()
    {
        var problems = AcceptanceShardBalance.Evaluate(Pair(maxDurationRatio: 2), PairDiscovered());

        string.Join('\n', problems).ShouldContain("maxDurationRatio");
    }

    [Test]
    public void BuildScript_WhenRead_BuildsShardFiltersFromTheExplicitLedger()
    {
        var script = RepositoryCheckout.Current.ReadText("build.ps1");

        script.ShouldContain("acceptance-shards.json");
        script.ShouldContain("Assert-AcceptanceShardBalance");
        script.ShouldNotContain("FullyQualifiedName!~");
    }

    [Test]
    public void CoverageCheck_WhenRead_AssertsTheShardBalance()
    {
        var job = WorkflowFile.Read(RepositoryCheckout.Current, "build.yml")
            .Job("acceptance-tests-coverage-check")
            .Text;

        job.ShouldContain("Assert-AcceptanceShardBalance");
    }

    private static AcceptanceShardAssignment Pair(double maxDurationRatio = 1.25)
    {
        return new AcceptanceShardAssignment
        {
            ShardCount = 2,
            Workers = AcceptanceShardBalance.Workers,
            MaxDurationRatio = maxDurationRatio,
            MaxCountRatio = 1.45,
            DefaultNewTestSeconds = 20,
            Classes =
            [
                Class("Alpha", shard: 1, seconds: 40),
                Class("Beta", shard: 2, seconds: 40),
            ],
        };
    }

    private static AcceptanceShardAssignment HeavyShard()
    {
        return new AcceptanceShardAssignment
        {
            ShardCount = 2,
            Workers = AcceptanceShardBalance.Workers,
            MaxDurationRatio = 1.25,
            MaxCountRatio = 1.45,
            DefaultNewTestSeconds = 20,
            Classes =
            [
                Class("Alpha", shard: 1, seconds: 400),
                Class("Beta", shard: 2, seconds: 40),
            ],
        };
    }

    private static DiscoveredAcceptanceClass[] PairDiscovered()
    {
        return [new DiscoveredAcceptanceClass("Alpha", 1), new DiscoveredAcceptanceClass("Beta", 1)];
    }

    private static AcceptanceShardClass Class(string name, int shard, double seconds)
    {
        return new AcceptanceShardClass
        {
            Name = name,
            Shard = shard,
            Tests = 1,
            X64Seconds = seconds,
            ArmSeconds = seconds,
            LongestX64Seconds = seconds,
            LongestArmSeconds = seconds,
        };
    }
}
