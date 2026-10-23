# Code Style Conventions

Follow the existing C# conventions documented in `CLAUDE.md` and illustrated throughout `src/`.

## Naming

- Use PascalCase for types and methods, and camelCase for variables.
- Keep names descriptive. Test names should identify the method or behavior, scenario, and expected result; see [Testing Conventions Cheat Sheet](testing-cheat-sheet.md).

## C# structure and nullability

- Enable nullable reference types and use file-scoped namespaces, such as `namespace ClearMeasure.Bootcamp.Core.Model;`.
- Initialize `[Inject]` and similar framework-populated fields with `null!` rather than `default!`.
- Add XML documentation to public APIs.

## Expressions and lambdas

- Do not let lambda parameters shadow variables from an outer scope; rename the parameter.
- Use `nameof(...)` instead of `.ToString()` on enum values in Razor or HTML attributes.
- Prefer a property pattern such as `value is SomeType { Property: expected }` over a separate type test and property comparison.
- In C# 13 and later, `nameof(SomeType<>.Member)` does not require a concrete type argument.

## Strings

- Do not prefix a string literal with `$` unless it contains interpolation.
