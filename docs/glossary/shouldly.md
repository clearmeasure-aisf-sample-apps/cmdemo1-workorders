# Shouldly

**Shouldly** is the assertion library this application's NUnit unit and integration tests use. [`UnitTests.csproj`](../../src/UnitTests/UnitTests.csproj) and [`IntegrationTests.csproj`](../../src/IntegrationTests/IntegrationTests.csproj) reference package `Shouldly` 4.3.0, and those projects' tests import `Shouldly`. [Testing conventions](../testing-conventions.md) says to use NUnit with Shouldly and to avoid FluentAssertions and NUnit's `Assert.That` style; `CLAUDE.md` and [the testing cheat sheet](../conventions/testing-cheat-sheet.md) record the same rule.
