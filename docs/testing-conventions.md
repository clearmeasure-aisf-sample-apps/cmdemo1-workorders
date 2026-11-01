# Testing Conventions

## NUnit and Shouldly

Use NUnit for test fixtures and test cases, with Shouldly for assertions. Avoid FluentAssertions and NUnit's `Assert.That` style.

```csharp
[Test]
public void GetWorkOrder_WithValidId_ReturnsWorkOrder()
{
    var result = GetWorkOrder(validId);

    result.ShouldBe(expectedWorkOrder);
}
```

## Test doubles

Prefix test doubles with `Stub`, not `Mock`. For example, name a test double `StubWorkOrderRepository`.

## Arrange, Act, Assert

Structure each test in Arrange, Act, Assert order. Keep the sections clear through the code rather than adding section comments.

## Test names

Name tests to describe the method or behavior, scenario, and expected result: `[MethodName]_[Scenario]_[ExpectedResult]`. Prefix names with `Should` or `When` where it reads naturally.

Examples: `GetWorkOrder_WithValidId_ReturnsWorkOrder` and `ShouldReturnBadRequest_WhenFileMissing`.
