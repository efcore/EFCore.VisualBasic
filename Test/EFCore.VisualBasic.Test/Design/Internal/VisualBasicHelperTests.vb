﻿Imports System.Linq.Expressions
Imports System.Numerics
Imports System.Reflection
Imports EFCore.Design.Tests.Shared
Imports EntityFrameworkCore.VisualBasic.TestUtilities.Xunit
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
        <InlineData(
            ChrW(0),
            "ChrW(0)")>
        <InlineData(
            """"c,
            """""""""c")>
        Public Sub Literal_works(value As Object, expected As String)

            Dim literal = New VisualBasicHelper(TypeMappingSource).UnknownLiteral(value)
            Assert.Equal(expected, literal)
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_empty_ByteArray()
            Literal_works(
                New Byte() {},
                "System.Array.Empty(Of Byte)")
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
        Public Sub Literal_works_when_empty_list()
            Literal_works(
                New List(Of String),
                "New List(Of String)")
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_list_with_single_element()
            Literal_works(
                New List(Of String) From {"one"},
                "New List(Of String) From {""one""}")
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_list_of_mixed_objects()
            Literal_works(
                New List(Of Object) From {1, "two"},
                "New List(Of Object) From {1, ""two""}")
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_list_with_ctor_arguments()
            Literal_works(
                New List(Of String)({"one"}) From {"two", "three"},
                "New List(Of String) From {""one"", ""two"", ""three""}")
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_list_in_vertical()

            Dim listToTest As New List(Of Object) From {New List(Of Integer)({1}), "two", 3}

            Dim result = New VisualBasicHelper(TypeMappingSource).Literal(listToTest, True)

            Assert.Equal(
"New List(Of Object) From {
    New List(Of Integer) From {1},
    ""two"",
    3
}",
            result,
            ignoreLineEndingDifferences:=True)
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_empty_dictionary()
            Literal_works(
                New Dictionary(Of String, Integer),
                "New Dictionary(Of String, Integer)")
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_dictionary_with_single_element()
            Literal_works(
                New Dictionary(Of String, String) From {{"one", "value"}},
                "New Dictionary(Of String, String) From {{""one"", ""value""}}")
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_dictionary_of_mixed_objects()
            Literal_works(
                New Dictionary(Of String, Object) From {{"one", 1}, {"two", "Two"}},
                "New Dictionary(Of String, Object) From {{""one"", 1}, {""two"", ""Two""}}")
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_dictionary_vertical()

            Dim dictToTest As New Dictionary(Of Integer, Object) From {{1, 1}, {2, "Two"}}

            Dim result = New VisualBasicHelper(TypeMappingSource).Literal(dictToTest, True)

            Assert.Equal(
"New Dictionary(Of Integer, Object) From {
    {1, 1},
    {2, ""Two""}
}",
            result,
            ignoreLineEndingDifferences:=True)
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_multiline_string()
            Dim nl = Environment.NewLine
            Literal_works(
$"multi-line{nl}string with """,
"""multi-line"" & " & If(Environment.NewLine = vbCrLf, "vbCrLf", "vbLf") & " & ""string with """"""")
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_value_tuple()
            Literal_works((1, "hello"), "(1, ""hello"")")
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_value_tuple_with_null_value_type()
            Literal_works((1, DirectCast(Nothing, Integer()), "hello"), "(1, DirectCast(Nothing, Integer()), ""hello"")")
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_value_tuple_with_null_reference_type()
            Literal_works((1, DirectCast(Nothing, String), "hello"), "(1, DirectCast(Nothing, String), ""hello"")")
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_value_tuple_of_length_1()
            Literal_works(ValueTuple.Create(1), "ValueTuple.Create(1)")
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_value_tuple_of_length_1_with_null_valu()
            Literal_works(ValueTuple.Create(CType(Nothing, String)), "ValueTuple.Create(DirectCast(Nothing, String))")
        End Sub

        <ConditionalFact>
        Public Sub Literal_works_when_value_tuple_of_length_9()
            Literal_works((1, 2, 3, 4, 5, 6, 7, 8, 9), "(1, 2, 3, 4, 5, 6, 7, 8, 9)")
        End Sub

        <ConditionalFact>
        <UseCulture("fr-CA")>
        Public Sub Literal_works_when_DateOnly()
            Literal_works(
                    New DateOnly(2021, 9, 26),
                    "New DateOnly(2021, 9, 26)")
        End Sub

        <ConditionalFact>
        <UseCulture("de-DE")>
        Public Sub Literal_works_when_DateTime()
            Literal_works(
                    New Date(2014, 5, 28, 20, 45, 17, 300, DateTimeKind.Local),
                    "New Date(2014, 5, 28, 20, 45, 17, 300, DateTimeKind.Local)")
        End Sub

        <ConditionalFact>
        <UseCulture("de-DE")>
        Public Sub Literal_works_when_DateTimeOffset()
            Literal_works(
            New DateTimeOffset(New Date(2014, 5, 28, 19, 43, 47, 500), New TimeSpan(-7, 0, 0)),
            "New DateTimeOffset(New Date(2014, 5, 28, 19, 43, 47, 500, DateTimeKind.Unspecified), New TimeSpan(0, -7, 0, 0, 0))")
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
        <UseCulture("fr-CA")>
        Public Sub Literal_works_when_TimeOnly()
            Literal_works(
                    New TimeOnly(20, 17, 37, 50).Add(TimeSpan.FromTicks(50)),
                    "New TimeOnly(20, 17, 37, 50).Add(TimeSpan.FromTicks(50))")
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
            Assert.Equal("System.Array.Empty(Of String)", literal)
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

        <ConditionalTheory()>
        <InlineData(GetType(Integer), "Integer")>
        <InlineData(GetType(Integer?), "Integer?")>
        <InlineData(GetType(Date), "Date")>
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

        <ConditionalFact>
        Public Sub NestedGenericOfNestedDoubleNested_Reference_works()
            Reference_works(GetType(NestedGeneric(Of Nested.DoubleNested)), "VisualBasicHelperTests.NestedGeneric(Of VisualBasicHelperTests.Nested.DoubleNested)")
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
        <InlineData("$Foo", "Foo")>
        <InlineData("$", "_0")>
        Public Sub Identifier_works(input As String, expected As String)
            Assert.Equal(expected, New VisualBasicHelper(TypeMappingSource).Identifier(input))
        End Sub

        <ConditionalTheory>
        <InlineData("var", {"var", "var0", "Var1", "VAR2"}, "var3")>
        <InlineData("var9", {"var9"}, "var90")>
        Public Sub Identifier_with_scope_works(input As String, scope As String(), expected As String)
            Assert.Equal(expected, New VisualBasicHelper(TypeMappingSource).Identifier(input, New List(Of String)(scope)))
        End Sub

        <ConditionalTheory>
        <InlineData({"WebApplication1", "Migration"}, "WebApplication1.Migration")>
        <InlineData({"WebApplication1.Migration"}, "WebApplication1.Migration")>
        <InlineData({"ef-xplat.Namespace"}, "efxplat.[Namespace]")>
        <InlineData({"#", "$"}, "_0._0")>
        <InlineData({""}, "Empty")>
        <InlineData(New String() {}, "Empty")>
        <InlineData(New String() {Nothing}, "Empty")>
        Public Sub Namespace_works(input As String(), excepted As String)
            Assert.Equal(excepted, New VisualBasicHelper(TypeMappingSource).Namespace(input))
        End Sub

        <ConditionalTheory>
        <InlineData("Foo.Bar", CStr(Nothing), "Foo.Bar")>
        <InlineData("Foo.Bar", "", "Foo.Bar")>
        <InlineData("Foo.Bar", "Foo.Bar", "Foo.Bar")>
        <InlineData("Foo.Bar", ".Foo.Bar", "Foo.Bar")>
        <InlineData("Foo.Bar", "Toto", "Foo.Bar.Toto")>
        <InlineData("integer.Foo", "Bar", "integer.Foo.Bar")>
        <InlineData("Foo.Bar", "Foo.Bar.Foo.integer", "Foo.Bar.Foo.[integer]")>
        <InlineData("Foo.Bar", "Global", "Foo.Bar")>
        <InlineData("Foo.Bar", "Global.Foo.Bar.Toto", "Foo.Bar.Toto")>
        <InlineData("Foo.Bar", "Global.Bar.Foo.Toto", "Bar.Foo.Toto")>
        <InlineData("Foo.Bar", "Foo.Bar.Foo.Bar.Toto", "Foo.Bar.Foo.Bar.Toto")>
        <InlineData("Global.Foo.Bar", "[Global].Foo.Bar", "Global.Foo.Bar")>
        <InlineData("global.Foo.Bar", "[Global].Foo.Bar.Toto", "global.Foo.Bar.Toto")>
        <InlineData("Global.Foo.Bar", "global.Global.Foo.Bar.Toto ", "Global.Foo.Bar.Toto")>
        Public Sub FullyQualifiedNamespace_works(rootNamespace As String, namespaceHint As String, excepted As String)
            Assert.Equal(excepted, New VisualBasicHelper(TypeMappingSource).FullyQualifiedNamespace(rootNamespace, namespaceHint))
        End Sub

        <ConditionalTheory>
        <InlineData("Foo.Bar", CStr(Nothing), Nothing)>
        <InlineData("Foo.Bar", "", Nothing)>
        <InlineData("Foo.Bar", "Foo.Bar", Nothing)>
        <InlineData("Foo.Bar", ".Foo.Bar", Nothing)>
        <InlineData("Foo.Bar", "Toto", "Toto")>
        <InlineData("Foo.Bar", "Foo.Bar.integer", "[integer]")>
        <InlineData("Foo.Bar", "Foo.Bar.Foo.integer", "Foo.[integer]")>
        <InlineData("Foo.Bar", "Global", "Global")>
        <InlineData("Foo.Bar", "Global.Foo.Bar.Toto", "Toto")>
        <InlineData("Foo.Bar", "Global.Bar.Foo.Toto", "Global.Bar.Foo.Toto")>
        <InlineData("Foo.Bar", "Foo.Bar.Foo.Bar.Toto", "Foo.Bar.Toto")>
        <InlineData("Global.Foo.Bar", "[Global].Foo.Bar", Nothing)>
        <InlineData("global.Foo.Bar", "[Global].Foo.Bar.Toto", "Toto")>
        <InlineData("Global.Foo.Bar", "global.Global.Foo.Bar.Toto ", "Toto")>
        Public Sub NamespaceIdentifier_works(rootNamespace As String, namespaceHint As String, excepted As String)
            Assert.Equal(excepted, New VisualBasicHelper(TypeMappingSource).NamespaceIdentifier(rootNamespace, namespaceHint))
        End Sub

        <ConditionalTheory>
        <InlineData("Foo.Bar", "Foo", Nothing)>
        <InlineData("Foo.Bar", "Foo.Bar", Nothing)>
        <InlineData("Foo.Bar", "Foo.Bar.Toto", "Foo.Bar.Toto")>
        <InlineData("Foo.Bar", "Toto.Foo.Bar", "Toto.Foo.Bar")>
        Public Sub ImportsClause_works(currentTypeNamespace As String, importedTypeNamespace As String, excepted As String)
            Assert.Equal(excepted, New VisualBasicHelper(TypeMappingSource).ImportsClause(currentTypeNamespace, importedTypeNamespace))
        End Sub

        <ConditionalFact>
        Public Sub Fragment_MethodCallCodeFragment_works()
            Dim method As New MethodCallCodeFragment(_testFuncMethodInfo, True, 42)

            Dim result = New VisualBasicHelper(TypeMappingSource).Fragment(method)

            Assert.Equal(".TestFunc(True, 42)", result)
        End Sub

        <ConditionalFact>
        Public Sub Fragment_MethodCallCodeFragment_works_with_arrays()
            Dim method As New MethodCallCodeFragment(_testFuncMethodInfo, New Byte() {1, 2}, {3, 4}, {"foo", "bar"})

            Dim result = New VisualBasicHelper(TypeMappingSource).Fragment(method)

            Assert.Equal(".TestFunc(New Byte() {1, 2}, {3, 4}, {""foo"", ""bar""})", result)
        End Sub

        <ConditionalFact>
        Public Sub Fragment_MethodCallCodeFragment_works_when_niladic()
            Dim method As New MethodCallCodeFragment(_testFuncMethodInfo)

            Dim result = New VisualBasicHelper(TypeMappingSource).Fragment(method)

            Assert.Equal(".TestFunc()", result)
        End Sub

        <ConditionalFact>
        Public Sub Fragment_MethodCallCodeFragment_works_when_chaining()
            Dim method = New MethodCallCodeFragment(_testFuncMethodInfo).Chain(_testFuncMethodInfo)

            Dim result = New VisualBasicHelper(TypeMappingSource).Fragment(method)

            Assert.Equal(
".
TestFunc().
TestFunc()", result, ignoreLineEndingDifferences:=True)
        End Sub

        <ConditionalFact>
        Public Sub Fragment_MethodCallCodeFragment_works_when_chaining_on_chain()
            Dim method = New MethodCallCodeFragment(_testFuncMethodInfo, "One").
                Chain(New MethodCallCodeFragment(_testFuncMethodInfo, "Two")).
                Chain(_testFuncMethodInfo, "Three")

            Dim result = New VisualBasicHelper(TypeMappingSource).Fragment(method)

            Assert.Equal(
".
TestFunc(""One"").
TestFunc(""Two"").
TestFunc(""Three"")", result, ignoreLineEndingDifferences:=True)
        End Sub

        <ConditionalFact>
        Public Sub Fragment_MethodCallCodeFragment_works_when_chaining_on_chain_with_call()
            Dim method = New MethodCallCodeFragment(_testFuncMethodInfo, "One").
                            Chain(New MethodCallCodeFragment(_testFuncMethodInfo, "Two")).
                            Chain(
                                New MethodCallCodeFragment(_testFuncMethodInfo, "Three").Chain(
                                    New MethodCallCodeFragment(_testFuncMethodInfo, "Four")))

            Dim result = New VisualBasicHelper(TypeMappingSource).Fragment(method)

            Assert.Equal(
".
TestFunc(""One"").
TestFunc(""Two"").
TestFunc(""Three"").
TestFunc(""Four"")", result, ignoreLineEndingDifferences:=True)
        End Sub

        <ConditionalFact>
        Public Sub Fragment_MethodCallCodeFragment_works_when_nested_closure()
            Dim method As New MethodCallCodeFragment(_testFuncMethodInfo,
                                                     New NestedClosureCodeFragment("x", New MethodCallCodeFragment(_testFuncMethodInfo)))

            Dim result = New VisualBasicHelper(TypeMappingSource).Fragment(method)

            Assert.Equal(".TestFunc(Sub(x) x.TestFunc())", result)
        End Sub

        <ConditionalFact>
        Public Sub Fragment_MethodCallCodeFragment_works_when_nested_closure_with_multiple_method_calls()
            Dim method As New MethodCallCodeFragment(_testFuncMethodInfo,
                          New NestedClosureCodeFragment("tb",
                            {New MethodCallCodeFragment(_testFuncMethodInfo),
                             New MethodCallCodeFragment(_testFuncMethodInfo, True, 42),
                             New MethodCallCodeFragment(_testFuncMethodInfo,
                                New NestedClosureCodeFragment("ttb", New MethodCallCodeFragment(_testFuncMethodInfo)))
                            }))

            Dim result = New VisualBasicHelper(TypeMappingSource).Fragment(method)

            Assert.Equal(
".TestFunc(Sub(tb)
    tb.TestFunc()
    tb.TestFunc(True, 42)
    tb.TestFunc(Sub(ttb) ttb.TestFunc())
End Sub)", result, ignoreLineEndingDifferences:=True)
        End Sub

        <ConditionalFact>
        Public Sub Fragment_MethodCallCodeFragment_works_when_nested_closure_with_chain()

            Dim method As New MethodCallCodeFragment(
            _testFuncMethodInfo,
            New NestedClosureCodeFragment(
                "x",
                New MethodCallCodeFragment(_testFuncMethodInfo, "One").
                    Chain(New MethodCallCodeFragment(_testFuncMethodInfo, "Two"))))

            Dim result = New VisualBasicHelper(TypeMappingSource).Fragment(method)

            Assert.Equal(
".TestFunc(Sub(x) x.
    TestFunc(""One"").
    TestFunc(""Two""))",
            result,
            ignoreLineEndingDifferences:=True)
        End Sub

        <ConditionalFact>
        Public Sub Fragment_MethodCallCodeFragment_with_indent_works_when_nested_closure_with_chain()

            Dim method As New MethodCallCodeFragment(
            _testFuncMethodInfo,
            New NestedClosureCodeFragment(
                "x",
                New MethodCallCodeFragment(_testFuncMethodInfo, "One").
                    Chain(New MethodCallCodeFragment(_testFuncMethodInfo, "Two"))))

            Dim result = New VisualBasicHelper(TypeMappingSource).Fragment(method, indent:=1)

            Assert.Equal(
".TestFunc(Sub(x) x.
        TestFunc(""One"").
        TestFunc(""Two""))",
            result,
            ignoreLineEndingDifferences:=True)
        End Sub

        <ConditionalFact>
        Public Sub Fragment_MethodCallCodeFragment_with_indent_works_when_chain_and_nested_closure_with_chain()

            Dim method = New MethodCallCodeFragment(_testFuncMethodInfo, "One").
                Chain(
                    New MethodCallCodeFragment(
                    _testFuncMethodInfo,
                    New NestedClosureCodeFragment(
                        "x",
                        New MethodCallCodeFragment(_testFuncMethodInfo, "Two").
                            Chain(New MethodCallCodeFragment(_testFuncMethodInfo, "Three")))))

            Dim result = New VisualBasicHelper(TypeMappingSource).Fragment(method, indent:=1)

            Assert.Equal(
".
    TestFunc(""One"").
    TestFunc(Sub(x) x.
        TestFunc(""Two"").
        TestFunc(""Three""))",
            result,
            ignoreLineEndingDifferences:=True)
        End Sub

        <ConditionalFact>
        Public Sub Fragment_MethodCallCodeFragment_works_when_nested_closure_with_two_calls()

            Dim method = New MethodCallCodeFragment(
                _testFuncMethodInfo,
                New NestedClosureCodeFragment(
                    "x",
                    {
                        New MethodCallCodeFragment(_testFuncMethodInfo, "One"),
                        New MethodCallCodeFragment(_testFuncMethodInfo, "Two")
                    }))

            Dim result = New VisualBasicHelper(TypeMappingSource).Fragment(method)

            Assert.Equal(
".TestFunc(Sub(x)
    x.TestFunc(""One"")
    x.TestFunc(""Two"")
End Sub)",
            result,
            ignoreLineEndingDifferences:=True)
        End Sub

        <ConditionalFact>
        Public Sub Fragment_MethodCallCodeFragment_with_indent_works_when_chain_and_nested_closure()
            Dim method = New MethodCallCodeFragment(_testFuncMethodInfo, "One").
                Chain(
                    New MethodCallCodeFragment(
                        _testFuncMethodInfo,
                    New NestedClosureCodeFragment(
                        "x",
                        {
                            New MethodCallCodeFragment(_testFuncMethodInfo, "Two"),
                            New MethodCallCodeFragment(_testFuncMethodInfo, "Three")
                        })))

            Dim result = New VisualBasicHelper(TypeMappingSource).Fragment(method, indent:=1)

            Assert.Equal(
".
    TestFunc(""One"").
    TestFunc(Sub(x)
        x.TestFunc(""Two"")
        x.TestFunc(""Three"")
    End Sub)",
            result,
            ignoreLineEndingDifferences:=True)
        End Sub

        <ConditionalFact>
        Public Sub Fragment_MethodCallCodeFragment_works_with_identifier()
            Dim method = New MethodCallCodeFragment(_testFuncMethodInfo, True, 42)

            Dim result = New VisualBasicHelper(TypeMappingSource).Fragment(method, instanceIdentifier:="builder", typeQualified:=False)

            Assert.Equal("builder.TestFunc(True, 42)", result)
        End Sub

        <ConditionalFact>
        Public Sub Fragment_MethodCallCodeFragment_works_with_identifier_chained()
            Dim method = New MethodCallCodeFragment(_testFuncMethodInfo, "One").
                Chain(New MethodCallCodeFragment(_testFuncMethodInfo))

            Dim result = New VisualBasicHelper(TypeMappingSource).Fragment(method, instanceIdentifier:="builder", typeQualified:=False)

            Assert.Equal(
$"builder.
    TestFunc(""One"").
    TestFunc()", result, ignoreLineEndingDifferences:=True)
        End Sub

        <ConditionalFact>
        Public Sub Fragment_MethodCallCodeFragment_works_with_type_qualified()
            Dim method = New MethodCallCodeFragment(_testFuncMethodInfo, True, 42)

            Dim result = New VisualBasicHelper(TypeMappingSource).Fragment(method, instanceIdentifier:="builder", typeQualified:=False)

            Assert.Equal("builder.TestFunc(True, 42)", result)
        End Sub

        <ConditionalFact>
        Public Sub Fragment_MethodCallCodeFragment_Complexe_with_identifier_works()
            Dim method1 As New MethodCallCodeFragment(_testFuncMethodInfo, True, 1)
            Dim method2 As New MethodCallCodeFragment(_testFuncMethodInfo, 2)
            Dim method3 As New MethodCallCodeFragment(_testFuncMethodInfo, 3)
            Dim method4 As New MethodCallCodeFragment(_testFuncMethodInfo, 4)
            Dim method5 As New MethodCallCodeFragment(_testFuncMethodInfo, 5)

            Dim method = method1.
                            Chain(
                                New MethodCallCodeFragment(
                                    _testFuncMethodInfo,
                                    New NestedClosureCodeFragment("tb", {method2}))).
                            Chain(
                                New MethodCallCodeFragment(
                                    _testFuncMethodInfo,
                                    New NestedClosureCodeFragment("tb", {
                                        method3,
                                        New MethodCallCodeFragment(
                                            _testFuncMethodInfo,
                                            New NestedClosureCodeFragment("tb", {
                                                method4,
                                                method5
                                            }))
                                     })))

            Dim result = New VisualBasicHelper(TypeMappingSource).Fragment(method, instanceIdentifier:="builder", typeQualified:=False)

            Assert.Equal(
"builder.
    TestFunc(True, 1).
    TestFunc(Sub(tb) tb.TestFunc(2)).
    TestFunc(Sub(tb)
        tb.TestFunc(3)
        tb.TestFunc(Sub(tb)
            tb.TestFunc(4)
            tb.TestFunc(5)
        End Sub)
    End Sub)", result, ignoreLineEndingDifferences:=True)
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
                                                                     Type.EmptyTypes))
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
                                Return Expression.Property(Nothing, GetType(SimpleTestType).
                                                  GetProperty(NameOf(SimpleTestType.SomeStaticProperty)))
                            End Function)

            Assert.Equal("EFCore.Design.Tests.Shared.SimpleTestType.SomeStaticProperty",
                         New VisualBasicHelper(typeMapping).UnknownLiteral(New SimpleTestType))
        End Sub

        <ConditionalFact>
        Public Sub Literal_with_instance_property()
            Dim typeMapping = CreateTypeMappingSource(Of SimpleTestType)(
                                    Function(v)
                                        Return Expression.Property(
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
                            New RelationalTypeMappingSourceDependencies(plugins))
        End Function

        Private Shared ReadOnly _testFuncMethodInfo As MethodInfo =
            GetType(VisualBasicHelperTests).GetRuntimeMethod(
                NameOf(TestFunc),
                {GetType(Object), GetType(Object), GetType(Object), GetType(Object)})

        Public Shared Sub TestFunc(builder As Object, o1 As Object, o2 As Object, o3 As Object)
            Throw New NotSupportedException()
        End Sub
    End Class

    Friend Class Generic(Of T)
    End Class

    Friend Class MultiGeneric(Of T1, T2)
    End Class
End Namespace
