# Note 100

EntityBase<T>.Equals(T? other) returns true when other is not null and IsSameIdentity holds. IsSameIdentity is true for the same instance (ReferenceEquals), or when HasSameNonEmptyId holds. HasSameNonEmptyId is `left.Equals(right) && !left.Equals(Guid.Empty)`, so two entities whose Id is Guid.Empty are not equal by identity. The method is `EntityBase<T>.Equals` in `src/Core/Model/EntityBase.cs`.
