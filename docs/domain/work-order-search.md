# Work Order Search

Work Order Search filters the results by creator, assignee, status, priority, and building. The page also has switches for “Assigned to me,” overdue work orders, and work orders waiting on a vendor. The URL query model additionally supports room, missing building or room, open work orders, work completed within a number of days, and free-text search across work-order number, title, room, and creator or assignee names. A specific building or room takes precedence over its corresponding “missing” filter.

Without a selected sort, results are newest first by creation date, with work-order number as the tie-breaker. Results can instead be sorted ascending or descending by work-order number, title, status, priority, assignee, room, or due date. Work orders without a due date remain last in either due-date direction.

The table shows 25 results per page. Its page number is kept in the URL; changing filters or sort resets the page to 1. The board view shows all matching results, grouped into columns by status, rather than using table pagination.

Filters, sort and direction, page, and the board view are encoded in the search URL, so the search can be bookmarked or shared. Default values are omitted; the default table view and an unfiltered search therefore need no query string.
