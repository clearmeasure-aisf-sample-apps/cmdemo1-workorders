
[assembly: Parallelizable(ParallelScope.Fixtures)]
// Match AcceptanceTests.runsettings NumberOfTestWorkers (4 on a 4-core GitHub runner).
// Override at runtime via runsettings; scripts/acceptance/Write-AcceptanceRunSettings.ps1
// can rewrite that value from Environment.ProcessorCount.
[assembly: LevelOfParallelism(4)]
