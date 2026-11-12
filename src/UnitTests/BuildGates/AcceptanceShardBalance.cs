namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// Fails when an acceptance class has no explicit shard, or when one shard's
/// test count or estimated runtime pulls ahead of the others.
/// Estimated runtime is the longer of the slowest test and the class-duration
/// total divided by the four NUnit workers.
/// </summary>
internal static class AcceptanceShardBalance
{
    public const int Workers = 4;

    private const double MaxDurationCeiling = 1.35;

    private const double MaxCountCeiling = 1.5;

    private const double MinimumNewTestSeconds = 10;

    /// <summary>Problems in <paramref name="assignment"/> given the fixtures in <paramref name="discovered"/>.</summary>
    public static IReadOnlyList<string> Evaluate(
        AcceptanceShardAssignment assignment,
        IReadOnlyList<DiscoveredAcceptanceClass> discovered)
    {
        var problems = new List<string>();
        problems.AddRange(Bounds(assignment));
        problems.AddRange(Membership(assignment, discovered));
        problems.AddRange(Collisions(assignment));
        problems.AddRange(Durations(assignment, discovered));
        problems.AddRange(Counts(assignment, discovered));
        return problems;
    }

    private static IEnumerable<string> Bounds(AcceptanceShardAssignment assignment)
    {
        if (assignment.Workers != Workers)
        {
            yield return $"workers must stay {Workers}, the acceptance runsettings worker count.";
        }

        if (assignment.MaxDurationRatio is < 1 or > MaxDurationCeiling)
        {
            yield return $"maxDurationRatio must stay between 1 and {MaxDurationCeiling}.";
        }

        if (assignment.MaxCountRatio is < 1 or > MaxCountCeiling)
        {
            yield return $"maxCountRatio must stay between 1 and {MaxCountCeiling}.";
        }

        if (assignment.DefaultNewTestSeconds < MinimumNewTestSeconds)
        {
            yield return $"defaultNewTestSeconds must be at least {MinimumNewTestSeconds}.";
        }
    }

    private static IEnumerable<string> Membership(
        AcceptanceShardAssignment assignment,
        IReadOnlyList<DiscoveredAcceptanceClass> discovered)
    {
        foreach (var name in Names(discovered).Except(Names(assignment.Classes)))
        {
            yield return $"Acceptance test class {name} is not assigned to a shard.";
        }

        foreach (var name in Names(assignment.Classes).Except(Names(discovered)))
        {
            yield return $"Assigned class {name} is not an acceptance test class.";
        }

        foreach (var name in DuplicateNames(assignment))
        {
            yield return $"Class {name} is assigned to more than one shard.";
        }

        foreach (var problem in ShardNumbers(assignment))
        {
            yield return problem;
        }
    }

    private static IEnumerable<string> ShardNumbers(AcceptanceShardAssignment assignment)
    {
        foreach (var assigned in assignment.Classes)
        {
            if (assigned.Shard < 1 || assigned.Shard > assignment.ShardCount)
            {
                yield return $"Class {assigned.Name} is assigned to shard {assigned.Shard}, outside 1..{assignment.ShardCount}.";
            }
        }

        for (var shard = 1; shard <= assignment.ShardCount; shard++)
        {
            if (assignment.Classes.All(item => item.Shard != shard))
            {
                yield return $"Shard {shard} has no acceptance tests.";
            }
        }
    }

    private static IEnumerable<string> Collisions(AcceptanceShardAssignment assignment)
    {
        var names = Names(assignment.Classes).ToArray();
        for (var left = 0; left < names.Length; left++)
        {
            for (var right = left + 1; right < names.Length; right++)
            {
                if (names[right].Contains(names[left], StringComparison.Ordinal))
                {
                    yield return $"Class name {names[left]} is a substring of {names[right]}, so the shard filter is ambiguous.";
                }
            }
        }
    }

    private static IEnumerable<string> Durations(
        AcceptanceShardAssignment assignment,
        IReadOnlyList<DiscoveredAcceptanceClass> discovered)
    {
        var counts = discovered.ToDictionary(item => item.Name, item => item.TestCount, StringComparer.Ordinal);
        return PlatformSpans(assignment, counts, arm: false)
            .Concat(PlatformSpans(assignment, counts, arm: true));
    }

    private static IEnumerable<string> PlatformSpans(
        AcceptanceShardAssignment assignment,
        Dictionary<string, int> counts,
        bool arm)
    {
        var label = arm ? "arm duration" : "x64 duration";
        var problem = Ratio(label, Spans(assignment, counts, arm), assignment.MaxDurationRatio);
        if (problem is not null)
        {
            yield return problem;
        }
    }

