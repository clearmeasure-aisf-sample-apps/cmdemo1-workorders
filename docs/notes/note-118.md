# Note 118

StateCommandBase.IsValid returns true only when the work order's status equals GetBeginStatus(), UserCanExecute(CurrentUser) is true, and IsAllowedInCurrentState() is true (`return beginStatusMatches && currentUserIsCorrectRole && IsAllowedInCurrentState();`). IsAllowedInCurrentState defaults to true (`protected virtual bool IsAllowedInCurrentState() => true`). The method is `StateCommandBase.IsValid` in `src/Core/Model/StateCommands/StateCommandBase.cs`.
