# Dashboard Recent Activity

The dashboard's recent-work-orders query returns the requested number of work orders, newest activity first. Activity is based on the latest available created, assigned, or completed date for each work order; it does not use a separate audit history.

To find the top results, the query reads up to that number of work orders for each of the three dates, ordered newest first and then by work-order number. It combines those results, removes duplicate work orders, ranks each by its latest date, breaks ties by work-order number, and takes the requested count. The count must be between 1 and 50.

When event dates are equal for one work order, the activity label uses this precedence: Completed, then Assigned, then Created.
