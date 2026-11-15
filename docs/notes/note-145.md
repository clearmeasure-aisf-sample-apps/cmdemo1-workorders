# Note 145

StatusPillPresenter.ForStatus returns a StatusPill whose CssClass is `status-badge status-badge--draft` when the status equals WorkOrderStatus.Draft, `status-badge status-badge--assigned` for Assigned, `status-badge status-badge--in-progress` for InProgress, `status-badge status-badge--complete` for Complete, and `status-badge status-badge--cancelled` for Cancelled (`$"{Base} {Base}--{Modifier(status)}"` with Base `"status-badge"` and modifiers from the status Key). The method is `StatusPillPresenter.ForStatus` in `src/UI.Shared/Models/StatusPillPresenter.cs`.
