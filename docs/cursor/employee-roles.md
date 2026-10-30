# Employee roles

An employee receives work-order permissions from assigned roles. A role has two independent flags:

- **CanCreateWorkOrder** — the role allows the employee to create work orders.
- **CanFulfillWorkOrder** — the role allows the employee to fulfill work orders.

`Employee.CanCreateWorkOrder()` is true when any assigned role has `CanCreateWorkOrder`. `Employee.CanFulfillWorkOrder()` is true when any assigned role has `CanFulfillWorkOrder`. The checks are independent, so one role can grant creation while another grants fulfillment. When no assigned role grants a permission, the employee does not have that permission.
