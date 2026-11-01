# Note 201

`OutputCachePolicyNames.VersionMetadata` in `src/UI/Api/OutputCachePolicyNames.cs` is `nameof(VersionMetadata)`, so the policy name is the string `VersionMetadata`. `ServerApplication` in `src/UI/Server/ServerApplication.cs` registers that policy with `Expire(TimeSpan.FromMinutes(10))`. `VersionController.Get` applies `[OutputCache(PolicyName = OutputCachePolicyNames.VersionMetadata)]`.
