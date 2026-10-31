# Note 200

`Team.MemberCount` in `src/Core/Model/Team.cs` is the count of `Members` (`public int MemberCount => Members.Count;`). `TeamMap` in `src/DataAccess/Mappings/TeamMap.cs` ignores that property (`entity.Ignore(t => t.MemberCount);`), so member count is not a column on `dbo.Team`.
