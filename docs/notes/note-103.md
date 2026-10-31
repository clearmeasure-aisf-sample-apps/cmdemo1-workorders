# Note 103

EntityBase<T> defines operator == as Equals(left, right) (`return Equals(left, right);`) and operator != as the negation of Equals (`return !Equals(left, right);`). The operators are `operator ==` and `operator !=` on `EntityBase<T>` in `src/Core/Model/EntityBase.cs`.
