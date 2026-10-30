# Employees and Roles

An `Employee` has a username, first and last name, email address, and a set of assigned `Role` values. A role has a name and two independent permissions:

- **CanCreateWorkOrder** — Allows an employee assigned to the role to create work orders.
- **CanFulfillWorkOrder** — Allows an employee assigned to the role to fulfill work orders.

An employee has each permission when any assigned role grants it. The permissions are checked independently, so different roles can grant each permission. With no role granting a permission, the employee does not have that permission.
