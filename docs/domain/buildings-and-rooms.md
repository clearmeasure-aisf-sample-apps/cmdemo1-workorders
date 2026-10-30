# Buildings and Rooms

`Building` and `RoomNumber` are optional work-order location fields. `Building` names the building where work takes place; `RoomNumber` identifies the room. Work-order searches can filter by either value.

Searches trim the filter and stored value before matching. A null, empty, or whitespace-only building or room is treated as no building or no room, respectively. The building-room summary applies the same trimming and database collation when grouping values.

## Building-room summary

The summary returns one entry per building, with one entry for each room that has at least one work order. A missing building or room appears as an empty name. Each room includes:

- **Open:** Draft, Assigned, or In Progress work orders.
- **Overdue:** Open work orders with a due date before the church's current date.
- **Completed in the last 30 days:** Work orders completed during that period.
- **Total:** All work orders in the room, regardless of status.

The summary uses the same predicates as Work Order Search and its summary cards, so counts for a building and room match a search for those values.
