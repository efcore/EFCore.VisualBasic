Imports System.Globalization
Imports System.Linq.Expressions
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Text
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.EntityFrameworkCore.Query
Imports E = System.Linq.Expressions.Expression
Imports SF = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory

Namespace Design.Query.Internal

    ''' <summary>
    '''     This Is an internal API that supports the Entity Framework Core infrastructure And Not subject to
    '''     the same compatibility standards as public APIs. It may be changed Or removed without notice in
    '''     any release. You should only use it directly in your code with extreme caution And knowing that
    '''     doing so can result in application failures when updating to a New Entity Framework Core release.
    ''' </summary>
    Public Class LinqToVisualBasicSyntaxTranslator
        Inherits ExpressionVisitor
        Implements ILinqToVisualBasicSyntaxTranslator

        Private NotInheritable Class StackFrame
            ReadOnly Property Variables As Dictionary(Of ParameterExpression, String)
            ReadOnly Property VariableNames As HashSet(Of String)

            Sub New(variables As Dictionary(Of ParameterExpression, String),
                    variableNames As HashSet(Of String))

                Me.Variables = variables
                Me.VariableNames = variableNames
            End Sub
        End Class

        Private ReadOnly _stack As New Stack(Of StackFrame)({New StackFrame(New Dictionary(Of ParameterExpression, String),
                                                                            New HashSet(Of String)(StringComparer.OrdinalIgnoreCase))})

        Private _labels As Dictionary(Of LabelTarget, String)
        Private _unnamedLabelNames As HashSet(Of String)

        Private _unnamedParameterCounter As Integer

        Private NotInheritable Class LiftedState
            ReadOnly Property Statements As List(Of StatementSyntax)
            ReadOnly Property Variables As Dictionary(Of ParameterExpression, String)
            ReadOnly Property VariableNames As HashSet(Of String)
            ReadOnly Property UnassignedVariableDeclarations As List(Of LocalDeclarationStatementSyntax)

            Sub New(statements As List(Of StatementSyntax),
                    variables As Dictionary(Of ParameterExpression, String),
                    variableNames As HashSet(Of String),
                    unassignedVariableDeclarations As List(Of LocalDeclarationStatementSyntax))

                Me.Statements = statements
                Me.Variables = variables
                Me.VariableNames = variableNames
                Me.UnassignedVariableDeclarations = unassignedVariableDeclarations
            End Sub

            Public Shared Function CreateEmpty() As LiftedState
                Return New LiftedState(New List(Of StatementSyntax),
                                       New Dictionary(Of ParameterExpression, String),
                                       New HashSet(Of String)(StringComparer.OrdinalIgnoreCase),
                                       New List(Of LocalDeclarationStatementSyntax))
            End Function
        End Class

        Private _liftedState As LiftedState = LiftedState.CreateEmpty()

        Private _context As ExpressionContext
        Private _onLastLambdaLine As Boolean

        Private ReadOnly _capturedVariables As New HashSet(Of ParameterExpression)
        Private _collectedNamespaces As ISet(Of String)

        Private Shared _activatorCreateInstanceMethod As MethodInfo
        Private Shared _typeGetFieldMethod As MethodInfo
        Private Shared _fieldGetValueMethod As MethodInfo

        Private ReadOnly _sideEffectDetector As New SideEffectDetectionSyntaxWalker
        Private ReadOnly _g As SyntaxGenerator

        ''' <summary>
        '''     This Is an internal API that supports the Entity Framework Core infrastructure And Not subject to
        '''     the same compatibility standards as public APIs. It may be changed Or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution And knowing that
        '''     doing so can result in application failures when updating to a New Entity Framework Core release.
        ''' </summary>
        Public Sub New(syntaxGenerator As SyntaxGenerator)
            _g = syntaxGenerator
        End Sub

        ''' <summary>
        '''     This Is an internal API that supports the Entity Framework Core infrastructure And Not subject to
        '''     the same compatibility standards as public APIs. It may be changed Or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution And knowing that
        '''     doing so can result in application failures when updating to a New Entity Framework Core release.
        ''' </summary>
        Public ReadOnly Property CapturedVariables As IReadOnlySet(Of ParameterExpression) Implements ILinqToVisualBasicSyntaxTranslator.CapturedVariables
            Get
                Return _capturedVariables.ToHashSet()
            End Get
        End Property

        ''' <summary>
        '''     This Is an internal API that supports the Entity Framework Core infrastructure And Not subject to
        '''     the same compatibility standards as public APIs. It may be changed Or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution And knowing that
        '''     doing so can result in application failures when updating to a New Entity Framework Core release.
        ''' </summary>        ''' 
        Protected Overridable Property Result As New GeneratedSyntaxNodes

        ''' <summary>
        '''     This Is an internal API that supports the Entity Framework Core infrastructure And Not subject to
        '''     the same compatibility standards as public APIs. It may be changed Or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution And knowing that
        '''     doing so can result in application failures when updating to a New Entity Framework Core release.
        ''' </summary>
        Public Overridable Function TranslateStatement(node As Expression, collectedNamespaces As ISet(Of String)) As IReadOnlyList(Of StatementSyntax) _
        Implements ILinqToVisualBasicSyntaxTranslator.TranslateStatement
            Return ResultAsStatementSyntaxList(TranslateCore(node, collectedNamespaces, statementContext:=True))
        End Function

        ''' <summary>
        '''     This Is an internal API that supports the Entity Framework Core infrastructure And Not subject to
        '''     the same compatibility standards as public APIs. It may be changed Or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution And knowing that
        '''     doing so can result in application failures when updating to a New Entity Framework Core release.
        ''' </summary>
        Public Overridable Function TranslateExpression(node As Expression, collectedNamespaces As ISet(Of String)) As ExpressionSyntax _
        Implements ILinqToVisualBasicSyntaxTranslator.TranslateExpression
            Return TranslateCore(node, collectedNamespaces, statementContext:=False).GetExpression
        End Function

        ''' <summary>
        '''     This Is an internal API that supports the Entity Framework Core infrastructure And Not subject to
        '''     the same compatibility standards as public APIs. It may be changed Or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution And knowing that
        '''     doing so can result in application failures when updating to a New Entity Framework Core release.
        ''' </summary>
        Protected Overridable Function TranslateCore(node As Expression,
                                                     collectedNamespaces As ISet(Of String),
                                                     Optional statementContext As Boolean = False) As GeneratedSyntaxNodes

            _labels = New Dictionary(Of LabelTarget, String)
            _unnamedLabelNames = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            _capturedVariables.Clear()
            _collectedNamespaces = collectedNamespaces
            _unnamedParameterCounter = 0
            _context = If(statementContext, ExpressionContext.Statement, ExpressionContext.Expression)
            _onLastLambdaLine = True

            Visit(node)

            If _liftedState.Statements.Count > 0 Then
                If _context = ExpressionContext.Expression Then
                    Throw New NotSupportedException("Lifted expressions remaining at top-level in expression context")
                End If
            End If

            DebugAssert(_stack.Count = 1, "_parameterStack.Count = 1")
            DebugAssert(_stack.Peek().Variables.Count = 0, "_stack.Peek().Parameters.Count = 0")
            DebugAssert(_stack.Peek().VariableNames.Count = 0, "_stack.Peek().ParameterNames.Count = 0")

            Return Result
        End Function

        ''' <summary>
        '''     This Is an internal API that supports the Entity Framework Core infrastructure And Not subject to
        '''     the same compatibility standards as public APIs. It may be changed Or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution And knowing that
        '''     doing so can result in application failures when updating to a New Entity Framework Core release.
        ''' </summary>
        Protected Overridable Function Translate(node As Expression) As GeneratedSyntaxNodes
            Visit(node)
            Return Result
        End Function

        ''' <summary>
        '''     This Is an internal API that supports the Entity Framework Core infrastructure And Not subject to
        '''     the same compatibility standards as public APIs. It may be changed Or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution And knowing that
        '''     doing so can result in application failures when updating to a New Entity Framework Core release.
        ''' </summary>
        Protected Overridable Function Translate(Of T As VisualBasicSyntaxNode)(node As Expression) As T
            Visit(node)

            Dim x = TryCast(Result.Node, T)

            If x Is Nothing Then
                Throw New InvalidOperationException($"Got translated node of type '{If(Result?.GetType().Name, "Nothing")}' instead of the expected {GetType(T)}.")
            End If

            Return x
        End Function

        ''' <summary>
        '''     This Is an internal API that supports the Entity Framework Core infrastructure And Not subject to
        '''     the same compatibility standards as public APIs. It may be changed Or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution And knowing that
        '''     doing so can result in application failures when updating to a New Entity Framework Core release.
        ''' </summary>
        Protected Overridable Function Translate(expression As Expression, lowerableAssignmentVariable As IdentifierNameSyntax) As ExpressionSyntax
            DebugAssert(_context = ExpressionContext.Expression OrElse _context = ExpressionContext.ExpressionLambda,
                        "Cannot lower in statement context")

            Dim switchExpression = TryCast(expression, SwitchExpression)
            If switchExpression IsNot Nothing Then Return DirectCast(TranslateSwitch(switchExpression, lowerableAssignmentVariable).Node, ExpressionSyntax)

            Dim conditionalExpression = TryCast(expression, ConditionalExpression)
            If conditionalExpression IsNot Nothing Then Return DirectCast(TranslateConditional(conditionalExpression, lowerableAssignmentVariable).Node, ExpressionSyntax)

            Return Translate(Of ExpressionSyntax)(expression)
        End Function

        ''' <summary>
        '''     This Is an internal API that supports the Entity Framework Core infrastructure And Not subject to
        '''     the same compatibility standards as public APIs. It may be changed Or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution And knowing that
        '''     doing so can result in application failures when updating to a New Entity Framework Core release.
        ''' </summary>
        Public Overrides Function Visit(node As Expression) As Expression
            If node Is Nothing Then
                Result = Nothing
                Return Nothing
            End If

            Return MyBase.Visit(node)
        End Function

        ''' <inheritdoc/>
        Protected Overrides Function VisitBinary(binary As BinaryExpression) As Expression
            Using ChangeContext(ExpressionContext.Expression)

                ' Handle special cases
                Select Case binary.NodeType
                    Case ExpressionType.Assign
                        Return VisitAssignment(binary)

                    Case ExpressionType.ModuloAssign
                        Return VisitAssignment(
                            E.Assign(
                                binary.Left,
                                E.Modulo(binary.Left, binary.Right)))

                    Case ExpressionType.AndAssign
                        Return VisitAssignment(
                            E.Assign(
                                binary.Left,
                                E.And(binary.Left, binary.Right)))

                    Case ExpressionType.OrAssign
                        Return VisitAssignment(
                            E.Assign(
                                binary.Left,
                                E.Or(binary.Left, binary.Right)))

                    Case ExpressionType.ExclusiveOrAssign
                        Return VisitAssignment(
                            E.Assign(
                                binary.Left,
                                E.ExclusiveOr(binary.Left, binary.Right)))

                    Case ExpressionType.Coalesce
                        Return VisitCoalesce(binary)

                End Select

                Dim liftedStatementOrigPosition = _liftedState.Statements.Count
                Dim left = Translate(Of ExpressionSyntax)(binary.Left)
                Dim liftedStatementLeftPosition = _liftedState.Statements.Count
                Dim right = Translate(Of ExpressionSyntax)(binary.Right)

                ' If both sides were lifted, we don't need to do anything special. Same if the left side was lifted.
                ' But if the right side was lifted And the left wasn't, then in order to preserve evaluation order we need to lift the left side
                ' out as well, otherwise the right side gets evaluated before the left.
                ' We refrain from doing this only if the two expressions can't possibly have side effects over each other, for nicer code.
                If _liftedState.Statements.Count > liftedStatementLeftPosition AndAlso
                    liftedStatementLeftPosition = liftedStatementOrigPosition AndAlso
                    Not _sideEffectDetector.CanBeReordered(left, right) Then

                    Dim name = UniquifyVariableName("lifted")
                    _liftedState.Statements.Insert(
                        liftedStatementLeftPosition,
                        GenerateDeclaration(name, left))
                    _liftedState.VariableNames.Add(name)
                    left = SF.IdentifierName(name)
                End If

                If binary.NodeType = ExpressionType.ArrayIndex Then
                    Result = New GeneratedSyntaxNodes(
                                SF.InvocationExpression(
                                    left,
                                    SF.ArgumentList(SF.SingletonSeparatedList(Of ArgumentSyntax)(SF.SimpleArgument(right)))))
                    Return binary
                End If

                Select Case binary.NodeType
                    Case ExpressionType.Equal
                        If Not IsReferenceEqualitySemantics(binary.Left, binary.Right) AndAlso
                           (binary.Method?.Name = "op_Equality" OrElse binary.Left.Type.IsValueType) Then
                            Result = New GeneratedSyntaxNodes(
                                        SF.EqualsExpression(left, right))
                        Else
                            Result = New GeneratedSyntaxNodes(
                                        SF.IsExpression(left, right))
                        End If

                        Return binary
                    Case ExpressionType.NotEqual
                        If Not IsReferenceEqualitySemantics(binary.Left, binary.Right) AndAlso
                           (binary.Method?.Name = "op_Inequality" OrElse binary.Left.Type.IsValueType) Then
                            Result = New GeneratedSyntaxNodes(
                                        SF.NotEqualsExpression(left, right))
                        Else
                            Result = New GeneratedSyntaxNodes(
                                        SF.IsNotExpression(left, right))
                        End If

                        Return binary
                End Select

                ' TODO: Confirm what to do with the unchecked expression types

                Dim sKind As SyntaxKind
                Dim operatorTokenKind As SyntaxKind
                Dim isAnAssignmentStatement = False

                Select Case binary.NodeType
                    Case ExpressionType.Add
                        sKind = SyntaxKind.AddExpression
                        operatorTokenKind = SyntaxKind.PlusToken

                    Case ExpressionType.AddChecked
                        sKind = SyntaxKind.AddExpression
                        operatorTokenKind = SyntaxKind.PlusToken

                    Case ExpressionType.Subtract
                        sKind = SyntaxKind.SubtractExpression
                        operatorTokenKind = SyntaxKind.MinusToken

                    Case ExpressionType.SubtractChecked
                        sKind = SyntaxKind.SubtractExpression
                        operatorTokenKind = SyntaxKind.MinusToken

                    Case ExpressionType.Multiply
                        sKind = SyntaxKind.MultiplyExpression
                        operatorTokenKind = SyntaxKind.AsteriskToken

                    Case ExpressionType.MultiplyChecked
                        sKind = SyntaxKind.MultiplyExpression
                        operatorTokenKind = SyntaxKind.AsteriskToken

                    Case ExpressionType.Divide
                        sKind = SyntaxKind.DivideExpression
                        operatorTokenKind = SyntaxKind.SlashToken

                    Case ExpressionType.Modulo
                        sKind = SyntaxKind.ModuloExpression
                        operatorTokenKind = SyntaxKind.ModKeyword

                    Case ExpressionType.Power
                        sKind = SyntaxKind.ExponentiateExpression
                        operatorTokenKind = SyntaxKind.CaretToken

                    Case ExpressionType.AddAssign
                        sKind = SyntaxKind.AddAssignmentStatement
                        operatorTokenKind = SyntaxKind.PlusEqualsToken
                        isAnAssignmentStatement = True

                    Case ExpressionType.AddAssignChecked
                        sKind = SyntaxKind.AddAssignmentStatement
                        operatorTokenKind = SyntaxKind.PlusEqualsToken
                        isAnAssignmentStatement = True

                    Case ExpressionType.SubtractAssign
                        sKind = SyntaxKind.SubtractAssignmentStatement
                        operatorTokenKind = SyntaxKind.MinusEqualsToken
                        isAnAssignmentStatement = True

                    Case ExpressionType.SubtractAssignChecked
                        sKind = SyntaxKind.SubtractAssignmentStatement
                        operatorTokenKind = SyntaxKind.MinusEqualsToken
                        isAnAssignmentStatement = True

                    Case ExpressionType.MultiplyAssign
                        sKind = SyntaxKind.MultiplyAssignmentStatement
                        operatorTokenKind = SyntaxKind.AsteriskEqualsToken
                        isAnAssignmentStatement = True

                    Case ExpressionType.MultiplyAssignChecked
                        sKind = SyntaxKind.MultiplyAssignmentStatement
                        operatorTokenKind = SyntaxKind.AsteriskEqualsToken
                        isAnAssignmentStatement = True

                    Case ExpressionType.DivideAssign
                        sKind = SyntaxKind.DivideAssignmentStatement
                        operatorTokenKind = SyntaxKind.SlashEqualsToken
                        isAnAssignmentStatement = True

                    Case ExpressionType.PowerAssign
                        sKind = SyntaxKind.ExponentiateAssignmentStatement
                        operatorTokenKind = SyntaxKind.CaretEqualsToken
                        isAnAssignmentStatement = True

                    Case ExpressionType.GreaterThan
                        sKind = SyntaxKind.GreaterThanExpression
                        operatorTokenKind = SyntaxKind.GreaterThanToken

                    Case ExpressionType.GreaterThanOrEqual
                        sKind = SyntaxKind.GreaterThanOrEqualExpression
                        operatorTokenKind = SyntaxKind.GreaterThanEqualsToken

                    Case ExpressionType.LessThan
                        sKind = SyntaxKind.LessThanExpression
                        operatorTokenKind = SyntaxKind.LessThanToken

                    Case ExpressionType.LessThanOrEqual
                        sKind = SyntaxKind.LessThanOrEqualExpression
                        operatorTokenKind = SyntaxKind.LessThanEqualsToken

                    Case ExpressionType.AndAlso
                        sKind = SyntaxKind.AndAlsoExpression
                        operatorTokenKind = SyntaxKind.AndAlsoKeyword

                    Case ExpressionType.OrElse
                        sKind = SyntaxKind.OrElseExpression
                        operatorTokenKind = SyntaxKind.OrElseKeyword

                    Case ExpressionType.And
                        sKind = SyntaxKind.AndExpression
                        operatorTokenKind = SyntaxKind.AndKeyword

                    Case ExpressionType.Or
                        sKind = SyntaxKind.OrExpression
                        operatorTokenKind = SyntaxKind.OrKeyword

                    Case ExpressionType.ExclusiveOr
                        sKind = SyntaxKind.ExclusiveOrExpression
                        operatorTokenKind = SyntaxKind.XorKeyword

                    Case ExpressionType.LeftShift
                        sKind = SyntaxKind.LeftShiftExpression
                        operatorTokenKind = SyntaxKind.LessThanLessThanToken

                    Case ExpressionType.RightShift
                        sKind = SyntaxKind.RightShiftExpression
                        operatorTokenKind = SyntaxKind.GreaterThanGreaterThanToken

                    Case ExpressionType.LeftShiftAssign
                        sKind = SyntaxKind.LeftShiftAssignmentStatement
                        operatorTokenKind = SyntaxKind.LessThanLessThanEqualsToken
                        isAnAssignmentStatement = True

                    Case ExpressionType.RightShiftAssign
                        sKind = SyntaxKind.RightShiftAssignmentStatement
                        operatorTokenKind = SyntaxKind.GreaterThanGreaterThanEqualsToken
                        isAnAssignmentStatement = True

                    Case Else
                        Throw New ArgumentOutOfRangeException("BinaryExpression with " & binary.NodeType)
                End Select

                If isAnAssignmentStatement Then
                    Result = New GeneratedSyntaxNodes(
                                SF.AssignmentStatement(sKind, left, SF.Token(operatorTokenKind), right))
                Else
                    Result = New GeneratedSyntaxNodes(
                                SF.BinaryExpression(sKind, left, SF.Token(operatorTokenKind), right))
                End If

                Return binary
            End Using
        End Function

        Private Function VisitAssignment(assignment As BinaryExpression) As Expression

            Dim translatedLeft = Translate(Of ExpressionSyntax)(assignment.Left)

            Dim translatedRight As ExpressionSyntax

            ' LINQ expression trees can directly access private members, but VB code cannot.
            ' If a private member Is being set, VisitMember generated a reflection GetValue invocation for it; detect
            ' that here And replace it with SetValue instead.
            ' TODO: Replace this With a more efficient API For .NET 8.0.
            ' TODO: Private Property

            Dim isPrivateMember = False

            Dim invocationExpressionSyntax = TryCast(translatedLeft, InvocationExpressionSyntax)
            If invocationExpressionSyntax IsNot Nothing AndAlso
               invocationExpressionSyntax.Expression IsNot Nothing Then

                Dim fieldInfoExpression As ExpressionSyntax = Nothing

                Dim memberAccessExpressionSyntax = TryCast(invocationExpressionSyntax.Expression, MemberAccessExpressionSyntax)
                If memberAccessExpressionSyntax IsNot Nothing AndAlso
                   memberAccessExpressionSyntax.Name.Identifier.Text = NameOf(FieldInfo.GetValue) Then

                    fieldInfoExpression = memberAccessExpressionSyntax.Expression
                    isPrivateMember = True
                    Dim lValue = invocationExpressionSyntax.ArgumentList?.Arguments(0)

                    translatedRight = Translate(Of ExpressionSyntax)(assignment.Right)

                    Result = New GeneratedSyntaxNodes(
                                SF.InvocationExpression(
                                    SF.SimpleMemberAccessExpression(
                                        fieldInfoExpression,
                                        SF.IdentifierName(NameOf(FieldInfo.SetValue))),
                                    SF.ArgumentList(
                                        SF.SeparatedList({lValue, SF.SimpleArgument(translatedRight)}))))
                End If
            End If

            If Not isPrivateMember Then
                ' Identify assignment where the RHS supports assignment lowering (switch, conditional). If the e.g. switch expression Is
                ' lifted out (because some arm contains a block), this will lower the variable to be assigned inside the resulting switch
                ' statement, rather then adding another useless temporary variable.
                translatedRight = Translate(assignment.Right,
                                            lowerableAssignmentVariable:=TryCast(translatedLeft, IdentifierNameSyntax))

                ' If the RHS was lifted out And the assignment lowering succeeded, Translate above returns the lowered assignment variable;
                ' this would mean that we return a useless identity assignment (i = i). Instead, just return it.
                If translatedRight Is translatedLeft Then
                    Result = New GeneratedSyntaxNodes(translatedRight)
                Else
                    Result = New GeneratedSyntaxNodes(
                                SF.SimpleAssignmentStatement(translatedLeft, translatedRight))
                End If
            End If

            Return assignment
        End Function

        Private Function VisitCoalesce(coalesxeExpr As BinaryExpression) As Expression

            If coalesxeExpr.Conversion IsNot Nothing Then Throw New NotSupportedException("Coalesce BinaryExpression with a conversion lambda.")

            Dim translatedLeft = Translate(Of ExpressionSyntax)(coalesxeExpr.Left)
            Dim translatedRight = Translate(Of ExpressionSyntax)(coalesxeExpr.Right)

            Result = New GeneratedSyntaxNodes(
                        SF.BinaryConditionalExpression(translatedLeft, translatedRight))

            Return coalesxeExpr
        End Function

        ''' <inheritdoc/>
        Protected Overrides Function VisitBlock(block As BlockExpression) As Expression
            Dim blockContext = _context

            Dim parentOnLastLambdaLine = _onLastLambdaLine
            Dim parentLiftedState = _liftedState

            ' Expression blocks have no stack of their own, since they're lifted directly to their parent non-expression block.
            Dim ownStackFrame As StackFrame = Nothing
            If blockContext <> ExpressionContext.Expression Then
                ownStackFrame = PushNewStackFrame()
                _liftedState = LiftedState.CreateEmpty()
            End If

            Dim stackFrame = _stack.Peek()

            ' Do a 1st pass to identify And register any labels, since GoTo can appear before its label.
            PreprocessLabels(block)

            Try
                ' Go over the block's variables, assign names to any unnamed ones and uniquify. Then add them to our stack frame, unless
                ' this Is an expression block that will get lifted.

                For Each parameterExpr In block.Variables
                    Dim variables = stackFrame.Variables
                    Dim variableNames = stackFrame.VariableNames

                    Dim uniquifiedName = UniquifyVariableName(If(parameterExpr.Name, "unnamed"))

                    If blockContext = ExpressionContext.Expression Then
                        _liftedState.Variables.Add(parameterExpr, uniquifiedName)
                        _liftedState.VariableNames.Add(uniquifiedName)
                    Else
                        variables.Add(parameterExpr, uniquifiedName)
                        variableNames.Add(uniquifiedName)
                    End If
                Next

                Dim unassignedVariables = block.Variables.ToList()

                Dim statements As New List(Of SyntaxNode)()

                ' Now visit the block's expressions
                For i = 0 To block.Expressions.Count - 1
                    Dim expression = block.Expressions(i)
                    Dim onLastBlockLine = i = block.Expressions.Count - 1
                    _onLastLambdaLine = parentOnLastLambdaLine AndAlso onLastBlockLine

                    ' Any lines before the last are evaluated in statement context (they aren't returned); the last line is evaluated in the
                    ' context of the block as a whole. _context now refers to the statement's context, blockContext to the block's.
                    Dim statementContext = If(onLastBlockLine, _context, ExpressionContext.Statement)

                    Dim translated As GeneratedSyntaxNodes
                    Using ChangeContext(statementContext)
                        translated = Translate(expression)
                    End Using

                    ' Syntax optimization. This is an assignment of a block variable to some value. Render this as
                    ' Dim x As <Type> = <expression>
                    ' ... instead of:
                    ' Dim x As <Type>
                    ' x = <expression>
                    ' ... except for expression context (i.e. on the last line), where we just return the value if needed.
                    Dim binaryExpr = TryCast(expression, BinaryExpression)
                    Dim lValue = TryCast(binaryExpr?.Left, ParameterExpression)
                    Dim AssignmentExpr As AssignmentStatementSyntax = Nothing

                    If translated.Count = 1 Then
                        AssignmentExpr = TryCast(translated.Node, AssignmentStatementSyntax)
                    End If

                    If binaryExpr IsNot Nothing AndAlso binaryExpr.NodeType = ExpressionType.Assign AndAlso
                       lValue IsNot Nothing AndAlso
                       AssignmentExpr IsNot Nothing AndAlso
                       statementContext = ExpressionContext.Statement AndAlso
                       unassignedVariables.Remove(lValue) Then

                        Dim valueSyntax = AssignmentExpr.Right

                        translated = New GeneratedSyntaxNodes(
                            _g.LocalDeclarationStatement(Translate(lValue.Type), LookupVariableName(lValue), valueSyntax))
                    End If

                    If statementContext = ExpressionContext.Expression Then
                        ' We're on the last line of a block in expression context - the block is being lifted out.
                        ' All statements before the last line (this one) have already been added to _liftedStatements, just return the last
                        ' expression.
                        DebugAssert(onLastBlockLine, "onLastBlockLine")
                        Result = translated
                        Exit For
                    End If

                    If blockContext <> ExpressionContext.Expression Then
                        If _liftedState.Statements.Count > 0 Then
                            ' If any expressions were lifted out of the current expression, flatten them into our own block, just before the
                            ' expression from which they were lifted. Note that we don't do this in Expression context, since our own block is
                            ' lifted out.
                            statements.AddRange(_liftedState.Statements)
                            _liftedState.Statements.Clear()
                        End If

                        ' Same for any variables being lifted out of the block; we add them to our own stack frame so that we can do proper
                        ' variable name uniquification etc.
                        If _liftedState.Variables.Count > 0 Then
                            For Each kv In _liftedState.Variables
                                Dim param = kv.Key
                                Dim name = kv.Value
                                stackFrame.Variables(param) = name
                                stackFrame.VariableNames.Add(name)
                            Next

                            _liftedState.Variables.Clear()
                        End If
                    End If

                    ' Skip useless expressions with no side effects in statement context (these can be the result of switch/conditional lifting
                    ' with assignment lowering)
                    If statementContext = ExpressionContext.Statement AndAlso Not _sideEffectDetector.MayHaveSideEffects(translated.Nodes) Then
                        Continue For
                    End If

                    Dim statementsBlock As SyntaxList(Of StatementSyntax)

                    If translated.Count > 0 AndAlso Not translated.IsASingleExpression Then
                        statementsBlock = ResultAsStatementSyntaxList(translated)

                    ElseIf Not translated.IsASingleExpression Then
                        Throw New ArgumentOutOfRangeException()

                    ElseIf _onLastLambdaLine AndAlso
                           statementContext = ExpressionContext.ExpressionLambda Then
                        ' If this is the last line in an expression lambda, wrap it in a return statement.
                        statementsBlock = SF.List(Of StatementSyntax)(
                                              {SF.ReturnStatement(DirectCast(translated.Node, ExpressionSyntax))})
                    Else
                        statementsBlock = SF.List(ResultAsStatementSyntaxList(translated))
                    End If

                    If blockContext = ExpressionContext.Expression Then
                        ' This block Is in expression context, And so will be lifted (we won't be returning a block).
                        _liftedState.Statements.AddRange(statementsBlock)
                    Else
                        statements.AddRange(statementsBlock)
                    End If
                Next

                ' Above we transform top-level assignments (i = 8) to declarations with initializers (Dim i = 8) those variables have
                ' already been taken care of and removed from the list.
                ' But there may still be variables that get assigned inside nested blocks Or other situations; prepare declarations for those
                ' And either add them to the block, Or lift them if we're an expression block.
                Dim unassignedVariableDeclarations =
                    unassignedVariables.Select(
                        Function(v) DirectCast(_g.LocalDeclarationStatement(Translate(v.Type), LookupVariableName(v)), LocalDeclarationStatementSyntax))

                If blockContext = ExpressionContext.Expression Then
                    _liftedState.UnassignedVariableDeclarations.AddRange(unassignedVariableDeclarations)
                Else
                    statements.InsertRange(0, unassignedVariableDeclarations.Concat(_liftedState.UnassignedVariableDeclarations))
                    _liftedState.UnassignedVariableDeclarations.Clear()

                    ' We're done. If the block is in an expression context, it needs to be lifted out; but not if it's in a lambda (in that
                    ' case we just added return above).
                    Result = New GeneratedSyntaxNodes(statements)
                End If

                Return block
            Finally
                _onLastLambdaLine = parentOnLastLambdaLine
                _liftedState = parentLiftedState

                If ownStackFrame IsNot Nothing Then
                    Dim popped = _stack.Pop()
                    DebugAssert(popped.Equals(ownStackFrame), "popped.Equals(ownStackFrame)")
                End If
            End Try
        End Function

        Private Sub PreprocessLabels(block As BlockExpression)

            ' LINQ label targets can be unnamed, so we need to generate names for unnamed ones and maintain a target->name mapping.
            For Each labelExpr In block.Expressions.OfType(Of LabelExpression)()

                Dim identifier As String = Nothing
                If _labels.TryGetValue(labelExpr.Target, identifier) Then
                    Continue For
                End If

                Dim labels = _labels
                Dim unnamedLabelNames = _unnamedLabelNames

                ' Generate names for unnamed label targets And uniquify
                identifier = If(labelExpr.Target.Name, "unnamedLabel")
                Dim identifierBase = identifier

                Dim i = 0
                While unnamedLabelNames.Contains(identifier)
                    identifier = identifierBase & i
                    i += 1
                End While

                If labelExpr.Target.Name Is Nothing Then
                    unnamedLabelNames.Add(identifier)
                End If

                labels.Add(labelExpr.Target, identifier)
            Next
        End Sub

        ''' <inheritdoc />
        Protected Overrides Function VisitCatchBlock(catchBlock As CatchBlock) As CatchBlock
            Result = New GeneratedSyntaxNodes(TranslateCatchBlock(catchBlock))
            Return catchBlock
        End Function

        ''' <summary>
        '''     This Is an internal API that supports the Entity Framework Core infrastructure And Not subject to
        '''     the same compatibility standards as public APIs. It may be changed Or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution And knowing that
        '''     doing so can result in application failures when updating to a New Entity Framework Core release.
        ''' </summary>
        Protected Overridable Function TranslateCatchBlock(catchBlock As CatchBlock, Optional noType As Boolean = False) As SyntaxNode
            Dim translatedBody = Translate(catchBlock.Body)

            Dim identifier As IdentifierNameSyntax = Nothing
            Dim asClause As SimpleAsClauseSyntax = Nothing

            Dim exceptionTestType = If(noType, Nothing, catchBlock.Test)

            If exceptionTestType IsNot Nothing Then
                asClause = SF.SimpleAsClause(Translate(catchBlock.Test))

                If catchBlock.Variable IsNot Nothing Then
                    If catchBlock.Variable.Name Is Nothing Then
                        Throw New NotSupportedException("TranslateCatchBlock: unnamed parameter as catch variable")
                    End If

                    identifier = SF.IdentifierName(catchBlock.Variable.Name)
                Else
                    Dim name = UniquifyVariableName(Nothing)
                    identifier = SF.IdentifierName(name)
                End If
            End If

            DebugAssert(Not (asClause Is Nothing AndAlso catchBlock.Filter IsNot Nothing),
                        "Not (asClause Is Nothing AndAlso catchBlock.Filter IsNot Nothing)")

            Dim whenClause = If(catchBlock.Filter Is Nothing, Nothing, SF.CatchFilterClause(Translate(Of ExpressionSyntax)(catchBlock.Filter)))

            Dim catchStatement = SF.CatchStatement(identifier, asClause, whenClause)

            Return SF.CatchBlock(catchStatement, ResultAsStatementSyntaxList(translatedBody))
        End Function

        ''' <inheritdoc />
        Protected Overrides Function VisitConditional(conditional As ConditionalExpression) As Expression
            Result = TranslateConditional(conditional, lowerableAssignmentVariable:=Nothing)

            Return conditional
        End Function

        ''' <summary>
        '''     This Is an internal API that supports the Entity Framework Core infrastructure And Not subject to
        '''     the same compatibility standards as public APIs. It may be changed Or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution And knowing that
        '''     doing so can result in application failures when updating to a New Entity Framework Core release.
        ''' </summary>
        Protected Overridable Function TranslateConditional(conditional As ConditionalExpression,
                                                            lowerableAssignmentVariable As IdentifierNameSyntax) As GeneratedSyntaxNodes

            ' ConditionalExpression can be an expression or an If/Else statement.
            Dim test = Translate(Of ExpressionSyntax)(conditional.Test)

            Dim defaultIfFalse = TryCast(conditional.IfFalse, DefaultExpression)
            Dim isFalseAbsent = defaultIfFalse IsNot Nothing AndAlso defaultIfFalse.Type = GetType(Void)

            Select Case _context
                Case ExpressionContext.Statement
                    Return New GeneratedSyntaxNodes(TranslateConditionalStatement(conditional, isFalseAbsent, test))

                Case ExpressionContext.Expression,
                     ExpressionContext.ExpressionLambda

                    If isFalseAbsent Then
                        Throw New NotSupportedException(
                            $"Missing {NameOf(ConditionalExpression.IfFalse)} in {NameOf(ConditionalExpression)} in expression context")
                    End If

                    Dim parentLiftedState = _liftedState
                    _liftedState = LiftedState.CreateEmpty()

                    ' If we're in a lambda body, we try to translate as an expression if possible (i.e. no blocks in the True/False arms).
                    Using ChangeContext(ExpressionContext.Expression)
                        Dim ifTrue = Translate(conditional.IfTrue)
                        Dim ifFalse = Translate(conditional.IfFalse)

                        If Not ifTrue.IsASingleExpression OrElse
                           Not ifFalse.IsASingleExpression Then
                            Throw New InvalidOperationException("Trying to evaluate a non-expression condition in expression context")
                        End If

                        Dim ifTrueExpression = ifTrue.GetExpression()
                        Dim ifFalseExpression = ifFalse.GetExpression()

                        ' There were no lifted expressions inside either arm - we can translate directly to a VB ternary conditional expression
                        If _liftedState.Statements.Count = 0 Then
                            _liftedState = parentLiftedState
                            Return New GeneratedSyntaxNodes(SF.TernaryConditionalExpression(test, ifTrueExpression, ifFalseExpression))
                        End If
                    End Using

                    ' If we're in a lambda body and couldn't translate as a conditional expression, translate as an If/Else statement with
                    ' return. Wrap the true/false sides in blocks to have "return" added.
                    If _context = ExpressionContext.ExpressionLambda Then
                        _liftedState = parentLiftedState

                        Return New GeneratedSyntaxNodes(
                                TranslateConditionalStatement(
                                    conditional.Update(conditional.Test,
                                        If(TypeOf conditional.IfTrue Is BlockExpression, conditional.IfTrue, E.Block(conditional.IfTrue)),
                                        If(TypeOf conditional.IfFalse Is BlockExpression, conditional.IfFalse, E.Block(conditional.IfFalse))),
                                    isFalseAbsent,
                                    test))
                    End If

                    ' We're in regular expression context, and there are lifted expressions inside one of the arms; we translate to an If/Else
                    ' statement but lowering an assignment into both sides of the condition
                    _liftedState = LiftedState.CreateEmpty()

                    Dim assignmentVariable As IdentifierNameSyntax
                    Dim loweredAssignmentVariableType As TypeSyntax = Nothing

                    If lowerableAssignmentVariable Is Nothing Then
                        Dim name = UniquifyVariableName("liftedConditional")
                        Dim parameter = E.Parameter(conditional.Type, name)
                        assignmentVariable = SF.IdentifierName(name)
                        loweredAssignmentVariableType = Translate(parameter.Type)
                    Else
                        assignmentVariable = lowerableAssignmentVariable
                    End If

                    Dim iftruestatements = ResultAsStatementSyntaxList(ProcessArmBody(conditional.IfTrue, assignmentVariable))
                    Dim iffalsestatements = ResultAsStatementSyntaxList(ProcessArmBody(conditional.IfFalse, assignmentVariable))

                    _liftedState = parentLiftedState

                    If lowerableAssignmentVariable Is Nothing Then
                        _liftedState.Statements.Add(
                            SF.LocalDeclarationStatement(
                                SF.TokenList(SF.Token(SyntaxKind.DimKeyword)),
                                SF.SeparatedList(
                                    {SF.VariableDeclarator(
                                        SF.SeparatedList(
                                            {SF.ModifiedIdentifier(assignmentVariable.Identifier.Text)}),
                                        Nothing,
                                        Nothing)})))
                    End If

                    _liftedState.Statements.Add(
                        SF.MultiLineIfBlock(
                            SF.IfStatement(SF.Token(SyntaxKind.IfKeyword), test, SF.Token(SyntaxKind.ThenKeyword)),
                            iftruestatements,
                            elseIfBlocks:=Nothing,
                            elseBlock:=SF.ElseBlock(iffalsestatements)))

                    Return New GeneratedSyntaxNodes(assignmentVariable)
                Case Else
                    Throw New ArgumentOutOfRangeException()
            End Select
        End Function

        Private Function ProcessArmBody(body As Expression, assignmentVariable As IdentifierNameSyntax) As GeneratedSyntaxNodes
            DebugAssert(_liftedState.Statements.Count = 0, "_liftedExpressions.Count = 0")

            Dim translatedBody = Translate(body, assignmentVariable)

            ' Usually we add an assignment for the variable.
            ' The exception Is if the body was itself lifted out And the assignment lowering succeeded (nested conditionals) -
            ' in this case we get back the lowered assignment variable, And don't need the assignment (i = i)
            If translatedBody IsNot assignmentVariable Then
                _liftedState.Statements.Add(
                    SF.SimpleAssignmentStatement(
                        assignmentVariable,
                        translatedBody))
            End If

            Dim sn = New GeneratedSyntaxNodes(_liftedState.Statements.Cast(Of SyntaxNode))

            _liftedState.Statements.Clear()
            Return sn
        End Function

        Private Function TranslateConditionalStatement(conditional As ConditionalExpression,
                                                       isFalseAbsent As Boolean,
                                                       test As ExpressionSyntax) As MultiLineIfBlockSyntax

            Dim ifStatement = SF.IfStatement(SF.Token(SyntaxKind.IfKeyword), test, SF.Token(SyntaxKind.ThenKeyword))
            Dim ifTrue = Translate(conditional.IfTrue)
            Dim ifFalse = Translate(conditional.IfFalse)

            Dim ifTrueStatements = ResultAsStatementSyntaxList(ifTrue)

            If isFalseAbsent Then
                Return SF.MultiLineIfBlock(ifStatement, ifTrueStatements, Nothing, Nothing)
            End If

            Dim ifFalseStatements = ResultAsStatementSyntaxList(ifFalse)

            Dim ifFalseIfBlockSyntax = TryCast(ifFalseStatements.FirstOrDefault, MultiLineIfBlockSyntax)

            ' We want to specifically exempt MultiLineIfBlockSyntax under the Else from being wrapped by a block,
            ' so as to get nice ElseIf syntax
            If ifFalseStatements.Count = 1 AndAlso
               ifFalseIfBlockSyntax IsNot Nothing Then

                Dim elseIfBlocks = SF.List(Of ElseIfBlockSyntax)(
                    {SF.ElseIfBlock(SF.ElseIfStatement(ifFalseIfBlockSyntax.IfStatement.Condition),
                                    ifFalseIfBlockSyntax.Statements)}.
                    Concat(
                        ifFalseIfBlockSyntax.ElseIfBlocks.Select(
                            Function(b) SF.ElseIfBlock(SF.ElseIfStatement(b.ElseIfStatement.Condition),
                                                       b.Statements))))

                Return SF.MultiLineIfBlock(ifStatement,
                                           ifTrueStatements,
                                           elseIfBlocks:=elseIfBlocks,
                                           elseBlock:=ifFalseIfBlockSyntax.ElseBlock)
            Else
                Return SF.MultiLineIfBlock(ifStatement,
                                           ifTrueStatements,
                                           elseIfBlocks:=Nothing,
                                           elseBlock:=SF.ElseBlock(ifFalseStatements))
            End If
        End Function

        ''' <inheritdoc />
        Protected Overrides Function VisitConstant(constant As ConstantExpression) As Expression
            Result = New GeneratedSyntaxNodes(GenerateValue(constant.Value))

            Return constant
        End Function

        Private Function GenerateValue(value As Object) As ExpressionSyntax

            If TypeOf value Is Integer OrElse
               TypeOf value Is Long OrElse
               TypeOf value Is UInteger OrElse
               TypeOf value Is ULong OrElse
               TypeOf value Is Short OrElse
               TypeOf value Is SByte OrElse
               TypeOf value Is UShort OrElse
               TypeOf value Is Byte OrElse
               TypeOf value Is Double OrElse
               TypeOf value Is Single OrElse
               TypeOf value Is Decimal OrElse
               TypeOf value Is Char Then

                Return DirectCast(_g.LiteralExpression(value), ExpressionSyntax)
            End If

            If TypeOf value Is String OrElse
               TypeOf value Is Boolean OrElse
               value Is Nothing Then

                Return DirectCast(_g.LiteralExpression(value), ExpressionSyntax)
            End If

            Dim t = TryCast(value, Type)
            If t IsNot Nothing Then
                Return SF.GetTypeExpression(Translate(t))
            End If

            Dim en = TryCast(value, [Enum])
            If en IsNot Nothing Then
                Return HandleEnum(en)
            End If

            Dim tuple = TryCast(value, ITuple)
            If tuple IsNot Nothing Then
                Dim tupleType = tuple.GetType
                If tupleType.IsGenericType AndAlso
                   tupleType.Name.StartsWith("ValueTuple`", StringComparison.Ordinal) AndAlso
                   tupleType.Namespace = "System" Then

                    Return HandleValueTuple(tuple)
                End If
            End If

            Dim c = TryCast(value, IEqualityComparer)
            If c IsNot Nothing AndAlso
               c Is StructuralComparisons.StructuralEqualityComparer Then
                Return SF.MemberAccessExpression(
                           SyntaxKind.SimpleMemberAccessExpression,
                           Translate(GetType(StructuralComparisons)),
                           SF.Token(SyntaxKind.DotToken),
                           SF.IdentifierName(NameOf(StructuralComparisons.StructuralEqualityComparer)))
            End If

            Dim cultureInfo = TryCast(value, CultureInfo)
            If cultureInfo IsNot Nothing Then
                If cultureInfo Is CultureInfo.InvariantCulture Then
                    Return SF.MemberAccessExpression(
                               SyntaxKind.SimpleMemberAccessExpression,
                               Translate(GetType(CultureInfo)),
                               SF.Token(SyntaxKind.DotToken),
                               SF.IdentifierName(NameOf(cultureInfo.InvariantCulture)))
                End If

                If cultureInfo Is CultureInfo.InstalledUICulture Then
                    Return SF.MemberAccessExpression(
                               SyntaxKind.SimpleMemberAccessExpression,
                               Translate(GetType(CultureInfo)),
                               SF.Token(SyntaxKind.DotToken),
                               SF.IdentifierName(NameOf(cultureInfo.InstalledUICulture)))
                End If

                If cultureInfo Is CultureInfo.CurrentCulture Then
                    Return SF.MemberAccessExpression(
                              SyntaxKind.SimpleMemberAccessExpression,
                              Translate(GetType(CultureInfo)),
                              SF.Token(SyntaxKind.DotToken),
                              SF.IdentifierName(NameOf(cultureInfo.CurrentCulture)))
                End If

                If cultureInfo Is CultureInfo.CurrentUICulture Then
                    Return SF.MemberAccessExpression(
                               SyntaxKind.SimpleMemberAccessExpression,
                               Translate(GetType(CultureInfo)),
                               SF.Token(SyntaxKind.DotToken),
                               SF.IdentifierName(NameOf(cultureInfo.CurrentUICulture)))
                End If

                If cultureInfo Is CultureInfo.DefaultThreadCurrentCulture Then
                    Return SF.MemberAccessExpression(
                               SyntaxKind.SimpleMemberAccessExpression,
                               Translate(GetType(CultureInfo)),
                               SF.Token(SyntaxKind.DotToken),
                               SF.IdentifierName(NameOf(cultureInfo.DefaultThreadCurrentCulture)))
                End If

                If cultureInfo Is CultureInfo.DefaultThreadCurrentUICulture Then
                    Return SF.MemberAccessExpression(
                               SyntaxKind.SimpleMemberAccessExpression,
                               Translate(GetType(CultureInfo)),
                               SF.Token(SyntaxKind.DotToken),
                               SF.IdentifierName(NameOf(cultureInfo.DefaultThreadCurrentUICulture)))
                End If
            End If

            Dim encoding = TryCast(value, Encoding)
            If encoding IsNot Nothing Then
                If encoding Is Encoding.ASCII Then
                    Return SF.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                Translate(GetType(Encoding)),
                                SF.Token(SyntaxKind.DotToken),
                                SF.IdentifierName(NameOf(encoding.ASCII)))
                End If

                If encoding Is Encoding.Unicode Then
                    Return SF.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                Translate(GetType(Encoding)),
                                SF.Token(SyntaxKind.DotToken),
                                SF.IdentifierName(NameOf(encoding.Unicode)))
                End If

                If encoding Is Encoding.BigEndianUnicode Then
                    Return SF.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            Translate(GetType(Encoding)),
                            SF.Token(SyntaxKind.DotToken),
                            SF.IdentifierName(NameOf(encoding.BigEndianUnicode)))
                End If

                If encoding Is Encoding.UTF8 Then
                    Return SF.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        Translate(GetType(Encoding)),
                        SF.Token(SyntaxKind.DotToken),
                        SF.IdentifierName(NameOf(encoding.UTF8)))
                End If

                If encoding Is Encoding.UTF32 Then
                    Return SF.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        Translate(GetType(Encoding)),
                        SF.Token(SyntaxKind.DotToken),
                        SF.IdentifierName(NameOf(encoding.UTF32)))
                End If

                If encoding Is Encoding.Latin1 Then
                    Return SF.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        Translate(GetType(Encoding)),
                        SF.Token(SyntaxKind.DotToken),
                        SF.IdentifierName(NameOf(encoding.Latin1)))
                End If

                If encoding Is Encoding.Default Then
                    Return SF.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        Translate(GetType(Encoding)),
                        SF.Token(SyntaxKind.DotToken),
                        SF.IdentifierName(NameOf(encoding.Default)))
                End If
            End If

            Throw New NotSupportedException(
                $"Encountered a constant of unsupported type '{value.GetType().Name}'. Only primitive constant nodes are supported.")
        End Function

        Private Function HandleValueTuple(tuple As ITuple) As ExpressionSyntax
            Dim arguments(tuple.Length - 1) As SimpleArgumentSyntax
            For i = 0 To tuple.Length - 1
                arguments(i) = SF.SimpleArgument(GenerateValue(tuple(i)))
            Next

            Return SF.TupleExpression(SF.SeparatedList(arguments))
        End Function

        Private Function HandleEnum(en As [Enum]) As ExpressionSyntax
            Dim enumType = en.GetType()

            Dim formatted = [Enum].Format(enumType, en, "G")
            If Char.IsDigit(formatted(0)) Then
                ' Unknown value, render as a cast of the underlying integral value
                If Not [Enum].IsDefined(en.GetType(), en) Then
                    Dim underlyingType = enumType.GetEnumUnderlyingType()

                    Return SF.CTypeExpression(
                              SF.LiteralExpression(
                                  SyntaxKind.NumericLiteralExpression,
                                  If(underlyingType = GetType(SByte) OrElse
                                     underlyingType = GetType(Short) OrElse
                                     underlyingType = GetType(Integer) OrElse
                                     underlyingType = GetType(Long),
                                        SF.Literal(Long.Parse(formatted)),
                                        SF.Literal(ULong.Parse(formatted)))),
                              Translate(enumType))
                End If
            End If

            Dim components = formatted.Split(", ")
            DebugAssert(components.Length > 0, "components.Length > 0")

            Return components.Aggregate(
                CType(Nothing, ExpressionSyntax),
                Function(last, [next])
                    If last Is Nothing Then
                        Return SF.MemberAccessExpression(
                                   SyntaxKind.SimpleMemberAccessExpression,
                                   SF.IdentifierName(enumType.Name),
                                   SF.Token(SyntaxKind.DotToken),
                                   SF.IdentifierName([next]))
                    Else
                        Return SyntaxFactory.BinaryExpression(
                                   SyntaxKind.OrExpression,
                                   last,
                                   SF.Token(SyntaxKind.OrKeyword),
                                   SF.MemberAccessExpression(
                                       SyntaxKind.SimpleMemberAccessExpression,
                                       SF.IdentifierName(enumType.Name),
                                       SF.Token(SyntaxKind.DotToken),
                                       SF.IdentifierName([next])))
                    End If
                End Function)
        End Function

        ''' <inheritdoc />
        Protected Overrides Function VisitDebugInfo(node As DebugInfoExpression) As Expression
            Throw New NotSupportedException("DebugInfo nodes are not supporting when translating expression trees to Visual Basic")
        End Function

        ''' <inheritdoc />
        Protected Overrides Function VisitDefault(node As DefaultExpression) As Expression
            Result = New GeneratedSyntaxNodes(
                        SF.CTypeExpression(SF.NothingLiteralExpression(SF.Token(SyntaxKind.NothingKeyword)), Translate(node.Type)))
            Return node
        End Function

        ''' <inheritdoc />
        Protected Overrides Function VisitGoto(gotoNode As GotoExpression) As Expression
            Result = New GeneratedSyntaxNodes(
                        SF.GoToStatement(SF.IdentifierLabel(TranslateLabelTarget(gotoNode.Target).Identifier)))
            Return gotoNode
        End Function

        ''' <inheritdoc />
        Protected Overrides Function VisitInvocation(invocation As InvocationExpression) As Expression
            Dim lambda = DirectCast(invocation.Expression, LambdaExpression)

            ' We need to inline the lambda invocation into the tree, by replacing parameters in the lambda body with the invocation arguments.
            ' However, if an argument to the invocation can have side effects (e.g. a method call), and it's referenced multiple times from
            ' the body, then that would cause multiple evaluation, which is wrong (same if the arguments are evaluated only once but in reverse
            ' order).
            ' So we have to lift such arguments.
            Dim arguments(invocation.Arguments.Count - 1) As Expression

            For i = 0 To arguments.Length - 1
                Dim argument = invocation.Arguments(i)

                If TypeOf argument Is ConstantExpression Then
                    ' No need to evaluate into a separate variable, just pass directly
                    arguments(i) = argument
                    Continue For
                End If

                ' Need to lift
                Dim name = UniquifyVariableName(If(lambda.Parameters(i).Name, "lifted"))
                Dim parameter = E.Parameter(argument.Type, name)
                _liftedState.Statements.Add(GenerateDeclaration(name, Translate(Of ExpressionSyntax)(argument)))
                arguments(i) = parameter
            Next

            Dim replacedBody = New ReplacingExpressionVisitor(lambda.Parameters, arguments).Visit(lambda.Body)
            Result = Translate(replacedBody)

            Return invocation
        End Function

        ''' <inheritdoc />
        Protected Overrides Function VisitLabel(Label As LabelExpression) As Expression
            Result = New GeneratedSyntaxNodes(
                        SF.LabelStatement(TranslateLabelTarget(Label.Target).Identifier.Text))
            Return Label
        End Function

        ''' <inheritdoc />
        Protected Overrides Function VisitLabelTarget(labelTarget As LabelTarget) As LabelTarget
            If labelTarget Is Nothing Then
                Throw New NotImplementedException("Null argument in VisitLabelTarget")
            End If

            Result = New GeneratedSyntaxNodes(TranslateLabelTarget(labelTarget))
            Return labelTarget
        End Function

        ''' <summary>
        '''     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        '''     the same compatibility standards as public APIs. It may be changed or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution and knowing that
        '''     doing so can result in application failures when updating to a new Entity Framework Core release.
        ''' </summary>
        Protected Overridable Function TranslateLabelTarget(labelTarget As LabelTarget) As IdentifierNameSyntax
            ' In LINQ expression trees, label targets can have a return type (they're expressions), which means they return the last evaluated
            ' thing if e.g. they're the last expression in a block. This would require lifting out the last evaluation before the goto/break,
            ' assigning it to a temporary variable, and adding a variable evaluation after the label.
            If labelTarget.Type <> GetType(Void) Then
                Throw New NotImplementedException("Non-void label target")
            End If

            ' We did a processing pass on the block's labels, so any labels should already be found in our label stack frame
            Return SF.IdentifierName(_labels(labelTarget))
        End Function

        Private Function Translate(type As Type) As TypeSyntax
            If type.IsGenericType Then
                Return SF.GenericName(
                    SF.Identifier(type.Name.Substring(0, type.Name.IndexOf("`"c))),
                    SF.TypeArgumentList(SF.SeparatedList(type.GenericTypeArguments.Select(AddressOf Translate))))
            End If

            If type = GetType(String) Then
                Return SF.PredefinedType(SF.Token(SyntaxKind.StringKeyword))
            End If

            If type = GetType(Boolean) Then
                Return SF.PredefinedType(SF.Token(SyntaxKind.BooleanKeyword))
            End If

            If type = GetType(Byte) Then
                Return SF.PredefinedType(SF.Token(SyntaxKind.ByteKeyword))
            End If

            If type = GetType(SByte) Then
                Return SF.PredefinedType(SF.Token(SyntaxKind.SByteKeyword))
            End If

            If type = GetType(Integer) Then
                Return SF.PredefinedType(SF.Token(SyntaxKind.IntegerKeyword))
            End If

            If type = GetType(UInteger) Then
                Return SF.PredefinedType(SF.Token(SyntaxKind.UIntegerKeyword))
            End If

            If type = GetType(Short) Then
                Return SF.PredefinedType(SF.Token(SyntaxKind.ShortKeyword))
            End If

            If type = GetType(UShort) Then
                Return SF.PredefinedType(SF.Token(SyntaxKind.UShortKeyword))
            End If

            If type = GetType(Long) Then
                Return SF.PredefinedType(SF.Token(SyntaxKind.LongKeyword))
            End If

            If type = GetType(ULong) Then
                Return SF.PredefinedType(SF.Token(SyntaxKind.ULongKeyword))
            End If

            If type = GetType(Single) Then
                Return SF.PredefinedType(SF.Token(SyntaxKind.SingleKeyword))
            End If

            If type = GetType(Double) Then
                Return SF.PredefinedType(SF.Token(SyntaxKind.DoubleKeyword))
            End If

            If type = GetType(Decimal) Then
                Return SF.PredefinedType(SF.Token(SyntaxKind.DecimalKeyword))
            End If

            If type = GetType(Char) Then
                Return SF.PredefinedType(SF.Token(SyntaxKind.CharKeyword))
            End If

            If type = GetType(Object) Then
                Return SF.PredefinedType(SF.Token(SyntaxKind.ObjectKeyword))
            End If

            If type.IsArray Then
                Return SF.ArrayType(Translate(type.GetElementType()))
            End If

            If type.IsNested Then
                Return SF.QualifiedName(
                    DirectCast(Translate(type.DeclaringType), NameSyntax),
                    SF.IdentifierName(type.Name))
            End If

            If type.Namespace IsNot Nothing Then
                _collectedNamespaces.Add(type.Namespace)
            End If

            Return SF.IdentifierName(type.Name)
        End Function

        ''' <inheritdoc />
        Protected Overrides Function VisitLambda(Of T)(lambda As Expression(Of T)) As Expression

            Using ChangeContext(If(lambda.ReturnType = GetType(Void),
                                      ExpressionContext.Statement,
                                      ExpressionContext.ExpressionLambda))

                Dim parentOnLastLambdaLine = _onLastLambdaLine
                _onLastLambdaLine = True

                Dim stackFrame = PushNewStackFrame()

                For Each param In lambda.Parameters
                    Dim name = UniquifyVariableName(param.Name)
                    stackFrame.Variables(param) = name
                    stackFrame.VariableNames.Add(name)
                Next

                Dim body = Translate(lambda.Body)

                ' If the lambda body was an expression that had lifted statements (e.g. some block in expression context),
                ' we need to insert those statements at the start
                If _liftedState.Statements.Count > 0 Then
                    DebugAssert(lambda.ReturnType <> GetType(Void), "lambda.ReturnType <> GetType(Void)")
                    DebugAssert(body.IsASingleExpression, "body.IsASingleExpression")

                    body = New GeneratedSyntaxNodes(
                              _liftedState.Statements.
                                  Append(SF.ReturnStatement(body.GetExpression)).
                                  ToList())

                    _liftedState.Statements.Clear()
                End If

                ' Note that we always explicitly include the parameters types.
                ' This is because in some cases, the parameter isn't actually used in the lambda body, and the compiler can't infer its type.
                ' However, we can't do that when the type is anonymous.
                Dim parameters =
                    SF.ParameterList(
                        SF.SeparatedList(
                            lambda.
                                Parameters.
                                    Select(Function(p) SF.Parameter(
                                        Nothing,
                                        Nothing,
                                        SF.ModifiedIdentifier(SF.Identifier(LookupVariableName(p))),
                                        SF.SimpleAsClause(If(p.Type.IsAnonymousType(), Nothing, Translate(p.Type))),
                                        Nothing))))

                Dim isSingleLine As Boolean
                Dim isProcedure = lambda.ReturnType = GetType(Void)

                Dim lambdaHeader As LambdaHeaderSyntax
                Dim lambdaKind As SyntaxKind

                If isProcedure Then
                    ' The body of a single-line subroutine must be single-line statement.
                    isSingleLine = body.Count = 1 AndAlso TypeOf body.Node Is InvocationExpressionSyntax
                    lambdaHeader = SF.LambdaHeader(SyntaxKind.SubLambdaHeader, SF.Token(SyntaxKind.SubKeyword))
                    lambdaKind = If(isSingleLine, SyntaxKind.SingleLineSubLambdaExpression, SyntaxKind.MultiLineSubLambdaExpression)
                Else
                    Dim asClause As SimpleAsClauseSyntax = Nothing
                    ' The body of a single-line function must be an expression that returns a value, not a statement.
                    isSingleLine = body.IsASingleExpression OrElse
                                   (body.Count = 1 AndAlso TypeOf body.Node Is ReturnStatementSyntax)

                    If Not isSingleLine Then
                        asClause = SF.SimpleAsClause(Translate(lambda.ReturnType))
                    End If

                    lambdaHeader = SF.LambdaHeader(SyntaxKind.FunctionLambdaHeader, Nothing, Nothing, SF.Token(SyntaxKind.FunctionKeyword), Nothing, asClause)
                    lambdaKind = If(isSingleLine, SyntaxKind.SingleLineFunctionLambdaExpression, SyntaxKind.MultiLineFunctionLambdaExpression)
                End If

                lambdaHeader = lambdaHeader.WithParameterList(parameters)

                If isSingleLine Then
                    Result = New GeneratedSyntaxNodes(
                                SF.SingleLineLambdaExpression(lambdaKind, lambdaHeader, DirectCast(body.Node, VisualBasicSyntaxNode)))
                Else
                    Dim endStatement = If(isProcedure, SF.EndSubStatement(), SF.EndFunctionStatement())
                    Dim statements = SF.List(ResultAsStatementSyntaxList(body))

                    Result = New GeneratedSyntaxNodes(
                                SF.MultiLineLambdaExpression(lambdaKind, lambdaHeader, statements, endStatement))
                End If

                Dim popped = _stack.Pop()
                DebugAssert(popped.Equals(stackFrame), "popped.Equals(stackFrame)")

                _onLastLambdaLine = parentOnLastLambdaLine

                Return lambda
            End Using
        End Function

        ''' <inheritdoc />
        Protected Overrides Function VisitLoop([loop] As LoopExpression) As Expression

            If _context = ExpressionContext.Expression Then
                Throw New NotImplementedException()
            End If

            Dim rewrittenLoop1 = [loop]

            If [loop].ContinueLabel IsNot Nothing Then
                Dim blockExpr = TryCast([loop].Body, BlockExpression)
                Dim blockBody = If(blockExpr, E.Block([loop].Body))

                blockBody = blockBody.Update(
                                blockBody.Variables,
                                New E() {E.Label([loop].ContinueLabel)}.Concat(blockBody.Expressions))

                rewrittenLoop1 = [loop].Update(
                                    [loop].BreakLabel,
                                    continueLabel:=Nothing,
                                    blockBody)
            End If

            Dim rewrittenLoop2 As Expression = rewrittenLoop1

            If [loop].BreakLabel IsNot Nothing Then
                rewrittenLoop2 =
                    E.Block(
                        rewrittenLoop1.Update(breakLabel:=Nothing, rewrittenLoop1.ContinueLabel, rewrittenLoop1.Body),
                        E.Label([loop].BreakLabel))
            End If

            If rewrittenLoop2 IsNot [loop] Then
                Return Visit(rewrittenLoop2)
            End If

            Dim translatedBody = ResultAsStatementSyntaxList(Translate([loop].Body))

            Result = New GeneratedSyntaxNodes(
                        SF.WhileBlock(
                             SF.WhileStatement(SF.TrueLiteralExpression(SF.Token(SyntaxKind.TrueKeyword))),
                            translatedBody))

            Return [loop]
        End Function

        ''' <inheritdoc />
        Protected Overrides Function VisitMember(member As MemberExpression) As Expression
            Using ChangeContext(ExpressionContext.Expression)

                ' LINQ expression trees can directly access private members, but VB code cannot; render (slow) reflection code that does the same
                ' thing. Note that assignment to private members is handled in VisitBinary.
                ' TODO: Replace this with a more efficient API for .NET 8.0.

                Dim fieldInfo = TryCast(member.Member, FieldInfo)
                If fieldInfo IsNot Nothing AndAlso fieldInfo.IsPrivate Then
                    If member.Expression Is Nothing Then
                        Throw New NotImplementedException("Private Shared field access")
                    End If

                    If member.Member.DeclaringType Is Nothing Then
                        Throw New NotSupportedException("Private field without a declaring type: " & member.Member.Name)
                    End If

                    If _typeGetFieldMethod Is Nothing Then
                        _typeGetFieldMethod = GetType(Type).GetMethod(NameOf(System.Type.GetField), {GetType(String), GetType(BindingFlags)})
                    End If

                    If _fieldGetValueMethod Is Nothing Then
                        _fieldGetValueMethod = GetType(FieldInfo).GetMethod(NameOf(fieldInfo.GetValue), {GetType(Object)})
                    End If

                    Result = Translate(
                        E.Call(
                            E.Call(
                                E.Constant(member.Member.DeclaringType),
                                _typeGetFieldMethod,
                                E.Constant(fieldInfo.Name),
                                E.Constant(BindingFlags.NonPublic Or BindingFlags.Instance)),
                            _fieldGetValueMethod,
                            member.Expression))

                    ' TODO: private property
                    ' TODO: private event

                    Return member
                End If

                Dim constantExpression = TryCast(member.Expression, ConstantExpression)
                If fieldInfo IsNot Nothing AndAlso constantExpression IsNot Nothing AndAlso
                   constantExpression.Type.Attributes.HasFlag(TypeAttributes.NestedPrivate) AndAlso
                   Attribute.IsDefined(constantExpression.Type, GetType(CompilerGeneratedAttribute), inherit:=True) Then

                    ' Unwrap closure
                    VisitConstant(E.Constant(fieldInfo.GetValue(constantExpression.Value), member.Type))
                    Return member
                End If

                Result = New GeneratedSyntaxNodes(
                                SF.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    If(member.Expression Is Nothing,
                                         Translate(member.Member.DeclaringType), ' Shared
                                         Translate(Of ExpressionSyntax)(member.Expression)),
                                    SF.Token(SyntaxKind.DotToken),
                                    SF.IdentifierName(member.Member.Name)))

                Return member
            End Using
        End Function

        ''' <inheritdoc />
        Protected Overrides Function VisitIndex(index As IndexExpression) As Expression
            Using ChangeContext(ExpressionContext.Expression)

                If index.Arguments.Count > 1 Then
                    Throw New NotImplementedException("IndexExpression with multiple arguments")
                End If

                Result = New GeneratedSyntaxNodes(
                            SF.InvocationExpression(
                                Translate(Of ExpressionSyntax)(index.Object),
                                SF.ArgumentList(
                                    SF.SingletonSeparatedList(Of ArgumentSyntax)(
                                        SF.SimpleArgument(
                                            Translate(Of ExpressionSyntax)(index.Arguments.Single()))))))

                Return index
            End Using
        End Function

        ''' <inheritdoc />
        Protected Overrides Function VisitMethodCall([call] As MethodCallExpression) As Expression

            If [call].Method.DeclaringType Is Nothing Then
                Throw New NotSupportedException($"Can't translate method '{[call].Method.Name}' which has no declaring type")
            End If

            Using ChangeContext(ExpressionContext.Expression)
                Dim arguments = TranslateMethodArguments([call].Method.GetParameters(), [call].Arguments)

                ' For generic methods, we check whether the generic type arguments are inferrable (e.g. they all appear in the parameters), and
                ' only explicitly specify the arguments if not. Note that this isn't just for prettier code: anonymous types cannot be explicitly
                ' named in code.
                Dim methodIdentifier As SimpleNameSyntax
                If Not [call].Method.IsGenericMethod OrElse GenericTypeParameterAreInferrable([call]) Then
                    methodIdentifier = SF.IdentifierName([call].Method.Name)
                Else
                    DebugAssert(
                        [call].Method.GetGenericArguments().All(Function(ga) Not ga.IsAnonymousType()),
                        "Anonymous type as generic type argument for method whose type arguments aren't inferrable")

                    methodIdentifier = SF.GenericName(
                        SF.Identifier([call].Method.Name),
                        SF.TypeArgumentList(
                            SF.SeparatedList(
                                [call].Method.GetGenericArguments().Select(AddressOf Translate))))
                End If

                ' Extension syntax
                Dim literal As LiteralExpressionSyntax = Nothing
                If arguments.Length > 0 Then literal = TryCast(arguments(0).Expression, LiteralExpressionSyntax)

                If [call].Method.IsDefined(GetType(ExtensionAttribute), inherit:=False) AndAlso
                    Not (literal IsNot Nothing AndAlso
                         literal.IsKind(SyntaxKind.NothingLiteralExpression)) Then

                    Result = New GeneratedSyntaxNodes(
                                SF.InvocationExpression(
                                    SF.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        arguments(0).Expression,
                                        SF.Token(SyntaxKind.DotToken),
                                        methodIdentifier),
                                    SF.ArgumentList(SF.SeparatedList(arguments.Skip(1).Cast(Of ArgumentSyntax)))))
                ElseIf [call].Method.Name = "op_Equality" AndAlso
                       [call].Method.IsHideBySig AndAlso
                       [call].Method.IsSpecialName Then

                    Result = New GeneratedSyntaxNodes(
                                SF.EqualsExpression(
                                    Translate(Of ExpressionSyntax)([call].Arguments(0)),
                                    Translate(Of ExpressionSyntax)([call].Arguments(1))))
                Else
                    Dim expression As ExpressionSyntax
                    If [call].Object Is Nothing Then
                        ' Shared method call. Recursively add MemberAccessExpressions for all declaring types (for methods on nested types)
                        expression = GetMemberAccessesForAllDeclaringTypes([call].Method.DeclaringType)
                    Else
                        expression = Translate(Of ExpressionSyntax)([call].Object)
                    End If

                    If [call].Method.Name.StartsWith("get_", StringComparison.Ordinal) AndAlso
                       [call].Method.GetParameters().Length = 1 AndAlso
                       [call].Method.IsHideBySig AndAlso
                       [call].Method.IsSpecialName Then

                        Result = New GeneratedSyntaxNodes(
                                    SF.InvocationExpression(
                                        expression,
                                        SF.ArgumentList(SF.SeparatedList(arguments.Cast(Of ArgumentSyntax)))))
                    Else
                        Result = New GeneratedSyntaxNodes(
                                    SF.InvocationExpression(
                                        SF.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            expression,
                                            SF.Token(SyntaxKind.DotToken),
                                            methodIdentifier),
                                        SF.ArgumentList(SF.SeparatedList(arguments.Cast(Of ArgumentSyntax)))))
                    End If
                End If

                If [call].Method.DeclaringType.Namespace <> "" Then
                    _collectedNamespaces.Add([call].Method.DeclaringType.Namespace)
                End If

                Return [call]
            End Using
        End Function

        Private Function GetMemberAccessesForAllDeclaringTypes(type As Type) As ExpressionSyntax
            Return If(type.DeclaringType Is Nothing,
                         DirectCast(Translate(type), ExpressionSyntax),
                         SF.MemberAccessExpression(
                             SyntaxKind.SimpleMemberAccessExpression,
                             GetMemberAccessesForAllDeclaringTypes(type.DeclaringType),
                             SF.Token(SyntaxKind.DotToken),
                             SF.IdentifierName(type.Name)))
        End Function

        Private Function GenericTypeParameterAreInferrable([call] As MethodCallExpression) As Boolean
            Dim originalDefinition = [call].Method.GetGenericMethodDefinition()
            Dim unseenTypeParameters = originalDefinition.GetGenericArguments().ToList()

            For Each parameter In originalDefinition.GetParameters()
                ProcessType(parameter.ParameterType, unseenTypeParameters)
            Next

            Return unseenTypeParameters.Count = 0
        End Function

        Private Sub ProcessType(type As Type, unseenTypeParameters As List(Of Type))
            If type.IsGenericParameter Then
                unseenTypeParameters.Remove(type)
            ElseIf type.IsGenericType Then
                For Each genericArgument In type.GetGenericArguments()
                    ProcessType(genericArgument, unseenTypeParameters)
                Next
            End If
        End Sub

        ''' <inheritdoc/>
        Protected Overrides Function VisitNewArray(newArray As NewArrayExpression) As Expression
            Using ChangeContext(ExpressionContext.Expression)
                Dim elementType = Translate(newArray.Type.GetElementType())
                Dim expressions = TranslateList(newArray.Expressions)

                If newArray.NodeType = ExpressionType.NewArrayBounds Then
                    Result = New GeneratedSyntaxNodes(
                                SF.ArrayCreationExpression(
                                    Nothing,
                                    elementType,
                                    GenerateArrayBounds(expressions),
                                    SF.CollectionInitializer()))

                    Return newArray
                End If

                DebugAssert(newArray.NodeType = ExpressionType.NewArrayInit, "newArray.NodeType = ExpressionType.NewArrayInit")

                Result = New GeneratedSyntaxNodes(
                            _g.ArrayCreationExpression(elementType, expressions))

                Return newArray
            End Using
        End Function

        Private Function GenerateArrayBounds(expressions As ExpressionSyntax()) As ArgumentListSyntax

            Dim arguments As New List(Of ArgumentSyntax)

            For Each expr In expressions
                arguments.Add(
                    DirectCast(
                        SF.SimpleArgument(
                            SF.SubtractExpression(
                                expr,
                                SF.NumericLiteralExpression(
                                    SF.Literal(1)))),
                    ArgumentSyntax))
            Next

            Return SF.ArgumentList(
                        SF.SeparatedList(arguments))
        End Function

        ''' <inheritdoc/>
        Protected Overrides Function VisitNew(node As NewExpression) As Expression
            Using ChangeContext(ExpressionContext.Expression)

                Dim arguments = If(node.Constructor Is Nothing,
                                   Array.Empty(Of SimpleArgumentSyntax),
                                   TranslateMethodArguments(node.Constructor.GetParameters(), node.Arguments))

                If node.Type.IsAnonymousType() Then
                    If node.Members Is Nothing Then
                        Throw New NotSupportedException("Anonymous type creation without members")
                    End If

                    Result = New GeneratedSyntaxNodes(
                                SF.AnonymousObjectCreationExpression(
                                    SF.ObjectMemberInitializer(
                                        SF.SeparatedList(Of FieldInitializerSyntax)(
                                            arguments.Select(Function(arg, i) SF.NamedFieldInitializer(SF.IdentifierName(node.Members(i).Name), arg.Expression))))))

                    Return node
                End If

                ' If the type has any required properties and the constructor doesn't have [SetsRequiredMembers], we can't just generate an
                ' instantiation expression.
                ' TODO: Currently matching attributes by name since we target .NET 6.0. If/when we target .NET 7.0 and above, match the type.
                If node.Type.GetCustomAttributes(inherit:=True).
                        Any(Function(a) a.GetType().FullName = "System.Runtime.CompilerServices.RequiredMemberAttribute") AndAlso
                   node.Constructor IsNot Nothing AndAlso
                   Not node.Constructor.GetCustomAttributes().
                        Any(Function(a) a.GetType().FullName = "System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute") Then

                    ' If the constructor is parameterless, we generate Activator.Create(Of T)() which is almost as fast (<10ns difference).
                    ' For constructors with parameters, we currently throw as not supported (we can pass parameters, but boxing, probably
                    ' speed degradation etc.).
                    If node.Constructor.GetParameters().Length = 0 Then

                        _activatorCreateInstanceMethod =
                            If(_activatorCreateInstanceMethod, GetType(Activator).GetMethod(
                                NameOf(Activator.CreateInstance), Array.Empty(Of Type)))

                        Result =
                            Translate(
                                E.Call(_activatorCreateInstanceMethod.MakeGenericMethod(node.Type)))
                    Else
                        Throw New NotImplementedException("Instantiation of type with required properties via constructor that has parameters")
                    End If

                Else
                    ' Normal case with plain old instantiation
                    Result = New GeneratedSyntaxNodes(
                                    SF.ObjectCreationExpression(
                                        Nothing,
                                        Translate(node.Type),
                                        SF.ArgumentList(SF.SeparatedList(Of ArgumentSyntax)(arguments)),
                                        Nothing))
                End If

                If node.Constructor?.DeclaringType?.Namespace IsNot Nothing Then
                    _collectedNamespaces.Add(node.Constructor.DeclaringType.Namespace)
                End If

                Return node
            End Using
        End Function

        ''' <inheritdoc />
        Protected Overrides Function VisitParameter(parameter As ParameterExpression) As Expression

            Dim name As String = Nothing

            ' Note that the parameter in the lambda declaration is handled separately in VisitLambda
            If _stack.Peek().Variables.TryGetValue(parameter, name) OrElse
               _liftedState.Variables.TryGetValue(parameter, name) Then

                Result = New GeneratedSyntaxNodes(SF.IdentifierName(name))
                Return parameter
            End If

            ' This parameter is unknown to us - it's captured from outside the entire expression tree.
            ' Simply return its name without worrying about uniquification, since the variable needs to correspond to the outside in any
            ' case (it's the callers responsibility).
            _capturedVariables.Add(parameter)

            If parameter.Name Is Nothing Then
                Throw New NotSupportedException("Unnamed captured variable")
            End If

            Result = New GeneratedSyntaxNodes(SF.IdentifierName(parameter.Name))
            Return parameter
        End Function

        ''' <inheritdoc />
        Protected Overrides Function VisitRuntimeVariables(node As RuntimeVariablesExpression) As Expression
            Throw New NotSupportedException()
        End Function

        ''' <inheritdoc />
        Protected Overrides Function VisitSwitchCase(node As SwitchCase) As SwitchCase
            Throw New NotSupportedException("Translation happens as part of VisitSwitch")
        End Function

        ''' <inheritdoc />
        Protected Overrides Function VisitSwitch(switchNode As SwitchExpression) As Expression
            Result = TranslateSwitch(switchNode, lowerableAssignmentVariable:=Nothing)

            Return switchNode
        End Function

        ''' <summary>
        '''     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        '''     the same compatibility standards as public APIs. It may be changed or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution and knowing that
        '''     doing so can result in application failures when updating to a new Entity Framework Core release.
        ''' </summary>
        Protected Overridable Function TranslateSwitch(switchNode As SwitchExpression, lowerableAssignmentVariable As IdentifierNameSyntax) As GeneratedSyntaxNodes

            If switchNode.Comparison IsNot Nothing Then
                Throw New NotImplementedException("Switch with non-null comparison method")
            End If

            Dim switchValue = Translate(Of ExpressionSyntax)(switchNode.SwitchValue)

            Select Case _context
                Case ExpressionContext.Statement

                    ' Rewrites the switch as a series of nested ConditionalExpressions if a reference equality is found.
                    If switchNode.Cases.SelectMany(Function(c) c.TestValues).Any(Function(tv) IsReferenceEqualitySemantics(switchNode.SwitchValue, tv)) Then
                        Return TranslateConditional(RewriteSwitchToConditionals(switchNode), lowerableAssignmentVariable)
                    End If

                    Dim parentLiftedState = _liftedState
                    _liftedState = LiftedState.CreateEmpty()

                    Dim cases =
                        SF.List(
                            switchNode.Cases.Select(
                                Function(c)
                                    Return SF.CaseBlock(
                                        SF.CaseStatement(
                                            SF.SeparatedList(Of CaseClauseSyntax)(
                                                c.TestValues.Select(
                                                    Function(tv) SF.SimpleCaseClause(Translate(Of ExpressionSyntax)(tv))))),
                                        statements:=ProcessCaseBody(c.Body))
                                End Function))

                    If switchNode.DefaultBody IsNot Nothing Then
                        cases = cases.Add(
                            SF.CaseElseBlock(
                                SF.CaseElseStatement(SF.ElseCaseClause()),
                                ProcessCaseBody(switchNode.DefaultBody)))
                    End If

                    Return New GeneratedSyntaxNodes(SF.SelectBlock(SF.SelectStatement(switchValue), cases))
                Case ExpressionContext.Expression,
                     ExpressionContext.ExpressionLambda

                    If switchNode.DefaultBody Is Nothing Then
                        Throw New NotSupportedException("Missing default arm for switch expression")
                    End If

                    Dim parentLiftedState = _liftedState
                    _liftedState = LiftedState.CreateEmpty()

                    ' VB does not support SwitchExpression. This rewrites the switch as a series of nested ConditionalExpressions.
                    Dim conditionalExpr = TranslateConditional(RewriteSwitchToConditionals(switchNode), lowerableAssignmentVariable)

                    ' If there were no lifted expressions inside any arm, we can return this TernaryConditionalExpression
                    If _liftedState.Statements.Count = 0 AndAlso
                       conditionalExpr.IsASingleExpression Then
                        _liftedState = parentLiftedState
                        Return conditionalExpr
                    End If

                    ' Rewriting it to a Select Case statement.
                    _liftedState = LiftedState.CreateEmpty()

                    Dim assignmentVariable As IdentifierNameSyntax
                    Dim loweredAssignmentVariableType As TypeSyntax = Nothing

                    If lowerableAssignmentVariable Is Nothing Then
                        Dim name = UniquifyVariableName("liftedSwitch")
                        Dim param = E.Parameter(switchNode.Type, name)
                        assignmentVariable = SF.IdentifierName(name)
                        loweredAssignmentVariableType = Translate(param.Type)
                    Else
                        assignmentVariable = lowerableAssignmentVariable
                    End If

                    Dim cases = SF.List(
                            switchNode.Cases.Select(
                                Function(c)
                                    Return SF.CaseBlock(
                                        SF.CaseStatement(
                                            SF.SeparatedList(Of CaseClauseSyntax)(
                                                c.TestValues.Select(
                                                    Function(tv) SF.SimpleCaseClause(Translate(Of ExpressionSyntax)(tv))))),
                                        statements:=ProcessCaseArmBody(c.Body, assignmentVariable))
                                End Function).
                            Append(
                                SF.CaseElseBlock(
                                    SF.CaseElseStatement(SF.ElseCaseClause()),
                                    ProcessCaseArmBody(switchNode.DefaultBody, assignmentVariable))))

                    _liftedState = parentLiftedState

                    If lowerableAssignmentVariable Is Nothing Then
                        _liftedState.Statements.Add(
                            SF.LocalDeclarationStatement(
                                New SyntaxTokenList(SF.Token(SyntaxKind.DimKeyword)),
                                SF.SingletonSeparatedList(
                                    SF.VariableDeclarator(
                                         SF.SingletonSeparatedList(SF.ModifiedIdentifier(assignmentVariable.Identifier.Text)),
                                         SF.SimpleAsClause(loweredAssignmentVariableType),
                                         Nothing))))
                    End If

                    _liftedState.Statements.Add(SF.SelectBlock(SF.SelectStatement(switchValue), cases))
                    Return New GeneratedSyntaxNodes(assignmentVariable)
                Case Else
                    Throw New ArgumentOutOfRangeException()
            End Select
        End Function

        Private Function ProcessCaseBody(body As Expression) As SyntaxList(Of StatementSyntax)
            Dim translatedBody = Translate(body)

            Return ResultAsStatementSyntaxList(translatedBody)
        End Function

        Private Function ProcessCaseArmBody(body As Expression, assignmentVariable As IdentifierNameSyntax) As SyntaxList(Of StatementSyntax)
            DebugAssert(_liftedState.Statements.Count = 0, "_liftedExpressions.Count = 0")

            Dim translatedBody = Translate(body, assignmentVariable)

            Dim AssignmentStatement = SF.SimpleAssignmentStatement(
                                            assignmentVariable,
                                            translatedBody)

            If _liftedState.Statements.Count = 0 Then
                ' Simple expression, can embed directly in the switch case
                Return SF.List(Of StatementSyntax)({AssignmentStatement})
            End If

            ' Usually we add an assignment for the variable.
            ' The exception is if the body was itself lifted out and the assignment lowering succeeded (nested conditionals) -
            ' in this case we get back the lowered assignment variable, and don't need the assignment (i = i)
            If translatedBody IsNot assignmentVariable Then
                _liftedState.Statements.Add(
                        SF.SimpleAssignmentStatement(
                            assignmentVariable,
                            translatedBody))
            End If

            Dim statements = SF.List(_liftedState.Statements.ToList())

            _liftedState.Statements.Clear()
            Return statements
        End Function

        Private Shared Function RewriteSwitchToConditionals(node As SwitchExpression) As ConditionalExpression
            If node.Type = GetType(Void) Then
                Dim newExpr =
                    node.Cases.
                            SelectMany(Function(c) c.TestValues, Function(c, tv) New With {c.Body, .Label = tv}).
                            Reverse().
                            Aggregate(
                                node.DefaultBody,
                                Function(expression, arm) If(expression Is Nothing,
                                                                E.IfThen(E.Equal(node.SwitchValue, arm.Label), arm.Body),
                                                                E.IfThenElse(E.Equal(node.SwitchValue, arm.Label), arm.Body, expression)))

                If newExpr Is Nothing Then
                    Throw New NotImplementedException("Empty switch statement")
                End If

                Return DirectCast(newExpr, ConditionalExpression)
            End If

            DebugAssert(node.DefaultBody IsNot Nothing, "Switch expression with non-void return type but no default body")

            Return DirectCast(
                      node.Cases.
                          SelectMany(Function(c) c.TestValues, Function(c, tv) New With {c.Body, .Label = tv}).
                          Reverse().
                          Aggregate(
                              node.DefaultBody,
                              Function(expression, arm) E.Condition(
                                  E.Equal(node.SwitchValue, arm.Label),
                                  arm.Body,
                                  expression)),
                      ConditionalExpression)
        End Function

        ''' <inheritdoc/>
        Protected Overrides Function VisitTry(tryNode As TryExpression) As Expression

            Dim translatedBody As IEnumerable(Of SyntaxNode) = Translate(tryNode.Body).Nodes

            Dim translatedFinally As IEnumerable(Of SyntaxNode) = Nothing

            If tryNode.Finally IsNot Nothing Then
                translatedFinally = Translate(tryNode.Finally).Nodes
            End If

            Select Case _context
                Case ExpressionContext.Statement
                    If tryNode.Fault IsNot Nothing Then
                        DebugAssert(
                            tryNode.Finally Is Nothing AndAlso tryNode.Handlers.Count = 0,
                            "tryNode.Finally is nothing AndAlso tryNode.Handlers.Count = 0")

                        Result = New GeneratedSyntaxNodes(
                                    _g.TryCatchStatement(
                                        translatedBody,
                                        catchClauses:={TranslateCatchBlock(E.Catch(GetType(Exception), tryNode.Fault), noType:=True)}))

                        Return tryNode
                    End If

                    Result = New GeneratedSyntaxNodes(
                                _g.TryCatchStatement(
                                    translatedBody,
                                    catchClauses:=tryNode.Handlers.Select(Function(h) TranslateCatchBlock(h)),
                                    translatedFinally))

                    Return tryNode

                Case ExpressionContext.Expression,
                     ExpressionContext.ExpressionLambda

                    Throw New NotImplementedException()
                Case Else
                    Throw New ArgumentOutOfRangeException()
            End Select
        End Function

        ''' <inheritdoc/>
        Protected Overrides Function VisitTypeBinary(node As TypeBinaryExpression) As Expression
            Dim visitedExpression = Translate(Of ExpressionSyntax)(node.Expression)

            Select Case node.NodeType
                Case ExpressionType.TypeIs
                    Result = New GeneratedSyntaxNodes(
                                SF.TypeOfIsExpression(visitedExpression, Translate(node.TypeOperand)))
                Case ExpressionType.TypeEqual
                    Result = New GeneratedSyntaxNodes(
                                SF.EqualsExpression(SF.InvocationExpression(SF.SimpleMemberAccessExpression(visitedExpression, SF.IdentifierName(NameOf(Object.GetType))), SF.ArgumentList()),
                                                    SF.GetTypeExpression(Translate(node.TypeOperand))))
                Case Else : Throw New ArgumentOutOfRangeException()
            End Select

            Return node
        End Function

        ''' <inheritdoc/>
        Protected Overrides Function VisitUnary(unary As UnaryExpression) As Expression
            If unary.Method IsNot Nothing AndAlso
               Not unary.Method.IsHideBySig AndAlso
               Not unary.Method.IsSpecialName AndAlso
               unary.Method.Name <> "op_Implicit" AndAlso
               unary.Method.Name <> "op_Explicit" Then

                Throw New NotImplementedException("Unary node with non-null method")
            End If

            Using ChangeContext(ExpressionContext.Expression)
                Dim operand = Translate(Of ExpressionSyntax)(unary.Operand)

                ' TODO: Confirm what to do with the unchecked expression types

                If unary.NodeType = ExpressionType.Not AndAlso unary.Type = GetType(Boolean) Then
                    Result = New GeneratedSyntaxNodes(_g.LogicalNotExpression(operand))
                ElseIf unary.NodeType = ExpressionType.Throw AndAlso unary.Type = GetType(Void) Then
                    Result = New GeneratedSyntaxNodes(_g.ThrowStatement(operand))
                Else
                    Select Case unary.NodeType
                        Case ExpressionType.Negate
                            Result = New GeneratedSyntaxNodes(_g.NegateExpression(operand))
                        Case ExpressionType.NegateChecked
                            Result = New GeneratedSyntaxNodes(_g.NegateExpression(operand))
                        Case ExpressionType.Not
                            Result = New GeneratedSyntaxNodes(_g.BitwiseNotExpression(operand))
                        Case ExpressionType.OnesComplement
                            Result = New GeneratedSyntaxNodes(_g.BitwiseNotExpression(operand))
                        Case ExpressionType.IsFalse
                            Result = New GeneratedSyntaxNodes(_g.LogicalNotExpression(operand))
                        Case ExpressionType.IsTrue
                            Result = New GeneratedSyntaxNodes(operand)
                        Case ExpressionType.ArrayLength
                            Result = New GeneratedSyntaxNodes(_g.MemberAccessExpression(operand, "Length"))
                        Case ExpressionType.Convert
                            Result = New GeneratedSyntaxNodes(_g.ConvertExpression(Translate(unary.Type), operand))
                        Case ExpressionType.ConvertChecked
                            Result = New GeneratedSyntaxNodes(_g.ConvertExpression(Translate(unary.Type), operand))
                        Case ExpressionType.Throw
                            Result = New GeneratedSyntaxNodes(_g.ThrowExpression(operand))
                        Case ExpressionType.TypeAs
                            Result = TranslateTypeAs(unary, operand)
                        Case ExpressionType.Quote
                            Result = New GeneratedSyntaxNodes(operand)
                        Case ExpressionType.UnaryPlus
                            Result = New GeneratedSyntaxNodes(SF.UnaryPlusExpression(operand))
                        Case ExpressionType.Unbox
                            Result = New GeneratedSyntaxNodes(operand)
                        Case ExpressionType.Increment
                            Result = Translate(E.Add(unary.Operand, E.Constant(1)))
                        Case ExpressionType.Decrement
                            Result = Translate(E.Subtract(unary.Operand, E.Constant(1)))
                        Case ExpressionType.PostIncrementAssign,
                             ExpressionType.PostDecrementAssign,
                             ExpressionType.PreIncrementAssign,
                             ExpressionType.PreDecrementAssign
                            Throw New NotSupportedException("Unsupported LINQ unary node: " & unary.NodeType.ToString())
                        Case Else
                            Throw New ArgumentOutOfRangeException("Unsupported LINQ unary node: " & unary.NodeType.ToString())
                    End Select
                End If

                Return unary
            End Using
        End Function

        Private Function TranslateTypeAs(unary As UnaryExpression, operand As ExpressionSyntax) As GeneratedSyntaxNodes
            If unary.Type.IsNullableValueType Then
                Dim underlyingType = unary.Type.UnwrapNullableType()
                Return New GeneratedSyntaxNodes(
                            SF.TernaryConditionalExpression(
                                SF.TypeOfIsExpression(operand, Translate(underlyingType)),
                                SF.CTypeExpression(operand, Translate(underlyingType)),
                                SF.ObjectCreationExpression(Translate(unary.Type))))
            End If

            Return New GeneratedSyntaxNodes(SF.TryCastExpression(operand, Translate(unary.Type)))
        End Function

        ''' <inheritdoc/>
        Protected Overrides Function VisitMemberInit(memberInit As MemberInitExpression) As Expression
            Dim objectCreation = Translate(Of ObjectCreationExpressionSyntax)(memberInit.NewExpression)

            Dim incompatibleListBindings As List(Of MemberListBinding) = Nothing

            Dim initializerExpressions As New List(Of FieldInitializerSyntax)(memberInit.Bindings.Count)

            For Each binding In memberInit.Bindings
                ' VB collection initialization syntax only works when Add is called on an IEnumerable, but LINQ supports arbitrary add
                ' methods. Skip these, we'll add them later outside the initializer

                Dim listBinding = TryCast(binding, MemberListBinding)
                If listBinding IsNot Nothing AndAlso
                   (Not GetMemberType(listBinding.Member).IsAssignableTo(GetType(IEnumerable)) OrElse
                    listBinding.Initializers.Any(Function(e) e.AddMethod.Name <> "Add" OrElse
                    e.Arguments.Count <> 1)) Then

                    incompatibleListBindings = If(incompatibleListBindings, New List(Of MemberListBinding))
                    incompatibleListBindings.Add(listBinding)
                    Continue For
                End If

                Dim liftedStatementsPosition = _liftedState.Statements.Count

                VisitMemberBinding(binding)

                initializerExpressions.Add(DirectCast(Result.Node, FieldInitializerSyntax))

                If _liftedState.Statements.Count > liftedStatementsPosition Then
                    ' TODO: This is tricky because of the recursive nature of MemberMemberBinding
                    Throw New NotImplementedException("MemberInit: lifted statements")
                End If
            Next

            If incompatibleListBindings IsNot Nothing Then
                ' TODO: Lift the instantiation and add extra statements to add the incompatible bindings after that
                Throw New NotImplementedException("MemberInit: incompatible MemberListBinding")
            End If

            Result = New GeneratedSyntaxNodes(
                        objectCreation.WithInitializer(
                            SF.ObjectMemberInitializer(
                                SF.SeparatedList(Of FieldInitializerSyntax)(initializerExpressions))))

            Return memberInit
        End Function

        Private Shared Function GetMemberType(memberInfo As MemberInfo) As Type
            Return If((TryCast(memberInfo, PropertyInfo))?.PropertyType, DirectCast(memberInfo, FieldInfo).FieldType)
        End Function

        ''' <inheritdoc/>
        Protected Overrides Function VisitListInit(listInit As ListInitExpression) As Expression
            Dim objectCreation = Translate(Of ObjectCreationExpressionSyntax)(listInit.NewExpression)

            Dim incompatibleListBindings As List(Of ElementInit) = Nothing

            Dim initializerExpressions As New List(Of ExpressionSyntax)(listInit.Initializers.Count)

            For Each initializer In listInit.Initializers
                ' VB collection initialization syntax only works when Add is called on an IEnumerable, but LINQ supports arbitrary add
                ' methods. Skip these, we'll add them later outside the initializer
                If Not listInit.NewExpression.Type.IsAssignableTo(GetType(IEnumerable)) OrElse
                   listInit.Initializers.Any(Function(e) e.AddMethod.Name <> "Add" OrElse
                   e.Arguments.Count <> 1) Then
                    incompatibleListBindings = If(incompatibleListBindings, New List(Of ElementInit))
                    incompatibleListBindings.Add(initializer)
                    Continue For
                End If

                Dim liftedStatementsPosition = _liftedState.Statements.Count

                VisitElementInit(initializer)

                initializerExpressions.Add(DirectCast(Result.Node, ExpressionSyntax))

                If _liftedState.Statements.Count > liftedStatementsPosition Then
                    Throw New NotImplementedException("ListInit: lifted statements")
                End If
            Next

            If incompatibleListBindings IsNot Nothing Then
                ' TODO: This requires lifting statements to *after* the instantiation - we usually lift to before.
                ' This is problematic: if such an expression is passed as an argument to a method, there's no way to faithfully translate it
                ' while preserving evaluation order.
                Throw New NotImplementedException("ListInit: incompatible ElementInit")
            End If

            Result = New GeneratedSyntaxNodes(
                        objectCreation.WithInitializer(
                            SF.ObjectCollectionInitializer(
                                SF.CollectionInitializer(SF.SeparatedList(initializerExpressions)))))

            Return listInit
        End Function

        ''' <inheritdoc/>
        Protected Overrides Function VisitElementInit(elementInit As ElementInit) As ElementInit
            DebugAssert(elementInit.Arguments.Count = 1, "elementInit.Arguments.Count = 1")

            Visit(elementInit.Arguments.Single())

            Return elementInit
        End Function

        ''' <inheritdoc/>
        Protected Overrides Function VisitMemberAssignment(memberAssignment As MemberAssignment) As MemberAssignment
            Result = New GeneratedSyntaxNodes(
                        SF.NamedFieldInitializer(
                            SF.IdentifierName(memberAssignment.Member.Name),
                            Translate(Of ExpressionSyntax)(memberAssignment.Expression)))

            Return memberAssignment
        End Function

        ''' <inheritdoc/>
        Protected Overrides Function VisitMemberMemberBinding(memberMemberBinding As MemberMemberBinding) As MemberMemberBinding

            Dim underlyingType As Type

            Select Case memberMemberBinding.Member.MemberType
                Case MemberTypes.Field
                    underlyingType = DirectCast(memberMemberBinding.Member, FieldInfo).FieldType
                Case MemberTypes.Property
                    underlyingType = DirectCast(memberMemberBinding.Member, PropertyInfo).PropertyType
                Case Else
                    Throw New NotSupportedException("Member in MemberMemberBinding must represent a field or property.")
            End Select

            Result = New GeneratedSyntaxNodes(
                        SF.NamedFieldInitializer(
                            SF.IdentifierName(memberMemberBinding.Member.Name),
                            SF.ObjectCreationExpression(
                                Nothing,
                                Translate(underlyingType),
                                Nothing,
                                SF.ObjectMemberInitializer(
                                    SF.SeparatedList(Of FieldInitializerSyntax)(
                                        memberMemberBinding.Bindings.Select(
                                            Function(x)
                                                VisitMemberBinding(x)
                                                Return DirectCast(Result.Node, FieldInitializerSyntax)
                                            End Function))))))

            Return memberMemberBinding
        End Function

        ''' <inheritdoc/>
        Protected Overrides Function VisitMemberListBinding(memberListBinding As MemberListBinding) As MemberListBinding

            Dim underlyingType As Type

            Select Case memberListBinding.Member.MemberType
                Case MemberTypes.Field
                    underlyingType = DirectCast(memberListBinding.Member, FieldInfo).FieldType
                Case MemberTypes.Property
                    underlyingType = DirectCast(memberListBinding.Member, PropertyInfo).PropertyType
                Case Else
                    Throw New NotSupportedException("Member in MemberListBinding must represent a field or property.")
            End Select

            Result = New GeneratedSyntaxNodes(
                        SF.NamedFieldInitializer(
                            SF.IdentifierName(memberListBinding.Member.Name),
                            SF.ObjectCreationExpression(
                                Nothing,
                                Translate(underlyingType),
                                Nothing,
                                SF.ObjectCollectionInitializer(
                                    SF.CollectionInitializer(
                                        SF.SeparatedList(Of ExpressionSyntax)(
                                            memberListBinding.Initializers.Select(
                                                Function(i)
                                                    VisitElementInit(i)
                                                    Return DirectCast(Result.Node, ExpressionSyntax)
                                                End Function)))))))

            Return memberListBinding
        End Function

        ''' <summary>
        '''     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        '''     the same compatibility standards as public APIs. It may be changed or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution and knowing that
        '''     doing so can result in application failures when updating to a new Entity Framework Core release.
        ''' </summary>
        Protected Overrides Function VisitExtension(node As Expression) As Expression
            ' TODO: Remove any EF-specific code from this visitor (extend if needed)
            ' TODO: Hack mode. Visit the expression beforehand to replace EntityQueryRootExpression with context.Set<>(), or receive it in this visitor as a replacement or something.
            Dim entityQueryRoot = TryCast(node, EntityQueryRootExpression)
            If entityQueryRoot IsNot Nothing Then
                ' TODO: STET
                Result = New GeneratedSyntaxNodes(
                            SF.ParseExpression($"context.Set(Of {entityQueryRoot.EntityType.ClrType.Name})()"))
                Return node
            End If

            Throw New NotSupportedException(
                $"Encountered non-quotable expression of type {node.GetType()} when translating expression tree to VB")
        End Function

        Private Function TranslateMethodArguments(parameters As ParameterInfo(), arguments As IReadOnlyList(Of Expression)) As SimpleArgumentSyntax()
            Dim translatedExpressions = TranslateList(arguments)
            Dim translatedArguments(arguments.Count - 1) As SimpleArgumentSyntax

            For i = 0 To translatedExpressions.Length - 1
                Dim parameter = parameters(i)
                Dim argument = SF.SimpleArgument(translatedExpressions(i))

                translatedArguments(i) = argument
            Next

            Return translatedArguments
        End Function

        Private Function TranslateList(list As IReadOnlyList(Of Expression)) As ExpressionSyntax()
            DebugAssert(_context = ExpressionContext.Expression, "_context = ExpressionContext.Expression")

            Dim translatedList(list.Count - 1) As ExpressionSyntax
            Dim lastLiftedArgumentPosition = 0

            For i = 0 To list.Count - 1

                Dim expression = list(i)

                Dim liftedStatementsPosition = _liftedState.Statements.Count

                Dim translated = Translate(Of ExpressionSyntax)(expression)

                If _liftedState.Statements.Count > liftedStatementsPosition Then
                    ' This argument contained lifted statements. In order to preserve evaluation order, we must also lift out all preceding
                    ' arguments to before this argument's lifted statements.
                    While lastLiftedArgumentPosition < i

                        Dim argumentExpression = translatedList(lastLiftedArgumentPosition)

                        If Not _sideEffectDetector.CanBeReordered(argumentExpression, translated) Then
                            Dim name = UniquifyVariableName("liftedArg")

                            _liftedState.Statements.Insert(
                                        liftedStatementsPosition,
                                        GenerateDeclaration(name, argumentExpression))

                            liftedStatementsPosition += 1

                            _liftedState.VariableNames.Add(name)

                            translatedList(lastLiftedArgumentPosition) = SF.IdentifierName(name)
                        End If
                        lastLiftedArgumentPosition += 1
                    End While
                End If

                translatedList(i) = translated
            Next

            Return translatedList
        End Function

        Private Function PushNewStackFrame() As StackFrame
            Dim previousFrame = _stack.Peek()
            Dim newFrame As New StackFrame(
                New Dictionary(Of ParameterExpression, String)(previousFrame.Variables),
                New HashSet(Of String)(previousFrame.VariableNames, StringComparer.OrdinalIgnoreCase))

            _stack.Push(newFrame)

            Return newFrame
        End Function

        Private Function LookupVariableName(parameter As ParameterExpression) As String
            Dim name As String = Nothing

            Return If(_stack.Peek().Variables.TryGetValue(parameter, name),
                      name,
                      _liftedState.Variables(parameter))
        End Function

        Private Function UniquifyVariableName(name As String) As String
            Dim isUnnamed = name Is Nothing
            name = If(name, "unnamed")

            Dim parameterNames = _stack.Peek().VariableNames

            If parameterNames.Contains(name) OrElse _liftedState.VariableNames.Contains(name) Then
                Dim baseName = name
                Dim j = If(isUnnamed, _unnamedParameterCounter, 0)
                _unnamedParameterCounter += 1
                While parameterNames.Contains(name) OrElse _liftedState.VariableNames.Contains(name)
                    name = baseName & j
                    j += 1
                End While
            End If

            Return name
        End Function

        Private Shared Function GenerateDeclaration(variableIdentifier As String, initializer As ExpressionSyntax) As LocalDeclarationStatementSyntax

            Return SF.LocalDeclarationStatement(
                New SyntaxTokenList(SF.Token(SyntaxKind.DimKeyword)),
                SF.SingletonSeparatedList(
                    SF.VariableDeclarator(
                         SF.SingletonSeparatedList(SF.ModifiedIdentifier(variableIdentifier)),
                        Nothing,
                        SF.EqualsValue(initializer))))
        End Function

        Private Function ChangeContext(newContext As ExpressionContext) As ContextChanger
            Return New ContextChanger(Me, newContext)
        End Function

        Private Structure ContextChanger
            Implements IDisposable

            Private ReadOnly _translator As LinqToVisualBasicSyntaxTranslator
            Private ReadOnly _oldContext As ExpressionContext

            Public Sub New(translator As LinqToVisualBasicSyntaxTranslator, newContext As ExpressionContext)
                _translator = translator
                _oldContext = translator._context
                translator._context = newContext
            End Sub

            Public Sub Dispose() Implements IDisposable.Dispose
                _translator._context = _oldContext
            End Sub
        End Structure

        Private Enum ExpressionContext
            Expression
            Statement
            ExpressionLambda
        End Enum

        Private NotInheritable Class SideEffectDetectionSyntaxWalker
            Inherits SyntaxWalker

            Private _mayHaveSideEffects As Boolean

            ''' <summary>
            '''     Returns whether the two provided nodes can be re-ordered without the reversed evaluation order having any effect.
            '''     For example, two literal expressions can be safely ordered, while two invocations cannot.
            ''' </summary>
            Public Function CanBeReordered(first As SyntaxNode, second As SyntaxNode) As Boolean
                Return TypeOf first Is LiteralExpressionSyntax OrElse (Not MayHaveSideEffects(first) AndAlso Not MayHaveSideEffects(second))
            End Function

            Public Function MayHaveSideEffects(node As SyntaxNode) As Boolean
                _mayHaveSideEffects = False

                Visit(node)

                Return _mayHaveSideEffects
            End Function

            Public Function MayHaveSideEffects(nodes As IList(Of SyntaxNode)) As Boolean
                _mayHaveSideEffects = False

                Dim i = 0
                While Not _mayHaveSideEffects AndAlso i < nodes.Count
                    Dim node = nodes(i)
                    Visit(node)
                    i += 1
                End While

                Return _mayHaveSideEffects
            End Function

            Public Overrides Sub Visit(node As SyntaxNode)
                _mayHaveSideEffects = _mayHaveSideEffects OrElse MayHaveSideEffectsCore(node)

                MyBase.Visit(node)
            End Sub

            Private Shared Function MayHaveSideEffectsCore(node As SyntaxNode) As Boolean

                ' TODO: we can exempt most binary and unary expressions as well, e.g. i + 5, but not anything involving assignment

                Select Case True
                    Case TypeOf node Is IdentifierNameSyntax,
                         TypeOf node Is LiteralExpressionSyntax,
                         TypeOf node Is EmptyStatementSyntax
                        Return False
                    Case Else
                        Return True
                End Select
            End Function
        End Class

        Protected Class GeneratedSyntaxNodes
            Private _Nodes As List(Of SyntaxNode)

            Sub New()
            End Sub

            Sub New(sn As SyntaxNode)
                _Nodes = New List(Of SyntaxNode)({sn})
            End Sub

            Sub New(nodes As IEnumerable(Of SyntaxNode))
                _Nodes = New List(Of SyntaxNode)(nodes)
            End Sub

            Public Overridable Property Node As SyntaxNode
                Get
                    If _Nodes Is Nothing OrElse _Nodes.Count = 0 Then Return Nothing
                    DebugAssert(Count = 1, "GeneratedSyntaxNodes.Count <> 1")
                    Return _Nodes.Last()
                End Get
                Set
                    _Nodes = New List(Of SyntaxNode)({Value})
                End Set
            End Property

            Public Overridable Property Nodes As List(Of SyntaxNode)
                Get
                    If _Nodes Is Nothing OrElse _Nodes.Count = 0 Then Return Nothing
                    Return _Nodes
                End Get
                Set
                    _Nodes = Value
                End Set
            End Property

            Public ReadOnly Property IsASingleExpression() As Boolean
                Get
                    If Count <> 1 Then Return False
                    Return TypeOf _Nodes.First Is ExpressionSyntax
                End Get
            End Property

            Public ReadOnly Property Count() As Integer
                Get
                    Return _Nodes.Count
                End Get
            End Property

            Public Function GetExpression() As ExpressionSyntax
                Return TryCast(Node, ExpressionSyntax)
            End Function
        End Class

        Private Function ResultAsStatementSyntaxList(result As GeneratedSyntaxNodes) As SyntaxList(Of StatementSyntax)
            If result.Count = 0 Then Return SF.List(Of StatementSyntax)

            If result.Count > 1 Then
                Return SF.List(result.Nodes.Select(Function(x) CastAsStatementSyntax(x)))
            End If

            Return SF.List({CastAsStatementSyntax(result.Nodes.First())})
        End Function

        Private Function CastAsStatementSyntax(s As SyntaxNode) As StatementSyntax

            Dim ss = TryCast(s, StatementSyntax)
            If ss IsNot Nothing Then
                Return ss
            End If

            Dim es = TryCast(s, ExpressionSyntax)
            If es IsNot Nothing Then
                Select Case es.Kind()
                    Case SyntaxKind.InvocationExpression
                        Return SF.ExpressionStatement(es)
                    Case Else
                        Return DirectCast(_g.LocalDeclarationStatement(UniquifyVariableName(Nothing), es), LocalDeclarationStatementSyntax)
                End Select
            End If

            Throw New InvalidCastException($"Cannot convert a {s.GetType()} to a StatementSyntax.")
        End Function

        Private Shared Function IsReferenceEqualitySemantics(left As Expression, right As Expression) As Boolean
            If left.NodeType <> ExpressionType.Convert OrElse left.Type IsNot GetType(Object) Then
                Return False
            End If

            If right.NodeType <> ExpressionType.Convert OrElse right.Type IsNot GetType(Object) Then
                Return False
            End If

            Return True
        End Function
    End Class
End Namespace
