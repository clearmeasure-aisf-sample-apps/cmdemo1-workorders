# Note 219

`ServerApplication` in `src/UI/Server/ServerApplication.cs` registers the output-cache policy `OutputCachePolicyNames.WeatherSample` with `.Expire(TimeSpan.FromSeconds(30))` (`options.AddPolicy(OutputCachePolicyNames.WeatherSample, policy => policy` followed by `.Expire(TimeSpan.FromSeconds(30))`).
