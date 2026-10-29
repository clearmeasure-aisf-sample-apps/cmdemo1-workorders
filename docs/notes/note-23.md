# Note 23

WorkOrderStatus.None is an empty sentinel: its code and key are empty strings, its friendly name is a single space, and its sort order is 0. GetAllItems returns Draft, Assigned, InProgress, Complete, and Cancelled, and does not include None.
