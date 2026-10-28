# Note 99

The parameterless Role constructor creates a role that cannot create or fulfill work orders. `public Role() : this(null!, false, false)` passes false for both permission arguments, and `CanCreateWorkOrder` and `CanFulfillWorkOrder` are initialized from those arguments (`CanCreateWorkOrder = canCreate`, `CanFulfillWorkOrder = canFulfill`), so both start false. The constructor is `Role()` in `src/Core/Model/Role.cs`.
