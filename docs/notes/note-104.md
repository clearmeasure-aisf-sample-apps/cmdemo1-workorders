# Note 104

EntityBase<T>.Equals(object?) returns true only when the argument is a T and IsSameTypedInstance holds (`obj is T typed && IsSameTypedInstance(typed)`). IsSameTypedInstance is true only when the runtime type matches and IsSameIdentity holds (`typed.GetType() == GetType() && IsSameIdentity(typed)`). The method is `EntityBase<T>.Equals` in `src/Core/Model/EntityBase.cs`.
