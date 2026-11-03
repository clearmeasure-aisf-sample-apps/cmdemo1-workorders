# Note 263

`WorkOrderAttachment.UploadedDate` in `src/Core/Model/WorkOrderAttachment.cs` is declared `public DateTime UploadedDate { get; set; }` with no initializer, so a new attachment's uploaded date is the default `DateTime` value (`DateTime.MinValue`).
