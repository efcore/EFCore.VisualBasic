Imports System.Linq.Expressions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Design.Query.Internal

    ''' <summary>
    '''     Translates a LINQ expression tree to a Roslyn syntax tree.
    ''' </summary>
    ''' <remarks>
    '''     This Is an internal API that supports the Entity Framework Core infrastructure And Not subject to
    '''     the same compatibility standards as public APIs. It may be changed Or removed without notice in
    '''     any release. You should only use it directly in your code with extreme caution And knowing that
    '''     doing so can result in application failures when updating to a New Entity Framework Core release.
    ''' </remarks>
    Public Interface ILinqToVisualBasicSyntaxTranslator

        ''' <summary>
        '''     Translates a node representing a statement into a collection of Roslyn syntax tree.
        ''' </summary>
        ''' <param name="node">The node to be translated.</param>
        ''' <param name="collectedNamespaces">Any namespaces required by the translated code will be added to this set.</param>
        ''' <returns>A collection of Roslyn syntax tree representation of <paramref name="node" />.</returns>
        ''' <remarks>
        '''     This Is an internal API that supports the Entity Framework Core infrastructure And Not subject to
        '''     the same compatibility standards as public APIs. It may be changed Or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution And knowing that
        '''     doing so can result in application failures when updating to a New Entity Framework Core release.
        ''' </remarks>
        Function TranslateStatement(node As Expression, collectedNamespaces As ISet(Of String)) As IReadOnlyList(Of StatementSyntax)

        ''' <summary>
        '''     Translates a node representing an expression into a Roslyn syntax tree.
        ''' </summary>
        ''' <param name="node">The node to be translated.</param>
        ''' <param name="collectedNamespaces">Any namespaces required by the translated code will be added to this set.</param>
        ''' <returns>A Roslyn syntax tree representation of <paramref name="node" />.</returns>
        ''' <remarks>
        '''     This Is an internal API that supports the Entity Framework Core infrastructure And Not subject to
        '''     the same compatibility standards as public APIs. It may be changed Or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution And knowing that
        '''     doing so can result in application failures when updating to a New Entity Framework Core release.
        ''' </remarks>
        Function TranslateExpression(node As Expression, collectedNamespaces As ISet(Of String)) As ExpressionSyntax

        ''' <summary>
        '''     Returns the captured variables detected in the last translation.
        ''' </summary>
        ''' <remarks>
        '''     This Is an internal API that supports the Entity Framework Core infrastructure And Not subject to
        '''     the same compatibility standards as public APIs. It may be changed Or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution And knowing that
        '''     doing so can result in application failures when updating to a New Entity Framework Core release.
        ''' </remarks>
        ReadOnly Property CapturedVariables As IReadOnlySet(Of ParameterExpression)
    End Interface
End Namespace
