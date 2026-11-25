# Dependencies per project

Read when checking which package a project already has, or before proposing a package. Moved from `CLAUDE.md`. The
source of truth is `src/Directory.Packages.props` (Central Package Management: no `Version` on a `PackageReference`,
`VersionOverride` disabled). A new package ID needs approval: see
[architecture-guardrails.md](../architecture-guardrails.md). Do NOT add packages or change versions without approval.

**.NET SDK:** 10.0.100 (see `global.json`)

**Core** — FluentValidation 11.11.0, MediatR.Contracts 2.0.1, Microsoft.CodeAnalysis.BannedApiAnalyzers 4.14.0 (build only), Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions 10.0.0, Microsoft.Extensions.Logging.Abstractions 10.0.0

**DataAccess** — MediatR 12.4.1, Microsoft.EntityFrameworkCore 10.0.0, Microsoft.EntityFrameworkCore.SqlServer 10.0.0, Microsoft.EntityFrameworkCore.Sqlite 10.0.0, NServiceBus.Persistence.Sql.TransactionalSession 8.3.0, ClearMeasureLabs.HostedEndpoint.SqlServerTransport 1.0.30, OpenFeature 2.14.1, Octopus.OpenFeature 4.0.0, Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions 10.0.0, Microsoft.Extensions.Logging.Abstractions 10.0.0, SQLitePCLRaw.bundle_e_sqlite3 2.1.12, System.Security.Cryptography.Xml 10.0.11

**Database** — DbUp 5.0.41, dbup-sqlserver 6.0.16, JetBrains.Annotations 2025.2.2, Spectre.Console 0.54.0, Spectre.Console.Cli 0.53.0

**UI.Server** — Asp.Versioning.Http 8.1.0, FluentValidation.DependencyInjectionExtensions 11.11.0, Asp.Versioning.Mvc 8.1.0, Google.Protobuf 3.27.0, Grpc.AspNetCore 2.70.0, Lamar.Microsoft.DependencyInjection 15.0.1, Microsoft.AspNetCore.Components.WebAssembly.Server 10.0.0, ModelContextProtocol 1.0.0, ModelContextProtocol.AspNetCore 1.0.0, NServiceBus.Extensions.Hosting 3.0.1, OpenTelemetry.Instrumentation.AspNetCore 1.15.2, OpenTelemetry.Instrumentation.Http 1.15.1, OpenTelemetry.Instrumentation.Runtime 1.15.1, OpenTelemetry.Instrumentation.SqlClient 1.15.2

**UI.Client** — BlazorMvc 2.1.1, MediatR 12.4.1, Lamar.Microsoft.DependencyInjection 15.0.1, Microsoft.AspNetCore.Components.WebAssembly 10.0.0, Microsoft.AspNetCore.Components.WebAssembly.DevServer 10.0.0, Microsoft.AspNetCore.Components.Authorization 10.0.0, Microsoft.Extensions.Diagnostics.HealthChecks 10.0.0, Toolbelt.Blazor.SpeechRecognition 1.0.0, Toolbelt.Blazor.SpeechSynthesis 11.0.0

**UI.Api** — Asp.Versioning.Abstractions 8.1.0, Lamar.Microsoft.DependencyInjection 15.0.1

**UI.Shared** — BlazorMvc 2.1.1, MediatR 12.4.1, Microsoft.AspNetCore.Components.Authorization 10.0.0, Microsoft.AspNetCore.Components.Web 10.0.0, Toolbelt.Blazor.SpeechRecognition 1.0.0, Toolbelt.Blazor.SpeechSynthesis 11.0.0

**LlmGateway** — Azure.AI.OpenAI 2.1.0, Azure.Core 1.46.1, MediatR 12.4.1, Microsoft.Extensions.AI 9.7.0, Microsoft.Extensions.AI.OpenAI 9.7.1-preview.1.25365.4, Microsoft.Extensions.AI.Abstractions 9.7.1, Microsoft.Extensions.Diagnostics.HealthChecks 10.0.0, Microsoft.Extensions.Logging.Console 10.0.0

