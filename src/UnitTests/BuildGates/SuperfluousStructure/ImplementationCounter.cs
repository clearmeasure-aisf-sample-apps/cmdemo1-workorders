namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

/// <summary>
/// Counts concrete implementations of interfaces declared in the solution, and direct subclasses of abstract classes.
/// External base types such as <c>IDisposable</c> are ignored.
/// </summary>
internal static class ImplementationCounter
{
    public static IReadOnlyList<string> SingleImplementationInterfaces(IReadOnlyList<ParsedType> types)
    {
        var graph = TypeGraph.Build(types);
        return types
            .Where(type => type.Kind == DeclaredTypeKind.Interface)
            .Where(type => graph.ConcreteImplementations(type) == 1)
            .Select(type => type.Id)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
    }

    public static IReadOnlyList<string> SingleSubclassAbstractClasses(IReadOnlyList<ParsedType> types)
    {
        var graph = TypeGraph.Build(types);
        return types
            .Where(IsAbstractClass)
            .Where(type => graph.DirectSubclasses(type) == 1)
            .Select(type => type.Id)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
    }

    private static bool IsAbstractClass(ParsedType type)
    {
        return type.IsAbstract
            && !type.IsStatic
            && type.Kind is DeclaredTypeKind.Class or DeclaredTypeKind.Record;
    }

    private sealed class TypeGraph
    {
        private readonly Dictionary<ParsedType, List<ParsedType>> _bases = [];

        public static TypeGraph Build(IReadOnlyList<ParsedType> types)
        {
            var index = TypeIndex.Create(types);
            var graph = new TypeGraph();
            foreach (var type in types)
            {
                graph._bases[type] = ResolveBases(type, index);
            }

            return graph;
        }

        public int ConcreteImplementations(ParsedType target)
        {
            return _bases.Keys.Count(type => IsConcrete(type) && Reaches(type, target));
        }

        public int DirectSubclasses(ParsedType parent)
        {
            return _bases.Count(pair => pair.Value.Contains(parent));
        }

        private bool Reaches(ParsedType start, ParsedType target)
        {
            var stack = new Stack<ParsedType>();
            var seen = new HashSet<ParsedType>();
            stack.Push(start);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (!seen.Add(current) || !_bases.TryGetValue(current, out var bases))
                {
                    continue;
                }

                foreach (var next in bases)
                {
                    if (ReferenceEquals(next, target))
                    {
                        return true;
                    }

                    stack.Push(next);
                }
            }

            return false;
        }

        private static List<ParsedType> ResolveBases(ParsedType type, TypeIndex index)
        {
            var resolved = new List<ParsedType>();
            foreach (var baseType in type.Bases)
            {
                var match = index.Resolve(type, baseType.Name, baseType.Arity);
                if (match != null && !ReferenceEquals(match, type))
                {
                    resolved.Add(match);
                }
            }

            return resolved;
        }

        private static bool IsConcrete(ParsedType type)
        {
            return !type.IsAbstract && !type.IsStatic && type.Kind != DeclaredTypeKind.Interface;
        }
    }

    private sealed class TypeIndex
    {
        private readonly Dictionary<string, ParsedType> _types = new(StringComparer.Ordinal);

        public static TypeIndex Create(IReadOnlyList<ParsedType> types)
        {
            var index = new TypeIndex();
            foreach (var type in types)
            {
                index._types.TryAdd(Key(type.Namespace, type.LookupName, type.Arity), type);
            }

            return index;
        }

        public ParsedType? Resolve(ParsedType owner, string name, int arity)
        {
            foreach (var alias in owner.Aliases)
            {
                if (alias.Alias == name)
                {
                    return Resolve(owner, alias.Name, alias.Arity);
                }
            }

            return SearchEnclosing(owner, name, arity) ?? SearchQualified(owner, name, arity);
        }

        private ParsedType? SearchEnclosing(ParsedType owner, string name, int arity)
        {
            var found = SearchNamespaceChain(owner.Namespace, name, arity);
            return found ?? SearchUsings(owner.Usings, name, arity);
        }

        private ParsedType? SearchQualified(ParsedType owner, string name, int arity)
        {
            var dot = name.LastIndexOf('.');
            if (dot <= 0)
            {
                return null;
            }

            var left = name[..dot];
            var right = name[(dot + 1)..];
            var absolute = Find(left, right, arity);
            if (absolute != null)
            {
                return absolute;
            }

            var prefixed = SearchNamespaceChain(owner.Namespace, left, right, arity);
            return prefixed ?? SearchUsings(owner.Usings, left + "." + right, arity);
        }

        private ParsedType? SearchNamespaceChain(string namespaceName, string name, int arity)
        {
            var current = namespaceName;
            while (true)
            {
                var found = Find(current, name, arity);
                if (found != null)
                {
                    return found;
                }

                if (current.Length == 0)
                {
                    return null;
                }

                var dot = current.LastIndexOf('.');
                current = dot < 0 ? "" : current[..dot];
            }
        }

        private ParsedType? SearchNamespaceChain(string namespaceName, string left, string right, int arity)
        {
            var current = namespaceName;
            while (true)
            {
                var prefix = current.Length == 0 ? left : current + "." + left;
                var found = Find(prefix, right, arity);
                if (found != null)
                {
                    return found;
                }

                if (current.Length == 0)
                {
                    return null;
                }

                var dot = current.LastIndexOf('.');
                current = dot < 0 ? "" : current[..dot];
            }
        }

        private ParsedType? SearchUsings(IReadOnlyList<string> usings, string name, int arity)
        {
            ParsedType? match = null;
            foreach (var used in usings)
            {
                var found = Find(used, name, arity);
                if (found == null)
                {
                    continue;
                }

                if (match != null && !ReferenceEquals(match, found))
                {
                    return null;
                }

                match = found;
            }

            return match;
        }

        private ParsedType? Find(string namespaceName, string name, int arity)
        {
            return _types.TryGetValue(Key(namespaceName, name, arity), out var type) ? type : null;
        }

        private static string Key(string namespaceName, string name, int arity)
        {
            return namespaceName + "|" + name + "|" + arity;
        }
    }
}
