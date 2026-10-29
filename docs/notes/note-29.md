# Note 29

A work order title cannot exceed 300 characters. WorkOrder.TitleMaxLength is 300. Setting Title trims leading and trailing whitespace, and a null value stays null. SaveDraftCommandValidator rejects a longer title with "Title cannot exceed 300 characters." The title column is required and mapped with HasMaxLength(WorkOrder.TitleMaxLength).
