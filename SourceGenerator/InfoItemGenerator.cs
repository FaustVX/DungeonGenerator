using System.Text;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DungeonGenerator.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class InfoItemGenerator : IIncrementalGenerator
{
    private const string AttributeName = "DungeonGenerator.App.Item.InfoItemAttribute";
    private const string InfoItemName = "DungeonGenerator.App.Item.IInfoItem";
    private const string ItemName = "DungeonGenerator.App.Item.Item";

    private static readonly DiagnosticDescriptor InvalidGetInfoMethod = new(
        "INFOITEM001",
        "Unsupported GetInfo method",
        "Interface '{0}' marked with InfoItem must declare a protected static string GetInfo(Item) or GetInfo<T>() method",
        "InfoItem",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor NonPartialItem = new(
        "INFOITEM002",
        "Item type must be partial",
        "Item type '{0}' implements IInfoItem and an InfoItem interface but is not partial; implement IInfoItem.GetInfo manually",
        "InfoItem",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingInfoContribution = new(
        "INFOITEM003",
        "Missing item info contribution",
        "IInfoItem.GetInfo for item '{0}' must call '{1}.GetInfo' to include all InfoItem data",
        "InfoItem",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static output =>
            output.AddSource("InfoItemAttribute.g.cs", SourceText.From(
                """
                namespace DungeonGenerator.App.Item
                {
                    [global::System.Diagnostics.Conditional("INFOITEM_COMPILE_TIME")]
                    [global::System.AttributeUsage(global::System.AttributeTargets.Interface, Inherited = false)]
                    internal sealed class InfoItemAttribute : global::System.Attribute
                    {
                    }
                }
                """,
                Encoding.UTF8)));

        var interfaces = context.SyntaxProvider.ForAttributeWithMetadataName(
            AttributeName,
            static (node, _) => node is InterfaceDeclarationSyntax,
            static (attributeContext, _) => (INamedTypeSymbol)attributeContext.TargetSymbol)
            .Collect();

        var compilationAndInterfaces = context.CompilationProvider.Combine(interfaces);
        context.RegisterSourceOutput(compilationAndInterfaces, static (output, source) =>
            Execute(output, source.Left, source.Right));
    }

    private static void Execute(
        SourceProductionContext context,
        Compilation compilation,
        IReadOnlyList<INamedTypeSymbol> attributedInterfaces)
    {
        var infoItemType = compilation.GetTypeByMetadataName(InfoItemName);
        var itemType = compilation.GetTypeByMetadataName(ItemName);
        if (infoItemType is null || itemType is null)
            return;

        var seenInterfaces = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        var infoMethods = new Dictionary<INamedTypeSymbol, IMethodSymbol>(SymbolEqualityComparer.Default);
        foreach (var interfaceType in attributedInterfaces)
        {
            if (!seenInterfaces.Add(interfaceType))
                continue;

            var getInfoMethod = FindGetInfoMethod(interfaceType, itemType);
            if (getInfoMethod is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    InvalidGetInfoMethod,
                    GetLocation(interfaceType),
                    interfaceType.ToDisplayString()));
                continue;
            }

            infoMethods.Add(interfaceType, getInfoMethod);
        }

        if (infoMethods.Count == 0)
            return;

        var infoInterfaceMethod = infoItemType.GetMembers("GetInfo")
            .OfType<IMethodSymbol>()
            .FirstOrDefault(method =>
                method.IsStatic &&
                method.IsAbstract &&
                method.Parameters.Length == 2);
        if (infoInterfaceMethod is null)
            return;

        var generatedImplementations = new StringBuilder();
        foreach (var itemClass in GetAllTypes(compilation.Assembly.GlobalNamespace)
            .Where(type => type.TypeKind == TypeKind.Class && !type.IsAbstract))
        {
            if (!itemClass.AllInterfaces.Any(type =>
                    SymbolEqualityComparer.Default.Equals(type, infoItemType)))
                continue;

            var implementations = GetInfoInterfaces(compilation, itemClass, infoMethods)
                .Select(interfaceType => new KeyValuePair<INamedTypeSymbol, IMethodSymbol>(
                    interfaceType,
                    infoMethods[interfaceType]))
                .ToArray();
            if (implementations.Length == 0)
                continue;

            var getInfoImplementation = FindGetInfoImplementation(itemClass, infoInterfaceMethod);
            if (getInfoImplementation is not null)
            {
                ReportMissingContributions(
                    context,
                    compilation,
                    itemClass,
                    getInfoImplementation,
                    implementations);
                continue;
            }

            if (!IsPartial(itemClass))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    NonPartialItem,
                    GetLocation(itemClass),
                    itemClass.ToDisplayString()));
                continue;
            }

            generatedImplementations.AppendLine(CreateClassPart(
                itemClass,
                infoItemType,
                infoInterfaceMethod,
                implementations));
        }

        if (generatedImplementations.Length > 0)
            context.AddSource("InfoItemImplementations.g.cs", SourceText.From(generatedImplementations.ToString(), Encoding.UTF8));
    }

    private static IReadOnlyList<INamedTypeSymbol> GetInfoInterfaces(
        Compilation compilation,
        INamedTypeSymbol itemClass,
        Dictionary<INamedTypeSymbol, IMethodSymbol> infoMethods)
    {
        var result = new List<INamedTypeSymbol>();
        var visited = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        for (var current = itemClass; current is not null; current = current.BaseType)
        {
            foreach (var syntaxReference in current.DeclaringSyntaxReferences
                .OrderBy(reference => reference.SyntaxTree.FilePath, StringComparer.Ordinal)
                .ThenBy(reference => reference.Span.Start))
            {
                if (syntaxReference.GetSyntax() is not TypeDeclarationSyntax declaration ||
                    declaration.BaseList is null)
                    continue;

                var semanticModel = compilation.GetSemanticModel(declaration.SyntaxTree);
                foreach (var baseType in declaration.BaseList.Types)
                {
                    if (semanticModel.GetTypeInfo(baseType.Type).Type is INamedTypeSymbol interfaceType &&
                        interfaceType.TypeKind == TypeKind.Interface)
                        AppendInfoInterface(compilation, interfaceType, infoMethods, visited, result);
                }
            }
        }

        foreach (var interfaceType in itemClass.AllInterfaces)
        {
            if (infoMethods.ContainsKey(interfaceType) && visited.Add(interfaceType))
                result.Add(interfaceType);
        }

        return result;
    }

    private static void AppendInfoInterface(
        Compilation compilation,
        INamedTypeSymbol interfaceType,
        Dictionary<INamedTypeSymbol, IMethodSymbol> infoMethods,
        HashSet<INamedTypeSymbol> visited,
        List<INamedTypeSymbol> result)
    {
        if (!visited.Add(interfaceType))
            return;

        if (infoMethods.ContainsKey(interfaceType))
            result.Add(interfaceType);

        foreach (var syntaxReference in interfaceType.DeclaringSyntaxReferences
            .OrderBy(reference => reference.SyntaxTree.FilePath, StringComparer.Ordinal)
            .ThenBy(reference => reference.Span.Start))
        {
            if (syntaxReference.GetSyntax() is not InterfaceDeclarationSyntax declaration ||
                declaration.BaseList is null)
                continue;

            var semanticModel = compilation.GetSemanticModel(declaration.SyntaxTree);
            foreach (var baseType in declaration.BaseList.Types)
            {
                if (semanticModel.GetTypeInfo(baseType.Type).Type is INamedTypeSymbol baseInterface &&
                    baseInterface.TypeKind == TypeKind.Interface)
                    AppendInfoInterface(compilation, baseInterface, infoMethods, visited, result);
            }
        }
    }

    private static IMethodSymbol? FindGetInfoMethod(INamedTypeSymbol interfaceType, INamedTypeSymbol itemType)
        => interfaceType.GetMembers("GetInfo")
            .OfType<IMethodSymbol>()
            .FirstOrDefault(method =>
                method.IsStatic &&
                !method.IsAbstract &&
                (method.DeclaredAccessibility is Accessibility.Protected) &&
                method.ReturnType.SpecialType == SpecialType.System_String &&
                ((method.Arity == 0 &&
                  method.Parameters.Length == 1 &&
                  SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, itemType)) ||
                (method.Arity == 1 &&
                 method.Parameters.Length == 0 &&
                 method.TypeParameters[0].ConstraintTypes.Any(type =>
                     SymbolEqualityComparer.Default.Equals(type, interfaceType)))));

    private static IMethodSymbol? FindGetInfoImplementation(
        INamedTypeSymbol itemClass,
        IMethodSymbol infoInterfaceMethod)
    {
        var implementation = itemClass.FindImplementationForInterfaceMember(infoInterfaceMethod);
        return implementation is IMethodSymbol method && !method.IsAbstract
            ? method
            : null;
    }

    private static void ReportMissingContributions(
        SourceProductionContext context,
        Compilation compilation,
        INamedTypeSymbol itemClass,
        IMethodSymbol getInfoImplementation,
        IReadOnlyList<KeyValuePair<INamedTypeSymbol, IMethodSymbol>> requiredContributions)
    {
        var syntax = getInfoImplementation.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault();
        if (syntax is null)
            return;

        var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
        var calledMethods = syntax.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(invocation => !IsDeferredInvocation(invocation, syntax))
            .Select(invocation => (
                Invocation: invocation,
                Method: semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol))
            .Where(call => call.Method is not null)
            .ToArray();
        var implementationType = getInfoImplementation.ContainingType;

        foreach (var requiredContribution in requiredContributions)
        {
            var interfaceType = requiredContribution.Key;
            var requiredMethod = requiredContribution.Value;
            var isCalled = calledMethods.Any(call =>
                SymbolEqualityComparer.Default.Equals(
                    call.Method!.OriginalDefinition,
                    requiredMethod.OriginalDefinition) &&
                (requiredMethod.Arity == 0 ||
                 call.Method!.TypeArguments.Length == 1 &&
                 (SymbolEqualityComparer.Default.Equals(call.Method.TypeArguments[0], itemClass) ||
                  SymbolEqualityComparer.Default.Equals(call.Method.TypeArguments[0], implementationType))));
            if (isCalled)
                continue;

            context.ReportDiagnostic(Diagnostic.Create(
                MissingInfoContribution,
                syntax.Identifier.GetLocation(),
                properties: ImmutableDictionary<string, string?>.Empty.Add(
                    "InfoInterface",
                    interfaceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                itemClass.ToDisplayString(),
                interfaceType.ToDisplayString()));
        }
    }

    private static string CreateClassPart(
        INamedTypeSymbol itemClass,
        INamedTypeSymbol infoItemType,
        IMethodSymbol infoInterfaceMethod,
        IReadOnlyList<KeyValuePair<INamedTypeSymbol, IMethodSymbol>> implementations)
    {
        var containingTypes = new Stack<INamedTypeSymbol>();
        for (var type = itemClass; type is not null; type = type.ContainingType)
            containingTypes.Push(type);

        var itemName = itemClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var infoItemName = infoItemType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var gameType = infoInterfaceMethod.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var itemParameterType = infoInterfaceMethod.Parameters[1].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var resultType = infoInterfaceMethod.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var namespaceName = itemClass.ContainingNamespace.IsGlobalNamespace
            ? null
            : itemClass.ContainingNamespace.ToDisplayString();

        var source = new StringBuilder();
        if (namespaceName is not null)
            source.Append("namespace ").Append(namespaceName).AppendLine().AppendLine("{");

        var indentation = namespaceName is null ? string.Empty : "    ";
        foreach (var type in containingTypes)
        {
            source.Append(indentation).Append("partial ");
            if (type.IsRecord)
                source.Append("record class ");
            else
                source.Append("class ");
            source.Append(EscapeIdentifier(type.Name));
            AppendTypeParameters(source, type);
            source.AppendLine().Append(indentation).AppendLine("{");
            indentation += "    ";
        }

        source.Append(indentation)
            .Append("static ").Append(resultType).Append(' ')
            .Append(infoItemName).Append('.').Append(infoInterfaceMethod.Name)
            .Append('(').Append(gameType).Append(" game, ")
            .Append(itemParameterType).AppendLine(" item)")
            .Append(indentation).AppendLine("=>")
            .Append(indentation).AppendLine("[");

        var bodyIndentation = indentation + "    ";
        foreach (var implementation in implementations)
        {
            source.Append(bodyIndentation)
                .Append("new(game, item, ")
                .Append(CreateGetInfoInvocation(
                    implementation.Key,
                    implementation.Value,
                    itemName))
                .AppendLine("),");
        }

        source.Append(indentation).AppendLine("];");

        while (containingTypes.Count > 0)
        {
            containingTypes.Pop();
            indentation = indentation.Substring(0, indentation.Length - 4);
            source.Append(indentation).AppendLine("}");
        }

        if (namespaceName is not null)
            source.AppendLine("}");

        return source.ToString();
    }

    private static string CreateGetInfoInvocation(
        INamedTypeSymbol interfaceType,
        IMethodSymbol method,
        string itemName)
    {
        var interfaceName = interfaceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return method.Arity == 1
            ? interfaceName + "." + method.Name + "<" + itemName + ">()"
            : interfaceName + "." + method.Name + "(item)";
    }

    private static void AppendTypeParameters(StringBuilder source, INamedTypeSymbol type)
    {
        if (type.TypeParameters.Length == 0)
            return;

        source.Append('<');
        for (var index = 0; index < type.TypeParameters.Length; index++)
        {
            if (index > 0)
                source.Append(", ");
            source.Append(EscapeIdentifier(type.TypeParameters[index].Name));
        }
        source.Append('>');
    }

    private static string EscapeIdentifier(string identifier)
        => SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None ||
           SyntaxFacts.GetContextualKeywordKind(identifier) != SyntaxKind.None
            ? "@" + identifier
            : identifier;

    private static bool IsPartial(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            if (current.DeclaringSyntaxReferences.Length == 0 ||
                !current.DeclaringSyntaxReferences.All(reference =>
                    reference.GetSyntax() is TypeDeclarationSyntax declaration &&
                    declaration.Modifiers.Any(SyntaxKind.PartialKeyword)))
                return false;
        }

        return true;
    }

    private static bool IsDeferredInvocation(
        InvocationExpressionSyntax invocation,
        MethodDeclarationSyntax method)
    {
        for (var node = invocation.Parent; node is not null && node != method; node = node.Parent)
        {
            if (node is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax)
                return true;
        }

        return false;
    }

    private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol namespaceSymbol)
    {
        foreach (var type in namespaceSymbol.GetTypeMembers())
        {
            yield return type;
            foreach (var nestedType in GetNestedTypes(type))
                yield return nestedType;
        }

        foreach (var childNamespace in namespaceSymbol.GetNamespaceMembers())
            foreach (var type in GetAllTypes(childNamespace))
                yield return type;
    }

    private static IEnumerable<INamedTypeSymbol> GetNestedTypes(INamedTypeSymbol type)
    {
        foreach (var nestedType in type.GetTypeMembers())
        {
            yield return nestedType;
            foreach (var child in GetNestedTypes(nestedType))
                yield return child;
        }
    }

    private static Location GetLocation(INamedTypeSymbol type)
        => type.Locations.FirstOrDefault(location => location.IsInSource) ?? Location.None;
}
