namespace ClearMeasure.Bootcamp.UnitTests.AcceptanceShards;

/// <summary>One acceptance class and its measured duration in seconds.</summary>
// ReSharper disable once NotAccessedPositionalProperty.Global -- Tests feeds the shard balance gate and packer
public sealed record DurationClass(string Name, double Seconds, int Tests = 1);
