Imports System.Linq.Expressions
Imports System.Numerics
Imports EFCore.Design.Tests.Shared
Imports EntityFrameworkCore.VisualBasic.Microsoft.EntityFrameworkCore.TestUtilities.Xunit
Imports Microsoft.EntityFrameworkCore.Design
Imports Microsoft.EntityFrameworkCore.Diagnostics
Imports Microsoft.EntityFrameworkCore.Internal
Imports Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal
Imports Microsoft.EntityFrameworkCore.Storage
Imports Microsoft.EntityFrameworkCore.TestUtilities
Imports Xunit

Namespace Design.Internal
    Public Class VisualBasicHelperTests

        <ConditionalTheory>
        <InlineData(
            "single-line string with \""",
            """single-line string with \""""""")>
        <InlineData(
            True,
            "True")>
        <InlineData(
            False,
            "False")>
        <InlineData(
            CByte(42),
            "CByte(42)")>
        <InlineData(
            "A",
            """A""")>
        <InlineData(
            "'",
            """'""")>
        <InlineData(
            4.2,
            "4.2000000000000002")>
        <InlineData(
            Double.NegativeInfinity,
            "Double.NegativeInfinity")>
        <InlineData(
            Double.PositiveInfinity,
            "Double.PositiveInfinity")>
        <InlineData(
            Double.NaN,
            "Double.NaN")>
        <InlineData(
            0.84551240822557006,
            "0.84551240822557006")>
        <InlineData(
            0.00000000000006,
            "5.9999999999999997E-14")>
        <InlineData(
            -1.7976931348623157E+308, ' Double MinValue
            "-1.7976931348623157E+308")>
        <InlineData(
            1.7976931348623157E+308, ' Double MaxValue
            "1.7976931348623157E+308")>
        <InlineData(
            4.2F,
            "4.2F")>
        <InlineData(
            -3.402823E+38F, ' Single MinValue
            "-3.402823E+38F")>
        <InlineData(
            3.402823E+38F, ' Single MaxValue
            "3.402823E+38F")>
        <InlineData(
            42,
            "42")>
        <InlineData(
            42L,
            "42L")>
        <InlineData(
            9000000000000000000L, ' Ensure Not printed as exponent
            "9000000000000000000L")>
        <InlineData(
            CSByte(42),
            "CSByte(42)")>
        <InlineData(
            42S,
            "42S")>
        <InlineData(
            42UL,
            "42UL")>
        <InlineData(
            18000000000000000000UL,
            "18000000000000000000UL")>
        <InlineData(
            42US,
            "42US")>
        <InlineData(
            "",
            """""")>
        <InlineData(
            SomeEnum.DefaultValue,
            "VisualBasicHelperTests.SomeEnum.DefaultValue")>
        Public Sub Literal_works(value As Object, expected As String)

            Dim literal = New VisualBasicHelper(TypeMappingSource).UnknownLiteral(value)
            Assert.Equal(expected, literal)
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_empty_ByteArray()
            Literal_works(
                New Byte() {},
                "New Byte() {}")
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_single_ByteArray()
            Literal_works(
                    New Byte() {1},
                    "New Byte() {1}")
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_many_ByteArray()
            Literal_works(
                    New Byte() {1, 2},
                    "New Byte() {1, 2}")
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_multiline_string()
            Literal_works(
"multi-line
string with """,
"""multi-line"" & " & If(Environment.Newline = vbCrLf, "vbCrLf", "vbLf") & " & ""string with """"""")
        End Sub

        <UseCulture("de-DE")>
        <ConditionalFact>
        Public Sub Literal_works_when_DateTime()
            Literal_works(
                    New Date(2014, 5, 28, 20, 45, 17, 300, DateTimeKind.Local),
                    "New Date(2014, 5, 28, 20, 45, 17, 300, DateTimeKind.Local)")
        End Sub

        <ConditionalFact>
        <UseCulture("de-DE")>
        Public Sub Literal_works_when_DateTimeOffset()
            Literal_works(
            New DateTimeOffset(New DateTime(2014, 5, 28, 19, 43, 47, 500), New TimeSpan(-7, 0, 0)),
            "New DateTimeOffset(New DateTime(2014, 5, 28, 19, 43, 47, 500, DateTimeKind.Unspecified), New TimeSpan(0, -7, 0, 0, 0))")
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_decimal()
            Literal_works(
                    4.2D,
                    "4.2D")
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_decimal_max_value()
            Literal_works(
                    79228162514264337593543950335D, ' Decimal MaxValue
                    "79228162514264337593543950335D")
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_decimal_min_value()
            Literal_works(
                    -79228162514264337593543950335D, ' Decimal MinValue
                    "-79228162514264337593543950335D")
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_Guid()
            Literal_works(
                    New Guid("fad4f3c3-9501-4b3a-af99-afeb496f7664"),
                    "New Guid(""fad4f3c3-9501-4b3a-af99-afeb496f7664"")")
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_TimeSpan()
            Literal_works(
                    New TimeSpan(17, 21, 42, 37, 250),
                    "New TimeSpan(17, 21, 42, 37, 250)")
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_NullableInt()
            Literal_works(
                    CType(42, Nullable(Of Integer)),
                    "42")
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_StringArray()
            Dim literal = New VisualBasicHelper(TypeMappingSource).Literal({"A", "B"})
            Assert.Equal("{""A"", ""B""}", literal)
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_IntegerArray()
            Dim literal = New VisualBasicHelper(TypeMappingSource).Literal(New Integer() {4, 5, 6})
            Assert.Equal("{4, 5, 6}", literal)
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_ShortArray()
            Dim literal = New VisualBasicHelper(TypeMappingSource).Literal(New Short() {4, 5, 6})
            Assert.Equal("{4S, 5S, 6S}", literal)
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_ObjectArray()
            Dim literal = New VisualBasicHelper(TypeMappingSource).Literal(New Object() {"A", 1})
            Assert.Equal("New Object() {""A"", 1}", literal)
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_empty_StringArray()
            Dim literal = New VisualBasicHelper(TypeMappingSource).Literal(New String() {})
            Assert.Equal("New String() {}", literal)
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_MultidimensionalArray()
            Dim value = New Object(,) {
                    {"A", 1},
                    {"B", 2}
                }

            Dim result = New VisualBasicHelper(TypeMappingSource).Literal(value)

            Assert.Equal(
"New Object(,) {
    {""A"", 1},
    {""B"", 2}
}",
                    result,
            ignoreLineEndingDifferences:=True)
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_BigInteger()
            Literal_works(New BigInteger(42), "BigInteger.Parse(""42"", NumberFormatInfo.InvariantInfo)")
        End Sub

        <ConditionalFact>
        Public Sub UnknownLiteral_throws_when_unknown()
            Dim ex = Assert.Throws(Of InvalidOperationException)(
                        Function() New VisualBasicHelper(TypeMappingSource).UnknownLiteral(New Object()))
            Assert.Equal(VBDesignStrings.UnknownLiteral(GetType(Object)), ex.Message)
        End Sub

        <ConditionalTheory>
        <InlineData(Short.MinValue, "Short.MinValue")>
        <InlineData(Short.MaxValue, "32767S")>
        <InlineData(Integer.MinValue, "Integer.MinValue")>
        <InlineData(Integer.MaxValue, "2147483647")>
        <InlineData(Long.MinValue, "Long.MinValue")>
        <InlineData(Long.MaxValue, "9223372036854775807L")>
        Public Sub Literal_works_VB_IntegerTypes(value As Object, expected As String)
            'Dim s As Short = -32768S ' does not compile
            'roslyn/issues/6868
            Literal_works(value, expected)
        End Sub

        <Theory()>
        <InlineData(GetType(Integer), "Integer")>
        <InlineData(GetType(Integer?), "Integer?")>
        Public Sub Reference_works(type As Type, expected As String)
            Assert.Equal(expected, New VisualBasicHelper(TypeMappingSource).Reference(type))
        End Sub

        <ConditionalFact>
        Public Sub IntegerArray_Reference_works()
            Reference_works(GetType(Integer()), "Integer()")
        End Sub

        <ConditionalFact>
        Public Sub Integer2dArray_Reference_works()
            Reference_works(GetType(Integer(,)), "Integer(,)")
        End Sub

        <ConditionalFact>
        Public Sub IntegerJaggedArray_Reference_works()
            Reference_works(GetType(Integer()()), "Integer()()")
        End Sub

        <ConditionalFact>
        Public Sub GenericOfInteger_Reference_works()
            Reference_works(GetType(Generic(Of Integer)), "Generic(Of Integer)")
        End Sub

        <ConditionalFact>
        Public Sub Nested_Reference_works()
            Reference_works(GetType(Nested), "VisualBasicHelperTests.Nested")
        End Sub

        <ConditionalFact>
        Public Sub GenericOfGenericOfInteger_Reference_works()
            Reference_works(GetType(Generic(Of Generic(Of Integer))), "Generic(Of Generic(Of Integer))")
        End Sub

        <ConditionalFact>
        Public Sub MultiGenericOfIntegerInteger_Reference_works()
            Reference_works(GetType(MultiGeneric(Of Integer, Integer)), "MultiGeneric(Of Integer, Integer)")
        End Sub

        <ConditionalFact>
        Public Sub NestedGenericOfInteger_Reference_works()
            Reference_works(GetType(NestedGeneric(Of Integer)), "VisualBasicHelperTests.NestedGeneric(Of Integer)")
        End Sub

        <ConditionalFact>
        Public Sub NestedDoubleNested_Reference_works()
            Reference_works(GetType(Nested.DoubleNested), "VisualBasicHelperTests.Nested.DoubleNested")
        End Sub

        Private Class Nested
            Public Class DoubleNested
            End Class
        End Class

        Private Class NestedGeneric(Of T)
        End Class
        Private Enum SomeEnum
            DefaultValue
        End Enum

        <ConditionalTheory>
        <InlineData("dash-er", "dasher")>
        <InlineData("ParamArray", "[ParamArray]")>
        <InlineData("True", "[True]")>
        <InlineData("true", "[true]")>
        <InlineData("yield", "yield")>
        <InlineData("spac ed", "spaced")>
        <InlineData("1nders", "_1nders")>
        <InlineData("Name.space", "[Namespace]")>
        <InlineData("$", "_")>
        Public Sub Identifier_works(input As String, expected As String)
            Assert.Equal(expected, New VisualBasicHelper(TypeMappingSource).Identifier(input))
        End Sub

        <ConditionalTheory>
        <InlineData({"WebApplication1", "Migration"}, "WebApplication1.Migration")>
        <InlineData({"WebApplication1.Migration"}, "WebApplication1.Migration")>
        <InlineData({"ef-xplat.Namespace"}, "efxplat.[Namespace]")>
        <InlineData({"#", "$"}, "_._")>
        <InlineData({""}, "_")>
        <InlineData(New String() {}, "_")>
        <InlineData(New String() {Nothing}, "_")>
        Public Sub Namespace_works(input As String(), excepted As String)
            Assert.Equal(excepted, New VisualBasicHelper(TypeMappingSource).Namespace(input))
        End Sub

        <ConditionalFact>
        Public Sub Fragment_MethodCallCodeFragment_works()
            Dim method As New MethodCallCodeFragment("Test", True, 42)

            Dim result = New VisualBasicHelper(TypeMappingSource).Fragment(method)

            Assert.Equal(".Test(True, 42)", result)
        End Sub

        <ConditionalFact>
        Public Sub Fragment_MethodCallCodeFragment_works_with_arrays()
            Dim method As New MethodCallCodeFragment("Test", New Byte() {1, 2}, {3, 4}, {"foo", "bar"})

            Dim result = New VisualBasicHelper(TypeMappingSource).Fragment(method)

            Assert.Equal(".Test(New Byte() {1, 2}, {3, 4}, {""foo"", ""bar""})", result)
        End Sub

        <ConditionalFact>
        Public Sub Fragment_MethodCallCodeFragment_works_when_niladic()
            Dim method As New MethodCallCodeFragment("Test")

            Dim result = New VisualBasicHelper(TypeMappingSource).Fragment(method)

            Assert.Equal(".Test()", result)
        End Sub

        <ConditionalFact>
        Public Sub Fragment_MethodCallCodeFragment_works_when_chaining()
            Dim method = New MethodCallCodeFragment("Test").Chain("Test")

            Dim result = New VisualBasicHelper(TypeMappingSource).Fragment(method)

            Assert.Equal(".Test().Test()", result)
        End Sub

        <ConditionalFact>
        Public Sub Fragment_MethodCallCodeFragment_works_when_chaining_on_chain()
            Dim method = New MethodCallCodeFragment("One", Array.Empty(Of Object)(),
                                                    New MethodCallCodeFragment("Two")).
                         Chain("Three")

            Dim result = New VisualBasicHelper(TypeMappingSource).Fragment(method)

            Assert.Equal(".One().Two().Three()", result)
        End Sub

        <ConditionalFact>
        Public Sub Fragment_MethodCallCodeFragment_works_when_chaining_on_chain_with_call()
            Dim method = New MethodCallCodeFragment("One", Array.Empty(Of Object)(),
                                                    New MethodCallCodeFragment("Two")).
                         Chain(New MethodCallCodeFragment("Three", Array.Empty(Of Object)(),
                                                          New MethodCallCodeFragment("Four")))

            Dim result = New VisualBasicHelper(TypeMappingSource).Fragment(method)

            Assert.Equal(".One().Two().Three().Four()", result)
        End Sub

        <ConditionalFact>
        Public Sub Fragment_MethodCallCodeFragment_works_when_nested_closure()
            Dim method As New MethodCallCodeFragment("Test",
                          New NestedClosureCodeFragment("x", New MethodCallCodeFragment("Test")))

            Dim result = New VisualBasicHelper(TypeMappingSource).Fragment(method)

            Assert.Equal(".Test(Function(x) x.Test())", result)
        End Sub

        <ConditionalFact>
        Public Sub Really_unknown_literal_with_no_mapping_support()
            Dim typeMapping = CreateTypeMappingSource(Of SimpleTestType)(Nothing)

            Assert.Equal(CoreStrings.LiteralGenerationNotSupported(NameOf(SimpleTestType)),
            Assert.Throws(Of NotSupportedException)(Function() New VisualBasicHelper(typeMapping).UnknownLiteral(New SimpleTestType)).Message)
        End Sub

        <ConditionalFact>
        Public Sub Literal_with_parameterless_constructor()
            Dim typeMapping = CreateTypeMappingSource(Of SimpleTestType)(
        Function(v) Expression.[New](GetType(SimpleTestType)))

            Assert.Equal("New EFCore.Design.Tests.Shared.SimpleTestType()",
                         New VisualBasicHelper(typeMapping).UnknownLiteral(New SimpleTestType))
        End Sub

        <ConditionalFact>
        Public Sub Literal_with_one_parameter_constructor()
            Dim typeMapping = CreateTypeMappingSource(Of SimpleTestType)(
                Function(v)
                    Return Expression.[New](
                                            GetType(SimpleTestType).GetConstructor({GetType(String)}),
                                            Expression.Constant(v.Arg1, GetType(String)))
                End Function)

            Assert.Equal("New EFCore.Design.Tests.Shared.SimpleTestType(""Jerry"")",
                         New VisualBasicHelper(typeMapping).UnknownLiteral(New SimpleTestType("Jerry")))
        End Sub

        <ConditionalFact>
        Public Sub Literal_with_two_parameter_constructor()
            Dim typeMapping = CreateTypeMappingSource(Of SimpleTestType)(
                Function(v)
                    Return Expression.[New](
                                            GetType(SimpleTestType).GetConstructor({GetType(String), GetType(Integer?)}),
                                            Expression.Constant(v.Arg1, GetType(String)),
                                            Expression.Constant(v.Arg2, GetType(Integer?)))
                End Function)

            Assert.Equal("New EFCore.Design.Tests.Shared.SimpleTestType(""Jerry"", 77)",
                         New VisualBasicHelper(typeMapping).UnknownLiteral(New SimpleTestType("Jerry", 77)))
        End Sub

        <ConditionalFact>
        Public Sub Literal_with_parameterless_static_factory()
            Dim typeMapping = CreateTypeMappingSource(Of SimpleTestType)(
                                        Function(v)
                                            Return Expression.Call(GetType(SimpleTestTypeFactory).GetMethod(
                                                                     NameOf(SimpleTestTypeFactory.StaticCreate),
                                                                     New Type() {}))
                                        End Function)

            Assert.Equal("EFCore.Design.Tests.Shared.SimpleTestTypeFactory.StaticCreate()",
                          New VisualBasicHelper(typeMapping).UnknownLiteral(New SimpleTestType))
        End Sub

        <ConditionalFact>
        Public Sub Literal_with_one_parameter_static_factory()
            Dim typeMapping = CreateTypeMappingSource(Of SimpleTestType)(
                                        Function(v)
                                            Return Expression.Call(GetType(SimpleTestTypeFactory).GetMethod(
                                                                     NameOf(SimpleTestTypeFactory.StaticCreate),
                                                                     {GetType(String)}),
                                                                     Expression.Constant(v.Arg1, GetType(String)))
                                        End Function)

            Assert.Equal("EFCore.Design.Tests.Shared.SimpleTestTypeFactory.StaticCreate(""Jerry"")",
                          New VisualBasicHelper(typeMapping).UnknownLiteral(New SimpleTestType("Jerry")))
        End Sub

        <ConditionalFact>
        Public Sub Literal_with_two_parameter_static_factory()
            Dim typeMapping = CreateTypeMappingSource(Of SimpleTestType)(
                                    Function(v)
                                        Return Expression.Call(
                                                GetType(SimpleTestTypeFactory).GetMethod(
                                                     NameOf(SimpleTestTypeFactory.StaticCreate),
                                                     {GetType(String), GetType(Integer?)}),
                                                 Expression.Constant(v.Arg1, GetType(String)),
                                                 Expression.Constant(v.Arg2, GetType(Integer?)))
                                    End Function)

            Assert.Equal("EFCore.Design.Tests.Shared.SimpleTestTypeFactory.StaticCreate(""Jerry"", 77)",
                          New VisualBasicHelper(typeMapping).UnknownLiteral(New SimpleTestType("Jerry", 77)))
        End Sub

        <ConditionalFact>
        Public Sub Literal_with_parameterless_instance_factory()
            Dim typeMapping = CreateTypeMappingSource(Of SimpleTestType)(
                                Function(v)
                                    Return Expression.Call(
                                                 Expression.[New](GetType(SimpleTestTypeFactory)),
                                                 GetType(SimpleTestTypeFactory).GetMethod(
                                                     NameOf(SimpleTestTypeFactory.Create),
                                                     New Type() {}))
                                End Function)

            Assert.Equal("New EFCore.Design.Tests.Shared.SimpleTestTypeFactory().Create()",
                         New VisualBasicHelper(typeMapping).UnknownLiteral(New SimpleTestType))
        End Sub

        <ConditionalFact>
        Public Sub Literal_with_one_parameter_instance_factory()
            Dim typeMapping = CreateTypeMappingSource(Of SimpleTestType)(
                                Function(v)
                                    Return Expression.Convert(
                                                 Expression.Call(
                                                     Expression.[New](GetType(SimpleTestTypeFactory)),
                                                     GetType(SimpleTestTypeFactory).GetMethod(
                                                         NameOf(SimpleTestTypeFactory.Create),
                                                         {GetType(String)}
                                                     ),
                                                     Expression.Constant(v.Arg1, GetType(String))
                                                 ),
                                            GetType(SimpleTestType))
                                End Function)

            Assert.Equal("CType(New EFCore.Design.Tests.Shared.SimpleTestTypeFactory().Create(""Jerry""), EFCore.Design.Tests.Shared.SimpleTestType)",
                         New VisualBasicHelper(typeMapping).UnknownLiteral(New SimpleTestType("Jerry", 77)))
        End Sub

        <ConditionalFact>
        Public Sub Literal_with_two_parameter_instance_factory()
            Dim typeMapping = CreateTypeMappingSource(Of SimpleTestType)(
                                        Function(v)
                                            Return Expression.Convert(
                                                    Expression.Call(
                                                        Expression.[New](
                                                            GetType(SimpleTestTypeFactory).GetConstructor({GetType(String)}),
                                                            Expression.Constant("4096", GetType(String))),
                                                        GetType(SimpleTestTypeFactory).GetMethod(
                                                            NameOf(SimpleTestTypeFactory.Create),
                                                            {GetType(String), GetType(Integer?)}),
                                                        Expression.Constant(v.Arg1, GetType(String)),
                                                        Expression.Constant(v.Arg2, GetType(Integer?))),
                                                    GetType(SimpleTestType))
                                        End Function)

            Assert.Equal("CType(New EFCore.Design.Tests.Shared.SimpleTestTypeFactory(""4096"").Create(""Jerry"", 77), EFCore.Design.Tests.Shared.SimpleTestType)",
                         New VisualBasicHelper(typeMapping).UnknownLiteral(New SimpleTestType("Jerry", 77)))
        End Sub

        <ConditionalFact>
        Public Sub Literal_with_two_parameter_instance_factory_and_internal_cast()
            Dim typeMapping = CreateTypeMappingSource(Of SimpleTestType)(
                                    Function(v)
                                        Return Expression.Convert(
                                                    Expression.Call(
                                                        Expression.[New](
                                                            GetType(SimpleTestTypeFactory).GetConstructor({GetType(String)}),
                                                            Expression.Constant("4096", GetType(String))),
                                                        GetType(SimpleTestTypeFactory).GetMethod(
                                                            NameOf(SimpleTestTypeFactory.Create),
                                                            {GetType(String), GetType(Integer?)}),
                                                        Expression.Constant(v.Arg1, GetType(String)),
                                                        Expression.Convert(
                                                            Expression.Constant(v.Arg2, GetType(Integer)),
                                                            GetType(Integer?)
                                                        )
                                                    ),
                                            GetType(SimpleTestType))
                                    End Function)

            Assert.Equal("CType(New EFCore.Design.Tests.Shared.SimpleTestTypeFactory(""4096"").Create(""Jerry"", CType(Integer.MinValue, Integer?)), EFCore.Design.Tests.Shared.SimpleTestType)",
                          New VisualBasicHelper(typeMapping).UnknownLiteral(New SimpleTestType("Jerry", Integer.MinValue)))
        End Sub

        <ConditionalFact>
        Public Sub Literal_with_static_field()
            Dim typeMapping = CreateTypeMappingSource(Of SimpleTestType)(
                                    Function(v)
                                        Return Expression.Field(Nothing, GetType(SimpleTestType).
                                                          GetField(NameOf(SimpleTestType.SomeStaticField)))
                                    End Function)

            Assert.Equal("EFCore.Design.Tests.Shared.SimpleTestType.SomeStaticField",
                         New VisualBasicHelper(typeMapping).UnknownLiteral(New SimpleTestType))
        End Sub

        <ConditionalFact>
        Public Sub Literal_with_static_property()
            Dim typeMapping = CreateTypeMappingSource(Of SimpleTestType)(
                            Function(v)
                                Return Expression.[Property](Nothing, GetType(SimpleTestType).
                                                  GetProperty(NameOf(SimpleTestType.SomeStaticProperty)))
                            End Function)

            Assert.Equal("EFCore.Design.Tests.Shared.SimpleTestType.SomeStaticProperty",
                         New VisualBasicHelper(typeMapping).UnknownLiteral(New SimpleTestType))
        End Sub

        <ConditionalFact>
        Public Sub Literal_with_instance_property()
            Dim typeMapping = CreateTypeMappingSource(Of SimpleTestType)(
                                    Function(v)
                                        Return Expression.[Property](
                                             Expression.[New](GetType(SimpleTestType)),
                                             GetType(SimpleTestType).GetProperty(NameOf(SimpleTestType.SomeInstanceProperty)))
                                    End Function)

            Assert.Equal("New EFCore.Design.Tests.Shared.SimpleTestType().SomeInstanceProperty",
                         New VisualBasicHelper(typeMapping).UnknownLiteral(New SimpleTestType))
        End Sub

        <ConditionalFact>
        Public Sub Literal_with_add()
            Dim typeMapping = CreateTypeMappingSource(Of SimpleTestType)(
                                    Function(v)
                                        Return Expression.Add(
                                             Expression.Constant(10),
                                             Expression.Constant(10))
                                    End Function)

            Assert.Equal("10 + 10",
                        New VisualBasicHelper(typeMapping).UnknownLiteral(New SimpleTestType))
        End Sub

        <ConditionalFact>
        Public Sub Literal_with_unsupported_node_throws()
            Dim typeMapping = CreateTypeMappingSource(Of SimpleTestType)(
                                    Function(v)
                                        Return Expression.Multiply(
                                            Expression.Constant(10),
                                            Expression.Constant(10))
                                    End Function)

            Assert.Equal(
                DesignStrings.LiteralExpressionNotSupported(
                    "(10 * 10)",
                    NameOf(SimpleTestType)),
                Assert.Throws(Of NotSupportedException)(
                    Function() New VisualBasicHelper(typeMapping).UnknownLiteral(New SimpleTestType)).Message)
        End Sub

        Private ReadOnly Property TypeMappingSource As IRelationalTypeMappingSource = CreateTypeMappingSource()

        Private Shared Function CreateTypeMappingSource(Of T)(
            literalExpressionFunc As Func(Of T, Expression)) As SqlServerTypeMappingSource

            Return CreateTypeMappingSource(New TestTypeMappingPlugin(Of T)(literalExpressionFunc))
        End Function

        Private Shared Function CreateTypeMappingSource(
            ParamArray plugins As IRelationalTypeMappingSourcePlugin()) As SqlServerTypeMappingSource

            Return New SqlServerTypeMappingSource(
                            TestServiceFactory.Instance.Create(Of TypeMappingSourceDependencies)(),
                            New RelationalTypeMappingSourceDependencies(plugins)
                       )
        End Function

    End Class

    Friend Class Generic(Of T)
    End Class

    Friend Class MultiGeneric(Of T1, T2)
    End Class

End Namespace