**McpServer** — ModelContextProtocol 1.0.0, ModelContextProtocol.AspNetCore 1.0.0, Lamar.Microsoft.DependencyInjection 15.0.1, MediatR 12.4.1, Microsoft.EntityFrameworkCore.SqlServer 10.0.0, Microsoft.EntityFrameworkCore.Sqlite 10.0.0, SQLitePCLRaw.bundle_e_sqlite3 2.1.12, OpenTelemetry.Instrumentation.AspNetCore 1.15.2, OpenTelemetry.Instrumentation.Http 1.15.1, OpenTelemetry.Instrumentation.Runtime 1.15.1, OpenTelemetry.Instrumentation.SqlClient 1.15.2

**Worker** — ClearMeasureLabs.HostedEndpoint.SqlServerTransport 1.0.30, Microsoft.Extensions.Hosting 10.0.2

**ChurchBulletin.AppHost** — MessagePack 2.5.302

**ChurchBulletin.ServiceDefaults** — Azure.Monitor.OpenTelemetry.AspNetCore 1.6.0, OpenTelemetry.Api 1.15.3, OpenTelemetry.Exporter.Console 1.15.3, OpenTelemetry.Exporter.OpenTelemetryProtocol 1.15.3, OpenTelemetry.Extensions.Hosting 1.15.3, OpenTelemetry.Instrumentation.AspNetCore 1.15.2, OpenTelemetry.Instrumentation.EntityFrameworkCore 1.0.0-beta.12, OpenTelemetry.Instrumentation.Http 1.15.1, OpenTelemetry.Instrumentation.Runtime 1.15.1, OpenTelemetry.Instrumentation.SqlClient 1.15.2, Microsoft.Extensions.Http.Resilience 9.9.0, Microsoft.Extensions.ServiceDiscovery 9.5.0, Serilog.AspNetCore 9.0.0, Serilog.Formatting.Compact 3.0.0

**UnitTests** — NUnit 4.3.2, NUnit3TestAdapter 5.0.0, NUnit.Analyzers 4.9.2, Shouldly 4.3.0, TngTech.ArchUnitNET 0.13.5, bunit 2.9.0, AutoBogus.Conventions 2.13.1, MediatR 12.4.1, coverlet.msbuild 6.0.4, coverlet.collector 6.0.4, AngleSharp 1.7.2, Microsoft.AspNetCore.Components.Authorization 10.0.11, Microsoft.AspNetCore.Mvc.Testing 10.0.0, Microsoft.NET.Test.Sdk 17.14.1, Serilog.Formatting.Compact 3.0.0, Toolbelt.Blazor.SpeechRecognition 1.0.0, Toolbelt.Blazor.SpeechSynthesis 11.0.0

**IntegrationTests** — NUnit 4.3.2, NUnit3TestAdapter 5.0.0, NUnit.Analyzers 4.9.2, Shouldly 4.3.0, Microsoft.EntityFrameworkCore 10.0.0, Microsoft.Extensions.Hosting 10.0.2, coverlet.msbuild 6.0.4, coverlet.collector 6.0.4, Grpc.Net.Client 2.70.0, Microsoft.AspNetCore.Mvc.Testing 10.0.0, Microsoft.NET.Test.Sdk 17.14.1

**AcceptanceTests** — NUnit 4.3.2, NUnit3TestAdapter 5.0.0, NUnit.Analyzers 4.9.2, microsoft.playwright.nunit 1.61.0, Azure.AI.OpenAI 2.1.0, ModelContextProtocol 1.0.0, Microsoft.Extensions.AI 9.7.0, Microsoft.Extensions.AI.OpenAI 9.7.1-preview.1.25365.4, coverlet.collector 6.0.4, coverlet.msbuild 6.0.4, Microsoft.NET.Test.Sdk 17.14.1, Microsoft.Extensions.Hosting 10.0.2, OpenTelemetry.Instrumentation.AspNetCore 1.15.2, OpenTelemetry.Instrumentation.Http 1.15.1, OpenTelemetry.Instrumentation.Runtime 1.15.1, OpenTelemetry.Instrumentation.SqlClient 1.15.2
