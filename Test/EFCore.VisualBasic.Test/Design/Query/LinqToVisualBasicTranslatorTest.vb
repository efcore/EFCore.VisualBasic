Imports System.Linq.Expressions
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports EntityFrameworkCore.VisualBasic.Design.Internal
Imports EntityFrameworkCore.VisualBasic.Design.Query.Internal
Imports Microsoft.CodeAnalysis
Imports Microsoft.EntityFrameworkCore.Query
Imports Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal
Imports Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal
Imports Microsoft.EntityFrameworkCore.Storage
Imports Microsoft.EntityFrameworkCore.TestUtilities
Imports Xunit
Imports Xunit.Abstractions
Imports Xunit.Sdk
Imports Assert = Xunit.Assert
Imports E = System.Linq.Expressions.Expression

Namespace Design.Query
    Public Class LinqToVisualBasicTranslatorTest

        Private ReadOnly _testOutputHelper As ITestOutputHelper

        Public Sub New(TestOutputHelper As ITestOutputHelper)
            _testOutputHelper = TestOutputHelper
            _outputExpressionTrees = True
        End Sub

        <Theory>
        <InlineData("hello", """hello""")>
        <InlineData("A"c, """A""c")>
        <InlineData(1, "1")>
        <InlineData(1L, "1L")>
        <InlineData(1S, "1S")>
        <InlineData(CSByte(1), "CSByte(1)")>
        <InlineData(1UI, "1UI")>
        <InlineData(1UL, "1UL")>
        <InlineData(1US, "1US")>
        <InlineData(CByte(1), "CByte(1)")>
        <InlineData(1.5R, "1.5R")>
        <InlineData(1.5F, "1.5F")>
        <InlineData(True, "True")>
        <InlineData(GetType(String), "GetType(String)")>
        Public Sub Constant_values(constantValue As Object, literalRepresentation As String)
            AssertExpression(
                E.Constant(constantValue),
                literalRepresentation)
        End Sub

        <Fact>
        Public Sub Constant_decimal()
            AssertExpression(E.Constant(1.5D), "1.5D")
        End Sub

        <Fact>
        Public Sub Constant_nothing()
            AssertExpression(E.Constant(Nothing, GetType(String)), "Nothing")
        End Sub

        <Fact>
        Public Sub Constant_throws_on_unsupported_type()
            Assert.Throws(Of NotSupportedException)(Sub() AssertExpression(E.Constant(CType(Nothing, DateTime)), ""))
        End Sub

        <Fact>
        Public Sub [Enum]()
            AssertExpression(E.Constant(SomeEnum.One), "SomeEnum.One")
        End Sub

        <Fact>
        Public Sub Enum_with_multiple_values()
            AssertExpression(E.Constant(SomeEnum.One Or SomeEnum.Two), "SomeEnum.One Or SomeEnum.Two")
        End Sub

        <Fact>
        Public Sub Enum_with_unknown_value()
            AssertExpression(E.Constant(CType(1000, SomeEnum)), "CType(1000L, LinqToVisualBasicTranslatorTest.SomeEnum)")
        End Sub

        <Theory>
        <InlineData(ExpressionType.Add, "+")>
        <InlineData(ExpressionType.AddChecked, "+")>
        <InlineData(ExpressionType.Subtract, "-")>
        <InlineData(ExpressionType.SubtractChecked, "-")>
        <InlineData(ExpressionType.Multiply, "*")>
        <InlineData(ExpressionType.MultiplyChecked, "*")>
        <InlineData(ExpressionType.Divide, "/")>
        <InlineData(ExpressionType.Modulo, "Mod")>
        <InlineData(ExpressionType.GreaterThan, ">")>
        <InlineData(ExpressionType.GreaterThanOrEqual, ">=")>
        <InlineData(ExpressionType.LessThan, "<")>
        <InlineData(ExpressionType.LessThanOrEqual, "<=")>
        <InlineData(ExpressionType.And, "And")>
        <InlineData(ExpressionType.Or, "Or")>
        <InlineData(ExpressionType.ExclusiveOr, "Xor")>
        <InlineData(ExpressionType.LeftShift, "<<")>
        <InlineData(ExpressionType.RightShift, ">>")>
        Public Sub Binary_numeric(expressionType As ExpressionType, op As String)
            AssertExpression(
                E.MakeBinary(expressionType, E.Constant(2), E.Constant(3)),
                $"2 {op} 3")
        End Sub

        <Fact>
        Public Sub Binary_Equal_value_type()
            AssertExpression(
                E.Equal(E.Constant(2), E.Constant(3)),
                "2 = 3")
        End Sub

        <Fact>
        Public Sub Binary_Equal_reference_type()
            AssertExpression(
                E.Equal(E.Parameter(GetType(String), "s1"), E.Parameter(GetType(String), "s2")),
                "s1 = s2")
        End Sub

        <Fact>
        Public Sub Binary_ReferenceEqual()
            AssertExpression(
                E.ReferenceEqual(E.Parameter(GetType(Blog), "b1"), E.Parameter(GetType(Blog), "b2")),
                "b1 Is b2")
        End Sub

        <Fact>
        Public Sub Binary_Equal_with_reference_equality_semantics()
            AssertExpression(
                E.Equal(E.Convert(E.Parameter(GetType(Blog), "b1"), GetType(Object)),
                        E.Convert(E.Parameter(GetType(Blog), "b2"), GetType(Object))),
                "CType(b1, Object) Is CType(b2, Object)")
        End Sub

        <Fact>
        Public Sub Binary_NotEqual_value_type()
            AssertExpression(
                E.NotEqual(E.Constant(2), E.Constant(3)),
                "2 <> 3")
        End Sub

        <Fact>
        Public Sub Binary_NotEqual_reference_type()
            AssertExpression(
                E.NotEqual(E.Parameter(GetType(String), "s1"), E.Parameter(GetType(String), "s2")),
                "s1 <> s2")
        End Sub

        <Fact>
        Public Sub Binary_ReferenceNotEqual()
            AssertExpression(
                E.ReferenceNotEqual(E.Parameter(GetType(Blog), "b1"), E.Parameter(GetType(Blog), "b2")),
                "b1 IsNot b2")
        End Sub

        <Fact>
        Public Sub Binary_ArrayIndex()
            AssertExpression(
                E.ArrayIndex(E.Parameter(GetType(Integer()), "i"), E.Constant(2)),
                "i(2)")
        End Sub

        <Fact>
        Public Sub Binary_Power()
            AssertExpression(
                E.Power(E.Constant(2.0), E.Constant(3.0)),
                "2R ^ 3R")
        End Sub

        <Fact>
        Public Sub Binary_Coalesce()
            AssertExpression(
                E.Coalesce(E.Parameter(GetType(Double?), "d"), E.Constant(3.0)),
                "If(d, 3R)")
        End Sub

        <Theory>
        <InlineData(ExpressionType.AndAlso, "AndAlso")>
        <InlineData(ExpressionType.OrElse, "OrElse")>
        <InlineData(ExpressionType.And, "And")>
        <InlineData(ExpressionType.Or, "Or")>
        <InlineData(ExpressionType.ExclusiveOr, "Xor")>
        Public Sub Binary_boolean(expressionType As ExpressionType, op As String)
            AssertExpression(
                E.MakeBinary(expressionType, E.Constant(True), E.Constant(False)),
                $"True {op} False")
        End Sub

        <Theory>
        <InlineData(ExpressionType.Assign, "=")>
        <InlineData(ExpressionType.AddAssign, "+=")>
        <InlineData(ExpressionType.AddAssignChecked, "+=")>
        <InlineData(ExpressionType.SubtractAssign, "-=")>
        <InlineData(ExpressionType.SubtractAssignChecked, "-=")>
        <InlineData(ExpressionType.MultiplyAssign, "*=")>
        <InlineData(ExpressionType.MultiplyAssignChecked, "*=")>
        <InlineData(ExpressionType.DivideAssign, "/=")>
        <InlineData(ExpressionType.LeftShiftAssign, "<<=")>
        <InlineData(ExpressionType.RightShiftAssign, ">>=")>
        Public Sub Binary_Assign_numeric(expressionType As ExpressionType, op As String)
            AssertStatement(
                E.MakeBinary(expressionType, E.Variable(GetType(Integer), "x"), E.Constant(3)),
                $"x {op} 3")
        End Sub

        <Fact>
        Public Sub Binary_PowerAssign()
            AssertStatement(
                E.PowerAssign(E.Variable(GetType(Double), "x"), E.Constant(3.0)),
                $"x ^= 3R")
        End Sub

        <Theory>
        <InlineData(ExpressionType.AndAssign, "l = l And 3")>
        <InlineData(ExpressionType.OrAssign, "l = l Or 3")>
        <InlineData(ExpressionType.ExclusiveOrAssign, "l = l Xor 3")>
        Public Sub Binary_transformed_Assign_numeric(expressionType As ExpressionType, result As String)
            AssertStatement(
                E.MakeBinary(expressionType, E.Variable(GetType(Integer), "l"), E.Constant(3)),
                result)
        End Sub

        <Theory>
        <InlineData(ExpressionType.Negate, "-(i)")>
        <InlineData(ExpressionType.NegateChecked, "-(i)")>
        <InlineData(ExpressionType.Not, "Not(i)")>
        <InlineData(ExpressionType.OnesComplement, "Not(i)")>
        <InlineData(ExpressionType.UnaryPlus, "+i")>
        <InlineData(ExpressionType.Increment, "i + 1")>
        <InlineData(ExpressionType.Decrement, "i - 1")>
        Public Sub Unary_expression_int(expressionType As ExpressionType, expected As String)
            AssertExpression(
                  E.MakeUnary(expressionType, E.Parameter(GetType(Integer), "i"), GetType(Integer)),
                  expected)
        End Sub

        <Theory>
        <InlineData(ExpressionType.Not, "Not(b)")>
        <InlineData(ExpressionType.IsFalse, "Not(b)")>
        <InlineData(ExpressionType.IsTrue, "b")>
        Public Sub Unary_expression_bool(expressionType As ExpressionType, expected As String)
            AssertExpression(
                E.MakeUnary(expressionType, E.Parameter(GetType(Boolean), "b"), GetType(Boolean)),
                expected)
        End Sub

        <Theory>
        <InlineData(ExpressionType.PostIncrementAssign)>
        <InlineData(ExpressionType.PostDecrementAssign)>
        <InlineData(ExpressionType.PreIncrementAssign)>
        <InlineData(ExpressionType.PreDecrementAssign)>
        Public Sub Not_supported_Unary_statement(expressionType As ExpressionType)
            Dim i = E.Parameter(GetType(Integer), "i")

            Assert.Throws(Of NotSupportedException)(
                Sub() AssertStatement(
                E.Block(
                    variables:={i},
                E.MakeUnary(expressionType, i, GetType(Integer))), "")
            )
        End Sub

        <Fact>
        Public Sub Unary_ArrayLength()
            AssertExpression(
                E.ArrayLength(E.Parameter(GetType(Integer()), "i")),
                "i.Length")
        End Sub

        <Fact>
        Public Sub Unary_Convert()
            AssertExpression(
                E.Convert(
                    E.Parameter(GetType(Object), "i"),
                    GetType(String)),
                "CType(i, String)")
        End Sub

        <Fact>
        Public Sub Unary_Throw()
            AssertStatement(
                E.Throw(E.[New](GetType(Exception))),
                "Throw New Exception()")
        End Sub

        <Fact>
        Public Sub Unary_Unbox()
            AssertExpression(
                E.Unbox(E.Parameter(GetType(Object), "i"), GetType(Integer)),
                "i")
        End Sub

        <Fact>
        Public Sub Unary_Quote()
            Dim expr As Expression(Of Func(Of String, Integer)) = Function(s) s.Length
            AssertExpression(
                E.Quote(expr),
                "Function(s As String) s.Length")
        End Sub

        <Fact>
        Public Sub Unary_TypeAs_with_reference_type()
            AssertExpression(
                E.TypeAs(E.Parameter(GetType(Object), "i"), GetType(String)),
                "TryCast(i, String)")
        End Sub

        <Fact>
        Public Sub Unary_TypeAs_with_nullable_type()
            AssertExpression(
                E.TypeAs(E.Parameter(GetType(Object), "i"), GetType(Integer?)),
                "If(TypeOf i Is Integer, CType(i, Integer), New Nullable(Of Integer))")
        End Sub

        <Fact>
        Public Sub Instance_property()
            AssertExpression(
                E.Property(
                    E.Constant("hello"),
                    GetType(String).GetProperty(NameOf(String.Length))),
                """hello"".Length")
        End Sub

        <Fact>
        Public Sub Static_property()
            AssertExpression(
                E.Property(
                    Nothing,
                    GetType(DateTime).GetProperty(NameOf(DateTime.Now))),
                "DateTime.Now")
        End Sub

        <Fact>
        Public Sub Private_instance_field_read()
            AssertExpression(
                E.Field(E.Parameter(GetType(Blog), "blog"), "_privateField"),
                "GetType(LinqToVisualBasicTranslatorTest.Blog).GetField(""_privateField"", BindingFlags.Instance Or BindingFlags.NonPublic).GetValue(blog)")
        End Sub

        <Fact>
        Public Sub Private_instance_field_write()
            AssertStatement(
                E.Assign(
                    E.Field(E.Parameter(GetType(Blog), "blog"), "_privateField"),
                    E.Constant(8)),
                "GetType(LinqToVisualBasicTranslatorTest.Blog).GetField(""_privateField"", BindingFlags.Instance Or BindingFlags.NonPublic).SetValue(blog, 8)")
        End Sub

        <Fact>
        Public Sub Internal_instance_field_read()
            AssertExpression(
                E.Field(E.Parameter(GetType(Blog), "blog"), "InternalField"),
                "blog.InternalField")
        End Sub

        <Fact>
        Public Sub [Not]()
            AssertExpression(
                E.Not(E.Constant(True)),
                "Not(True)")
        End Sub

        <Fact>
        Public Sub MemberInit_with_MemberAssignment()
            AssertExpression(
                E.MemberInit(
                    E.[New](
                        GetType(Blog).GetConstructor({GetType(String)}),
                        E.Constant("foo")),
                    E.Bind(GetType(Blog).GetProperty(NameOf(Blog.PublicProperty)), E.Constant(8)),
                    E.Bind(GetType(Blog).GetField(NameOf(Blog.PublicField)), E.Constant(9))),
                "New LinqToVisualBasicTranslatorTest.Blog(""foo"") With {.PublicProperty = 8, .PublicField = 9}")
        End Sub

        <Fact>
        Public Sub MemberInit_with_MemberListBinding()
            AssertExpression(
                E.MemberInit(
                    E.[New](
                        GetType(Blog).GetConstructor({GetType(String)}),
                        E.Constant("foo")),
                    E.ListBind(GetType(Blog).GetProperty(NameOf(Blog.ListOfInts)),
                        E.ElementInit(GetType(List(Of Integer)).GetMethod(NameOf(List(Of Integer).Add)), E.Constant(8)),
                        E.ElementInit(GetType(List(Of Integer)).GetMethod(NameOf(List(Of Integer).Add)), E.Constant(9)))),
                "New LinqToVisualBasicTranslatorTest.Blog(""foo"") With {.ListOfInts = New List(Of Integer) From {8, 9}}")
        End Sub

        <Fact>
        Public Sub MemberInit_with_MemberMemberBinding()
            AssertExpression(
                E.MemberInit(
                    E.[New](
                        GetType(Blog).GetConstructor({GetType(String)}),
                        E.Constant("foo")),
                    E.MemberBind(GetType(Blog).GetProperty(NameOf(Blog.Details)),
                        E.Bind(GetType(BlogDetails).GetProperty(NameOf(BlogDetails.Foo)), E.Constant(5)),
                        E.ListBind(GetType(BlogDetails).GetProperty(NameOf(BlogDetails.ListOfInts)),
                            E.ElementInit(GetType(List(Of Integer)).GetMethod(NameOf(List(Of Integer).Add)), E.Constant(8)),
                            E.ElementInit(GetType(List(Of Integer)).GetMethod(NameOf(List(Of Integer).Add)), E.Constant(9))))),
                "New LinqToVisualBasicTranslatorTest.Blog(""foo"") With {.Details = New LinqToVisualBasicTranslatorTest.BlogDetails With {.Foo = 5, .ListOfInts = New List(Of Integer) From {8, 9}}}")
        End Sub

        <Fact>
        Public Sub Method_call_instance()

            Dim blogExpr = E.Parameter(GetType(Blog), "blog")

            AssertStatement(
                E.Block(
                    variables:={blogExpr},
                    E.Assign(blogExpr, E.[New](Blog.Constructor)),
                    E.Call(
                        blogExpr,
                        GetType(Blog).GetMethod(NameOf(Blog.SomeInstanceMethod)))),
"Dim blog As LinqToVisualBasicTranslatorTest.Blog = New LinqToVisualBasicTranslatorTest.Blog()
blog.SomeInstanceMethod()")
        End Sub

        <Fact>
        Public Sub Method_call_static()
            AssertExpression(
                E.Call(ReturnsIntWithParamMethod, E.Constant(8)),
                "LinqToVisualBasicTranslatorTest.ReturnsIntWithParam(8)")
        End Sub

        <Fact>
        Public Sub Method_call_static_on_nested_type()
            AssertExpression(
                E.Call(GetType(Blog).GetMethod(NameOf(Blog.Static_method_on_nested_type))),
                "LinqToVisualBasicTranslatorTest.Blog.Static_method_on_nested_type()")
        End Sub

        <Fact>
        Public Sub Method_call_extension()
            Dim blog = E.Parameter(GetType(LinqExpressionToRoslynTranslatorExtensionType), "someType")

            AssertStatement(
                E.Block(
                    variables:={blog},
                    E.Assign(blog, E.[New](LinqExpressionToRoslynTranslatorExtensionType.Constructor)),
                    E.Call(LinqExpressionToRoslynTranslatorExtensions.SomeExtensionMethod, blog)),
"Dim someType As LinqExpressionToRoslynTranslatorExtensionType = New LinqExpressionToRoslynTranslatorExtensionType()
someType.SomeExtension()")
        End Sub

        <Fact>
        Public Sub Method_call_extension_with_null_this()
            AssertExpression(
                E.Call(
                    LinqExpressionToRoslynTranslatorExtensions.SomeExtensionMethod,
                    E.Constant(Nothing, GetType(LinqExpressionToRoslynTranslatorExtensionType))),
                "LinqExpressionToRoslynTranslatorExtensions.SomeExtension(Nothing)")
        End Sub

        <Fact>
        Public Sub Method_call_generic()
            Dim blogExpr = E.Parameter(GetType(Blog), "blog")

            AssertStatement(
                E.Block(
                    variables:={blogExpr},
                    E.Assign(blogExpr, E.[New](Blog.Constructor)),
                    E.Call(
                        GenericMethod.MakeGenericMethod(GetType(Blog)),
                        blogExpr)),
"Dim blog As LinqToVisualBasicTranslatorTest.Blog = New LinqToVisualBasicTranslatorTest.Blog()
LinqToVisualBasicTranslatorTest.GenericMethodImplementation(blog)")
        End Sub

        <Fact>
        Public Sub Method_call_namespace_is_collected()
            Dim translator = CreateTranslator().Item1
            Dim namespaces As New HashSet(Of String)()
            translator.TranslateExpression(E.Call(FooMethod), namespaces)
            Assert.Collection(namespaces,
                              Sub(ns) Assert.Equal(GetType(LinqToVisualBasicTranslatorTest).Namespace, ns))
        End Sub

        <Fact>
        Public Sub Instantiation()
            AssertExpression(
                E.[New](
                    GetType(Blog).GetConstructor({GetType(String)}),
                    E.Constant("foo")),
                "New LinqToVisualBasicTranslatorTest.Blog(""foo"")")
        End Sub

        <Fact>
        Public Sub Lambda_with_expression_body()
            AssertExpression(
               E.Lambda(Of Func(Of Boolean))(E.Constant(True)),
               "Function() True")
        End Sub

        <Fact>
        Public Sub Lambda_with_block_body()
            Dim i = E.Parameter(GetType(Integer), "i")

            AssertExpression(
                E.Lambda(Of Func(Of Integer))(
                    E.Block(
                        variables:={i},
                        E.Assign(i, E.Constant(8)),
                        i)),
"Function() As Integer
    Dim i As Integer = 8
    Return i
End Function")
        End Sub

        <Fact>
        Public Sub Lambda_procedure_single_line()

            AssertExpression(
                E.Lambda(Of Action)(
                    E.Call(FooMethod)),
                "Sub() LinqToVisualBasicTranslatorTest.Foo()")
        End Sub

        <Fact>
        Public Sub Lambda_procedure_with_block_body()
            Dim i = E.Parameter(GetType(Integer), "i")

            AssertExpression(
                E.Lambda(Of Action)(
                    E.Block(
                        variables:={i},
                        E.Assign(i, E.Constant(8)),
                        i)),
"Sub()
    Dim i As Integer = 8
End Sub")
        End Sub

        <Fact>
        Public Sub Lambda_with_no_parameters()
            AssertExpression(
                E.Lambda(Of Func(Of Boolean))(E.Constant(True)),
                "Function() True")
        End Sub

        <Fact>
        Public Sub Lambda_with_one_parameter()
            Dim i = E.Parameter(GetType(Integer), "i")

            AssertExpression(
                E.Lambda(Of Func(Of Integer, Boolean))(E.Constant(True), i),
                "Function(i As Integer) True")
        End Sub

        <Fact>
        Public Sub Lambda_with_two_parameters()
            Dim i = E.Parameter(GetType(Integer), "i")
            Dim j = E.Parameter(GetType(Integer), "j")

            AssertExpression(
                E.Lambda(Of Func(Of Integer, Integer, Integer))(E.Add(i, j), i, j),
                "Function(i As Integer, j As Integer) i + j")
        End Sub

        <Fact>
        Public Sub Lambda_parameter_names_are_made_unique()
            Dim i = E.Parameter(GetType(Integer), "i")
            Dim i0 = E.Parameter(GetType(Integer), "i")
            Dim j = E.Parameter(GetType(Integer), "j")

            AssertStatement(
                E.Block(
                    variables:={i},
                    E.Lambda(Of Func(Of Integer, Integer, Integer))(E.Add(E.Add(i, j), i0), i0, j)),
"Dim i As Integer
Dim unnamed = Function(i0 As Integer, j As Integer) i + j + i0")
        End Sub

        <Fact>
        Public Sub Invocation_with_literal_argument()
            Dim expr As Expression(Of Func(Of Integer, Boolean)) = Function(f) f > 5

            AssertExpression(
                E.AndAlso(
                    E.Constant(True),
                    E.Invoke(expr, E.Constant(8))),
                "True AndAlso 8 > 5")
        End Sub

        <Fact>
        Public Sub Invocation_with_argument_that_has_side_effects()
            Dim i = E.Parameter(GetType(Integer), "i")
            Dim expr As Expression(Of Func(Of Integer, Integer)) = Function(f) f + f

            AssertStatement(
                E.Block(
                    variables:={i},
                    E.Assign(
                        i,
                        E.Add(
                            E.Constant(5),
                            E.Invoke(expr, E.Call(FooMethod))))),
"Dim f = LinqToVisualBasicTranslatorTest.Foo()
Dim i As Integer = 5 + f + f")
        End Sub

        <Fact>
        Public Sub Conditional_expression()
            AssertExpression(
                E.Condition(E.Constant(True), E.Constant(1), E.Constant(2)),
                "If(True, 1, 2)")
        End Sub

        <Fact>
        Public Sub Conditional_without_false_value_fails()
            Assert.Throws(Of NotSupportedException)(
                Sub() AssertExpression(
                        E.IfThen(E.Constant(True), E.Constant(8)),
                        "If(True, 8, )"))
        End Sub

        <Fact>
        Public Sub Conditional_statement()
            AssertStatement(
                E.Block(
                    E.Condition(E.Constant(True), E.Call(FooMethod), E.Call(BarMethod)),
                    E.Constant(8)),
"If True Then
    LinqToVisualBasicTranslatorTest.Foo()
Else
    LinqToVisualBasicTranslatorTest.Bar()
End If")
        End Sub

        <Fact>
        Public Sub IfThen_statement()

            Dim parameter = E.Parameter(GetType(Integer), "i")
            Dim block = E.Block(
                variables:={parameter},
                expressions:={E.Assign(parameter, E.Constant(8))})

            AssertStatement(
                E.Block(E.IfThen(E.Constant(True), block)),
"If True Then
    Dim i As Integer = 8
End If")
        End Sub

        <Fact>
        Public Sub IfThenElse_statement()
            Dim parameter1 = E.Parameter(GetType(Integer), "i")
            Dim block1 = E.Block(
                variables:={parameter1},
                expressions:={E.Assign(parameter1, E.Constant(8))})

            Dim parameter2 = E.Parameter(GetType(Integer), "j")
            Dim block2 = E.Block(
                variables:={parameter2},
                expressions:={E.Assign(parameter2, E.Constant(9))})

            AssertStatement(
                E.Block(E.IfThenElse(E.Constant(True), block1, block2)),
"If True Then
    Dim i As Integer = 8
Else
    Dim j As Integer = 9
End If")
        End Sub

        <Fact>
        Public Sub IfThenElse_nested()

            Dim Variable = E.Parameter(GetType(Integer), "i")

            AssertStatement(
                E.Block(
                    variables:={Variable},
                    expressions:={E.IfThenElse(
                        E.Constant(True),
                        E.Block(E.Assign(Variable, E.Constant(1))),
                        E.IfThenElse(
                            E.Constant(False),
                            E.Block(E.Assign(Variable, E.Constant(2))),
                            E.IfThenElse(
                                E.Constant(False),
                                E.Block(E.Assign(Variable, E.Constant(3))),
                                E.Block({E.Assign(Variable, E.Constant(4)),
                                         E.Add(E.Constant(5), E.Constant(6))}))))}),
"Dim i As Integer
If True Then
    i = 1
ElseIf False
    i = 2
ElseIf False
    i = 3
Else
    i = 4
    Dim unnamed = 5 + 6
End If")
        End Sub

        <Fact>
        Public Sub Conditional_expression_with_block_in_lambda()
            AssertExpression(
                E.Lambda(Of Func(Of Integer))(
                    E.Condition(
                        E.Constant(True),
                        E.Block(
                            E.Call(FooMethod),
                            E.Constant(8)),
                        E.Constant(9))),
"Function() As Integer
    If True Then
        LinqToVisualBasicTranslatorTest.Foo()
        Return 8
    Else
        Return 9
    End If
End Function")
        End Sub

        <Fact>
        Public Sub IfThen_with_block_inside_expression_block_with_lifted_statements()
            Dim i = E.Parameter(GetType(Integer), "i")

            AssertStatement(
                E.Block(
                    variables:={i},
                    E.Assign(
                        i, E.Block( ' We're in expression context. Do anything that will get lifted.
                            E.Call(FooMethod), ' Statement condition
                            E.IfThen(
                                E.Constant(True),
                                E.Block(
                                   E.Call(BarMethod),
                                   E.Call(BazMethod))), ' Last expression (to make the block above evaluate as statement
                            E.Constant(8)))),
"LinqToVisualBasicTranslatorTest.Foo()
If True Then
    LinqToVisualBasicTranslatorTest.Bar()
    LinqToVisualBasicTranslatorTest.Baz()
End If
Dim i As Integer = 8")
        End Sub

        <Fact>
        Public Sub Switch_expression()
            AssertExpression(
                E.Switch(
                    E.Constant(8),
                    E.Constant(0),
                    E.SwitchCase(E.Constant(-9), E.Constant(9)),
                    E.SwitchCase(E.Constant(-10), E.Constant(10))),
                "If(8 = 9, -9, If(8 = 10, -10, 0))")
        End Sub

        <Fact>
        Public Sub Switch_expression_nested()
            Dim i = E.Parameter(GetType(Integer), "i")
            Dim j = E.Parameter(GetType(Integer), "j")
            Dim k = E.Parameter(GetType(Integer), "k")

            AssertStatement(
                E.Block(
                    variables:={i, j, k},
                    E.Assign(j, E.Constant(8)),
                    E.Assign(
                        i,
                        E.Switch(
                            j,
                            defaultBody:=E.Constant(0),
                            E.SwitchCase(E.Constant(1), E.Constant(100)),
                            E.SwitchCase(
                                E.Switch(
                                    k,
                                    defaultBody:=E.Constant(0),
                                    E.SwitchCase(E.Constant(2), E.Constant(200)),
                                    E.SwitchCase(E.Constant(3), E.Constant(300))),
                                E.Constant(200))))),
"Dim k As Integer
Dim j As Integer = 8
Dim i As Integer = If(j = 100, 1, If(j = 200, If(k = 200, 2, If(k = 300, 3, 0)), 0))")
        End Sub

        <Fact>
        Public Sub Switch_expression_with_reference_equality()
            AssertExpression(
                E.Switch(
                    E.Parameter(GetType(Blog), "blog1"),
                    E.Constant(0),
                    E.SwitchCase(E.Constant(2), E.Parameter(GetType(Blog), "blog2")),
                    E.SwitchCase(E.Constant(3), E.Parameter(GetType(Blog), "blog3"))),
                "If(blog1 Is blog2, 2, If(blog1 Is blog3, 3, 0))")
        End Sub

        <Fact>
        Public Sub Switch_statement_with_reference_equality()
            AssertStatement(
                E.Switch(
                    E.Convert(E.Parameter(GetType(Blog), "blog1"), GetType(Object)),
                    E.Constant(0),
                    E.SwitchCase(E.Constant(1), E.Convert(E.Parameter(GetType(Blog), "blog2"), GetType(Object))),
                    E.SwitchCase(E.Constant(2), E.Convert(E.Parameter(GetType(Blog), "blog3"), GetType(Object)))),
"If CType(blog1, Object) Is CType(blog2, Object) Then
    Dim unnamed = 1
ElseIf CType(blog1, Object) Is CType(blog3, Object)
    Dim unnamed = 2
Else
    Dim unnamed = 0
End If")
        End Sub

        <Fact>
        Public Sub Switch_statement_without_default()
            Dim parameter = E.Parameter(GetType(Integer), "i")

            AssertStatement(
                E.Block(
                    variables:={parameter},
                    expressions:={E.Switch(
                        E.Constant(7),
                        E.SwitchCase(E.Block(GetType(Void), E.Assign(parameter, E.Constant(9))), E.Constant(-9)),
                        E.SwitchCase(E.Block(GetType(Void), E.Assign(parameter, E.Constant(10))), E.Constant(-10)))}),
"Dim i As Integer
Select 7
    Case -9
        i = 9
    Case -10
        i = 10
End Select")
        End Sub

        <Fact>
        Public Sub Switch_statement_with_default()
            Dim parameter = E.Parameter(GetType(Integer), "i")

            AssertStatement(
                E.Block(
                    variables:={parameter},
                    expressions:={E.Switch(
                        E.Constant(7),
                        E.Assign(parameter, E.Constant(0)),
                        E.SwitchCase(E.Assign(parameter, E.Constant(9)), E.Constant(-9)),
                        E.SwitchCase(E.Assign(parameter, E.Constant(10)), E.Constant(-10)))}),
"Dim i As Integer
Select 7
    Case -9
        i = 9
    Case -10
        i = 10
    Case Else
        i = 0
End Select")
        End Sub

        <Fact>
        Public Sub Switch_statement_with_multiple_labels()

            Dim parameter = E.Parameter(GetType(Integer), "i")

            AssertStatement(
                E.Block(
                    variables:={parameter},
                    expressions:={E.Switch(
                        E.Constant(7),
                        E.Assign(parameter, E.Constant(0)),
                        E.SwitchCase(E.Assign(parameter, E.Constant(9)), E.Constant(-9), E.Constant(-8)),
                        E.SwitchCase(E.Assign(parameter, E.Constant(10)), E.Constant(-10)))}),
"Dim i As Integer
Select 7
    Case -9, -8
        i = 9
    Case -10
        i = 10
    Case Else
        i = 0
End Select")
        End Sub

        <Fact>
        Public Sub Variable_assignment_uses_Dim()
            Dim i = E.Parameter(GetType(Integer), "i")

            AssertStatement(
                E.Block(
                    variables:={i},
                    E.Assign(i, E.Constant(8))),
"Dim i As Integer = 8")
        End Sub

        <Fact>
        Public Sub Variable_assignment_to_nothing()
            Dim s = E.Parameter(GetType(String), "s")

            AssertStatement(
                E.Block(
                    variables:={s},
                    E.Assign(s, E.Constant(Nothing, GetType(String)))),
                "Dim s As String = Nothing")
        End Sub

        <Fact()>
        Public Sub Variables_with_same_name_in_sibling_blocks_do_get_renamed()
            Dim i1 = E.Parameter(GetType(Integer), "i")
            Dim i2 = E.Parameter(GetType(Integer), "i")

            AssertStatement(
                E.Block(
                    E.Block(
                        variables:={i1},
                        E.Assign(i1, E.Constant(8)),
                        E.Call(ReturnsIntWithParamMethod, i1)),
                    E.Block(
                        variables:={i2},
                        E.Assign(i2, E.Constant(8)),
                        E.Call(ReturnsIntWithParamMethod, i2))),
"Dim i As Integer = 8
LinqToVisualBasicTranslatorTest.ReturnsIntWithParam(i)
Dim i As Integer = 8
LinqToVisualBasicTranslatorTest.ReturnsIntWithParam(i)")
        End Sub

        <Fact>
        Public Sub Variable_with_same_name_in_child_block_gets_renamed()

            Dim i1 = E.Parameter(GetType(Integer), "i")
            Dim i2 = E.Parameter(GetType(Integer), "i")

            AssertStatement(
                E.Block(
                    variables:={i1},
                    E.Assign(i1, E.Constant(8)),
                    E.Call(ReturnsIntWithParamMethod, i1),
                    E.Block(
                        variables:={i2},
                        E.Assign(i2, E.Constant(8)),
                        E.Call(ReturnsIntWithParamMethod, i2),
                        E.Call(ReturnsIntWithParamMethod, i1))),
"Dim i As Integer = 8
LinqToVisualBasicTranslatorTest.ReturnsIntWithParam(i)
Dim i0 As Integer = 8
LinqToVisualBasicTranslatorTest.ReturnsIntWithParam(i0)
LinqToVisualBasicTranslatorTest.ReturnsIntWithParam(i)")
        End Sub

        <Fact>
        Public Sub Variable_with_same_name_in_lambda_get_renamed()

            Dim i1 = E.Parameter(GetType(Integer), "i")
            Dim i2 = E.Parameter(GetType(Integer), "i")
            Dim f = E.Parameter(GetType(Func(Of Integer, Boolean)), "f")

            AssertStatement(
                E.Block(
                    variables:={i1},
                    E.Assign(i1, E.Constant(8)),
                    E.Assign(
                        f, E.Lambda(Of Func(Of Integer, Boolean))(
                            E.Equal(i2, E.Constant(5)),
                            i2))),
"Dim i As Integer = 8
f = Function(i0 As Integer) i0 = 5")
        End Sub

        <Fact>
        Public Sub Same_parameter_instance_is_used_twice_in_nested_lambdas()
            Dim f1 = E.Parameter(GetType(Func(Of Integer, Boolean)), "f1")
            Dim f2 = E.Parameter(GetType(Func(Of Integer, Boolean)), "f2")
            Dim i = E.Parameter(GetType(Integer), "i")

            AssertStatement(
                E.Assign(
                    f1,
                    E.Lambda(Of Func(Of Integer, Boolean))(
                        E.Block(
                            E.Assign(
                                f2,
                                E.Lambda(Of Func(Of Integer, Boolean))(
                                    E.Equal(i, E.Constant(5)),
                                    i)),
                            E.Constant(True)),
                        i)),
"f1 = Function(i As Integer) As Boolean
    f2 = Function(i0 As Integer) i0 = 5
    Return True
End Function")
        End Sub

        <Fact>
        Public Sub Block_with_non_standalone_expression_as_statement()
            AssertStatement(
                E.Block(E.Add(E.Constant(1), E.Constant(2))),
                "Dim unnamed = 1 + 2")
        End Sub

        <Fact>
        Public Sub Lift_block_in_assignment_context()
            Dim i = E.Parameter(GetType(Integer), "i")
            Dim j = E.Parameter(GetType(Integer), "j")

            AssertStatement(
                E.Block(
                    variables:={i},
                    E.Assign(i, E.Block(
                        variables:={j},
                        E.Assign(j, E.Call(FooMethod)),
                        E.Call(ReturnsIntWithParamMethod, j)))),
"Dim j As Integer = LinqToVisualBasicTranslatorTest.Foo()
Dim i As Integer = LinqToVisualBasicTranslatorTest.ReturnsIntWithParam(j)")
        End Sub

        <Fact>
        Public Sub Lift_block_in_method_call_context()
            AssertStatement(
                E.Block(
                    E.Call(
                        ReturnsIntWithParamMethod,
                        E.Block(
                            E.Call(FooMethod),
                            E.Call(BarMethod)))),
"LinqToVisualBasicTranslatorTest.Foo()
LinqToVisualBasicTranslatorTest.ReturnsIntWithParam(LinqToVisualBasicTranslatorTest.Bar())")
        End Sub

        <Fact>
        Public Sub Lift_nested_block()
            Dim i = E.Parameter(GetType(Integer), "i")
            Dim j = E.Parameter(GetType(Integer), "j")

            AssertStatement(
                E.Block(variables:={i},
                E.Assign(
                    i,
                    E.Block(
                        variables:={j},
                        E.Assign(j, E.Call(FooMethod)),
                        E.Block(
                            E.Call(BarMethod),
                            E.Call(ReturnsIntWithParamMethod, j))))),
"Dim j As Integer = LinqToVisualBasicTranslatorTest.Foo()
LinqToVisualBasicTranslatorTest.Bar()
Dim i As Integer = LinqToVisualBasicTranslatorTest.ReturnsIntWithParam(j)")
        End Sub

        <Fact>
        Public Sub Binary_lifts_left_side_if_right_is_lifted()

            Dim i = E.Parameter(GetType(Integer), "i")

            AssertStatement(
                E.Block(
                    variables:={i},
                    E.Assign(i,
                        E.Add(
                            E.Call(FooMethod),
                            E.Block(
                                E.Call(BarMethod),
                                E.Call(BazMethod))))),
"Dim lifted = LinqToVisualBasicTranslatorTest.Foo()
LinqToVisualBasicTranslatorTest.Bar()
Dim i As Integer = lifted + LinqToVisualBasicTranslatorTest.Baz()")
        End Sub

        <Fact>
        Public Sub Binary_does_not_lift_left_side_if_it_has_no_side_effects()
            Dim i = E.Parameter(GetType(Integer), "i")

            AssertStatement(
                E.Block(
                    variables:={i},
                    E.Assign(i,
                        E.Add(
                            E.Constant(5),
                            E.Block(
                                E.Call(BarMethod),
                                E.Call(BazMethod))))),
"LinqToVisualBasicTranslatorTest.Bar()
Dim i As Integer = 5 + LinqToVisualBasicTranslatorTest.Baz()")
        End Sub

        <Fact>
        Public Sub Method_lifts_earlier_args_if_later_arg_is_lifted()
            Dim i = E.Parameter(GetType(Integer), "i")

            AssertStatement(
                E.Block(
                    variables:={i},
                    E.Assign(i,
                        E.Call(
                            GetType(LinqToVisualBasicTranslatorTest).GetMethod(NameOf(MethodWithSixParams)),
                            E.Call(FooMethod),
                            E.Constant(5),
                            E.Block(E.Call(BarMethod), E.Call(BazMethod)),
                            E.Call(FooMethod),
                            E.Block(E.Call(BazMethod), E.Call(BarMethod)),
                            E.Call(FooMethod)))),
"Dim liftedArg = LinqToVisualBasicTranslatorTest.Foo()
LinqToVisualBasicTranslatorTest.Bar()
Dim liftedArg0 = LinqToVisualBasicTranslatorTest.Baz()
Dim liftedArg1 = LinqToVisualBasicTranslatorTest.Foo()
LinqToVisualBasicTranslatorTest.Baz()
Dim i As Integer = LinqToVisualBasicTranslatorTest.MethodWithSixParams(liftedArg, 5, liftedArg0, liftedArg1, LinqToVisualBasicTranslatorTest.Bar(), LinqToVisualBasicTranslatorTest.Foo())")
        End Sub

        <Fact>
        Public Sub New_lifts_earlier_args_if_later_arg_is_lifted()
            Dim b = E.Parameter(GetType(Blog), "b")

            AssertStatement(
                E.Block(
                    variables:={b},
                    E.Assign(b,
                        E.[New](
                            GetType(Blog).GetConstructor({GetType(Integer), GetType(Integer)}),
                            E.Call(FooMethod),
                            E.Block(
                                E.Call(BarMethod),
                                E.Call(BazMethod))))),
"Dim liftedArg = LinqToVisualBasicTranslatorTest.Foo()
LinqToVisualBasicTranslatorTest.Bar()
Dim b As LinqToVisualBasicTranslatorTest.Blog = New LinqToVisualBasicTranslatorTest.Blog(liftedArg, LinqToVisualBasicTranslatorTest.Baz())")
        End Sub

        <Fact(Skip:="TODO: Implement")>
        Public Sub Index_lifts_earlier_args_if_later_arg_is_lifted()
            ' TODO: Implement
        End Sub

        <Fact>
        Public Sub New_array()
            AssertExpression(
                E.NewArrayInit(GetType(Integer)),
                "New Integer() {}")
        End Sub

        <Fact>
        Public Sub New_array_with_bounds()
            AssertExpression(
                E.NewArrayBounds(GetType(Integer), E.Constant(3)),
                "New Integer(3 - 1) {}")
        End Sub

        <Fact>
        Public Sub New_array_with_initializers()
            AssertExpression(
                E.NewArrayInit(GetType(Integer), E.Constant(3), E.Constant(4)),
                "New Integer() {3, 4}")
        End Sub

        <Fact>
        Public Sub New_array_lifts_earlier_args_if_later_arg_is_lifted()
            Dim a = E.Parameter(GetType(Integer()), "a")

            AssertStatement(
            E.Block(
                variables:={a},
                E.Assign(a,
                    E.NewArrayInit(
                        GetType(Integer),
                        E.Call(FooMethod),
                        E.Block(
                            E.Call(BarMethod),
                            E.Call(BazMethod))))),
"Dim liftedArg = LinqToVisualBasicTranslatorTest.Foo()
LinqToVisualBasicTranslatorTest.Bar()
Dim a As Integer() = New Integer() {liftedArg, LinqToVisualBasicTranslatorTest.Baz()}")
        End Sub

        <Fact>
        Public Sub Lift_variable_in_expression_block()

            Dim i = E.Parameter(GetType(Integer), "i")
            Dim j = E.Parameter(GetType(Integer), "j")

            AssertStatement(
            E.Block(
                variables:={i},
                E.Assign(i, E.Block(
                    variables:={j},
                    E.Block(
                        E.Call(FooMethod),
                        E.Assign(j, E.Constant(8)),
                        E.Constant(9))))),
"Dim j As Integer
LinqToVisualBasicTranslatorTest.Foo()
j = 8
Dim i As Integer = 9")
        End Sub

        <Fact>
        Public Sub Lift_block_in_lambda_body_expression()
            AssertExpression(
            E.Lambda(Of Func(Of Integer))(
                E.Call(
                    ReturnsIntWithParamMethod,
                    E.Block(
                        E.Call(FooMethod),
                        E.Call(BarMethod))),
                Array.Empty(Of ParameterExpression)()),
"Function() As Integer
    LinqToVisualBasicTranslatorTest.Foo()
    Return LinqToVisualBasicTranslatorTest.ReturnsIntWithParam(LinqToVisualBasicTranslatorTest.Bar())
End Function")
        End Sub

        <Fact>
        Public Sub Do_not_lift_block_in_lambda_body()
            AssertExpression(
            E.Lambda(Of Func(Of Integer))(
                E.Block(E.Block(E.Constant(8))),
                Array.Empty(Of ParameterExpression)()),
            "Function() Return 8")
        End Sub

        <Fact>
        Public Sub Simplify_block_with_single_expression()
            AssertStatement(
                E.Assign(E.Parameter(GetType(Integer), "i"), E.Block(E.Constant(8))),
                "i = 8")
        End Sub

        <Fact>
        Public Sub Cannot_lift_out_of_expression_context()
            Assert.Throws(Of NotSupportedException)(
                Sub() AssertExpression(
                        E.Assign(
                            E.Parameter(GetType(Integer), "i"),
                            E.Block(
                                E.Call(FooMethod),
                                E.Constant(8))),
                        ""))
        End Sub

        <Fact>
        Public Sub Lift_switch_expression()

            Dim i = E.Parameter(GetType(Integer), "i")
            Dim j = E.Parameter(GetType(Integer), "j")
            Dim k = E.Parameter(GetType(Integer), "k")

            AssertStatement(
            E.Block(
                variables:={i, j},
                E.Assign(j, E.Constant(8)),
                E.Assign(
                    i,
                    E.Switch(
                        j,
                        defaultBody:=E.Block(E.Constant(0)),
                        E.SwitchCase(
                            E.Block(
                                E.Block(
                                    E.Assign(k, E.Call(FooMethod)),
                                    E.Call(ReturnsIntWithParamMethod, k))),
                            E.Constant(8)),
                        E.SwitchCase(E.Constant(2), E.Constant(9))))),
"Dim i As Integer
Dim j As Integer = 8
Select j
    Case 8
        k = LinqToVisualBasicTranslatorTest.Foo()
        i = LinqToVisualBasicTranslatorTest.ReturnsIntWithParam(k)
    Case 9
        i = 2
    Case Else
        i = 0
End Select")
        End Sub

        <Fact>
        Public Sub Lift_nested_switch_expression()
            Dim i = E.Parameter(GetType(Integer), "i")
            Dim j = E.Parameter(GetType(Integer), "j")
            Dim k = E.Parameter(GetType(Integer), "k")
            Dim l = E.Parameter(GetType(Integer), "l")

            AssertStatement(
                E.Block(
                    variables:={i, j, k},
                    E.Assign(j, E.Constant(8)),
                    E.Assign(
                        i,
                        E.Switch(
                            j,
                            defaultBody:=E.Constant(0),
                            E.SwitchCase(E.Constant(1), E.Constant(100)),
                            E.SwitchCase(
                                E.Switch(
                                    k,
                                    defaultBody:=E.Constant(0),
                                    E.SwitchCase(
                                        E.Block(
                                            variables:={l},
                                            E.Assign(l, E.Call(FooMethod)),
                                            E.Call(ReturnsIntWithParamMethod, l)),
                                        E.Constant(200)),
                                    E.SwitchCase(E.Constant(3), E.Constant(300))),
                                E.Constant(200))))),
"Dim i As Integer
Dim k As Integer
Dim j As Integer = 8
Select j
    Case 100
        i = 1
    Case 200
        Select k
            Case 200
                Dim l As Integer = LinqToVisualBasicTranslatorTest.Foo()
                i = LinqToVisualBasicTranslatorTest.ReturnsIntWithParam(l)
            Case 300
                i = 3
            Case Else
                i = 0
        End Select

    Case Else
        i = 0
End Select")
        End Sub

        <Fact>
        Public Sub ListInit_node()
            AssertExpression(
                E.ListInit(
                    E.[New](GetType(List(Of Integer))),
                    GetType(List(Of Integer)).GetMethod(NameOf(List(Of Integer).Add)),
                    E.Constant(8),
                    E.Constant(9)),
                "New List(Of Integer)() From {8, 9}")
        End Sub

        <Fact>
        Public Sub TypeEqual_node()
            AssertExpression(
                E.TypeEqual(E.Parameter(GetType(Object), "p"), GetType(Integer)),
                "p.GetType() = GetType(Integer)")
        End Sub

        <Fact>
        Public Sub TypeIs_node()
            AssertExpression(
                E.TypeIs(E.Parameter(GetType(Object), "p"), GetType(Integer)),
                "TypeOf p Is Integer")
        End Sub

        <Fact>
        Public Sub Goto_with_named_label()
            Dim labelTarget = E.Label("label1")

            AssertStatement(
                E.Block(
                    E.Goto(labelTarget),
                    E.Label(labelTarget),
                    E.Call(FooMethod)),
"GoTo label1
label1:
LinqToVisualBasicTranslatorTest.Foo()")
        End Sub

        <Fact>
        Public Sub Goto_with_label_on_last_line()
            Dim labelTarget = E.Label("label1")

            AssertStatement(
                E.Block(
                     E.Goto(labelTarget),
                     E.Label(labelTarget)),
"GoTo label1
label1:")
        End Sub

        <Fact>
        Public Sub Goto_outside_label()

            Dim labelTarget = E.Label()

            AssertStatement(
                E.Block(
                    E.IfThen(
                        E.Constant(True),
                        E.Block(
                            E.Call(FooMethod),
                            E.Goto(labelTarget))),
                    E.Label(labelTarget)),
"If True Then
    LinqToVisualBasicTranslatorTest.Foo()
    GoTo unnamedLabel
End If
unnamedLabel:")
        End Sub

        <Fact>
        Public Sub Goto_with_unnamed_labels_in_sibling_blocks()
            Dim labelTarget1 = E.Label()
            Dim labelTarget2 = E.Label()

            AssertStatement(
            E.Block(
                E.Block(
                    E.Goto(labelTarget1),
                    E.Label(labelTarget1)),
                E.Block(
                    E.Goto(labelTarget2),
                    E.Label(labelTarget2))),
"GoTo unnamedLabel
unnamedLabel:
GoTo unnamedLabel0
unnamedLabel0:")
        End Sub

        <Fact>
        Public Sub Loop_statement_infinite()
            AssertStatement(
                E.Loop(E.Call(FooMethod)),
"While True
    LinqToVisualBasicTranslatorTest.Foo()
End While")
        End Sub

        <Fact>
        Public Sub Loop_statement_with_break_and_continue()
            Dim i = E.Parameter(GetType(Integer), "i")
            Dim breakLabel = E.Label()
            Dim continueLabel = E.Label()

            AssertStatement(
                E.Block(
                    variables:={i},
                    E.Assign(i, E.Constant(0)),
                    E.Loop(
                        E.Block(
                            E.IfThen(
                                E.Equal(i, E.Constant(100)),
                                E.Break(breakLabel)),
                            E.IfThen(
                                E.Equal(E.Modulo(i, E.Constant(2)), E.Constant(0)),
                                E.Continue(continueLabel)),
                            E.AddAssign(i, E.Constant(1, GetType(Integer)))),
                        breakLabel,
                        continueLabel)),
"Dim i As Integer = 0
While True
unnamedLabel0:
    If i = 100 Then
        GoTo unnamedLabel
    End If

    If i Mod 2 = 0 Then
        GoTo unnamedLabel0
    End If

    i += 1
End While
unnamedLabel:")
        End Sub

        <Fact>
        Public Sub Try_catch_statement()
            Dim expr = E.Parameter(GetType(InvalidOperationException), "e")

            AssertStatement(
                E.TryCatch(
                    E.Call(FooMethod),
                    E.Catch(expr, E.Call(BarMethod)),
                    E.Catch(expr, E.Call(BazMethod))),
"Try
    LinqToVisualBasicTranslatorTest.Foo()
Catch e As InvalidOperationException
    LinqToVisualBasicTranslatorTest.Bar()
Catch e As InvalidOperationException
    LinqToVisualBasicTranslatorTest.Baz()
End Try")
        End Sub

        <Fact>
        Public Sub Try_finally_statement()
            AssertStatement(
                E.TryFinally(
                    E.Call(FooMethod),
                    E.Call(BarMethod)),
"Try
    LinqToVisualBasicTranslatorTest.Foo()
Finally
    LinqToVisualBasicTranslatorTest.Bar()
End Try")
        End Sub

        <Fact>
        Public Sub Try_catch_finally_statement()

            Dim expr = E.Parameter(GetType(InvalidOperationException), "e")

            AssertStatement(
                E.TryCatchFinally(
                    E.Call(FooMethod),
                    E.Block(
                        E.Call(BarMethod),
                        E.Call(BazMethod)),
                    E.Catch(expr, E.Call(BarMethod)),
                    E.Catch(
                        expr,
                        E.Call(BazMethod),
                        E.Equal(
                            E.Property(expr, NameOf(Exception.Message)),
                            E.Constant("foo")))),
"Try
    LinqToVisualBasicTranslatorTest.Foo()
Catch e As InvalidOperationException
    LinqToVisualBasicTranslatorTest.Bar()
Catch e As InvalidOperationException When e.Message = ""foo""
    LinqToVisualBasicTranslatorTest.Baz()
Finally
    LinqToVisualBasicTranslatorTest.Bar()
    LinqToVisualBasicTranslatorTest.Baz()
End Try")
        End Sub

        <Fact>
        Public Sub Try_catch_statement_with_filter()
            Dim expr = E.Parameter(GetType(InvalidOperationException), "e")

            AssertStatement(
                E.TryCatch(
                    E.Call(FooMethod),
                    E.Catch(
                        expr,
                        E.Call(BarMethod),
                        E.Equal(
                            E.Property(expr, NameOf(Exception.Message)),
                            E.Constant("foo")))),
"Try
    LinqToVisualBasicTranslatorTest.Foo()
Catch e As InvalidOperationException When e.Message = ""foo""
    LinqToVisualBasicTranslatorTest.Bar()
End Try")
        End Sub

        <Fact>
        Public Sub Try_catch_statement_without_exception_reference()
            AssertStatement(
                E.TryCatch(
                    E.Call(FooMethod),
                    E.Catch(
                        GetType(InvalidOperationException),
                        E.Call(BarMethod))),
"Try
    LinqToVisualBasicTranslatorTest.Foo()
Catch unnamed As InvalidOperationException
    LinqToVisualBasicTranslatorTest.Bar()
End Try")
        End Sub

        <Fact>
        Public Sub Try_fault_statement()
            AssertStatement(
                E.TryFault(
                    E.Call(FooMethod),
                    E.Call(BarMethod)),
"Try
    LinqToVisualBasicTranslatorTest.Foo()
Catch
    LinqToVisualBasicTranslatorTest.Bar()
End Try")
        End Sub

        'TODO Try/Catch expressions

        Private Sub AssertStatement(expression As Expression, expected As String)
            AssertCore(expression, isStatement:=True, expected)
        End Sub

        Private Sub AssertExpression(expression As Expression, expected As String)
            AssertCore(expression, isStatement:=False, expected)
        End Sub

        Private Sub AssertCore(expression As Expression, isStatement As Boolean, expected As String)
            Dim typeMappingSource As New SqlServerTypeMappingSource(
                TestServiceFactory.Instance.Create(Of TypeMappingSourceDependencies)(),
                New RelationalTypeMappingSourceDependencies(Array.Empty(Of IRelationalTypeMappingSourcePlugin)()),
                New SqlServerSingletonOptions())

            Dim translator = New VisualBasicHelper(typeMappingSource)
            Dim namespaces = New HashSet(Of String)()
            Dim actual = If(isStatement,
                                translator.Statement(expression, namespaces),
                                translator.Expression(expression, namespaces))

            If _outputExpressionTrees Then
                _testOutputHelper.WriteLine("---- Input LINQ expression tree:")
                _testOutputHelper.WriteLine(_expressionPrinter.PrintExpression(expression))
            End If

            ' TODO Actually compile the output VB code To make sure it's valid.

            Try
                Assert.Equal(expected, actual.TrimEnd({vbCr(0), vbLf(0)}), ignoreLineEndingDifferences:=True)

                If _outputExpressionTrees Then
                    _testOutputHelper.WriteLine("---- Output Roslyn syntax tree:")
                    _testOutputHelper.WriteLine(actual)
                End If
            Catch ex As EqualException
                _testOutputHelper.WriteLine("---- Output Roslyn syntax tree:")
                _testOutputHelper.WriteLine(actual)

                Throw
            End Try
        End Sub

        Private Function CreateTranslator() As (LinqToVisualBasicSyntaxTranslator, AdhocWorkspace)
            Dim workspace As New AdhocWorkspace()
            Dim syntaxGenerator = Editing.SyntaxGenerator.GetGenerator(workspace, LanguageNames.VisualBasic)
            Return (New LinqToVisualBasicSyntaxTranslator(syntaxGenerator), workspace)
        End Function

        Private Shared ReadOnly ReturnsIntWithParamMethod As MethodInfo =
            GetType(LinqToVisualBasicTranslatorTest).GetMethod(NameOf(ReturnsIntWithParam))

        Public Shared Function ReturnsIntWithParam(i As Integer) As Integer
            Return i + 1
        End Function

        Private Shared ReadOnly GenericMethod As MethodInfo =
            GetType(LinqToVisualBasicTranslatorTest).GetMethods().Single(Function(m) m.Name = NameOf(GenericMethodImplementation))

        Public Shared Function GenericMethodImplementation(Of T)(value As T) As Integer
            Return 0
        End Function

        Private Shared ReadOnly FooMethod As MethodInfo =
            GetType(LinqToVisualBasicTranslatorTest).GetMethod(NameOf(Foo))

        Public Shared Function Foo() As Integer
            Return 1
        End Function

        Private Shared ReadOnly BarMethod As MethodInfo =
            GetType(LinqToVisualBasicTranslatorTest).GetMethod(NameOf(Bar))

        Public Shared Function Bar() As Integer
            Return 1
        End Function

        Private Shared ReadOnly BazMethod As MethodInfo =
            GetType(LinqToVisualBasicTranslatorTest).GetMethod(NameOf(Baz))

        Public Shared Function Baz() As Integer
            Return 1
        End Function

        Public Shared Function MethodWithSixParams(a%, b%, c%, d%, e%, f%) As Integer
            Return a + b + c + d + e + f
        End Function

        Private Class Blog

            Public PublicField As Integer
            Public Property PublicProperty As Integer

            Friend InternalField As Integer
            Friend Property InternalProperty As Integer

            Private _privateField As Integer
            Private Property PrivateProperty As Integer

            Public Property ListOfInts As New List(Of Integer)
            Public Property Details As New BlogDetails

            Public Sub New() : End Sub
            Public Sub New(name As String) : End Sub
            Public Sub New(foo As Integer, bar As Integer) : End Sub

            Public Function SomeInstanceMethod() As Integer
                Return 3
            End Function

            Public Shared ReadOnly Constructor As ConstructorInfo =
            GetType(Blog).GetConstructor(Array.Empty(Of Type)())

            Public Shared Function Static_method_on_nested_type() As Integer
                Return 3
            End Function
        End Class

        Public Class BlogDetails
            Public Property Foo As Integer
            Public Property ListOfInts As New List(Of Integer)
        End Class

        <Flags>
        Public Enum SomeEnum
            One = 1
            Two = 2
        End Enum

        Private ReadOnly _expressionPrinter As New ExpressionPrinter
        Private ReadOnly _outputExpressionTrees As Boolean
    End Class

    Friend Class LinqExpressionToRoslynTranslatorExtensionType

        Public Shared ReadOnly Constructor As ConstructorInfo =
        GetType(LinqExpressionToRoslynTranslatorExtensionType).GetConstructor(Array.Empty(Of Type)())
    End Class

    Friend Module LinqExpressionToRoslynTranslatorExtensions
        Public ReadOnly SomeExtensionMethod As MethodInfo =
            GetType(LinqExpressionToRoslynTranslatorExtensions).GetMethod(
            NameOf(SomeExtension), {GetType(LinqExpressionToRoslynTranslatorExtensionType)})

        <Extension>
        Public Function SomeExtension(someType As LinqExpressionToRoslynTranslatorExtensionType) As Integer
            Return 3
        End Function
    End Module
End Namespace
