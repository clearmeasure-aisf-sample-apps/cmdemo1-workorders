# Note 145

StatusBadgeCssClassMapper.CssClass throws ArgumentNullException when status is null (`ArgumentNullException.ThrowIfNull(status)`). Otherwise it returns `status-badge--draft` when the status equals WorkOrderStatus.Draft, `status-badge--assigned` for Assigned, `status-badge--in-progress` for InProgress, `status-badge--complete` for Complete, `status-badge--cancelled` for Cancelled, and an empty string for any other status. The method is `StatusBadgeCssClassMapper.CssClass` in `src/Core/Services/StatusBadgeCssClassMapper.cs`.
