# Note 260

`WorkOrderAttachment.FileSize` in `src/Core/Model/WorkOrderAttachment.cs` is declared `public long FileSize { get; set; }` with no initializer, so a new attachment's file size is the default `long` value 0.
