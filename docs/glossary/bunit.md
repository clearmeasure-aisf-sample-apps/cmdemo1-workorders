# bUnit

**bUnit** is the library this application's unit tests use to render Blazor components in memory. [`UnitTests.csproj`](../../src/UnitTests/UnitTests.csproj) references package `bunit` 2.9.0, and page tests such as [`WorkOrderSearchTests`](../../src/UnitTests/UI.Shared/Pages/WorkOrderSearchTests.cs) import `Bunit` and construct a `BunitContext`. [Lab 05](../../labs/lab-05-bunit-component-testing.md) describes these in-memory Blazor component tests, and `CLAUDE.md` lists them as the UI component tests.
