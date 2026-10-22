# Onion Architecture Dependency Rules

Project dependencies point inward: outer layers can use inner-layer capabilities, but inner layers must not depend on outer layers.

- **Core** (`src/Core/`) contains domain models, interfaces, and queries. It has no project references.
- **DataAccess** (`src/DataAccess/`) contains EF Core data access and MediatR handlers. It may reference Core only.
- **UI** (`src/UI/` and `src/UI.Shared/`) is an outer layer. UI and host projects may reference Core and DataAccess as needed, along with other outer-layer services required for composition. Core and DataAccess must not reference UI projects.
- **Database** (`src/Database/`) contains DbUp schema migrations and is independent of the application layers; application projects must not reference it.

Keep domain logic in Core independent of infrastructure and presentation. The exact project references vary by host; the rules above describe the permitted direction, not a requirement that every UI project reference every inner layer.
