# Note 55

POST /api/tools/guid-generator returns a JSON array of newly generated GUIDs in D format. ToolsGuidGeneratorController.DefaultCount is 1 and MaxCount is 100. The optional count query parameter defaults to 1 and must be from 1 through 100 inclusive. A count outside that range returns HTTP 400. Each value is Guid.NewGuid().ToString("D"). The same action is also routed at api/v{version:apiVersion}/tools/guid-generator with API version 1.0.
