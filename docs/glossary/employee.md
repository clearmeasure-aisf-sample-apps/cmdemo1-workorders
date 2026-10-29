# Employee

An **Employee** is the person record in this application (`Employee` in [`src/Core/Model/Employee.cs`](../../src/Core/Model/Employee.cs)): user name, first name, last name, and email address, an optional middle name, a preferred language that defaults to `en-US`, and a set of roles. `CanCreateWorkOrder()` is true when any assigned role has `CanCreateWorkOrder`, and `CanFulfillWorkOrder()` is true when any assigned role has `CanFulfillWorkOrder` ([Employees and Roles](../domain/employees-and-roles.md), [Employees](../employees.md)).
