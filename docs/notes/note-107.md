# Note 107

Employee.GetDisplayName returns LastName, a comma, a space, and FirstName when both names are set (`return string.IsNullOrWhiteSpace(LastName) ? FirstName : $"{LastName}, {FirstName}";`). When FirstName is null or whitespace, it returns LastName if that is set, otherwise UserName (`return string.IsNullOrWhiteSpace(LastName) ? UserName : LastName;`). When FirstName is set and LastName is null or whitespace, it returns FirstName. Names are not trimmed. The method is `Employee.GetDisplayName` in `src/Core/Model/Employee.cs`.
