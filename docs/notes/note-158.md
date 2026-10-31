# Note 158

StateCommandContext holds the clock for a state command as an init-only DateTime property CurrentDateTime (`public DateTime CurrentDateTime { get; init; }`). The class is `StateCommandContext` in `src/Core/Services/StateCommandContext.cs`. StateCommandHandler.Handle sets that property from `time.GetUtcNow().DateTime` (`var currentDateTime = time.GetUtcNow().DateTime`) and then calls `request.Execute(new StateCommandContext { CurrentDateTime = currentDateTime })`. The method is `StateCommandHandler.Handle` in `src/DataAccess/Handlers/StateCommandHandler.cs`.
