# Note 167

GET `/_lamar/scanning` returns the string from `IContainer.WhatDidIScan()`. `WhatDoIHaveController.Scanning` in `src/UI/Api/Controllers/WhatDoIHaveController.cs` is mapped with `[HttpGet("/_lamar/scanning")]` and returns `container.WhatDidIScan()`.
