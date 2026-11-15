using ClearMeasure.Bootcamp.UnitTests.BuildGates;

namespace ClearMeasure.Bootcamp.UnitTests.Architecture;

/// <summary>
/// Applies <see cref="UiSharedConventionRules"/> across the hand-written sources of a checkout.
/// </summary>
internal static class UiSharedConventionScan
{
    /// <summary>UI.Shared <c>[Inject]</c> properties declared nullable.</summary>
    internal static IReadOnlyList<string> NullableInjectViolations(RepositoryCheckout checkout)
    {
        var violations = new List<string>();
        foreach (var sourceFile in checkout.SourceFiles(".cs"))
        {
            var relative = checkout.Relative(sourceFile);
            if (!IsUiSharedProduction(relative))
            {
                continue;
            }

            AppendNamed(violations, relative, UiSharedConventionRules.NullableInjectPropertyNames(File.ReadAllText(sourceFile)));
        }

        return violations;
    }

    /// <summary>Auth-provider injections outside Authentication and UI/Client.</summary>
    internal static IReadOnlyList<string> AuthenticationProviderViolations(RepositoryCheckout checkout)
    {
        var violations = new List<string>();
        foreach (var sourceFile in checkout.SourceFiles(".cs", ".razor"))
        {
            CollectAuthProviderViolation(checkout, sourceFile, violations);
        }

        return violations;
    }

    /// <summary>Production sources that throw the bare <c>Exception</c> type.</summary>
    internal static IReadOnlyList<string> BareExceptionViolations(RepositoryCheckout checkout)
    {
        var violations = new List<string>();
        foreach (var sourceFile in checkout.SourceFiles(".cs"))
        {
            var relative = checkout.Relative(sourceFile);
            if (IsTestPath(relative))
            {
                continue;
            }

            if (UiSharedConventionRules.ThrowsBareException(File.ReadAllText(sourceFile)))
            {
                violations.Add(relative);
            }
        }

        return violations;
    }

    /// <summary>UI.Shared injections or constructions of <c>HttpClient</c> (allow-list empty).</summary>
    internal static IReadOnlyList<string> HttpClientViolations(RepositoryCheckout checkout)
    {
        var violations = new List<string>();
        foreach (var sourceFile in checkout.SourceFiles(".cs", ".razor"))
        {
            var relative = checkout.Relative(sourceFile);
            if (!IsUiSharedProduction(relative))
            {
                continue;
            }

            var text = File.ReadAllText(sourceFile);
            AppendNamed(violations, relative, UiSharedConventionRules.HttpClientInjections(text));
            foreach (var _ in UiSharedConventionRules.HttpClientConstructions(text))
            {
                violations.Add($"{relative}:new HttpClient");
            }
        }

        return violations;
    }

    /// <summary>
    /// Empty <c>catch</c> blocks in UI.Shared production sources outside the JS-module script allow-list
    /// (those classes own circuit-disconnect tolerance for one imported module).
    /// </summary>
    internal static IReadOnlyList<string> EmptyCatchViolations(RepositoryCheckout checkout)
    {
        var violations = new List<string>();
        foreach (var sourceFile in checkout.SourceFiles(".cs", ".razor"))
        {
            var relative = checkout.Relative(sourceFile);
            if (!IsUiSharedProduction(relative) || IsJsModuleScriptAllowListed(relative))
            {
                continue;
            }

            foreach (var line in UiSharedConventionRules.EmptyCatchLineNumbers(File.ReadAllText(sourceFile)))
            {
                violations.Add($"{relative}:{line}");
            }
        }

        return violations;
    }

