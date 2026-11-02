# Note 233

`WorkOrderSpecificationQueryValidator` in `src/Core/Validation/WorkOrderSpecificationQueryValidator.cs` applies `IsInEnum()` to `Priority` (`RuleFor(x => x.Priority).IsInEnum();`).
