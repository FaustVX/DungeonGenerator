using System.Collections.Immutable;
using System.Composition;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace DungeonGenerator.SourceGenerator.CodeFix;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InfoItemCodeFixProvider)), Shared]
public sealed class InfoItemCodeFixProvider : CodeFixProvider
{
    private const string InfoAttributeName = "DungeonGenerator.App.Item.InfoItemAttribute";
    private const string InfoItemName = "DungeonGenerator.App.Item.IInfoItem";
    private const string ItemName = "DungeonGenerator.App.Item.Item";
    private const string InfoInterfaceProperty = "InfoInterface";

    public override ImmutableArray<string> FixableDiagnosticIds
        => ["INFOITEM001", "INFOITEM002", "INFOITEM003"];

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
                case "INFOITEM001":
                    if (node.FirstAncestorOrSelf<InterfaceDeclarationSyntax>() is { } infoInterface)
                    {
                        context.RegisterCodeFix(
                            Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(
                                "Generate generic GetInfo<T>()",
                                cancellationToken => AddInfoMethodAsync(
                                    context.Document,
                                    infoInterface,
                                    generic: true,
                                    cancellationToken),
                                "GenerateGenericGetInfo"),
                            diagnostic);
                        context.RegisterCodeFix(
                            Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(
                                "Generate GetInfo(Item)",
                                cancellationToken => AddInfoMethodAsync(
                                    context.Document,
                                    infoInterface,
                                    generic: false,
                                    cancellationToken),
                                "GenerateNonGenericGetInfo"),
                            diagnostic);
                    }
                    break;

                case "INFOITEM002":
                    if (node.FirstAncestorOrSelf<TypeDeclarationSyntax>() is { } itemDeclaration)
                        context.RegisterCodeFix(
                            Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(
                                "Generate IInfoItem.GetInfo implementation",
                                cancellationToken => AddInfoImplementationAsync(
                                    context.Document,
                                    itemDeclaration,
                                    cancellationToken),
                                "GenerateInfoImplementation"),
                            diagnostic);
                    break;

