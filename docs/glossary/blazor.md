# Blazor

**Blazor** is the UI technology this application uses for its browser client and the server that hosts it. [`Program`](../../src/UI/Client/Program.cs) in `src/UI/Client` runs Blazor WebAssembly (`WebAssemblyHostBuilder`; SDK `Microsoft.NET.Sdk.BlazorWebAssembly`), and [`ServerApplication`](../../src/UI/Server/ServerApplication.cs) hosts those files with `UseBlazorFrameworkFiles`. [Application Architecture Layers](../architecture-layers.md) describes this UI as the Blazor Server and WebAssembly applications.
