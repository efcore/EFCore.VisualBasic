Imports Bricelam.EntityFrameworkCore.VisualBasic.Microsoft.EntityFrameworkCore.TestUtilities.Xunit
Imports Xunit

Public Class VisualBasicHelperTests

    <Theory>
    <InlineData(
            "single-line string with \",
            """single-line string with \""")>
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
            "4.2")>
    <InlineData(
            -1.7976931348623157E+308, ' Double MinValue
            "-1.7976931348623157E+308")>
    <InlineData(
            1.7976931348623157E+308, ' Double MaxValue
            "1.7976931348623157E+308")>
    <InlineData(
            4.2F,
            "4.2f")>
    <InlineData(
            -3.402823E+38F, ' Single MinValue
            "-3.402823E+38f")>
    <InlineData(
            3.402823E+38F, ' Single MaxValue
            "3.402823E+38f")>
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
            CShort(42),
            "CShort(42)")>
    <InlineData(
            CUInt(42),
            "CUInt(42)")>
    <InlineData(
            42UL,
            "42ul")>
    <InlineData(
            18000000000000000000UL,
            "18000000000000000000ul")>
    <InlineData(
            CUShort(42),
            "CUShort(42)")>
    <InlineData(
            "",
            """""")>
    <InlineData(
            SomeEnum.DefaultValue,
            "VisualBasicHelperTests.SomeEnum.DefaultValue")>
    Public Sub Literal_works(value As Object, expected As String)

        Dim literal = New VisualBasicHelper().UnknownLiteral(value)
        Assert.Equal(expected, literal)
    End Sub

    <Fact>
    Public Sub Literal_works_when_empty_ByteArray()
        Literal_works(
                New Byte() {},
                "New Byte() {}")
    End Sub

    <Fact>
    Public Sub Literal_works_when_single_ByteArray()
        Literal_works(
                    New Byte() {1},
                    "New Byte() {1}")
    End Sub

    <Fact>
    Public Sub Literal_works_when_many_ByteArray()
        Literal_works(
                    New Byte() {1, 2},
                    "New Byte() {1, 2}")
    End Sub

    <Fact>
    Public Sub Literal_works_when_multiline_string()
        Literal_works(
"multi-line
string with """,
"""multi-line
string with """"""")
    End Sub

    <UseCulture("de-DE")>
    <Fact>
    Public Sub Literal_works_when_DateTime()
        Literal_works(
                    New DateTime(2014, 5, 28, 20, 45, 17, 300, DateTimeKind.Local),
                    "New DateTime(2014, 5, 28, 20, 45, 17, 300, DateTimeKind.Local)")
    End Sub

    <Fact>
    <UseCulture("de-DE")>
    Public Sub Literal_works_when_DateTimeOffset()
        Literal_works(
            New DateTimeOffset(New DateTime(2014, 5, 28, 19, 43, 47, 500), New TimeSpan(-7, 0, 0)),
            "New DateTimeOffset(New DateTime(2014, 5, 28, 19, 43, 47, 500, DateTimeKind.Unspecified), New TimeSpan(0, -7, 0, 0, 0))")
    End Sub

    <Fact>
    Public Sub Literal_works_when_decimal()
        Literal_works(
                    4.2D,
                    "4.2D")
    End Sub

    <Fact>
    Public Sub Literal_works_when_decimal_max_value()
        Literal_works(
                    79228162514264337593543950335D, ' Decimal MaxValue
                    "79228162514264337593543950335D")
    End Sub

    <Fact>
    Public Sub Literal_works_when_decimal_min_value()
        Literal_works(
                    -79228162514264337593543950335D, ' Decimal MinValue
                    "-79228162514264337593543950335D")
    End Sub

    <Fact>
    Public Sub Literal_works_when_Guid()
        Literal_works(
                    New Guid("fad4f3c3-9501-4b3a-af99-afeb496f7664"),
                    "New Guid(""fad4f3c3-9501-4b3a-af99-afeb496f7664"")")
    End Sub

    <Fact>
    Public Sub Literal_works_when_TimeSpan()
        Literal_works(
                    New TimeSpan(17, 21, 42, 37, 250),
                    "New TimeSpan(17, 21, 42, 37, 250)")
    End Sub

    <Fact>
    Public Sub Literal_works_when_NullableInt()
        Literal_works(
                    CType(42, Nullable(Of Integer)),
                    "42")
    End Sub

    <Fact>
    Public Sub Literal_works_when_StringArray()
        Dim literal = New VisualBasicHelper().Literal({"A", "B"})
        Assert.Equal("{""A"", ""B""}", literal)
    End Sub

    <Fact>
    Public Sub Literal_works_when_ObjectArray()
        Dim literal = New VisualBasicHelper().Literal(New Object() {"A", 1})
        Assert.Equal("New Object() {""A"", 1}", literal)
    End Sub

    <Fact>
    Public Sub Literal_works_when_MultidimensionalArray()
        Dim value = New Object(,) {
                    {"A", 1},
                    {"B", 2}
                }

        Dim result = New VisualBasicHelper().Literal(value)

        Assert.Equal(
"New Object(,) {
    {""A"", 1},
    {""B"", 2}
}",
                    result)
    End Sub

    <Fact>
    Public Sub UnknownLiteral_throws_when_unknown()
        Dim ex = Assert.Throws(Of InvalidOperationException)(
                        Function() New VisualBasicHelper().UnknownLiteral(New Object()))
        Assert.Equal(VBDesignStrings.UnknownLiteral(GetType(Object)), ex.Message)
    End Sub

    <Theory>
    <InlineData(GetType(Integer), "Integer")>
    <InlineData(GetType(Integer?), "Integer?")>
    <InlineData(GetType(Integer()), "Integer()")>
    <InlineData(GetType(Integer(,)), "Integer(,)")>
    <InlineData(GetType(Integer()()), "Integer()()")>
    <InlineData(GetType(Generic(Of Integer)), "Generic(Of Integer)")>
    <InlineData(GetType(Nested), "VisualBasicHelperTests.Nested")>
    <InlineData(GetType(Generic(Of Generic(Of Integer))), "Generic(Of Generic(Of Integer))")>
    <InlineData(GetType(MultiGeneric(Of Integer, Integer)), "MultiGeneric(Of Integer, Integer)")>
    <InlineData(GetType(NestedGeneric(Of Integer)), "VisualBasicHelperTests.NestedGeneric(Of Integer)")>
    <InlineData(GetType(Nested.DoubleNested), "VisualBasicHelperTests.Nested.DoubleNested")>
    Public Sub Reference_works(type As Type, expected As String)
        Assert.Equal(expected, New VisualBasicHelper().Reference(type))
    End Sub

    Private Class Nested

        Public Class DoubleNested
        End Class

    End Class

    Private Class NestedGeneric(Of T)
    End Class

    <Theory>
    <InlineData("dash-er", "dasher")>
    <InlineData("ParamArray", "[ParamArray]")>
    <InlineData("True", "[True]")>
    <InlineData("spac ed", "spaced")>
    <InlineData("1nders", "_1nders")>
    <InlineData("Name.space", "[Namespace]")>
    <InlineData("$", "_")>
    Public Sub Identifier_works( input As String,  expected As String)
                Assert.Equal(expected, New VisualBasicHelper().Identifier(input))
            End Sub

    <Theory>
    <InlineData({"WebApplication1", "Migration"}, "WebApplication1.Migration")>
    <InlineData({"WebApplication1.Migration"}, "WebApplication1.Migration")>
    <InlineData({"ef-xplat.Namespace"}, "efxplat.[Namespace]")>
    <InlineData({"#", "$"}, "_._")>
    <InlineData({""}, "_")>
    <InlineData(CType({}, String()), "_")>
    <InlineData({Nothing}, "_")>
    Public Sub Namespace_works(input As String(), excepted As String)
        Assert.Equal(excepted, New VisualBasicHelper().Namespace(input))
    End Sub

    Private Enum SomeEnum
        DefaultValue
    End Enum

End Class

Friend Class Generic(Of T)
End Class

Friend Class MultiGeneric(Of T1, T2)
End Class