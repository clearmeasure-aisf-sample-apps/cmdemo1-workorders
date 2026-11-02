# Note 253

`Role.Id` in `src/Core/Model/Role.cs` overrides `EntityBase<Role>.Id` (`public override Guid Id { get; set; }`). `EntityBase<T>` in `src/Core/Model/EntityBase.cs` declares `public abstract Guid Id { get; set; }`.
