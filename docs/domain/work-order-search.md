# Work Order Search

Work Order Search keeps every filter in the URL query string of `workorder/search` (`WorkOrderSearchUrlState`), and default values are omitted so an unfiltered search has no query string. Each filter below is one sentence, and set filters narrow the same result set together.

Creator keeps work orders created by the selected employee, and a blank creator matches every creator.

Assignee keeps work orders assigned to the selected employee, and a blank assignee matches every assignee.

Status keeps work orders in the selected status, and a blank status matches every status.

Overdue only keeps open work orders (Draft, Assigned, or In Progress) whose due date is before today on the church calendar, and a work order with no due date is not overdue.

Priority keeps work orders at the selected priority (Low, Medium, or High), and a blank priority matches every priority.

Building keeps work orders whose building equals the selected value after trimming.

Room keeps work orders whose room number equals the entered value after trimming, compared without regard to case.

Open only keeps work orders in Draft, Assigned, or In Progress.

Completed within N days keeps Complete work orders whose completion falls within the last N days, and the URL accepts a whole number of days from 1 through 3650.

Free text keeps work orders whose number, title, or room contains the text, or whose creator or assignee first or last name contains it, ignoring case.

Assigned to me keeps work orders assigned to the signed-in user by setting the assignee to that user, and the URL stores the flag instead of an assignee name.

Due this week keeps open work orders whose due date falls from today through the next six days on the church calendar.

Waiting on a vendor keeps In Progress work orders that have a waiting-on-vendor time.

No building keeps work orders whose building is missing or blank when no specific building is selected, and a specific building takes precedence over this flag.

No room keeps work orders whose room is missing or blank when no specific room is entered, and a specific room takes precedence over this flag.
