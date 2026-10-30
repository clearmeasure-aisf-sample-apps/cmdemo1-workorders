# Application Architecture Layers

The application follows Onion Architecture. Dependencies point inward: outer
layers may depend on inner layers, but inner layers must not depend on outer
layers.

- **Core** (`src/Core/`) contains domain models, interfaces, and queries. It has
  no project dependencies.
- **DataAccess** (`src/DataAccess/`) contains Entity Framework Core data access
  and MediatR handlers. It references Core only.
- **UI** (`src/UI/` and `src/UI.Shared/`) contains the Blazor Server and
  WebAssembly applications and API endpoints. It is an outer layer that uses
  the application and data-access capabilities.
- **Database** (`src/Database/`) contains DbUp schema migrations and is
  independent of the application layers.

This dependency rule keeps domain logic in Core independent of infrastructure
and presentation details.