    private static IEnumerable<string> Counts(
        AcceptanceShardAssignment assignment,
        IReadOnlyList<DiscoveredAcceptanceClass> discovered)
    {
        var counts = discovered.ToDictionary(item => item.Name, item => item.TestCount, StringComparer.Ordinal);
        var share = Share(assignment, counts);
        var problem = Ratio("test count", share, assignment.MaxCountRatio);
        if (problem is not null)
        {
            yield return problem;
        }
    }

    private static double[] Spans(
        AcceptanceShardAssignment assignment,
        Dictionary<string, int> counts,
        bool arm)
    {
        var spans = new double[assignment.ShardCount];
        foreach (var group in assignment.Classes.GroupBy(item => item.Shard))
        {
            if (group.Key < 1 || group.Key > assignment.ShardCount)
            {
                continue;
            }

            spans[group.Key - 1] = Span(group.Select(item => Weight(assignment, counts, item, arm)));
        }

        return spans;
    }

    private static double[] Share(AcceptanceShardAssignment assignment, Dictionary<string, int> counts)
    {
        var share = new double[assignment.ShardCount];
        foreach (var assigned in assignment.Classes)
        {
            if (assigned.Shard < 1 || assigned.Shard > assignment.ShardCount)
            {
                continue;
            }

            if (counts.TryGetValue(assigned.Name, out var discovered))
            {
                share[assigned.Shard - 1] += discovered;
            }
        }

        return share;
    }

    private static ClassWeight Weight(
        AcceptanceShardAssignment assignment,
        Dictionary<string, int> counts,
        AcceptanceShardClass assigned,
        bool arm)
    {
        var recorded = arm ? assigned.ArmSeconds : assigned.X64Seconds;
        var longest = arm ? assigned.LongestArmSeconds : assigned.LongestX64Seconds;
        var discovered = counts.TryGetValue(assigned.Name, out var count) ? count : assigned.Tests;
        var seconds = Scale(recorded, assigned.Tests, discovered, assignment.DefaultNewTestSeconds);
        if (discovered > assigned.Tests)
        {
            longest = Math.Max(longest, PerTest(recorded, assigned.Tests, assignment.DefaultNewTestSeconds));
        }

        return new ClassWeight(seconds, longest);
    }

    private static double Scale(double recordedSeconds, int recordedTests, int discoveredTests, double fallback)
    {
        if (discoveredTests < recordedTests && recordedTests > 0)
        {
            return recordedSeconds * discoveredTests / recordedTests;
        }

        var extra = discoveredTests - Math.Max(recordedTests, 0);
        return recordedSeconds + (extra * PerTest(recordedSeconds, recordedTests, fallback));
    }

    private static double PerTest(double recordedSeconds, int recordedTests, double fallback)
    {
        if (recordedTests <= 0 || recordedSeconds < recordedTests)
        {
            return fallback;
        }

        return recordedSeconds / recordedTests;
    }

    private static double Span(IEnumerable<ClassWeight> weights)
    {
        var sum = 0.0;
        var longest = 0.0;
        foreach (var weight in weights)
        {
            sum += weight.Seconds;
            longest = Math.Max(longest, weight.Longest);
        }

        return Math.Max(longest, sum / Workers);
    }

    private static string? Ratio(string label, IReadOnlyList<double> values, double limit)
    {
        if (values.Count == 0 || values.Average() <= 0)
        {
            return $"{label} has no weight to balance.";
        }

        var mean = values.Average();
        var ratio = values.Max() / mean;
        if (ratio <= limit)
        {
            return null;
        }

        var shard = IndexOfMax(values) + 1;
        return $"{label} shard {shard} is {ratio:0.00}x the mean {mean:0.0}; limit is {limit:0.00}x.";
    }

    private static int IndexOfMax(IReadOnlyList<double> values)
    {
        var index = 0;
        for (var i = 1; i < values.Count; i++)
        {
            if (values[i] > values[index])
            {
                index = i;
            }
        }

        return index;
    }

    private static IEnumerable<string> Names(IReadOnlyList<DiscoveredAcceptanceClass> discovered)
    {
        return discovered.Select(item => item.Name).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
    }

    private static IEnumerable<string> Names(IReadOnlyList<AcceptanceShardClass> classes)
    {
        return classes.Select(item => item.Name).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
    }

    private static IEnumerable<string> DuplicateNames(AcceptanceShardAssignment assignment)
    {
        return assignment.Classes
            .GroupBy(item => item.Name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);
    }

    private readonly record struct ClassWeight(double Seconds, double Longest);
}
