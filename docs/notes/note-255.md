# Note 255

`RecentWorkOrdersQueryValidator` in `src/Core/Validation/RecentWorkOrdersQueryValidator.cs` requires `RecentWorkOrdersQuery.Count` to be from 1 through 50 (`RuleFor(q => q.Count).InclusiveBetween(1, 50);`). The query is `RecentWorkOrdersQuery` in `src/Core/Queries/RecentWorkOrdersQuery.cs`.
