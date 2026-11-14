using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace DungeonGenerator.SourceGenerator.CodeFix;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InitItemCodeFixProvider)), Shared]
public sealed class InitItemCodeFixProvider : CodeFixProvider
{
    private const string AttributeName = "DungeonGenerator.App.Item.InitItemAttribute";
    private const string ItemName = "DungeonGenerator.App.Item.Item";

    public override ImmutableArray<string> FixableDiagnosticIds
        => ["INITITEM001", "INITITEM002", "INITITEM003"];

    public override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        var registeredIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var diagnostic in context.Diagnostics)
        {
            if (!registeredIds.Add(diagnostic.Id))
                continue;

            var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            switch (diagnostic.Id)
            {
                case "INITITEM001":
                    if (node.FirstAncestorOrSelf<InterfaceDeclarationSyntax>() is { } interfaceDeclaration)
                        context.RegisterCodeFix(
                            Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(
                                "Generate Init<T> method",
                                cancellationToken => AddInitMethodAsync(context.Document, interfaceDeclaration, cancellationToken),
                                "GenerateInitMethod"),
                            diagnostic);
                    break;

                case "INITITEM002":
                    if (node.FirstAncestorOrSelf<TypeDeclarationSyntax>() is { } typeDeclaration)
                    {
                        context.RegisterCodeFix(
                            Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(
                                "Add partial modifier",
                                cancellationToken => AddPartialModifiersAsync(context.Document, typeDeclaration, cancellationToken),
                                "AddPartialModifier"),
                            diagnostic);
                        context.RegisterCodeFix(
                            Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(
                                "Generate static constructor",
                                cancellationToken => AddStaticConstructorAsync(context.Document, typeDeclaration, cancellationToken),
                                "GenerateStaticConstructor"),
                            diagnostic);
                    }
                    break;

                case "INITITEM003":
                    if (node.FirstAncestorOrSelf<ConstructorDeclarationSyntax>() is { } constructor)
                        context.RegisterCodeFix(
                            Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(
                                "Complete static constructor",
                                cancellationToken => CompleteStaticConstructorAsync(context.Document, constructor, cancellationToken),
                                "CompleteStaticConstructor"),
                            diagnostic);
                    break;
            }
        }
    }

    private static async Task<Document> AddInitMethodAsync(
        Document document,
        InterfaceDeclarationSyntax declaration,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var interfaceSymbol = semanticModel?.GetDeclaredSymbol(declaration, cancellationToken);
        var itemType = semanticModel?.Compilation.GetTypeByMetadataName(ItemName);
        if (interfaceSymbol is null || itemType is null)
            return document;

        var interfaceName = interfaceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var itemName = itemType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var method = SyntaxFactory.ParseMemberDeclaration(
            $$"""
            protected static void Init<T>()
            where T : {{itemName}}, {{interfaceName}}
            {
                {{itemName}}._actionsCache[(typeof(T), global::DungeonGenerator.App.Item.ItemLocation.Inventory)].Add("Action", delegate { });
            }
            """)!;

        var updated = declaration.AddMembers(
            SimplifyNames(method).WithAdditionalAnnotations(Formatter.Annotation));
        return document.WithSyntaxRoot((await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!
            .ReplaceNode(declaration, updated));
    }

    private static async Task<Document> AddPartialModifiersAsync(
        Document document,
        TypeDeclarationSyntax declaration,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var symbol = semanticModel?.GetDeclaredSymbol(declaration, cancellationToken);
        if (root is null || symbol is null)
            return document;

        var replacements = new Dictionary<TypeDeclarationSyntax, TypeDeclarationSyntax>();
        for (var current = symbol; current is not null; current = current.ContainingType)
        {
            foreach (var syntaxReference in current.DeclaringSyntaxReferences)
            {
                if (syntaxReference.GetSyntax(cancellationToken) is not TypeDeclarationSyntax typeSyntax ||
                    !typeSyntax.SyntaxTree.Equals(root.SyntaxTree) ||
                    typeSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
                    continue;

                replacements[typeSyntax] = typeSyntax.WithModifiers(
                    typeSyntax.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword)))
                    .WithAdditionalAnnotations(Formatter.Annotation);
            }
        }

        return replacements.Count == 0
            ? document
            : document.WithSyntaxRoot(root.ReplaceNodes(
                replacements.Keys,
                (original, _) => replacements[original]));
    }

    private static async Task<Document> AddStaticConstructorAsync(
        Document document,
        TypeDeclarationSyntax declaration,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var symbol = semanticModel?.GetDeclaredSymbol(declaration, cancellationToken);
        if (semanticModel is null || symbol is null)
            return document;

        var calls = GetRequiredInitializerCalls(semanticModel.Compilation, symbol, cancellationToken);
        if (calls.Count == 0)
            return document;

        var constructor = CreateStaticConstructor(symbol, calls);
        return document.WithSyntaxRoot((await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!
            .ReplaceNode(declaration, declaration.AddMembers(constructor)));
    }

    private static async Task<Document> CompleteStaticConstructorAsync(
        Document document,
        ConstructorDeclarationSyntax constructor,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var containingType = constructor.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        var symbol = containingType is null
            ? null
            : semanticModel?.GetDeclaredSymbol(containingType, cancellationToken);
        if (semanticModel is null || symbol is null)
            return document;

        var missingCalls = GetMissingInitializerCalls(semanticModel, constructor, symbol, cancellationToken);
        if (missingCalls.Count == 0)
            return document;

        var statements = constructor.Body?.Statements ?? default;
        if (constructor.ExpressionBody is not null)
            statements = statements.Add(SyntaxFactory.ExpressionStatement(constructor.ExpressionBody.Expression));

        var updatedConstructor = constructor.WithBody(
                SyntaxFactory.Block(missingCalls.Concat(statements))
                    .WithAdditionalAnnotations(Formatter.Annotation))
            .WithExpressionBody(null)
            .WithSemicolonToken(default);
        return document.WithSyntaxRoot((await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!
            .ReplaceNode(constructor, updatedConstructor));
    }

    private static ConstructorDeclarationSyntax CreateStaticConstructor(
        INamedTypeSymbol type,
        IReadOnlyList<StatementSyntax> calls)
        => SyntaxFactory.ConstructorDeclaration(SyntaxFactory.Identifier(type.Name))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
            .WithBody(SyntaxFactory.Block(calls))
            .WithAdditionalAnnotations(Formatter.Annotation);

    private static List<StatementSyntax> GetRequiredInitializerCalls(
        Compilation compilation,
        INamedTypeSymbol type,
        CancellationToken cancellationToken)
    {
        var itemType = compilation.GetTypeByMetadataName(ItemName);
        var initItemAttribute = compilation.GetTypeByMetadataName(AttributeName);
        if (itemType is null || initItemAttribute is null)
            return [];

        var interfaces = GetInitInterfaces(compilation, type, initItemAttribute, cancellationToken);
        var calls = interfaces.Select(interfaceType => CreateInterfaceInitCall(interfaceType, type)).ToList();
        calls.Add(SimplifyNames(SyntaxFactory.ParseStatement(
            $"Init<{type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>();")));
        return calls;
    }

    private static List<StatementSyntax> GetMissingInitializerCalls(
        SemanticModel semanticModel,
        ConstructorDeclarationSyntax constructor,
        INamedTypeSymbol type,
        CancellationToken cancellationToken)
    {
        var calledMethods = constructor.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(invocation => !IsDeferredInvocation(invocation, constructor))
            .Select(invocation => semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol)
            .Where(method => method is not null)
            .Cast<IMethodSymbol>()
            .ToArray();

        var compilation = semanticModel.Compilation;
        var itemType = compilation.GetTypeByMetadataName(ItemName);
        var interfaceAttribute = compilation.GetTypeByMetadataName(AttributeName);
        if (itemType is null || interfaceAttribute is null)
            return [];

        var missing = new List<StatementSyntax>();
        foreach (var interfaceType in GetInitInterfaces(compilation, type, interfaceAttribute, cancellationToken))
        {
            if (!HasInitializerCall(calledMethods, interfaceType, type, "Init"))
                missing.Add(CreateInterfaceInitCall(interfaceType, type));
        }

        if (!HasInitializerCall(calledMethods, itemType, type, "Init"))
            missing.Add(SyntaxFactory.ParseStatement(
                $"Init<{type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>();"));
        return missing;
    }

    private static bool HasInitializerCall(
        IReadOnlyList<IMethodSymbol> calledMethods,
        INamedTypeSymbol containingType,
        INamedTypeSymbol targetType,
        string methodName)
    {
        foreach (var calledMethod in calledMethods)
        {
            if (calledMethod.Name != methodName ||
                !SymbolEqualityComparer.Default.Equals(calledMethod.ContainingType, containingType) ||
                calledMethod.TypeArguments.Length != 1 ||
                !SymbolEqualityComparer.Default.Equals(calledMethod.TypeArguments[0], targetType))
                continue;
            return true;
        }

        return false;
    }

    private static StatementSyntax CreateInterfaceInitCall(
        INamedTypeSymbol interfaceType,
        INamedTypeSymbol targetType)
        => SimplifyNames(SyntaxFactory.ParseStatement(
            $"{interfaceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.Init<{targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>();\n"));

    private static TNode SimplifyNames<TNode>(TNode node)
        where TNode : SyntaxNode
    {
        var names = node.DescendantNodesAndSelf().OfType<NameSyntax>().ToArray();
        return names.Length == 0
            ? node
            : node.ReplaceNodes(
                names,
                (original, _) => original.WithAdditionalAnnotations(Simplifier.Annotation));
    }

    private static List<INamedTypeSymbol> GetInitInterfaces(
        Compilation compilation,
        INamedTypeSymbol type,
        INamedTypeSymbol initItemAttribute,
        CancellationToken cancellationToken)
    {
        var result = new List<INamedTypeSymbol>();
        var visited = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        for (var current = type; current is not null; current = current.BaseType)
        {
            foreach (var reference in current.DeclaringSyntaxReferences
                .OrderBy(reference => reference.SyntaxTree.FilePath, StringComparer.Ordinal)
                .ThenBy(reference => reference.Span.Start))
            {
                if (reference.GetSyntax(cancellationToken) is not TypeDeclarationSyntax declaration ||
                    declaration.BaseList is null)
                    continue;

                var model = compilation.GetSemanticModel(declaration.SyntaxTree);
                foreach (var baseType in declaration.BaseList.Types)
                {
                    if (model.GetTypeInfo(baseType.Type, cancellationToken).Type is INamedTypeSymbol interfaceType &&
                        interfaceType.TypeKind == TypeKind.Interface)
                        AppendInterface(compilation, interfaceType, initItemAttribute, visited, result, cancellationToken);
                }
            }
        }

        foreach (var interfaceType in type.AllInterfaces)
            AppendInterface(compilation, interfaceType, initItemAttribute, visited, result, cancellationToken);

        return result;
    }

    private static void AppendInterface(
        Compilation compilation,
        INamedTypeSymbol interfaceType,
        INamedTypeSymbol initItemAttribute,
        HashSet<INamedTypeSymbol> visited,
        List<INamedTypeSymbol> result,
        CancellationToken cancellationToken)
    {
        if (!visited.Add(interfaceType))
            return;

        if (interfaceType.GetAttributes().Any(attribute =>
                SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, initItemAttribute)))
            result.Add(interfaceType);

        foreach (var reference in interfaceType.DeclaringSyntaxReferences
            .OrderBy(reference => reference.SyntaxTree.FilePath, StringComparer.Ordinal)
            .ThenBy(reference => reference.Span.Start))
        {
            if (reference.GetSyntax(cancellationToken) is not InterfaceDeclarationSyntax declaration ||
                declaration.BaseList is null)
                continue;

            var model = compilation.GetSemanticModel(declaration.SyntaxTree);
            foreach (var baseType in declaration.BaseList.Types)
            {
                if (model.GetTypeInfo(baseType.Type, cancellationToken).Type is INamedTypeSymbol baseInterface &&
                    baseInterface.TypeKind == TypeKind.Interface)
                    AppendInterface(compilation, baseInterface, initItemAttribute, visited, result, cancellationToken);
            }
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
}