                case "INFOITEM003":
                    if (node.FirstAncestorOrSelf<MethodDeclarationSyntax>() is { } infoMethod &&
                        diagnostic.Properties.ContainsKey(InfoInterfaceProperty))
                        context.RegisterCodeFix(
                            Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(
                                "Add missing info contribution",
                                cancellationToken => AddInfoContributionAsync(
                                    context.Document,
                                    infoMethod,
                                    cancellationToken),
                                "AddInfoContribution"),
                            diagnostic);
                    break;
            }
        }
    }

    private static async Task<Document> AddInfoMethodAsync(
        Document document,
        InterfaceDeclarationSyntax declaration,
        bool generic,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var interfaceSymbol = semanticModel?.GetDeclaredSymbol(declaration, cancellationToken);
        var itemType = semanticModel?.Compilation.GetTypeByMetadataName(ItemName);
        if (interfaceSymbol is null || itemType is null)
            return document;

        var interfaceName = interfaceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var itemName = itemType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var declarationText = generic
            ? $$"""
                protected static string GetInfo<T>()
                where T : {{interfaceName}}
                => string.Empty;
                """
            : $$"""
                protected static string GetInfo({{itemName}} item)
                => string.Empty;
                """;
        var method = SyntaxFactory.ParseMemberDeclaration(declarationText);
        if (method is null)
            return document;

        var updated = declaration.AddMembers(
            SimplifyNames(method).WithAdditionalAnnotations(Formatter.Annotation));
        return document.WithSyntaxRoot((await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!
            .ReplaceNode(declaration, updated));
    }

    private static async Task<Document> AddInfoImplementationAsync(
        Document document,
        TypeDeclarationSyntax declaration,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var itemClass = semanticModel?.GetDeclaredSymbol(declaration, cancellationToken);
        var compilation = semanticModel?.Compilation;
        var infoItemType = compilation?.GetTypeByMetadataName(InfoItemName);
        var itemType = compilation?.GetTypeByMetadataName(ItemName);
        var infoAttribute = compilation?.GetTypeByMetadataName(InfoAttributeName);
        var infoInterfaceMethod = infoItemType?.GetMembers("GetInfo")
            .OfType<IMethodSymbol>()
            .FirstOrDefault(method => method.IsStatic && method.IsAbstract && method.Parameters.Length == 2);
        if (itemClass is null || compilation is null || infoItemType is null ||
            itemType is null || infoAttribute is null || infoInterfaceMethod is null)
            return document;

        var infoInterfaces = GetInfoInterfaces(compilation, itemClass, infoAttribute, itemType, cancellationToken);
        if (infoInterfaces.Count == 0)
            return document;

        var method = CreateInfoImplementation(itemClass, infoItemType, infoInterfaceMethod, infoInterfaces);
        var updated = declaration.AddMembers(method.WithAdditionalAnnotations(Formatter.Annotation));
        return document.WithSyntaxRoot((await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!
            .ReplaceNode(declaration, updated));
    }

    private static async Task<Document> AddInfoContributionAsync(
        Document document,
        MethodDeclarationSyntax method,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var compilation = semanticModel?.Compilation;
        var itemTypeDeclaration = method.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        var itemClass = itemTypeDeclaration is null
            ? null
            : semanticModel?.GetDeclaredSymbol(itemTypeDeclaration, cancellationToken);
        var implementationType = semanticModel?.GetDeclaredSymbol(method, cancellationToken)?.ContainingType;
        var itemType = compilation?.GetTypeByMetadataName(ItemName);
        var infoAttribute = compilation?.GetTypeByMetadataName(InfoAttributeName);
        if (itemClass is null || itemType is null || infoAttribute is null || compilation is null || semanticModel is null)
            return document;

        var calledMethods = method.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(invocation => !IsDeferredInvocation(invocation, method))
            .Select(invocation => semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol)
            .Where(calledMethod => calledMethod is not null)
            .Cast<IMethodSymbol>()
            .ToArray();
        var missingContributions = GetInfoInterfaces(
                compilation,
                itemClass,
                infoAttribute,
                itemType,
                cancellationToken)
            .Where(infoInterface => !HasInfoContribution(
                calledMethods,
                infoInterface.Value,
                itemClass,
                implementationType ?? itemClass))
            .Select(infoInterface => SyntaxFactory.ParseExpression(
                $"new(game, item, {CreateGetInfoInvocation(infoInterface.Key, infoInterface.Value, itemClass)})"))
            .ToArray();
        if (missingContributions.Length == 0)
            return document;

        var body = method.Body;
        if (method.ExpressionBody?.Expression is CollectionExpressionSyntax expressionCollection)
        {
            var updatedCollection = expressionCollection.WithElements(
                expressionCollection.Elements.AddRange(
                    missingContributions.Select(SyntaxFactory.ExpressionElement)));
            return document.WithSyntaxRoot((await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!
                .ReplaceNode(expressionCollection, updatedCollection.WithAdditionalAnnotations(Formatter.Annotation)));
        }

        var collection = body?.DescendantNodes()
            .OfType<CollectionExpressionSyntax>()
            .FirstOrDefault();
        if (collection is null)
        {
            var initializer = FindReturnedCollectionInitializer(method);
            if (initializer is not null)
            {
                var updatedInitializer = initializer.WithExpressions(
                    initializer.Expressions.AddRange(missingContributions));
                return document.WithSyntaxRoot((await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!
                    .ReplaceNode(initializer, updatedInitializer.WithAdditionalAnnotations(Formatter.Annotation)));
            }

            var returnStatement = SyntaxFactory.ReturnStatement(
                SyntaxFactory.CollectionExpression(SyntaxFactory.SeparatedList(
                    missingContributions.Select(entry =>
                        (CollectionElementSyntax)SyntaxFactory.ExpressionElement(entry)))));
            if (method.ExpressionBody is { Expression: ThrowExpressionSyntax throwExpression } &&
                IsNotImplementedException(semanticModel, throwExpression.Expression, cancellationToken))
            {
                var updatedMethod = method
                    .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(returnStatement.Expression!))
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                    .WithAdditionalAnnotations(Formatter.Annotation);
                return document.WithSyntaxRoot((await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!
                    .ReplaceNode(method, updatedMethod));
            }

            if (body is null ||
                body.Statements.Count > 1 ||
                body.Statements.Count == 1 &&
                (body.Statements[0] is not ThrowStatementSyntax throwStatement ||
                 throwStatement.Expression is null ||
                 !IsNotImplementedException(semanticModel, throwStatement.Expression, cancellationToken)))
                return document;

            var updatedBody = body.WithStatements(
                SyntaxFactory.SingletonList<StatementSyntax>(returnStatement))
                .WithAdditionalAnnotations(Formatter.Annotation);
            return document.WithSyntaxRoot((await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!
                .ReplaceNode(body, updatedBody));
        }

        var updated = collection.WithElements(
            collection.Elements.AddRange(missingContributions.Select(SyntaxFactory.ExpressionElement)));
        return document.WithSyntaxRoot((await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!
            .ReplaceNode(collection, updated.WithAdditionalAnnotations(Formatter.Annotation)));
    }

    private static bool HasInfoContribution(
        IReadOnlyList<IMethodSymbol> calledMethods,
        IMethodSymbol requiredMethod,
        INamedTypeSymbol itemClass,
        INamedTypeSymbol implementationType)
        => calledMethods.Any(calledMethod =>
            SymbolEqualityComparer.Default.Equals(
                calledMethod.OriginalDefinition,
                requiredMethod.OriginalDefinition) &&
            (requiredMethod.Arity == 0 ||
             calledMethod.TypeArguments.Length == 1 &&
             (SymbolEqualityComparer.Default.Equals(calledMethod.TypeArguments[0], itemClass) ||
              SymbolEqualityComparer.Default.Equals(calledMethod.TypeArguments[0], implementationType))));

    private static bool IsNotImplementedException(
        SemanticModel semanticModel,
        ExpressionSyntax expression,
        CancellationToken cancellationToken)
        => semanticModel.GetTypeInfo(expression, cancellationToken).Type?.ToDisplayString() ==
           "System.NotImplementedException";

    private static InitializerExpressionSyntax? FindReturnedCollectionInitializer(MethodDeclarationSyntax method)
    {
        static bool IsCollectionInitializer(InitializerExpressionSyntax initializer)
            => initializer.IsKind(SyntaxKind.CollectionInitializerExpression) ||
               initializer.IsKind(SyntaxKind.ArrayInitializerExpression) &&
               initializer.Parent is ArrayCreationExpressionSyntax or ImplicitArrayCreationExpressionSyntax;

        var expressions = method.ExpressionBody is { } expressionBody
            ? new[] { expressionBody.Expression }
            : method.Body?.DescendantNodes()
                .OfType<ReturnStatementSyntax>()
                .Where(statement => statement.Expression is not null)
                .Select(statement => statement.Expression!)
                .ToArray() ?? [];

        return expressions
            .SelectMany(expression => expression.DescendantNodesAndSelf().OfType<InitializerExpressionSyntax>())
            .FirstOrDefault(IsCollectionInitializer);
    }

    private static MethodDeclarationSyntax CreateInfoImplementation(
        INamedTypeSymbol itemClass,
        INamedTypeSymbol infoItemType,
        IMethodSymbol infoInterfaceMethod,
        IReadOnlyList<KeyValuePair<INamedTypeSymbol, IMethodSymbol>> infoInterfaces)
    {
        var infoItemName = infoItemType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var gameType = infoInterfaceMethod.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var itemType = infoInterfaceMethod.Parameters[1].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var returnType = infoInterfaceMethod.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var itemName = itemClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var source = new StringBuilder()
            .Append("static ").Append(returnType).Append(' ')
            .Append(infoItemName).Append('.').Append(infoInterfaceMethod.Name)
            .Append('(').Append(gameType).Append(" game, ")
            .Append(itemType).AppendLine(" item)")
            .AppendLine("=>")
            .AppendLine("[");

        foreach (var infoInterface in infoInterfaces)
        {
            source.Append("    new(game, item, ")
                .Append(CreateGetInfoInvocation(infoInterface.Key, infoInterface.Value, itemClass))
                .AppendLine("),");
        }

        source.Append("];");
        return (MethodDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration(source.ToString())!;
    }

    private static IReadOnlyList<KeyValuePair<INamedTypeSymbol, IMethodSymbol>> GetInfoInterfaces(
        Compilation compilation,
        INamedTypeSymbol itemClass,
        INamedTypeSymbol infoAttribute,
        INamedTypeSymbol itemType,
        CancellationToken cancellationToken)
    {
        var result = new List<KeyValuePair<INamedTypeSymbol, IMethodSymbol>>();
        var visited = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        for (var current = itemClass; current is not null; current = current.BaseType)
        {
            foreach (var reference in current.DeclaringSyntaxReferences
                .OrderBy(reference => reference.SyntaxTree.FilePath, StringComparer.Ordinal)
                .ThenBy(reference => reference.Span.Start))
            {
                if (reference.GetSyntax(cancellationToken) is not TypeDeclarationSyntax declaration ||
                    declaration.BaseList is null)
                    continue;

                var semanticModel = compilation.GetSemanticModel(declaration.SyntaxTree);
                foreach (var baseType in declaration.BaseList.Types)
                {
                    if (semanticModel.GetTypeInfo(baseType.Type, cancellationToken).Type is INamedTypeSymbol interfaceType &&
                        interfaceType.TypeKind == TypeKind.Interface)
                        AppendInfoInterface(compilation, interfaceType, infoAttribute, itemType, visited, result, cancellationToken);
                }
            }
        }

        foreach (var interfaceType in itemClass.AllInterfaces)
            AppendInfoInterface(compilation, interfaceType, infoAttribute, itemType, visited, result, cancellationToken);

        return result;
    }

    private static void AppendInfoInterface(
        Compilation compilation,
        INamedTypeSymbol interfaceType,
        INamedTypeSymbol infoAttribute,
        INamedTypeSymbol itemType,
        HashSet<INamedTypeSymbol> visited,
        List<KeyValuePair<INamedTypeSymbol, IMethodSymbol>> result,
        CancellationToken cancellationToken)
    {
        if (!visited.Add(interfaceType))
            return;

        var isInfoInterface = interfaceType.GetAttributes().Any(attribute =>
            SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, infoAttribute));
        if (isInfoInterface && FindInfoMethod(interfaceType, itemType) is { } method)
            result.Add(new KeyValuePair<INamedTypeSymbol, IMethodSymbol>(interfaceType, method));

        foreach (var reference in interfaceType.DeclaringSyntaxReferences
            .OrderBy(reference => reference.SyntaxTree.FilePath, StringComparer.Ordinal)
            .ThenBy(reference => reference.Span.Start))
        {
            if (reference.GetSyntax(cancellationToken) is not InterfaceDeclarationSyntax declaration ||
                declaration.BaseList is null)
                continue;

            var semanticModel = compilation.GetSemanticModel(declaration.SyntaxTree);
            foreach (var baseType in declaration.BaseList.Types)
            {
                if (semanticModel.GetTypeInfo(baseType.Type, cancellationToken).Type is INamedTypeSymbol baseInterface &&
                    baseInterface.TypeKind == TypeKind.Interface)
                    AppendInfoInterface(compilation, baseInterface, infoAttribute, itemType, visited, result, cancellationToken);
            }
        }
    }

    private static IMethodSymbol? FindInfoMethod(INamedTypeSymbol interfaceType, INamedTypeSymbol itemType)
        => interfaceType.GetMembers("GetInfo")
            .OfType<IMethodSymbol>()
            .FirstOrDefault(method =>
                method.IsStatic &&
                !method.IsAbstract &&
                (method.DeclaredAccessibility is Accessibility.Public or
                    Accessibility.Protected or
                    Accessibility.Internal or
                    Accessibility.ProtectedOrInternal) &&
                method.ReturnType.SpecialType == SpecialType.System_String &&
                ((method.Arity == 0 &&
                  method.Parameters.Length == 1 &&
                  SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, itemType)) ||
                 (method.Arity == 1 && method.Parameters.Length == 0)));

    private static string CreateGetInfoInvocation(
        INamedTypeSymbol interfaceType,
        IMethodSymbol method,
        INamedTypeSymbol itemClass)
    {
        var interfaceName = interfaceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return method.Arity == 1
            ? interfaceName + "." + method.Name + "<" +
              itemClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ">()"
            : interfaceName + "." + method.Name + "(item)";
    }

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
}