    /// <summary><c>IJSObjectReference</c> usage in UI.Shared <c>.razor.cs</c> code-behinds.</summary>
    internal static IReadOnlyList<string> RazorCodeBehindIjsObjectReferenceViolations(RepositoryCheckout checkout)
    {
        var violations = new List<string>();
        foreach (var sourceFile in checkout.SourceFiles(".cs"))
        {
            var relative = checkout.Relative(sourceFile);
            if (!IsUiSharedProduction(relative) ||
                !relative.EndsWith(".razor.cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (UiSharedConventionRules.UsesIjsObjectReference(File.ReadAllText(sourceFile)))
            {
                violations.Add(relative);
            }
        }

        return violations;
    }

    /// <summary>
    /// Silent catch-alls in UI.Shared (<c>catch</c> / <c>catch (Exception)</c> that neither log nor rethrow).
    /// </summary>
    internal static IReadOnlyList<string> CatchExceptionWithoutLogViolations(RepositoryCheckout checkout)
    {
        var violations = new List<string>();
        foreach (var sourceFile in checkout.SourceFiles(".cs", ".razor"))
        {
            var relative = checkout.Relative(sourceFile);
            if (!IsUiSharedProduction(relative) || IsJsModuleScriptAllowListed(relative))
            {
                continue;
            }

            foreach (var line in UiSharedConventionRules.CatchExceptionWithoutLogLineNumbers(
                         File.ReadAllText(sourceFile)))
            {
                violations.Add($"{relative}:{line}");
            }
        }

        return violations;
    }

    /// <summary>
    /// JS-module script classes that may empty-catch circuit disconnect (path → why).
    /// A random <c>*Script.cs</c> file is not exempt.
    /// </summary>
    private static readonly Dictionary<string, string> JsModuleScriptAllowList = new(StringComparer.OrdinalIgnoreCase)
    {
        ["src/UI.Shared/Components/Search/SearchPageScript.cs"] =
            "Owns searchExport.js CSV/focus/indeterminate and dispose on disconnect.",
        ["src/UI.Shared/Components/MainLayoutNavScript.cs"] =
            "Owns mainLayoutNav.js nav/search init, focus, and dispose on disconnect.",
        ["src/UI.Shared/Components/NotificationsPanelScript.cs"] =
            "Owns notificationsPanel.js escape-handler lifecycle and dispose on disconnect.",
        ["src/UI.Shared/Services/ThemeScript.cs"] =
            "Owns theme.js get/set preference and dispose on disconnect.",
        ["src/UI.Shared/Components/ClipboardScript.cs"] =
            "Owns clipboard.js copy and dispose on disconnect.",
    };

    /// <summary>True when <paramref name="relative"/> is one of the five module script classes.</summary>
    internal static bool IsJsModuleScriptAllowListed(string relative) =>
        JsModuleScriptAllowList.ContainsKey(relative.Replace('\\', '/'));

    private static void CollectAuthProviderViolation(
        RepositoryCheckout checkout, string sourceFile, List<string> violations)
    {
        var relative = checkout.Relative(sourceFile);
        if (!relative.StartsWith("src/", StringComparison.OrdinalIgnoreCase) ||
            IsTestPath(relative) ||
            IsAuthProviderAllowListed(relative))
        {
            return;
        }

        var text = File.ReadAllText(sourceFile);
        var names = relative.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
            ? UiSharedConventionRules.AuthenticationProviderRazorInjects(text)
            : UiSharedConventionRules.AuthenticationProviderInjections(text);
        AppendNamed(violations, relative, names);
    }

    private static void AppendNamed(List<string> violations, string relative, IReadOnlyList<string> names)
    {
        foreach (var name in names)
        {
            violations.Add($"{relative}:{name}");
        }
    }

    private static bool IsUiSharedProduction(string relative) =>
        relative.StartsWith("src/UI.Shared/", StringComparison.OrdinalIgnoreCase) && !IsTestPath(relative);

    private static bool IsAuthProviderAllowListed(string relative) =>
        relative.StartsWith("src/UI.Shared/Authentication/", StringComparison.OrdinalIgnoreCase) ||
        relative.StartsWith("src/UI/Client/", StringComparison.OrdinalIgnoreCase);

    private static bool IsTestPath(string relative) =>
        relative.Contains("/UnitTests/", StringComparison.OrdinalIgnoreCase) ||
        relative.Contains("/IntegrationTests/", StringComparison.OrdinalIgnoreCase) ||
        relative.Contains("/AcceptanceTests/", StringComparison.OrdinalIgnoreCase);
}
