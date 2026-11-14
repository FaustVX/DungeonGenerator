using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DungeonGenerator.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class InitItemGenerator : IIncrementalGenerator
{
    private const string AttributeName = "DungeonGenerator.App.Item.InitItemAttribute";
    private const string ItemName = "DungeonGenerator.App.Item.Item";

    private static readonly DiagnosticDescriptor MissingInitMethod = new(
        "INITITEM001",
        "Missing Init method",
        "Interface '{0}' marked with InitItem must declare protected static void Init<T>() where T : Item, {0}",
        "InitItem",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor NonPartialItemType = new(
        "INITITEM002",
        "Item type must be partial",
        "Item type '{0}' implements an InitItem interface but is not partial and has no static constructor",
        "InitItem",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingStaticInitializer = new(
        "INITITEM003",
        "Static constructor is missing an initializer",
        "Static constructor for item type '{0}' must call '{1}' with its own item type",
        "InitItem",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnsupportedInterface = new(
        "INITITEM004",
        "InitItem interface shape is unsupported",
        "InitItem interface '{0}' must be top-level and non-generic",
        "InitItem",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingItemInitMethod = new(
        "INITITEM005",
        "Missing Item.Init method",
        "Item type '{0}' must declare protected static void Init<T>() where T : Item",
        "InitItem",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static output =>
            output.AddSource("InitItemAttribute.g.cs", SourceText.From(
                """
                namespace DungeonGenerator.App.Item
                {
                    [global::System.Diagnostics.Conditional("INITITEM_COMPILE_TIME")]
                    [global::System.AttributeUsage(global::System.AttributeTargets.Interface, Inherited = false)]
                    internal sealed class InitItemAttribute : global::System.Attribute
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
        var itemType = compilation.GetTypeByMetadataName(ItemName);
        var seenInterfaces = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        var validInterfaces = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var interfaceType in attributedInterfaces)
        {
            if (!seenInterfaces.Add(interfaceType))
                continue;

            if (interfaceType.Arity != 0 || interfaceType.ContainingType is not null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    UnsupportedInterface,
                    GetLocation(interfaceType),
                    interfaceType.ToDisplayString()));
                continue;
            }

            if (itemType is null || !HasValidInitMethod(interfaceType, itemType))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    MissingInitMethod,
                    GetLocation(interfaceType),
                    interfaceType.ToDisplayString()));
                continue;
            }

            validInterfaces.Add(interfaceType);
        }

        if (itemType is null || validInterfaces.Count == 0)
            return;

        var itemInitMethod = FindItemInitMethod(itemType);
        if (itemInitMethod is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                MissingItemInitMethod,
                GetLocation(itemType),
                itemType.ToDisplayString()));
            return;
        }

        var generatedConstructors = new StringBuilder();
        foreach (var itemClass in GetAllTypes(compilation.Assembly.GlobalNamespace)
            .Where(type => type.TypeKind == TypeKind.Class && !type.IsAbstract))
        {
            var initInterfaces = GetInitInterfaces(compilation, itemClass, validInterfaces);
            if (initInterfaces.Length == 0)
                continue;

            var staticConstructor = itemClass
                .GetMembers()
                .OfType<IMethodSymbol>()
                .FirstOrDefault(method => method.MethodKind == MethodKind.StaticConstructor);
            var hasStaticConstructor = staticConstructor is not null;
            if (hasStaticConstructor)
            {
                ReportMissingInitializers(
                    context,
                    compilation,
                    itemClass,
                    staticConstructor!,
                    initInterfaces,
                    itemType,
                    itemInitMethod);
                continue;
            }

            if (!IsPartial(itemClass))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    NonPartialItemType,
                    GetLocation(itemClass),
                    itemClass.ToDisplayString()));
                continue;
            }

            generatedConstructors.AppendLine(CreateClassPart(itemClass, initInterfaces));
        }

        if (generatedConstructors.Length > 0)
            context.AddSource("InitItemConstructors.g.cs", SourceText.From(generatedConstructors.ToString(), Encoding.UTF8));
    }

    private static INamedTypeSymbol[] GetInitInterfaces(
        Compilation compilation,
        INamedTypeSymbol itemClass,
        HashSet<INamedTypeSymbol> validInterfaces)
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
                        AppendInitInterface(compilation, interfaceType, validInterfaces, visited, result);
                }
            }
        }

        foreach (var interfaceType in itemClass.AllInterfaces)
        {
            if (validInterfaces.Contains(interfaceType) && visited.Add(interfaceType))
                result.Add(interfaceType);
        }

        return [.. result];
    }

    private static void AppendInitInterface(
        Compilation compilation,
        INamedTypeSymbol interfaceType,
        HashSet<INamedTypeSymbol> validInterfaces,
        HashSet<INamedTypeSymbol> visited,
        List<INamedTypeSymbol> result)
    {
        if (!visited.Add(interfaceType))
            return;

        if (validInterfaces.Contains(interfaceType))
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
                    AppendInitInterface(compilation, baseInterface, validInterfaces, visited, result);
            }
        }
    }

    private static bool HasValidInitMethod(INamedTypeSymbol interfaceType, INamedTypeSymbol itemType)
        => FindInterfaceInitMethod(interfaceType, itemType) is not null;

    private static IMethodSymbol? FindInterfaceInitMethod(
        INamedTypeSymbol interfaceType,
        INamedTypeSymbol itemType)
    {
        foreach (var method in interfaceType.GetMembers("Init").OfType<IMethodSymbol>())
        {
            if (!method.IsStatic ||
                method.DeclaredAccessibility != Accessibility.Protected ||
                method.Arity != 1 ||
                method.Parameters.Length != 0 ||
                !method.ReturnsVoid)
                continue;

            var constraints = method.TypeParameters[0].ConstraintTypes;
            if (constraints.Any(type => SymbolEqualityComparer.Default.Equals(type, itemType)) &&
                constraints.Any(type => SymbolEqualityComparer.Default.Equals(type, interfaceType)))
                return method;
        }

        return null;
    }

    private static IMethodSymbol? FindItemInitMethod(INamedTypeSymbol itemType)
        => itemType.GetMembers("Init")
            .OfType<IMethodSymbol>()
            .FirstOrDefault(method =>
                method.IsStatic &&
                method.DeclaredAccessibility == Accessibility.Protected &&
                method.Arity == 1 &&
                method.Parameters.Length == 0 &&
                method.ReturnsVoid &&
                method.TypeParameters[0].ConstraintTypes.Any(type =>
                    SymbolEqualityComparer.Default.Equals(type, itemType)));

    private static void ReportMissingInitializers(
        SourceProductionContext context,
        Compilation compilation,
        INamedTypeSymbol itemClass,
        IMethodSymbol staticConstructor,
        IReadOnlyList<INamedTypeSymbol> initInterfaces,
        INamedTypeSymbol itemType,
        IMethodSymbol itemInitMethod)
    {
        var syntax = staticConstructor.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<ConstructorDeclarationSyntax>()
            .FirstOrDefault();
        var calledMethods = new List<IMethodSymbol>();
        if (syntax is not null)
        {
            var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
            foreach (var invocation in syntax.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (IsDeferredInvocation(invocation, syntax))
                    continue;

                if (semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol method)
                    calledMethods.Add(method);
            }
        }

        var requiredMethods = initInterfaces
            .Select(interfaceType => FindInterfaceInitMethod(interfaceType, itemType))
            .OfType<IMethodSymbol>()
            .Append(itemInitMethod);

        foreach (var requiredMethod in requiredMethods)
        {
            var isCalled = calledMethods.Any(calledMethod =>
                SymbolEqualityComparer.Default.Equals(
                    calledMethod.OriginalDefinition,
                    requiredMethod.OriginalDefinition) &&
                calledMethod.TypeArguments.Length == 1 &&
                SymbolEqualityComparer.Default.Equals(calledMethod.TypeArguments[0], itemClass));
            if (isCalled)
                continue;

            context.ReportDiagnostic(Diagnostic.Create(
                MissingStaticInitializer,
                syntax?.Identifier.GetLocation() ?? GetLocation(itemClass),
                itemClass.ToDisplayString(),
                requiredMethod.ContainingType.ToDisplayString() + "." + requiredMethod.Name));
        }
    }

    private static bool IsDeferredInvocation(
        InvocationExpressionSyntax invocation,
        ConstructorDeclarationSyntax constructor)
    {
        for (var node = invocation.Parent; node is not null && node != constructor; node = node.Parent)
        {
            if (node is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax)
                return true;
        }

        return false;
    }

    private static string CreateClassPart(
        INamedTypeSymbol itemClass,
        IReadOnlyList<INamedTypeSymbol> initInterfaces)
    {
        var containingTypes = new Stack<INamedTypeSymbol>();
        for (var type = itemClass; type is not null; type = type.ContainingType)
            containingTypes.Push(type);

        var source = new StringBuilder();
        var namespaceName = itemClass.ContainingNamespace.IsGlobalNamespace
            ? null
            : itemClass.ContainingNamespace.ToDisplayString();
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
            .Append("static ").Append(EscapeIdentifier(itemClass.Name)).AppendLine("()")
            .Append(indentation).AppendLine("{");
        var bodyIndentation = indentation + "    ";
        var className = itemClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        foreach (var interfaceType in initInterfaces)
        {
            source.Append(bodyIndentation)
                .Append(interfaceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                .Append(".Init<").Append(className).AppendLine(">();");
        }
        source.Append(bodyIndentation).Append("Init<").Append(className).AppendLine(">();")
            .Append(indentation).AppendLine("}");

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
