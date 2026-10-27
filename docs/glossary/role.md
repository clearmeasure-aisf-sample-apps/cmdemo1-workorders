# Role

A **role** is the permission record an employee can be assigned (`Role` in [`src/Core/Model/Role.cs`](../../src/Core/Model/Role.cs)). It has a name and two independent permissions, `CanCreateWorkOrder` and `CanFulfillWorkOrder`. An employee holds a set of roles and has a permission when any assigned role grants it ([Employees and Roles](../domain/employees-and-roles.md), [Roles](../roles.md)).
