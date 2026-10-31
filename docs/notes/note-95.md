# Note 95

Employee implements IComparable<Employee>. Employee.CompareTo orders employees by LastName and, when the last names are equal, by FirstName, both compared with ordinal comparison (`string.Compare(LastName, other!.LastName, StringComparison.Ordinal)`, then `string.Compare(FirstName, other.FirstName, StringComparison.Ordinal)` when that result is 0). The method is `Employee.CompareTo` in `src/Core/Model/Employee.cs`.
