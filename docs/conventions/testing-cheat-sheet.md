# Testing Conventions Cheat Sheet

Use NUnit for test fixtures and cases, with Shouldly for assertions. Avoid FluentAssertions and NUnit's `Assert.That` style.

Name tests to describe the method or behavior, scenario, and expected result: `[MethodName]_[Scenario]_[ExpectedResult]`. Prefix names with `Should` or `When` where it reads naturally.

Structure tests in Arrange, Act, Assert order without section comments:

```csharp
[Test]
public void GetWorkOrder_WithValidId_ReturnsWorkOrder()
{
    var workOrder = CreateWorkOrder(validId);

    workOrder.Id.ShouldBe(validId);
}
```

Prefix test doubles with `Stub`, not `Mock`; for example, `StubWorkOrderRepository`.
