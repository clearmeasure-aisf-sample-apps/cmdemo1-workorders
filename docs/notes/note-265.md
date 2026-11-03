# Note 265

The Building filter's list of buildings comes from `WorkOrderBuildingsQuery` (`src/Core/Queries/WorkOrderBuildingsQuery.cs`), answered by `WorkOrderBuildingsQueryHandler` (`src/DataAccess/Handlers/WorkOrderBuildingsQueryHandler.cs`). The handler reads the distinct non-null `Building` values of work orders from the database, then trims each one, drops the ones that are empty after trimming, removes duplicates with an ordinal (case-sensitive) comparison and sorts the rest ignoring case (`StringComparer.OrdinalIgnoreCase`). It returns them as a `string[]`.
