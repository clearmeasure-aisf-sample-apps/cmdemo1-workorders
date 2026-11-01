# Note 53

When a completed-within window is set, work-order search accepts it only from 1 through 3650 days. WorkOrderSpecificationQueryValidator.CompletedWithinDaysMax is 3650, and the validator applies InclusiveBetween(1, CompletedWithinDaysMax) to CompletedWithinDays only when that value is set. An unset value is not checked.
