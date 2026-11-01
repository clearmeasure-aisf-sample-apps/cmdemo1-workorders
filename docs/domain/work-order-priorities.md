# Work Order Priorities

Priority describes how urgently a work order should be handled:

| Priority | Meaning | Stored value |
| --- | --- | --- |
| Low | Can wait until the team has capacity. | `1` |
| Medium | Normal handling; the default for new work orders. | `2` |
| High | Handle before other work. | `3` |

The values are defined by `WorkOrderPriority` and stored in the required `WorkOrder.Priority` database column as a `tinyint`. New work orders default to Medium.

Work-order searches can filter by one priority. When a priority is supplied, only work orders with that priority match; when it is omitted, priority does not restrict the results.
