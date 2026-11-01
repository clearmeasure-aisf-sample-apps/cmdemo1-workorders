# Note 85

GET `/_lamar/services` returns the string from `IContainer.WhatDoIHave()`. `WhatDoIHaveController.Services` in `src/UI/Api/Controllers/WhatDoIHaveController.cs` is mapped with `[HttpGet("/_lamar/services")]` and returns `container.WhatDoIHave()`.
