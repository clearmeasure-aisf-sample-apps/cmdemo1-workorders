# Role

A **role** is a named set of work-order permissions an employee can hold (`Role` in [`src/Core/Model/Role.cs`](../../src/Core/Model/Role.cs)). It has a name and two independent permissions, `CanCreateWorkOrder` and `CanFulfillWorkOrder`. An employee holds a set of roles and has a permission when any assigned role grants it ([Employees and Roles](../domain/employees-and-roles.md), [Roles](../roles.md)).
