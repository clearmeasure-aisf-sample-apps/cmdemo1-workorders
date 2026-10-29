# Work Order Numbers

Each work order has a unique Number. The database requires it and enforces uniqueness with the `IX_WorkOrder_Number` index.

The stored Number is unprefixed. In the work-order search table and board, and on the dashboard's recent-activity links, it is shown with a `WO-` prefix (for example, `WO-38173BA`). The work-order details page shows the stored Number.

Work orders are looked up by their stored Number. The details route is `/workorder/manage/{id}`; the page sends a `WorkOrderByNumberQuery`, whose handler matches the supplied number against the stored `Number`. The print page also loads a work order by Number.
