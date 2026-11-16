# Exp 8: Native AOT test-runner pilot

32 Core test classes from `src/UnitTests/Core` (221 tests) converted mechanically from NUnit to TUnit, which runs on
Microsoft.Testing.Platform and supports Native AOT. Conversion: remove `[TestFixture]`, `[TestCase(...)]` becomes
`[Test]` plus `[Arguments(...)]`, Shouldly stays. Files with `[SetUp]`, `[TestCaseSource]`, or shared test helpers were
left out. This folder is outside `src` and the solution, so the build gates do not see it.

    docker run --rm -v "$PWD:/src" -w /src/experiments/aot-test-pilot/CoreTests.Aot mcr.microsoft.com/dotnet/sdk:11.0-aot \
      dotnet publish -c Release -r linux-x64 -o /src/experiments/aot-test-pilot/out-aot
    ./out-aot/CoreTests.Aot --results-directory /tmp/tr          # Native AOT
    dotnet publish -c Release -p:Jit=true ...                       # JIT build of the same tests

Measured 2026-11-15 (aspnet:11.0, --cpus 2, 6 runs each): AOT 106-200 ms per full run, JIT 794-1010 ms; 221/221 pass
on both; a deliberately failing test makes the AOT run exit 2 with the Shouldly message.
