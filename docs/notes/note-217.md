# Note 217

`FunJeffreyCustomEventHealthCheck.EventName` in `src/UI.Shared/FunJeffreyCustomEventHealthCheck.cs` is the public constant `JeffreyHealthCheckEvent` (`public const string EventName = "JeffreyHealthCheckEvent";`). `CheckHealthAsync` adds an activity event with that name (`activity?.AddEvent(new ActivityEvent(EventName, tags: new ActivityTagsCollection { ... }))`).
